using System.Net;
using FindThatBook.Application;
using FindThatBook.Domain;
using FindThatBook.Infrastructure.OpenLibrary;
using FindThatBook.Infrastructure.OpenLibrary.Search;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.OpenLibrary;

public sealed class OpenLibraryBookCatalogTests
{
    private static readonly QueryHypothesis _tolkienHobbit = new("The Hobbit", "J.R.R. Tolkien", [], [], 1937);

    [Fact]
    public async Task FindCandidatesAsync_UsesCanonicalAuthorsAndSplitsContributors()
    {
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(OpenLibraryTestFactory.CreateHobbitRoutes());

        IReadOnlyList<CatalogWork> works = await catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None);

        CatalogWork canonical = works.Single(work => work.WorkKey == "/works/OL27482W");
        Assert.Equal<string>(new[] { "J.R.R. Tolkien" }, canonical.PrimaryAuthors);
        Assert.Empty(canonical.Contributors);
        Assert.Equal(482, canonical.EditionCount);
        Assert.Equal("https://covers.openlibrary.test/b/id/14627509-M.jpg?default=false", canonical.CoverUrl!.ToString());
        Assert.Equal("https://openlibrary.org/works/OL27482W", canonical.CatalogUrl.ToString());
        Assert.Equal("https://openlibrary.org/authors/OL26320A", canonical.PrimaryAuthorProfiles!.Single().OpenLibraryUrl.ToString());

        CatalogWork graphicNovel = works.Single(work => work.WorkKey == "/works/OL219602W");
        Assert.Equal<string>(new[] { "Charles Dixon", "Sean Deming", "J.R.R. Tolkien" }, graphicNovel.PrimaryAuthors);
    }

    [Fact]
    public async Task FindCandidatesAsync_WorkMissingFromCatalog_IsSkipped()
    {
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(OpenLibraryTestFactory.CreateHobbitRoutes());

        IReadOnlyList<CatalogWork> works = await catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None);

        Assert.Equal(2, works.Count);
    }

    [Fact]
    public async Task FindCandidatesAsync_SearchFails_ThrowsBookCatalogUnavailable()
    {
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler().Route("/search.json", "{}", HttpStatusCode.ServiceUnavailable);
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler);

        BookCatalogUnavailableException exception = await Assert.ThrowsAsync<BookCatalogUnavailableException>(
            () => catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None));

        Assert.Contains("503", exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FindCandidatesAsync_MalformedWorkRecord_IsSkippedNotFatal()
    {
        RoutingHttpMessageHandler handler = OpenLibraryTestFactory.CreateHobbitRoutes()
            .Route("/works/OL219602W.json", "{\"key\": \"/works/OL219602W\", \"authors\": 42}");
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler);

        IReadOnlyList<CatalogWork> works = await catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None);

        CatalogWork remaining = Assert.Single(works);
        Assert.Equal("/works/OL27482W", remaining.WorkKey);
    }

    [Fact]
    public async Task FindCandidatesAsync_EnrichesWorksTheQueriedAuthorWroteFirst()
    {
        RoutingHttpMessageHandler handler = OpenLibraryTestFactory.CreateHobbitRoutes()
            .Route("/authors/OL26320A/works.json", OpenLibraryTestFactory.ReadFixture("author-works-OL26320A.json"));
        OpenLibraryOptions options = OpenLibraryTestFactory.CreateOptions();
        options.EnrichLimit = 2;
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler, options);

        await catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None);

        List<string> enrichedWorks = handler.RequestedUris.Select(uri => uri.AbsolutePath).Where(path => path.StartsWith("/works/", StringComparison.Ordinal)).ToList();
        Uri authorWorksRequest = handler.RequestedUris.Single(uri => uri.AbsolutePath == "/authors/OL26320A/works.json");
        Assert.Contains("limit=1000", authorWorksRequest.Query, StringComparison.Ordinal);
        Assert.Equal<string>(["/works/OL14933471W.json", "/works/OL27482W.json"], enrichedWorks.Distinct().Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    public async Task FindCandidatesAsync_AuthorWorksUnavailable_KeepsSearchOrderAndSucceeds()
    {
        RoutingHttpMessageHandler handler = OpenLibraryTestFactory.CreateHobbitRoutes()
            .Route("/authors/OL26320A/works.json", "{}", HttpStatusCode.InternalServerError);
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler);

        IReadOnlyList<CatalogWork> works = await catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None);

        Assert.Equal(2, works.Count);
    }

    [Fact]
    public async Task FindCandidatesAsync_FewResults_SkipsAuthorWorksLookup()
    {
        RoutingHttpMessageHandler handler = OpenLibraryTestFactory.CreateHobbitRoutes()
            .Route("/authors/OL26320A/works.json", OpenLibraryTestFactory.ReadFixture("author-works-OL26320A.json"));
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler);

        await catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None);

        Assert.DoesNotContain(handler.RequestedUris, uri => uri.AbsolutePath == "/authors/OL26320A/works.json");
    }

    [Fact]
    public async Task FindCandidatesAsync_OneStrategyTimesOut_UsesTheOthers()
    {
        TimeoutInjectingHandler handler = new(
            OpenLibraryTestFactory.CreateHobbitRoutes(),
            uri => uri.AbsolutePath == "/search.json" && uri.Query.Contains("sort=editions", StringComparison.Ordinal));
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler);

        IReadOnlyList<CatalogWork> works = await catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None);

        Assert.Equal(2, works.Count);
    }

    [Fact]
    public async Task FindCandidatesAsync_EveryStrategyTimesOut_ThrowsBookCatalogUnavailable()
    {
        TimeoutInjectingHandler handler = new(OpenLibraryTestFactory.CreateHobbitRoutes(), uri => uri.AbsolutePath == "/search.json");
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler);

        BookCatalogUnavailableException exception = await Assert.ThrowsAsync<BookCatalogUnavailableException>(
            () => catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None));

        Assert.Contains("Timeout", exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FindCandidatesAsync_OneWorkLookupTimesOut_SkipsJustThatWork()
    {
        TimeoutInjectingHandler handler = new(OpenLibraryTestFactory.CreateHobbitRoutes(), uri => uri.AbsolutePath == "/works/OL219602W.json");
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler);

        IReadOnlyList<CatalogWork> works = await catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None);

        Assert.Equal("/works/OL27482W", works.Single().WorkKey);
    }

    [Fact]
    public async Task FindCandidatesAsync_EveryWorkLookupTimesOut_ThrowsBookCatalogUnavailable()
    {
        TimeoutInjectingHandler handler = new(OpenLibraryTestFactory.CreateHobbitRoutes(), uri => uri.AbsolutePath.StartsWith("/works/", StringComparison.Ordinal));
        OpenLibraryBookCatalog catalog = OpenLibraryTestFactory.CreateCatalog(handler);

        BookCatalogUnavailableException exception = await Assert.ThrowsAsync<BookCatalogUnavailableException>(
            () => catalog.FindCandidatesAsync("tolkien hobbit", _tolkienHobbit, CancellationToken.None));

        Assert.Contains("every work lookup", exception.Reason, StringComparison.Ordinal);
    }
}
