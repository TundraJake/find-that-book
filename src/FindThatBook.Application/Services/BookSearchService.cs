using FindThatBook.Application.Abstractions;
using FindThatBook.Application.Matching;
using FindThatBook.Domain;

namespace FindThatBook.Application.Services;

/// <summary>Book search pipeline: extract (Gemini) → find (Open Library) → rank (match hierarchy) → explain (Gemini), plus the queried author's page.</summary>
public sealed class BookSearchService(IQueryExtractor queryExtractor, IBookCatalog bookCatalog, CandidateExplainer candidateExplainer) : IBookSearchService
{
    private readonly IQueryExtractor _queryExtractor = queryExtractor;
    private readonly IBookCatalog _bookCatalog = bookCatalog;
    private readonly CandidateExplainer _candidateExplainer = candidateExplainer;

    public async Task<BookSearchResult> SearchAsync(string query, CancellationToken cancellationToken)
    {
        QueryHypothesis hypothesis = await _queryExtractor.ExtractAsync(query, cancellationToken);
        IReadOnlyList<CatalogWork> works = await _bookCatalog.FindCandidatesAsync(query, hypothesis, cancellationToken);
        IReadOnlyList<BookCandidate> ranked = CandidateRanker.Rank(hypothesis, works, BookSearchRules.MaximumCandidates);
        IReadOnlyList<BookCandidate> explained = await _candidateExplainer.ExplainAsync(query, hypothesis, ranked, cancellationToken);
        return new BookSearchResult(query, hypothesis, explained, AuthorProfileFinder.Find(hypothesis, works));
    }
}
