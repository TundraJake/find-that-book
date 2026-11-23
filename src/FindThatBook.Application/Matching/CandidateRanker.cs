using FindThatBook.Domain;

namespace FindThatBook.Application.Matching;

/// <summary>
/// Ranks catalog works into <see cref="MatchTier"/>s (the match hierarchy) and returns the top candidates.
/// Within a tier: closer title, queried author listed earlier, closer to the year hint, more editions.
/// </summary>
public static class CandidateRanker
{
    private sealed record ScoredWork(CatalogWork Work, MatchTier Tier, TitleMatch Title, AuthorMatch Author, int YearDistance);

    public static IReadOnlyList<BookCandidate> Rank(QueryHypothesis hypothesis, IReadOnlyList<CatalogWork> works, int maximumCandidates) =>
        DeduplicateToCanonical(works)
            .Select(work => Score(hypothesis, work))
            .OrderBy(scored => scored.Tier)
            .ThenByDescending(scored => scored.Title)
            .ThenByDescending(scored => scored.Author.Kind)
            .ThenBy(scored => scored.Author.PrimaryPosition)
            .ThenBy(scored => scored.YearDistance)
            .ThenByDescending(scored => scored.Work.EditionCount)
            .ThenBy(scored => scored.Work.WorkKey, StringComparer.Ordinal)
            .Take(maximumCandidates)
            .Select(ToCandidate)
            .ToList();

    /// <summary>
    /// Tier rules. "Author satisfied" means the queried author is a primary author, or the query named no author.
    /// a) exact title + author satisfied · b) exact title + author only a contributor · c) near title + author satisfied or contributor ·
    /// d) author matches (primary or contributor) without a title match · e) anything else.
    /// </summary>
    internal static MatchTier ClassifyTier(QueryHypothesis hypothesis, TitleMatch title, AuthorMatch author)
    {
        bool queriedAuthor = hypothesis.Author is not null;
        bool authorSatisfied = !queriedAuthor || author.Kind == AuthorMatchKind.Primary;
        bool authorMentioned = author.Kind != AuthorMatchKind.None;

        return (title, authorSatisfied, author.Kind) switch
        {
            (TitleMatch.Exact, true, _) => MatchTier.ExactTitlePrimaryAuthor,
            (TitleMatch.Exact, false, AuthorMatchKind.Contributor) => MatchTier.ExactTitleContributorAuthor,
            (TitleMatch.Near, true, _) or (TitleMatch.Near, false, AuthorMatchKind.Contributor) => MatchTier.NearTitleAuthor,
            _ when authorMentioned => MatchTier.AuthorOnly,
            _ => MatchTier.Fallback,
        };
    }

    /// <summary>
    /// Collapses duplicate catalog records of the same book (same normalized title and first primary author),
    /// keeping the one with the most editions. Exact key duplicates are dropped too.
    /// </summary>
    internal static IReadOnlyList<CatalogWork> DeduplicateToCanonical(IReadOnlyList<CatalogWork> works) =>
        works
            .GroupBy(work => work.WorkKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .GroupBy(work => (TextNormalizer.Normalize(work.Title), FirstAuthorSurname(work)))
            .Select(group => group.OrderByDescending(work => work.EditionCount).First())
            .ToList();

    private static string FirstAuthorSurname(CatalogWork work) =>
        work.PrimaryAuthors.Count == 0 ? string.Empty : TextNormalizer.Surname(work.PrimaryAuthors[0]) ?? string.Empty;

    private static ScoredWork Score(QueryHypothesis hypothesis, CatalogWork work)
    {
        TitleMatch title = WorkMatcher.MatchTitle(hypothesis, work);
        AuthorMatch author = WorkMatcher.MatchAuthor(hypothesis, work);
        int yearDistance = hypothesis.YearHint is int year && work.FirstPublishYear is int published ? Math.Abs(published - year) : int.MaxValue;
        return new ScoredWork(work, ClassifyTier(hypothesis, title, author), title, author, yearDistance);
    }

    private static BookCandidate ToCandidate(ScoredWork scored) => new(
        scored.Work.Title,
        scored.Work.PrimaryAuthors,
        scored.Work.Contributors,
        scored.Work.FirstPublishYear,
        scored.Work.WorkKey,
        scored.Work.CatalogUrl,
        scored.Work.CoverUrl,
        scored.Tier,
        Explanation: string.Empty);
}
