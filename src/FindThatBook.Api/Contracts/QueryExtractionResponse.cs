using FindThatBook.Domain;

namespace FindThatBook.Api.Contracts;

/// <summary>Response of <c>POST /api/diagnostics/extract</c>.</summary>
public sealed record QueryExtractionResponse(string Query, QueryHypothesisResponse Hypothesis)
{
    public static QueryExtractionResponse FromDomain(string query, QueryHypothesis hypothesis) =>
        new(query, QueryHypothesisResponse.FromDomain(hypothesis));
}
