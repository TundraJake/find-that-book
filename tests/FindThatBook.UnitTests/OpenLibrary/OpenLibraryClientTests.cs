using System.Net;
using FindThatBook.Infrastructure.OpenLibrary;
using FindThatBook.Infrastructure.OpenLibrary.Models;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.OpenLibrary;

public sealed class OpenLibraryClientTests
{
    [Fact]
    public async Task SearchAsync_SendsParametersFieldsAndLimit()
    {
        RoutingHttpMessageHandler handler = OpenLibraryTestFactory.CreateHobbitRoutes();
        OpenLibraryClient client = OpenLibraryTestFactory.CreateClient(handler);

        IReadOnlyList<SearchDocument> documents = await client.SearchAsync([new("title", "The Hobbit"), new("author", "J.R.R. Tolkien")], CancellationToken.None);

        Assert.Equal(5, documents.Count);
        Assert.Equal<string>(new[] { "Charles Dixon", "Sean Deming", "J.R.R. Tolkien" }, documents[1].AuthorName!);
        string query = Uri.UnescapeDataString(handler.RequestedUris.Single().Query);
        Assert.Contains("title=The Hobbit", query, StringComparison.Ordinal);
        Assert.Contains("author=J.R.R. Tolkien", query, StringComparison.Ordinal);
        Assert.Contains($"fields={OpenLibraryClient.SearchFields}", query, StringComparison.Ordinal);
        Assert.Contains("limit=10", query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWorkAsync_RedirectRecord_FollowsToCanonicalWork()
    {
        RoutingHttpMessageHandler handler = OpenLibraryTestFactory.CreateHobbitRoutes();
        OpenLibraryClient client = OpenLibraryTestFactory.CreateClient(handler);

        WorkResponse? work = await client.GetWorkAsync("/works/OL262758W", CancellationToken.None);

        Assert.Equal("/works/OL27482W", work!.Key);
        Assert.Equal("The Hobbit", work.Title);
    }

    [Fact]
    public async Task GetWorkAsync_SecondCall_IsServedFromCache()
    {
        RoutingHttpMessageHandler handler = OpenLibraryTestFactory.CreateHobbitRoutes();
        OpenLibraryClient client = OpenLibraryTestFactory.CreateClient(handler);

        await client.GetWorkAsync("/works/OL27482W", CancellationToken.None);
        await client.GetWorkAsync("/works/OL27482W", CancellationToken.None);

        Assert.Single(handler.RequestedUris);
    }

    [Fact]
    public async Task GetWorkAsync_MissingWork_ReturnsNull()
    {
        OpenLibraryClient client = OpenLibraryTestFactory.CreateClient(new RoutingHttpMessageHandler());

        Assert.Null(await client.GetWorkAsync("/works/OL0W", CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_ServerError_ThrowsOpenLibraryException()
    {
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler().Route("/search.json", "{}", HttpStatusCode.InternalServerError);
        OpenLibraryClient client = OpenLibraryTestFactory.CreateClient(handler);

        OpenLibraryException exception = await Assert.ThrowsAsync<OpenLibraryException>(() => client.SearchAsync([new("q", "x")], CancellationToken.None));

        Assert.StartsWith("HTTP 500", exception.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWorkAsync_LegacyStringReferences_AreAccepted()
    {
        RoutingHttpMessageHandler handler = new RoutingHttpMessageHandler()
            .Route("/works/OL27482W.json", OpenLibraryTestFactory.ReadFixture("work-legacy-string-references.json"));
        OpenLibraryClient client = OpenLibraryTestFactory.CreateClient(handler);

        WorkResponse? work = await client.GetWorkAsync("/works/OL27482W", CancellationToken.None);

        Assert.Equal("/authors/OL26320A", work!.Authors!.Single().Author!.Key);
        Assert.False(work.IsRedirect);
    }
}
