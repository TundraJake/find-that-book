using FindThatBook.Application.Matching;
using FindThatBook.Infrastructure.OpenLibrary.Models;

namespace FindThatBook.Infrastructure.OpenLibrary.Search;

/// <summary>
/// Uses the queried author's <c>/authors/{id}/works.json</c> list to move works they actually wrote ahead of works where they are
/// only mentioned (anthologies, adaptations, contributor credits) before the enrichment limit is applied. Nothing is dropped.
/// </summary>
internal static class AuthorWorksPrioritizer
{
    /// <summary>The Open Library author key of the queried author, matched by surname against search results' <c>author_name</c>.</summary>
    public static string? ResolveAuthorKey(string queriedAuthor, IEnumerable<SearchDocument> documents)
    {
        string? surname = TextNormalizer.Surname(queriedAuthor);
        if (surname is null)
        {
            return null;
        }

        foreach (SearchDocument document in documents)
        {
            IReadOnlyList<string> names = document.AuthorName ?? [];
            IReadOnlyList<string> keys = document.AuthorKey ?? [];
            for (int index = 0; index < Math.Min(names.Count, keys.Count); index++)
            {
                if (string.Equals(TextNormalizer.Surname(names[index]), surname, StringComparison.Ordinal))
                {
                    return keys[index];
                }
            }
        }

        return null;
    }

    /// <summary>Stable reorder: documents confirmed by the author's works list first, the rest after, each group in original order.</summary>
    public static List<SearchDocument> PrioritizeConfirmed(IReadOnlyList<SearchDocument> documents, IReadOnlySet<string> confirmedWorkKeys) =>
        documents.Where(document => confirmedWorkKeys.Contains(document.Key))
            .Concat(documents.Where(document => !confirmedWorkKeys.Contains(document.Key)))
            .ToList();
}
