using System.Text.Json;
using FindThatBook.Application;
using FindThatBook.Application.Abstractions;
using FindThatBook.Domain;
using FindThatBook.Infrastructure.Gemini;
using FindThatBook.Infrastructure.Gemini.Explanation;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Gemini;

public sealed class GeminiExplanationWriterTests
{
    private const string Query = "tolkien hobbit illustrated deluxe 1937";
    private const string HobbitKey = "/works/OL262758W";
    private const string RingsKey = "/works/OL27448W";

    private static readonly QueryHypothesis _hypothesis = new("The Hobbit", "J.R.R. Tolkien", ["illustrated"], [], 1937);

    private static readonly BookCandidate[] _candidates =
    [
        GeminiTestFactory.CreateCandidate(HobbitKey, "The Hobbit"),
        GeminiTestFactory.CreateCandidate(RingsKey, "The Lord of the Rings", MatchTier.AuthorOnly),
    ];

    [Fact]
    public async Task ExplainAsync_ValidResponse_ReturnsExplanationPerCandidate()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith(GeminiTestFactory.ReadFixture("Gemini/explanation-response.json"));
        GeminiExplanationWriter writer = GeminiTestFactory.CreateExplanationWriter(handler);

        IReadOnlyDictionary<string, CandidateAssessment> explanations = await writer.ExplainAsync(Query, _hypothesis, _candidates, CancellationToken.None);

