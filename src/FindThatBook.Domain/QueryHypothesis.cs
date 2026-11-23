namespace FindThatBook.Domain;

/// <summary>Structured guess at what a messy reader query is asking for.</summary>
public sealed record QueryHypothesis(
    string? Title,
    string? Author,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> CharacterHints,
    int? YearHint)
{
    public static QueryHypothesis Empty { get; } = new(null, null, [], [], null);
}
