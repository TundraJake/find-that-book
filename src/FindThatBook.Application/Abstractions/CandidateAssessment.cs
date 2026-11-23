namespace FindThatBook.Application.Abstractions;

/// <summary>Gemini's view of one candidate: why it matched, and where it ranks among the candidates (1 = most likely).</summary>
/// <param name="AiRank">Null when Gemini's ranks were unusable (not each of 1..N exactly once); the explanation is still valid.</param>
public sealed record CandidateAssessment(string Explanation, int? AiRank);
