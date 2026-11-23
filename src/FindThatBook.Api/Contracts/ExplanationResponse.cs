namespace FindThatBook.Api.Contracts;

/// <summary>Response of <c>POST /api/diagnostics/explain</c>.</summary>
public sealed record ExplanationResponse(string Query, QueryHypothesisResponse Hypothesis, IReadOnlyList<BookCandidateResponse> Candidates);
