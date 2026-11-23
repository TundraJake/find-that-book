namespace FindThatBook.Infrastructure.Gemini.Explanation;

/// <summary>The JSON object Gemini returns for an explanation request. Mirrors <see cref="ExplanationSchema"/>.</summary>
internal sealed record ExplanationResult
{
    public required IReadOnlyList<CandidateExplanation> Explanations { get; init; }
}

internal sealed record CandidateExplanation
{
    public required string Id { get; init; }

    public required string Explanation { get; init; }

    /// <summary>Nullable so a missing rank only drops the AI ranking instead of failing the explanations.</summary>
    public int? Rank { get; init; }
}
