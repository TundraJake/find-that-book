using FindThatBook.Application.Abstractions;
using FindThatBook.Application.Services;
using FindThatBook.Domain;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Services;

public sealed class CandidateExplainerTests
{
    internal sealed class RecordingExplanationWriter : IExplanationWriter
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyDictionary<string, CandidateAssessment>> ExplainAsync(
            string query,
            QueryHypothesis hypothesis,
            IReadOnlyList<BookCandidate> candidates,
            CancellationToken cancellationToken)
        {
            CallCount++;
            IReadOnlyDictionary<string, CandidateAssessment> assessments = candidates
                .Select((candidate, index) => (candidate, rank: candidates.Count - index))
                .ToDictionary(item => item.candidate.WorkKey, item => new CandidateAssessment($"Because of {item.candidate.Title}.", item.rank));
            return Task.FromResult(assessments);
        }
    }

    [Fact]
    public async Task ExplainAsync_AttachesExplanationToEachCandidate()
    {
        RecordingExplanationWriter writer = new();
        CandidateExplainer explainer = new(writer);
        BookCandidate[] candidates = [GeminiTestFactory.CreateCandidate("/works/A", "Alpha"), GeminiTestFactory.CreateCandidate("/works/B", "Beta")];

        IReadOnlyList<BookCandidate> explained = await explainer.ExplainAsync("query", QueryHypothesis.Empty, candidates, CancellationToken.None);

        Assert.Equal<string>(new[] { "Because of Alpha.", "Because of Beta." }, explained.Select(candidate => candidate.Explanation));
        Assert.Equal<int?>(new int?[] { 2, 1 }, explained.Select(candidate => candidate.AiRank));
        Assert.Equal<string>(new[] { "/works/A", "/works/B" }, explained.Select(candidate => candidate.WorkKey));
        Assert.Equal(1, writer.CallCount);
    }

    [Fact]
    public async Task ExplainAsync_NoCandidates_SkipsWriter()
    {
        RecordingExplanationWriter writer = new();
        CandidateExplainer explainer = new(writer);

        IReadOnlyList<BookCandidate> explained = await explainer.ExplainAsync("query", QueryHypothesis.Empty, [], CancellationToken.None);

        Assert.Empty(explained);
        Assert.Equal(0, writer.CallCount);
    }
}
