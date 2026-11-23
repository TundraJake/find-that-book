namespace FindThatBook.Infrastructure.Gemini.Extraction;

/// <summary>The JSON object Gemini returns for an extraction request. Mirrors <see cref="ExtractionSchema"/>.</summary>
internal sealed record ExtractionResult
{
    public string? Title { get; init; }

    public string? Author { get; init; }

    public required IReadOnlyList<string> Keywords { get; init; }

    public required IReadOnlyList<string> CharacterHints { get; init; }

    public int? YearHint { get; init; }
}
