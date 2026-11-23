using System.Text.Json;
using System.Text.Json.Serialization;
using FindThatBook.Application;
using FindThatBook.Application.Abstractions;
using FindThatBook.Domain;
using FindThatBook.Infrastructure.Gemini.Prompts;
using FindThatBook.Infrastructure.Gemini.Quota;
using FindThatBook.Infrastructure.Gemini.Safety;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.Gemini.Extraction;

/// <summary>
/// Extracts search fields with Gemini. Layer 3 of the JSON guarantee lives here: strict parsing plus value validation.
/// Any failure surfaces as <see cref="QueryExtractionFailedException"/>; there is no non-AI fallback.
/// </summary>
internal sealed partial class GeminiQueryExtractor(GeminiClient geminiClient, IOptions<GeminiOptions> options, ILogger<GeminiQueryExtractor> logger)
    : IQueryExtractor
{
    private const int MaximumFieldLength = 200;
    private const int MaximumListItems = 20;
    private const int EarliestPlausibleYear = 1000;
    private const string NotConfiguredReason = "Gemini API key is not configured.";

    private static readonly JsonSerializerOptions _strictSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly GeminiClient _geminiClient = geminiClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly ILogger<GeminiQueryExtractor> _logger = logger;

    public async Task<QueryHypothesis> ExtractAsync(string query, CancellationToken cancellationToken)
    {
        if (!_options.HasApiKey)
        {
            throw new QueryExtractionFailedException(NotConfiguredReason);
        }

        try
        {
            string json = await _geminiClient.GenerateJsonAsync(QueryExtractionPrompt.Build(query), ExtractionSchema.Create(), cancellationToken);
            return ParseAndValidate(json);
        }
        catch (GeminiQuotaExhaustedException exception)
        {
            throw exception.ToApplicationException();
        }
        catch (GeminiBusyException exception)
        {
            throw exception.ToApplicationException();
        }
        catch (GeminiContentBlockedException exception)
        {
            LogContentBlocked(_logger, exception.Reason);
            throw exception.ToApplicationException();
        }
        catch (Exception exception) when (GeminiFailures.IsGeminiFailure(exception, cancellationToken))
        {
            LogExtractionFailed(_logger, exception, QueryExtractionPrompt.Version);
            throw new QueryExtractionFailedException(GeminiFailures.DescribeReason(exception), exception);
        }
    }

    internal static QueryHypothesis ParseAndValidate(string json)
    {
        ExtractionResult result = JsonSerializer.Deserialize<ExtractionResult>(json, _strictSerializerOptions)
            ?? throw new GeminiException("JSON was null");

        return new QueryHypothesis(
            Title: CleanField(result.Title),
            Author: CleanField(result.Author),
            Keywords: CleanList(result.Keywords),
            CharacterHints: CleanList(result.CharacterHints),
            YearHint: result.YearHint is int year && IsPlausibleYear(year) ? year : null);
    }

    internal static bool IsPlausibleYear(int year) => year >= EarliestPlausibleYear && year <= DateTime.UtcNow.Year + 1;

    private static string? CleanField(string? value)
    {
        string? trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaximumFieldLength ? trimmed[..MaximumFieldLength] : trimmed;
    }

    private static List<string> CleanList(IReadOnlyList<string> values) =>
        values
            .Select(CleanField)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaximumListItems)
            .ToList();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini extraction failed (prompt {PromptVersion}).")]
    private static partial void LogExtractionFailed(ILogger logger, Exception exception, string promptVersion);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini content blocked: {Reason}")]
    private static partial void LogContentBlocked(ILogger logger, string reason);
}
