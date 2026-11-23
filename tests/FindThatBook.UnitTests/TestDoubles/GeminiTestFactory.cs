using System.Net;
using FindThatBook.Infrastructure.Gemini;
using FindThatBook.Domain;
using FindThatBook.Infrastructure.Gemini.Explanation;
using FindThatBook.Infrastructure.Gemini.Extraction;
using FindThatBook.Infrastructure.Gemini.Quota;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FindThatBook.UnitTests.TestDoubles;

internal static class GeminiTestFactory
{
    public const string TestApiKey = "test-key";
    public const string PrimaryModel = "gemini-primary";
    public const string FallbackModel = "gemini-fallback";

    public static string ModelPath(string model) => $"/v1beta/models/{model}:generateContent";

    public static GeminiOptions CreateOptions(string apiKey = TestApiKey) => new()
    {
        BaseUrl = "https://gemini.test/",
        ApiVersion = "v1beta",
        Models = [PrimaryModel, FallbackModel],
        ThinkingLevel = "high",
        SafetyCategories = ["HARM_CATEGORY_HARASSMENT", "HARM_CATEGORY_JAILBREAK"],
        SafetyThreshold = "BLOCK_MEDIUM_AND_ABOVE",
        ApiKey = apiKey,
        TimeoutSeconds = 5,
        RequestsPerMinuteLimit = 10,
        BusyRetryAttempts = 3,
        BusyRetryDelaySeconds = 0,
        BusyRetrySeconds = 5,
    };

    public static GeminiClient CreateClient(
        HttpMessageHandler handler,
        GeminiOptions options,
        GeminiUsageTracker? usageTracker = null,
        GeminiModelAvailability? modelAvailability = null) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri(options.BaseUrl) },
            Options.Create(options),
            usageTracker ?? CreateUsageTracker(options),
            modelAvailability ?? new GeminiModelAvailability(TimeProvider.System),
            NullLogger<GeminiClient>.Instance);

    public static GeminiUsageTracker CreateUsageTracker(GeminiOptions options, TimeProvider? timeProvider = null) =>
        new(timeProvider ?? TimeProvider.System, Options.Create(options));

    public static GeminiQueryExtractor CreateExtractor(HttpMessageHandler handler, GeminiOptions? options = null)
    {
        GeminiOptions resolvedOptions = options ?? CreateOptions();
        return new GeminiQueryExtractor(CreateClient(handler, resolvedOptions), Options.Create(resolvedOptions), NullLogger<GeminiQueryExtractor>.Instance);
    }

    public static GeminiExplanationWriter CreateExplanationWriter(HttpMessageHandler handler, GeminiOptions? options = null)
    {
        GeminiOptions resolvedOptions = options ?? CreateOptions();
        return new GeminiExplanationWriter(CreateClient(handler, resolvedOptions), Options.Create(resolvedOptions), NullLogger<GeminiExplanationWriter>.Instance);
    }

    public static BookCandidate CreateCandidate(string workKey, string title, MatchTier matchTier = MatchTier.ExactTitlePrimaryAuthor) => new(
        title,
        PrimaryAuthors: ["J.R.R. Tolkien"],
        Contributors: [],
        FirstPublishYear: 1937,
        workKey,
        new Uri($"https://openlibrary.org{workKey}"),
        CoverUrl: null,
        matchTier,
        Explanation: string.Empty);

    public static string ReadFixture(string relativePath) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath));

    public static StubHttpMessageHandler RespondWith(string body, HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode, body);
}
