namespace FindThatBook.Domain;

/// <summary>A canonical work from the book catalog, before ranking.</summary>
/// <param name="PrimaryAuthors">Authors from the canonical work record, in catalog order.</param>
/// <param name="Contributors">Names attached to the work's editions but absent from the canonical record (illustrators, adaptors, editors).</param>
/// <param name="EditionCount">Number of editions; used as a popularity tie-breaker.</param>
/// <param name="PrimaryAuthorProfiles">Primary authors with their catalog pages, when the catalog provides author keys.</param>
public sealed record CatalogWork(
    string WorkKey,
    string Title,
    string? Subtitle,
    IReadOnlyList<string> PrimaryAuthors,
    IReadOnlyList<string> Contributors,
    int? FirstPublishYear,
    int EditionCount,
    Uri CatalogUrl,
    Uri? CoverUrl,
    IReadOnlyList<AuthorProfile>? PrimaryAuthorProfiles = null);
