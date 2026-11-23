using FindThatBook.Application.Abstractions;
using FindThatBook.Domain;

namespace FindThatBook.Application.Services;

/// <summary>Attaches a "why it matched" explanation and Gemini's rank to every candidate; makes no AI call when there is nothing to explain.</summary>
public sealed class CandidateExplainer(IExplanationWriter explanationWriter)
{
    private readonly IExplanationWriter _explanationWriter = explanationWriter;

    public async Task<IReadOnlyList<BookCandidate>> ExplainAsync(
        string query,
        QueryHypothesis hypothesis,
        IReadOnlyList<BookCandidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return candidates;
        }

        IReadOnlyDictionary<string, CandidateAssessment> assessments = await _explanationWriter.ExplainAsync(query, hypothesis, candidates, cancellationToken);
        return candidates.Select(candidate => Apply(candidate, assessments[candidate.WorkKey])).ToList();
    }

    private static BookCandidate Apply(BookCandidate candidate, CandidateAssessment assessment) =>
        candidate with { Explanation = assessment.Explanation, AiRank = assessment.AiRank };
}
