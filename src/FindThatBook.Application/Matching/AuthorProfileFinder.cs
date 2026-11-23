using FindThatBook.Domain;

namespace FindThatBook.Application.Matching;

/// <summary>
/// Finds the queried author among the catalog works' primary authors (matched by surname), so the UI can link to their page
/// when no exact title match exists. Contributors are ignored: the link should go to someone who wrote the books.
/// </summary>
public static class AuthorProfileFinder
{
    /// <summary>Null when the query named no author or no catalog work lists them as a primary author.</summary>
    public static AuthorProfile? Find(QueryHypothesis hypothesis, IReadOnlyList<CatalogWork> works)
    {
        string? surname = hypothesis.Author is null ? null : TextNormalizer.Surname(hypothesis.Author);
        if (surname is null)
        {
            return null;
        }

        // Most-published works first, so a well-known author wins over a namesake with one obscure title.
        return works
            .OrderByDescending(work => work.EditionCount)
            .SelectMany(work => work.PrimaryAuthorProfiles ?? [])
            .FirstOrDefault(profile => string.Equals(TextNormalizer.Surname(profile.Name), surname, StringComparison.Ordinal));
    }
}
