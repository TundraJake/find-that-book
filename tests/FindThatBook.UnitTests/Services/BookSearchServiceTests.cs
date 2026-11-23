using FindThatBook.Application.Abstractions;
using FindThatBook.Application.Services;
using FindThatBook.Domain;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Services;

public sealed class BookSearchServiceTests
{
    private sealed class StubQueryExtractor(QueryHypothesis hypothesis) : IQueryExtractor
    {
        public Task<QueryHypothesis> ExtractAsync(string query, CancellationToken cancellationToken) => Task.FromResult(hypothesis);
    }

    private sealed class StubBookCatalog(IReadOnlyList<CatalogWork> works) : IBookCatalog
    {
        public Task<IReadOnlyList<CatalogWork>> FindCandidatesAsync(string query, QueryHypothesis hypothesis, CancellationToken cancellationToken) =>
            Task.FromResult(works);
    }

    [Fact]
    public async Task SearchAsync_RanksCatalogWorksAndExplainsEachOne()
    {
        QueryHypothesis hypothesis = new("Emma", "Jane Austen", [], [], 1815);
        CatalogWork[] works =
        [
            OpenLibraryTestFactory.Work("/works/PERSUASION", "Persuasion", ["Jane Austen"], 900, 1817),
            OpenLibraryTestFactory.Work("/works/EMMA", "Emma", ["Jane Austen"], 1500, 1815),
        ];
        CandidateExplainerTests.RecordingExplanationWriter writer = new();
        BookSearchService service = new(new StubQueryExtractor(hypothesis), new StubBookCatalog(works), new CandidateExplainer(writer));

        BookSearchResult result = await service.SearchAsync("austen emma", CancellationToken.None);

        Assert.Equal(hypothesis, result.Hypothesis);
        Assert.Equal<string>(new[] { "/works/EMMA", "/works/PERSUASION" }, result.Candidates.Select(candidate => candidate.WorkKey));
        Assert.Equal(MatchTier.ExactTitlePrimaryAuthor, result.Candidates[0].MatchTier);
        Assert.Equal("Because of Emma.", result.Candidates[0].Explanation);
        Assert.Equal(1, writer.CallCount);
        Assert.Equal("Jane Austen", result.MatchedAuthor!.Name);
    }

    [Fact]
    public async Task SearchAsync_TitleNotFoundButAuthorIs_ReturnsAuthorPage()
    {
        QueryHypothesis hypothesis = new("A Book She Never Wrote", "Jane Austen", [], [], null);
        CatalogWork[] works = [OpenLibraryTestFactory.Work("/works/EMMA", "Emma", ["Jane Austen"], 1500, 1815)];
        BookSearchService service = new(new StubQueryExtractor(hypothesis), new StubBookCatalog(works), new CandidateExplainer(new CandidateExplainerTests.RecordingExplanationWriter()));

        BookSearchResult result = await service.SearchAsync("austen never wrote", CancellationToken.None);

        Assert.Equal(MatchTier.AuthorOnly, result.Candidates[0].MatchTier);
        Assert.Equal("https://openlibrary.org/authors/OL-JANE-AUSTEN-A", result.MatchedAuthor!.OpenLibraryUrl.ToString());
    }

    [Fact]
    public async Task SearchAsync_NoCatalogResults_SkipsExplanations()
    {
        CandidateExplainerTests.RecordingExplanationWriter writer = new();
        BookSearchService service = new(new StubQueryExtractor(QueryHypothesis.Empty), new StubBookCatalog([]), new CandidateExplainer(writer));

        BookSearchResult result = await service.SearchAsync("nothing", CancellationToken.None);

        Assert.Empty(result.Candidates);
        Assert.Equal(0, writer.CallCount);
        Assert.Null(result.MatchedAuthor);
    }
}
