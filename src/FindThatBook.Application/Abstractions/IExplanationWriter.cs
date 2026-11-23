using FindThatBook.Domain;

namespace FindThatBook.Application.Abstractions;

/// <summary>
/// Writes a short "why it matched" explanation for each candidate, grounded only in the candidate's fetched fields,
/// and ranks the candidates by how likely each is the book the reader means (same AI call).
/// </summary>
public interface IExplanationWriter
{
    /// <returns>Assessment per candidate, keyed by <see cref="BookCandidate.WorkKey"/>; one entry for every candidate.</returns>
    /// <exception cref="ExplanationFailedException">Explanations could not be produced for every candidate.</exception>
    Task<IReadOnlyDictionary<string, CandidateAssessment>> ExplainAsync(
        string query,
        QueryHypothesis hypothesis,
        IReadOnlyList<BookCandidate> candidates,
        CancellationToken cancellationToken);
}
