using FindThatBook.Domain;

namespace FindThatBook.Api.Contracts;

/// <summary>Response of <c>POST /api/books/search</c>.</summary>
/// <param name="MatchedAuthor">The queried author's Open Library page when they were found as a primary author; null otherwise.</param>
public sealed record BookSearchResponse(
    string Query,
    QueryHypothesisResponse Hypothesis,
    IReadOnlyList<BookCandidateResponse> Candidates,
    MatchedAuthorResponse? MatchedAuthor)
{
    public static BookSearchResponse FromDomain(BookSearchResult result) => new(
        result.Query,
        QueryHypothesisResponse.FromDomain(result.Hypothesis),
        result.Candidates.Select(BookCandidateResponse.FromDomain).ToList(),
        result.MatchedAuthor is { } author ? new MatchedAuthorResponse(author.Name, author.OpenLibraryUrl.AbsoluteUri) : null);
}

public sealed record MatchedAuthorResponse(string Name, string OpenLibraryUrl);

public sealed record QueryHypothesisResponse(
    string? Title,
    string? Author,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> CharacterHints,
    int? YearHint)
{
    public static QueryHypothesisResponse FromDomain(QueryHypothesis hypothesis) =>
        new(hypothesis.Title, hypothesis.Author, hypothesis.Keywords, hypothesis.CharacterHints, hypothesis.YearHint);
}

/// <param name="Author">Primary author(s) joined with ", ": the single <c>author</c> string of the requested result format.</param>
public sealed record BookCandidateResponse(
    string Title,
    string Author,
    IReadOnlyList<string> PrimaryAuthors,
    IReadOnlyList<string> Contributors,
    int? FirstPublishYear,
    string WorkKey,
    string OpenLibraryUrl,
    string? CoverUrl,
    MatchTier MatchTier,
    string Explanation,
    int? AiRank)
{
    private const string AuthorSeparator = ", ";

    public static BookCandidateResponse FromDomain(BookCandidate candidate) => new(
        candidate.Title,
        string.Join(AuthorSeparator, candidate.PrimaryAuthors),
        candidate.PrimaryAuthors,
        candidate.Contributors,
        candidate.FirstPublishYear,
        candidate.WorkKey,
        candidate.OpenLibraryUrl.AbsoluteUri,
        candidate.CoverUrl?.AbsoluteUri,
        candidate.MatchTier,
        candidate.Explanation,
        candidate.AiRank);
}
