using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FindThatBook.Application;
using FindThatBook.Infrastructure.Gemini.Models;
using FindThatBook.Infrastructure.Gemini.Quota;
using FindThatBook.Infrastructure.Gemini.Safety;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.Gemini;

/// <summary>
/// The only class that talks to Google. Sends a prompt with a response schema and returns the model's JSON text
/// after checking the response envelope (layer 2 of the JSON guarantee; layer 1 is the schema itself).
/// Models are tried in configured order: a quota error (HTTP 429) moves the same request to the next model,
/// and <see cref="GeminiQuotaExhaustedException"/> is thrown only once every model is over quota. An overloaded model (HTTP 503,
/// "high demand") is retried on the same model (<see cref="GeminiOptions.BusyRetryAttempts"/> times, <see cref="GeminiOptions.BusyRetryDelaySeconds"/>
/// apart) and never switched, so answers stay consistent; if it is still overloaded, <see cref="GeminiBusyException"/> tells the caller to retry
/// shortly. Other errors never fall back.
/// </summary>
internal sealed partial class GeminiClient(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    GeminiUsageTracker usageTracker,
    GeminiModelAvailability modelAvailability,
    ILogger<GeminiClient> logger)
{
    private const string ApiKeyHeader = "x-goog-api-key";
    private const string JsonMimeType = "application/json";
    /// <summary>TextResponseFormat.MimeType enum value (not a MIME string) per the generateContent API reference.</summary>
    private const string ResponseFormatJsonMimeType = "APPLICATION_JSON";
    private const string CompletedFinishReason = "STOP";
    private const int MaximumErrorBodyCharacters = 300;

    internal static readonly JsonSerializerOptions WireSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly GeminiUsageTracker _usageTracker = usageTracker;
    private readonly GeminiModelAvailability _modelAvailability = modelAvailability;
    private readonly ILogger<GeminiClient> _logger = logger;

    public async Task<string> GenerateJsonAsync(string prompt, JsonNode responseSchema, CancellationToken cancellationToken)
    {
        GenerateContentRequest body = new(
            Contents: [new RequestContent([new ContentPart(prompt)])],
            GenerationConfig: new GenerationConfig(
                new ResponseFormat(new TextResponseFormat(ResponseFormatJsonMimeType, responseSchema)),
                _options.HasThinkingLevel ? new ThinkingConfig(_options.ThinkingLevel) : null),
            SafetySettings: GeminiSafetySettings.Create(_options));
        // Serialized once, up front, so every attempt carries Content-Length instead of chunked encoding.
        string serializedBody = JsonSerializer.Serialize(body, WireSerializerOptions);

        foreach (string model in _options.Models)
        {
            if (!_modelAvailability.IsAvailable(model))
            {
                continue;
            }

            using HttpResponseMessage response = await SendWithBusyRetriesAsync(model, serializedBody, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                GeminiQuotaError quotaError = GeminiQuotaError.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                _modelAvailability.MarkExhausted(model, quotaError);
                LogModelOverQuota(_logger, model, quotaError.Scope);
                continue;
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                LogModelStillBusy(_logger, model, _options.BusyRetryAttempts);
                throw new GeminiBusyException(TimeSpan.FromSeconds(_options.BusyRetrySeconds));
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                string excerpt = errorBody.Length > MaximumErrorBodyCharacters ? errorBody[..MaximumErrorBodyCharacters] : errorBody;
                throw new GeminiException($"HTTP {(int)response.StatusCode} from {model}: {excerpt}");
            }

            GenerateContentResponse? envelope = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(WireSerializerOptions, cancellationToken);
            return ReadJsonText(envelope);
        }

        throw _modelAvailability.CreateExhaustedException(_options.Models);
    }

    /// <summary>Sends to one model; while it answers 503 ("high demand"), waits and retries the same model. Returns the last response.</summary>
    private async Task<HttpResponseMessage> SendWithBusyRetriesAsync(string model, string serializedBody, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await SendAsync(model, serializedBody, cancellationToken);
        for (int retry = 1; retry <= _options.BusyRetryAttempts && response.StatusCode == HttpStatusCode.ServiceUnavailable; retry++)
        {
            response.Dispose();
            LogModelBusyRetrying(_logger, model, retry, _options.BusyRetryAttempts, _options.BusyRetryDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.BusyRetryDelaySeconds), cancellationToken);
            response = await SendAsync(model, serializedBody, cancellationToken);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendAsync(string model, string serializedBody, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{_options.ApiVersion}/models/{Uri.EscapeDataString(model)}:generateContent")
        {
            Content = new StringContent(serializedBody, Encoding.UTF8, JsonMimeType),
        };
        request.Headers.Add(ApiKeyHeader, _options.ApiKey);

        _usageTracker.RecordRequest();
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    internal static string ReadJsonText(GenerateContentResponse? envelope)
    {
        if (envelope is null)
        {
            throw new GeminiException("empty response body");
        }

        if (envelope.PromptFeedback?.BlockReason is { } blockReason)
        {
            throw new GeminiContentBlockedException($"prompt blocked by Gemini safety filters ({blockReason})");
        }

        ResponseCandidate candidate = envelope.Candidates is { Count: > 0 } candidates
            ? candidates[0]
            : throw new GeminiException("no candidates");

        if (GeminiSafetySettings.IsBlockedFinishReason(candidate.FinishReason))
        {
            throw new GeminiContentBlockedException($"response blocked by Gemini safety filters ({candidate.FinishReason})");
        }

        if (!string.Equals(candidate.FinishReason, CompletedFinishReason, StringComparison.Ordinal))
        {
            throw new GeminiException($"finish reason {candidate.FinishReason ?? "missing"}");
        }

        string text = string.Concat(candidate.Content?.Parts?.Select(part => part.Text) ?? []);
        return string.IsNullOrWhiteSpace(text) ? throw new GeminiException("empty text") : text;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini model {Model} is over its {QuotaScope} quota; trying the next model")]
    private static partial void LogModelOverQuota(ILogger logger, string model, AiQuotaScope quotaScope);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini model {Model} is overloaded (high demand); retry {Retry} of {MaximumRetries} in {DelaySeconds}s")]
    private static partial void LogModelBusyRetrying(ILogger logger, string model, int retry, int maximumRetries, int delaySeconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini model {Model} still overloaded after {Retries} retries; asking the client to try again")]
    private static partial void LogModelStillBusy(ILogger logger, string model, int retries);
}
