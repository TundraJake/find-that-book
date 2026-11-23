using FindThatBook.Domain;

namespace FindThatBook.Infrastructure.OpenLibrary.Search;

/// <summary>Builds the <c>/search.json</c> queries to run for a hypothesis, most specific first.</summary>
internal static class SearchStrategy
{
    private const string TitleParameter = "title";
    private const string AuthorParameter = "author";
    private const string FreeTextParameter = "q";
    private const string SortParameter = "sort";
    private const string MostEditions = "editions";

    /// <summary>
    /// title+author · title · author (sorted by editions, for "top works by that author") ·
    /// free text when there are character hints, or when nothing structured was extracted.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<KeyValuePair<string, string>>> Build(string query, QueryHypothesis hypothesis)
    {
        List<IReadOnlyList<KeyValuePair<string, string>>> strategies = [];

        if (hypothesis.Title is { } title && hypothesis.Author is { } author)
        {
            strategies.Add([new(TitleParameter, title), new(AuthorParameter, author)]);
        }

        if (hypothesis.Title is { } titleOnly)
        {
            strategies.Add([new(TitleParameter, titleOnly)]);
        }

        if (hypothesis.Author is { } authorOnly)
        {
            strategies.Add([new(AuthorParameter, authorOnly), new(SortParameter, MostEditions)]);
        }

        if (hypothesis.CharacterHints.Count > 0)
        {
            IEnumerable<string> freeTextParts = [.. hypothesis.CharacterHints, hypothesis.Author ?? string.Empty];
            strategies.Add([new(FreeTextParameter, string.Join(' ', freeTextParts.Where(part => part.Length > 0)))]);
        }
        else if (strategies.Count == 0)
        {
            strategies.Add([new(FreeTextParameter, query)]);
        }

        return strategies;
    }
}
