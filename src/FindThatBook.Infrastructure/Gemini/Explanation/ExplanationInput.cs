using FindThatBook.Domain;

namespace FindThatBook.Infrastructure.Gemini.Explanation;

/// <summary>Facts handed to Gemini for writing explanations. Only these fields may be cited.</summary>
internal sealed record ExplanationInput(string Query, InterpretedQuery InterpretedAs, IReadOnlyList<CandidateFacts> Candidates)
{
    public static ExplanationInput Create(string query, QueryHypothesis hypothesis, IReadOnlyList<BookCandidate> candidates) => new(
        query,
        new InterpretedQuery(hypothesis.Title, hypothesis.Author, hypothesis.Keywords, hypothesis.CharacterHints, hypothesis.YearHint),
        candidates.Select(CandidateFacts.FromDomain).ToList());
}

internal sealed record InterpretedQuery(
    string? Title,
    string? Author,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> CharacterHints,
    int? YearHint);

internal sealed record CandidateFacts(
    string Id,
    string Title,
    IReadOnlyList<string> PrimaryAuthors,
    IReadOnlyList<string> Contributors,
    int? FirstPublishYear,
    string MatchReason)
{
    private static readonly Dictionary<MatchTier, string> _matchReasons = new()
    {
        [MatchTier.ExactTitlePrimaryAuthor] = "exact title match and the queried author is a primary author",
        [MatchTier.ExactTitleContributorAuthor] = "exact title match but the queried author is only a contributor",
        [MatchTier.NearTitleAuthor] = "close title match with a matching author",
        [MatchTier.AuthorOnly] = "matches the queried author only (no title given or matched)",
        [MatchTier.Fallback] = "possible match; no strong title or author signal",
    };

    public static CandidateFacts FromDomain(BookCandidate candidate) => new(
        candidate.WorkKey,
        candidate.Title,
        candidate.PrimaryAuthors,
        candidate.Contributors,
        candidate.FirstPublishYear,
        _matchReasons[candidate.MatchTier]);
}
