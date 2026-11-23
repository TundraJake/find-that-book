namespace FindThatBook.Domain;

/// <summary>Outcome of a book search: the interpreted query, ordered candidates, and the queried author's page when they were found.</summary>
/// <param name="MatchedAuthor">The queried author as a primary author in the catalog results; null when no author was queried or found.</param>
public sealed record BookSearchResult(
    string Query,
    QueryHypothesis Hypothesis,
    IReadOnlyList<BookCandidate> Candidates,
    AuthorProfile? MatchedAuthor = null);
