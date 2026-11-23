namespace FindThatBook.Domain;

/// <summary>A canonical Open Library work proposed as an answer to a query.</summary>
/// <param name="AiRank">Gemini's own ranking (1 = most likely); an alternative view only, the list order always follows <see cref="MatchTier"/>.</param>
public sealed record BookCandidate(
    string Title,
    IReadOnlyList<string> PrimaryAuthors,
    IReadOnlyList<string> Contributors,
    int? FirstPublishYear,
    string WorkKey,
    Uri OpenLibraryUrl,
    Uri? CoverUrl,
    MatchTier MatchTier,
    string Explanation,
    int? AiRank = null);
