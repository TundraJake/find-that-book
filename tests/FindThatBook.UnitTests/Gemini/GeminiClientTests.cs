using System.Net;
using System.Text.Json;
using FindThatBook.Application;
using FindThatBook.Infrastructure.Gemini;
using FindThatBook.Infrastructure.Gemini.Extraction;
using FindThatBook.Infrastructure.Gemini.Models;
using FindThatBook.Infrastructure.Gemini.Quota;
using FindThatBook.Infrastructure.Gemini.Safety;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Gemini;

public sealed class GeminiClientTests
{
    [Fact]
    public async Task GenerateJsonAsync_ValidResponse_SendsModelUrlKeyAndSchema()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith(GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        string json = await client.GenerateJsonAsync("prompt text", ExtractionSchema.Create(), CancellationToken.None);

        Assert.Contains("\"title\":\"The Hobbit\"", json, StringComparison.Ordinal);
        Assert.Equal("https://gemini.test/v1beta/models/gemini-primary:generateContent", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(GeminiTestFactory.TestApiKey, handler.LastRequest.Headers.GetValues("x-goog-api-key").Single());

        using JsonDocument body = JsonDocument.Parse(handler.LastRequestBody!);
        JsonElement textFormat = body.RootElement.GetProperty("generationConfig").GetProperty("responseFormat").GetProperty("text");
        Assert.Equal("APPLICATION_JSON", textFormat.GetProperty("mimeType").GetString());
        Assert.Equal("object", textFormat.GetProperty("schema").GetProperty("type").GetString());
        Assert.Equal("prompt text", body.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal("high", body.RootElement.GetProperty("generationConfig").GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString());
    }

    [Fact]
    public async Task GenerateJsonAsync_NoThinkingLevel_OmitsThinkingConfig()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith(GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiOptions options = GeminiTestFactory.CreateOptions();
        options.ThinkingLevel = string.Empty;
        GeminiClient client = GeminiTestFactory.CreateClient(handler, options);

        await client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None);

        using JsonDocument body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.False(body.RootElement.GetProperty("generationConfig").TryGetProperty("thinkingConfig", out _));
    }

    [Fact]
    public async Task GenerateJsonAsync_NonQuotaHttpError_ThrowsWithoutFallingBack()
    {
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler()
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.PrimaryModel), "{\"error\":\"internal\"}", HttpStatusCode.InternalServerError)
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.FallbackModel), GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        GeminiException exception = await Assert.ThrowsAsync<GeminiException>(
            () => client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None));

        Assert.StartsWith("HTTP 500", exception.Reason, StringComparison.Ordinal);
        Assert.Single(handler.RequestedUris);
    }

    private const string HighDemandBody = "{\"error\":{\"code\":503,\"message\":\"This model is currently experiencing high demand. Please try again later.\",\"status\":\"UNAVAILABLE\"}}";

    private static readonly string _primaryPath = GeminiTestFactory.ModelPath(GeminiTestFactory.PrimaryModel);
    private static readonly string _fallbackPath = GeminiTestFactory.ModelPath(GeminiTestFactory.FallbackModel);

    [Fact]
    public async Task GenerateJsonAsync_PrimaryOverloadedThenRecovers_RetriesSameModel()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler()
            .Then(_primaryPath, HighDemandBody, HttpStatusCode.ServiceUnavailable)
            .Then(_primaryPath, HighDemandBody, HttpStatusCode.ServiceUnavailable)
            .Then(_primaryPath, GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"))
            .Then(_fallbackPath, GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        string json = await client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None);

        Assert.Contains("The Hobbit", json, StringComparison.Ordinal);
        Assert.Equal(3, handler.CountRequests(_primaryPath));
        Assert.Equal(0, handler.CountRequests(_fallbackPath));
    }

    [Fact]
    public async Task GenerateJsonAsync_PrimaryStillOverloadedAfterRetries_ThrowsBusyWithoutSwitchingModel()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler()
            .Then(_primaryPath, HighDemandBody, HttpStatusCode.ServiceUnavailable)
            .Then(_fallbackPath, GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        GeminiBusyException exception = await Assert.ThrowsAsync<GeminiBusyException>(
            () => client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None));

        Assert.Equal(TimeSpan.FromSeconds(5), exception.RetryAfter);
        Assert.Equal(1 + 3, handler.CountRequests(_primaryPath));
        Assert.Equal(0, handler.CountRequests(_fallbackPath));
    }

    [Fact]
    public async Task GenerateJsonAsync_NoBusyRetriesConfigured_SingleAttempt()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler().Then(_primaryPath, HighDemandBody, HttpStatusCode.ServiceUnavailable);
        GeminiOptions options = GeminiTestFactory.CreateOptions();
        options.BusyRetryAttempts = 0;
        GeminiClient client = GeminiTestFactory.CreateClient(handler, options);

        await Assert.ThrowsAsync<GeminiBusyException>(() => client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None));

        Assert.Equal(1, handler.CountRequests(_primaryPath));
    }

    [Fact]
    public async Task GenerateJsonAsync_PrimaryOverQuotaFallbackOverloaded_RetriesFallbackThenBusy()
    {
        SequenceHttpMessageHandler handler = new SequenceHttpMessageHandler()
            .Then(_primaryPath, GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-daily.json"), HttpStatusCode.TooManyRequests)
            .Then(_fallbackPath, HighDemandBody, HttpStatusCode.ServiceUnavailable);
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        await Assert.ThrowsAsync<GeminiBusyException>(() => client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None));

        Assert.Equal(1, handler.CountRequests(_primaryPath));
        Assert.Equal(1 + 3, handler.CountRequests(_fallbackPath));
    }

    [Fact]
    public async Task GenerateJsonAsync_PrimaryOverQuota_RetriesOnFallbackModel()
    {
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler()
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.PrimaryModel), GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-daily.json"), HttpStatusCode.TooManyRequests)
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.FallbackModel), GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        string json = await client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None);

        Assert.Contains("\"title\":\"The Hobbit\"", json, StringComparison.Ordinal);
        Assert.Equal<string>(
            [GeminiTestFactory.ModelPath(GeminiTestFactory.PrimaryModel), GeminiTestFactory.ModelPath(GeminiTestFactory.FallbackModel)],
            handler.RequestedUris.Select(uri => uri.AbsolutePath).ToList());
    }

    [Fact]
    public async Task GenerateJsonAsync_PrimaryBlockedForTheDay_SkipsStraightToFallback()
    {
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler()
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.PrimaryModel), GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-daily.json"), HttpStatusCode.TooManyRequests)
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.FallbackModel), GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        await client.GenerateJsonAsync("first", ExtractionSchema.Create(), CancellationToken.None);
        await client.GenerateJsonAsync("second", ExtractionSchema.Create(), CancellationToken.None);

        Assert.Equal(1, handler.RequestedUris.Count(uri => uri.AbsolutePath == GeminiTestFactory.ModelPath(GeminiTestFactory.PrimaryModel)));
        Assert.Equal(2, handler.RequestedUris.Count(uri => uri.AbsolutePath == GeminiTestFactory.ModelPath(GeminiTestFactory.FallbackModel)));
    }

    [Fact]
    public async Task GenerateJsonAsync_AllModelsOverDailyQuota_ThrowsDailyExhaustion()
    {
        string dailyQuota = GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-daily.json");
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler()
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.PrimaryModel), dailyQuota, HttpStatusCode.TooManyRequests)
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.FallbackModel), dailyQuota, HttpStatusCode.TooManyRequests);
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        GeminiQuotaExhaustedException exception = await Assert.ThrowsAsync<GeminiQuotaExhaustedException>(
            () => client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None));

        Assert.Equal(AiQuotaScope.Daily, exception.Scope);
        Assert.InRange(exception.RetryAfter, TimeSpan.FromSeconds(1), TimeSpan.FromHours(25));
        Assert.Equal(2, handler.RequestedUris.Count);
    }

    [Fact]
    public async Task GenerateJsonAsync_FallbackOnlyOverMinuteQuota_ThrowsPerMinuteExhaustion()
    {
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler()
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.PrimaryModel), GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-daily.json"), HttpStatusCode.TooManyRequests)
            .Route(GeminiTestFactory.ModelPath(GeminiTestFactory.FallbackModel), GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-per-minute.json"), HttpStatusCode.TooManyRequests);
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        GeminiQuotaExhaustedException exception = await Assert.ThrowsAsync<GeminiQuotaExhaustedException>(
            () => client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None));

        Assert.Equal(AiQuotaScope.PerMinute, exception.Scope);
        Assert.InRange(exception.RetryAfter, TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(43));
    }

    [Theory]
    [InlineData("MAX_TOKENS")]
    [InlineData("RECITATION")]
    public void ReadJsonText_NonStopFinishReason_Throws(string finishReason)
    {
        GenerateContentResponse envelope = new([new ResponseCandidate(new ResponseContent([new ContentPart("{}")]), finishReason)], null);

        GeminiException exception = Assert.Throws<GeminiException>(() => GeminiClient.ReadJsonText(envelope));

        Assert.Contains(finishReason, exception.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SAFETY")]
    [InlineData("PROHIBITED_CONTENT")]
    [InlineData("BLOCKLIST")]
    public void ReadJsonText_BlockedPrompt_ThrowsContentBlocked(string blockReason)
    {
        GenerateContentResponse envelope = new(null, new PromptFeedback(blockReason));

        GeminiContentBlockedException exception = Assert.Throws<GeminiContentBlockedException>(() => GeminiClient.ReadJsonText(envelope));

        Assert.Contains(blockReason, exception.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SAFETY")]
    [InlineData("PROHIBITED_CONTENT")]
    [InlineData("SPII")]
    public void ReadJsonText_SafetyFinishReason_ThrowsContentBlocked(string finishReason)
    {
        GenerateContentResponse envelope = new([new ResponseCandidate(new ResponseContent([new ContentPart("{}")]), finishReason)], null);

        GeminiContentBlockedException exception = Assert.Throws<GeminiContentBlockedException>(() => GeminiClient.ReadJsonText(envelope));

        Assert.Contains(finishReason, exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateJsonAsync_SendsConfiguredSafetySettings()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith(GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiClient client = GeminiTestFactory.CreateClient(handler, GeminiTestFactory.CreateOptions());

        await client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None);

        using JsonDocument body = JsonDocument.Parse(handler.LastRequestBody!);
        JsonElement[] settings = body.RootElement.GetProperty("safetySettings").EnumerateArray().ToArray();
        Assert.Equal<string>(
            ["HARM_CATEGORY_HARASSMENT", "HARM_CATEGORY_JAILBREAK"],
            settings.Select(setting => setting.GetProperty("category").GetString()!).ToList());
        Assert.All(settings, setting => Assert.Equal("BLOCK_MEDIUM_AND_ABOVE", setting.GetProperty("threshold").GetString()));
    }

    [Fact]
    public async Task GenerateJsonAsync_NoSafetyCategories_OmitsSafetySettings()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith(GeminiTestFactory.ReadFixture("Gemini/extraction-response.json"));
        GeminiOptions options = GeminiTestFactory.CreateOptions();
        options.SafetyCategories = [];
        GeminiClient client = GeminiTestFactory.CreateClient(handler, options);

        await client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None);

        using JsonDocument body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.False(body.RootElement.TryGetProperty("safetySettings", out _));
    }

    [Fact]
    public void ReadJsonText_NoCandidates_Throws()
    {
        Assert.Throws<GeminiException>(() => GeminiClient.ReadJsonText(new GenerateContentResponse([], null)));
    }

    [Fact]
    public void ReadJsonText_EmptyText_Throws()
    {
        GenerateContentResponse envelope = new([new ResponseCandidate(new ResponseContent([new ContentPart("  ")]), "STOP")], null);

        Assert.Throws<GeminiException>(() => GeminiClient.ReadJsonText(envelope));
    }
}