        Assert.Equal(2, explanations.Count);
        Assert.StartsWith("Exact title match; Tolkien is primary author", explanations[HobbitKey].Explanation, StringComparison.Ordinal);
        Assert.Contains("1954", explanations[RingsKey].Explanation, StringComparison.Ordinal);
        Assert.Equal(1, explanations[HobbitKey].AiRank);
        Assert.Equal(2, explanations[RingsKey].AiRank);
    }

    [Fact]
    public async Task ExplainAsync_SendsCandidateFactsAsJsonData()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith(GeminiTestFactory.ReadFixture("Gemini/explanation-response.json"));
        GeminiExplanationWriter writer = GeminiTestFactory.CreateExplanationWriter(handler);

        await writer.ExplainAsync(Query, _hypothesis, _candidates, CancellationToken.None);

        using JsonDocument body = JsonDocument.Parse(handler.LastRequestBody!);
        string prompt = body.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString()!;
        Assert.Contains("\"id\":\"/works/OL262758W\"", prompt, StringComparison.Ordinal);
        Assert.Contains("matches the queried author only", prompt, StringComparison.Ordinal);
        JsonElement schema = body.RootElement.GetProperty("generationConfig").GetProperty("responseFormat").GetProperty("text").GetProperty("schema");
        Assert.Equal("array", schema.GetProperty("properties").GetProperty("explanations").GetProperty("type").GetString());
    }

    [Fact]
    public async Task ExplainAsync_NoCandidates_DoesNotCallGemini()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith("{}");
        GeminiExplanationWriter writer = GeminiTestFactory.CreateExplanationWriter(handler);

        IReadOnlyDictionary<string, CandidateAssessment> explanations = await writer.ExplainAsync(Query, _hypothesis, [], CancellationToken.None);

        Assert.Empty(explanations);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task ExplainAsync_NoApiKey_ThrowsWithoutCallingGemini()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith("{}");
        GeminiExplanationWriter writer = GeminiTestFactory.CreateExplanationWriter(handler, GeminiTestFactory.CreateOptions(apiKey: string.Empty));

        ExplanationFailedException exception = await Assert.ThrowsAsync<ExplanationFailedException>(
            () => writer.ExplainAsync(Query, _hypothesis, _candidates, CancellationToken.None));

        Assert.Contains("not configured", exception.Reason, StringComparison.Ordinal);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task ExplainAsync_EveryModelOverMinuteQuota_ThrowsAiQuotaExhausted()
    {
        StubHttpMessageHandler handler = GeminiTestFactory.RespondWith(
            GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-per-minute.json"), System.Net.HttpStatusCode.TooManyRequests);
        GeminiExplanationWriter writer = GeminiTestFactory.CreateExplanationWriter(handler);

        AiQuotaExhaustedException exception = await Assert.ThrowsAsync<AiQuotaExhaustedException>(
            () => writer.ExplainAsync(Query, _hypothesis, _candidates, CancellationToken.None));

        Assert.Equal(AiQuotaScope.PerMinute, exception.Scope);
    }

    [Fact]
    public async Task ExplainAsync_InjectedLinkInExplanation_ThrowsSearchRejected()
    {
        string injected = GeminiTestFactory.ReadFixture("Gemini/explanation-response.json")
            .Replace("Matches the queried author: Tolkien is primary author of this 1954 work.", "Visit https://evil.example for free books!", StringComparison.Ordinal);
        GeminiExplanationWriter writer = GeminiTestFactory.CreateExplanationWriter(GeminiTestFactory.RespondWith(injected));

        SearchRejectedException exception = await Assert.ThrowsAsync<SearchRejectedException>(
            () => writer.ExplainAsync(Query, _hypothesis, _candidates, CancellationToken.None));

        Assert.Contains("link", exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplainAsync_MissingCandidateInResponse_ThrowsExplanationFailed()
    {
        string onlyOne = "{\"explanations\":[{\"id\":\"/works/OL262758W\",\"explanation\":\"Exact title match.\"}]}";
        string envelope = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":" + JsonSerializer.Serialize(onlyOne) + "}]},\"finishReason\":\"STOP\"}]}";
        GeminiExplanationWriter writer = GeminiTestFactory.CreateExplanationWriter(GeminiTestFactory.RespondWith(envelope));

        ExplanationFailedException exception = await Assert.ThrowsAsync<ExplanationFailedException>(
            () => writer.ExplainAsync(Query, _hypothesis, _candidates, CancellationToken.None));

        Assert.Contains(RingsKey, exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAndValidate_UnknownCandidate_Throws()
    {
        string json = "{\"explanations\":[{\"id\":\"/works/OTHER\",\"explanation\":\"Something.\"}]}";

        GeminiException exception = Assert.Throws<GeminiException>(() => GeminiExplanationWriter.ParseAndValidate(json, [HobbitKey]));

        Assert.Contains("unknown", exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAndValidate_EmptyExplanation_Throws()
    {
        string json = "{\"explanations\":[{\"id\":\"/works/OL262758W\",\"explanation\":\"   \"}]}";

        Assert.Throws<GeminiException>(() => GeminiExplanationWriter.ParseAndValidate(json, [HobbitKey]));
    }

    [Fact]
    public void ParseAndValidate_OverlongExplanation_IsShortened()
    {
        string json = "{\"explanations\":[{\"id\":\"/works/OL262758W\",\"explanation\":\"" + new string('a', 1000) + "\"}]}";

        Dictionary<string, CandidateAssessment> explanations = GeminiExplanationWriter.ParseAndValidate(json, [HobbitKey]);

        Assert.True(explanations[HobbitKey].Explanation.Length <= 400);
        Assert.EndsWith("…", explanations[HobbitKey].Explanation, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("2", "2")]
    [InlineData("1", "3")]
    [InlineData("0", "1")]
    public void ParseAndValidate_RanksNotOneToCount_DropsAllRanksButKeepsExplanations(string hobbitRank, string ringsRank)
    {
        string json = $"{{\"explanations\":[{{\"id\":\"{HobbitKey}\",\"explanation\":\"Tolkien.\",\"rank\":{hobbitRank}}},{{\"id\":\"{RingsKey}\",\"explanation\":\"Tolkien.\",\"rank\":{ringsRank}}}]}}";

        Dictionary<string, CandidateAssessment> assessments = GeminiExplanationWriter.ParseAndValidate(json, [HobbitKey, RingsKey]);

        Assert.All(assessments.Values, assessment => Assert.Null(assessment.AiRank));
        Assert.All(assessments.Values, assessment => Assert.Equal("Tolkien.", assessment.Explanation));
    }

    [Fact]
    public void ParseAndValidate_MissingRank_DropsAllRanks()
    {
        string json = $"{{\"explanations\":[{{\"id\":\"{HobbitKey}\",\"explanation\":\"Tolkien.\"}}]}}";

        Dictionary<string, CandidateAssessment> assessments = GeminiExplanationWriter.ParseAndValidate(json, [HobbitKey]);

        Assert.Null(assessments[HobbitKey].AiRank);
    }

    [Fact]
    public void ParseAndValidate_ValidRanks_AreKept()
    {
        string json = $"{{\"explanations\":[{{\"id\":\"{HobbitKey}\",\"explanation\":\"Tolkien.\",\"rank\":2}},{{\"id\":\"{RingsKey}\",\"explanation\":\"Tolkien.\",\"rank\":1}}]}}";

        Dictionary<string, CandidateAssessment> assessments = GeminiExplanationWriter.ParseAndValidate(json, [HobbitKey, RingsKey]);

        Assert.Equal(2, assessments[HobbitKey].AiRank);
        Assert.Equal(1, assessments[RingsKey].AiRank);
    }
}
