using System.Net;
using System.Text.Json;
using FindThatBook.Application;
using FindThatBook.Domain;
using FindThatBook.Infrastructure.Gemini.Extraction;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Gemini;

public sealed class GeminiQueryExtractorTests
{
    private const string Query = "tolkien hobbit illustrated deluxe 1937";

    [Fact]
    public async Task ExtractAsync_ValidGeminiResponse_ReturnsHypothesis()
    {
        GeminiQueryExtractor extractor = GeminiTestFactory.CreateExtractor(
            GeminiTestFactory.RespondWith(GeminiTestFactory.ReadFixture("Gemini/extraction-response.json")));

        QueryHypothesis hypothesis = await extractor.ExtractAsync(Query, CancellationToken.None);

        Assert.Equal("The Hobbit", hypothesis.Title);
        Assert.Equal("J.R.R. Tolkien", hypothesis.Author);
        Assert.Equal<string>(new[] { "illustrated", "deluxe" }, hypothesis.Keywords);
        Assert.Equal(1937, hypothesis.YearHint);
    }

    [Fact]
    public async Task ExtractAsync_GeminiHttpError_ThrowsQueryExtractionFailed()
    {
        GeminiQueryExtractor extractor = GeminiTestFactory.CreateExtractor(
            GeminiTestFactory.RespondWith("{\"error\":\"internal\"}", HttpStatusCode.InternalServerError));

        QueryExtractionFailedException exception = await Assert.ThrowsAsync<QueryExtractionFailedException>(
            () => extractor.ExtractAsync(Query, CancellationToken.None));

        Assert.StartsWith("HTTP 500", exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractAsync_EveryModelOverloaded_ThrowsAiServiceBusy()
    {
        GeminiQueryExtractor extractor = GeminiTestFactory.CreateExtractor(
            GeminiTestFactory.RespondWith("{\"error\":{\"code\":503,\"status\":\"UNAVAILABLE\"}}", HttpStatusCode.ServiceUnavailable));

        AiServiceBusyException exception = await Assert.ThrowsAsync<AiServiceBusyException>(
            () => extractor.ExtractAsync(Query, CancellationToken.None));

        Assert.Equal(TimeSpan.FromSeconds(5), exception.RetryAfter);
    }

    [Fact]
    public async Task ExtractAsync_EveryModelOverDailyQuota_ThrowsAiQuotaExhausted()
    {
        GeminiQueryExtractor extractor = GeminiTestFactory.CreateExtractor(
            GeminiTestFactory.RespondWith(GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-daily.json"), HttpStatusCode.TooManyRequests));

        AiQuotaExhaustedException exception = await Assert.ThrowsAsync<AiQuotaExhaustedException>(
            () => extractor.ExtractAsync(Query, CancellationToken.None));

        Assert.Equal(AiQuotaScope.Daily, exception.Scope);
        Assert.Equal("Daily usage limits reached", exception.Title);
    }

    [Fact]
    public async Task ExtractAsync_PromptBlockedBySafetyFilter_ThrowsSearchRejected()
    {
        GeminiQueryExtractor extractor = GeminiTestFactory.CreateExtractor(
            GeminiTestFactory.RespondWith("{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}"));

        SearchRejectedException exception = await Assert.ThrowsAsync<SearchRejectedException>(
            () => extractor.ExtractAsync(Query, CancellationToken.None));

        Assert.Contains("SAFETY", exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractAsync_TruncatedResponse_ThrowsQueryExtractionFailed()
    {
        string truncated = GeminiTestFactory.ReadFixture("Gemini/extraction-response.json").Replace("\"STOP\"", "\"MAX_TOKENS\"", StringComparison.Ordinal);
        GeminiQueryExtractor extractor = GeminiTestFactory.CreateExtractor(GeminiTestFactory.RespondWith(truncated));

        QueryExtractionFailedException exception = await Assert.ThrowsAsync<QueryExtractionFailedException>(
            () => extractor.ExtractAsync(Query, CancellationToken.None));

        Assert.Contains("MAX_TOKENS", exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractAsync_NoApiKey_ThrowsWithoutCallingGemini()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith("{}");
        GeminiQueryExtractor extractor = GeminiTestFactory.CreateExtractor(handler, GeminiTestFactory.CreateOptions(apiKey: string.Empty));

        QueryExtractionFailedException exception = await Assert.ThrowsAsync<QueryExtractionFailedException>(
            () => extractor.ExtractAsync(Query, CancellationToken.None));

        Assert.Contains("not configured", exception.Reason, StringComparison.Ordinal);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public void ParseAndValidate_UnknownField_Throws()
    {
        Assert.Throws<JsonException>(() =>
            GeminiQueryExtractor.ParseAndValidate("{\"keywords\":[],\"characterHints\":[],\"genre\":\"fantasy\"}"));
    }

    [Fact]
    public void ParseAndValidate_MissingRequiredList_Throws()
    {
        Assert.Throws<JsonException>(() => GeminiQueryExtractor.ParseAndValidate("{\"title\":\"Emma\"}"));
    }

    [Fact]
    public void ParseAndValidate_MessyValues_AreCleaned()
    {
        QueryHypothesis hypothesis = GeminiQueryExtractor.ParseAndValidate(
            "{\"title\":\"  \",\"author\":\" Jane Austen \",\"keywords\":[\"Deluxe\",\"deluxe\",\"\"],\"characterHints\":[\"Bennet\"],\"yearHint\":12}");

        Assert.Null(hypothesis.Title);
        Assert.Equal("Jane Austen", hypothesis.Author);
        Assert.Equal<string>(new[] { "Deluxe" }, hypothesis.Keywords);
        Assert.Equal<string>(new[] { "Bennet" }, hypothesis.CharacterHints);
        Assert.Null(hypothesis.YearHint);
    }

    [Theory]
    [InlineData(999, false)]
    [InlineData(1000, true)]
    [InlineData(1937, true)]
    [InlineData(9999, false)]
    public void IsPlausibleYear_Boundaries(int year, bool expected)
    {
        Assert.Equal(expected, GeminiQueryExtractor.IsPlausibleYear(year));
    }
}
