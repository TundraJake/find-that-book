using FindThatBook.Domain;
using FindThatBook.Infrastructure.OpenLibrary;
using FindThatBook.Infrastructure.OpenLibrary.Enrichment;
using FindThatBook.Infrastructure.OpenLibrary.Search;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FindThatBook.UnitTests.TestDoubles;

internal static class OpenLibraryTestFactory
{
    public static OpenLibraryOptions CreateOptions() => new()
    {
        BaseUrl = "https://openlibrary.test/",
        CoversBaseUrl = "https://covers.openlibrary.test/",
        UserAgent = "FindThatBook-Tests/1.0",
        TimeoutSeconds = 5,
        SearchLimit = 10,
        EnrichLimit = 12,
        AuthorWorksLimit = 1000,
        MaximumConcurrentRequests = 4,
        CacheMinutes = 5,
    };

    public static OpenLibraryClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(CreateOptions().BaseUrl) }, new MemoryCache(new MemoryCacheOptions()), Options.Create(CreateOptions()));

    public static OpenLibraryBookCatalog CreateCatalog(HttpMessageHandler handler, OpenLibraryOptions? catalogOptions = null)
    {
        IOptions<OpenLibraryOptions> options = Options.Create(catalogOptions ?? CreateOptions());
        OpenLibraryClient client = new(new HttpClient(handler) { BaseAddress = new Uri(options.Value.BaseUrl) }, new MemoryCache(new MemoryCacheOptions()), options);
        return new OpenLibraryBookCatalog(client, new WorkEnricher(client, options, NullLogger<WorkEnricher>.Instance), options, NullLogger<OpenLibraryBookCatalog>.Instance);
    }

    public static string ReadFixture(string fileName) => GeminiTestFactory.ReadFixture(Path.Combine("OpenLibrary", fileName));

    /// <summary>Routes for the Tolkien/Hobbit fixtures captured from openlibrary.org.</summary>
    public static RoutingHttpMessageHandler CreateHobbitRoutes() => new RoutingHttpMessageHandler()
        .Route("/search.json", ReadFixture("search-title-author-hobbit.json"))
        .Route("/works/OL27482W.json", ReadFixture("work-OL27482W.json"))
        .Route("/works/OL219602W.json", ReadFixture("work-OL219602W.json"))
        .Route("/works/OL262758W.json", ReadFixture("work-OL262758W-redirect.json"));

    public static CatalogWork Work(
        string key,
        string title,
        string[] primaryAuthors,
        int editions,
        int? year = null,
        string[]? contributors = null,
        string? subtitle = null) =>
        new(
            key,
            title,
            subtitle,
            primaryAuthors,
            contributors ?? [],
            year,
            editions,
            new Uri($"https://openlibrary.org{key}"),
            CoverUrl: null,
            primaryAuthors.Select(name => new AuthorProfile(name, new Uri($"https://openlibrary.org/authors/{ToTestAuthorKey(name)}"))).ToList());

    /// <summary>"Jane Austen" → "OL-JANE-AUSTEN-A": a readable stand-in for an Open Library author key.</summary>
    public static string ToTestAuthorKey(string name) => $"OL-{name.ToUpperInvariant().Replace(' ', '-')}-A";
}
