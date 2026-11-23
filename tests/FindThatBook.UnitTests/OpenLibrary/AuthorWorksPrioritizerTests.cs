using System.Text.Json;
using FindThatBook.Infrastructure.OpenLibrary;
using FindThatBook.Infrastructure.OpenLibrary.Models;
using FindThatBook.Infrastructure.OpenLibrary.Search;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.OpenLibrary;

public sealed class AuthorWorksPrioritizerTests
{
    private static IReadOnlyList<SearchDocument> HobbitDocuments() =>
        JsonSerializer.Deserialize<SearchResponse>(OpenLibraryTestFactory.ReadFixture("search-title-author-hobbit.json"), OpenLibraryClient.WireSerializerOptions)!.Docs!;

    [Theory]
    [InlineData("J.R.R. Tolkien")]
    [InlineData("tolkien")]
    public void ResolveAuthorKey_MatchesBySurname(string queriedAuthor)
    {
        Assert.Equal("OL26320A", AuthorWorksPrioritizer.ResolveAuthorKey(queriedAuthor, HobbitDocuments()));
    }

    [Fact]
    public void ResolveAuthorKey_ContributorNameResolvesToTheirOwnKey()
    {
        Assert.Equal("OL2453782A", AuthorWorksPrioritizer.ResolveAuthorKey("Charles Dixon", HobbitDocuments()));
    }

    [Fact]
    public void ResolveAuthorKey_UnknownAuthor_ReturnsNull()
    {
        Assert.Null(AuthorWorksPrioritizer.ResolveAuthorKey("Jane Austen", HobbitDocuments()));
    }

    [Fact]
    public void PrioritizeConfirmed_MovesConfirmedWorksFirstKeepingOrderAndDroppingNothing()
    {
        IReadOnlyList<SearchDocument> documents = HobbitDocuments();
        HashSet<string> confirmed = new(StringComparer.Ordinal) { "/works/OL14933471W", "/works/OL27482W" };

        List<SearchDocument> prioritized = AuthorWorksPrioritizer.PrioritizeConfirmed(documents, confirmed);

        Assert.Equal<string>(["/works/OL27482W", "/works/OL14933471W"], prioritized.Take(2).Select(document => document.Key).ToList());
        Assert.Equal(documents.Count, prioritized.Count);
    }

    [Fact]
    public async Task GetAuthorWorksAsync_ParsesRealEnvelope()
    {
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler()
            .Route("/authors/OL26320A/works.json", OpenLibraryTestFactory.ReadFixture("author-works-OL26320A.json"));
        OpenLibraryClient client = OpenLibraryTestFactory.CreateClient(handler);

        AuthorWorksResponse? works = await client.GetAuthorWorksAsync("/authors/OL26320A", CancellationToken.None);

        Assert.Equal(367, works!.Size);
        Assert.Equal("/works/OL46241531W", works.Entries![0].Key);
        Assert.Equal(3, works.Entries.Count);
    }
}
