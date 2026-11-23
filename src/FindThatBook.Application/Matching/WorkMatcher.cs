using FindThatBook.Domain;

namespace FindThatBook.Application.Matching;

/// <summary>Compares a query hypothesis with a catalog work.</summary>
public static class WorkMatcher
{
    /// <summary>
    /// Exact: the query's significant words equal the work's main title (or main title + subtitle).
    /// Near: one side's words are all contained in the other's, or a character hint appears in the title.
    /// </summary>
    public static TitleMatch MatchTitle(QueryHypothesis hypothesis, CatalogWork work)
    {
        IReadOnlyList<string> titleWords = TextNormalizer.SignificantWords(work.Title);
        IReadOnlyList<string> fullTitleWords = work.Subtitle is null ? titleWords : TextNormalizer.SignificantWords($"{work.Title} {work.Subtitle}");

        if (hypothesis.Title is not null)
        {
            IReadOnlyList<string> queryWords = TextNormalizer.SignificantWords(hypothesis.Title);
            if (queryWords.Count > 0)
            {
                if (queryWords.SequenceEqual(titleWords) || queryWords.SequenceEqual(fullTitleWords))
                {
                    return TitleMatch.Exact;
                }

                if (ContainsAll(fullTitleWords, queryWords) || (titleWords.Count > 0 && ContainsAll(queryWords, titleWords)))
                {
                    return TitleMatch.Near;
                }
            }
        }

        bool characterInTitle = hypothesis.CharacterHints
            .Select(TextNormalizer.SignificantWords)
            .Any(hintWords => hintWords.Count > 0 && ContainsAll(fullTitleWords, hintWords));
        return characterInTitle ? TitleMatch.Near : TitleMatch.None;
    }

    /// <summary>Matches the queried author by surname against primary authors first, then contributors.</summary>
    public static AuthorMatch MatchAuthor(QueryHypothesis hypothesis, CatalogWork work)
    {
        string? surname = hypothesis.Author is null ? null : TextNormalizer.Surname(hypothesis.Author);
        if (surname is null)
        {
            return AuthorMatch.None;
        }

        for (int position = 0; position < work.PrimaryAuthors.Count; position++)
        {
            if (string.Equals(TextNormalizer.Surname(work.PrimaryAuthors[position]), surname, StringComparison.Ordinal))
            {
                return new AuthorMatch(AuthorMatchKind.Primary, position);
            }
        }

        bool isContributor = work.Contributors.Any(name => string.Equals(TextNormalizer.Surname(name), surname, StringComparison.Ordinal));
        return isContributor ? new AuthorMatch(AuthorMatchKind.Contributor, int.MaxValue) : AuthorMatch.None;
    }

    private static bool ContainsAll(IReadOnlyList<string> container, IReadOnlyList<string> words) =>
        words.All(word => container.Contains(word, StringComparer.Ordinal));
}
