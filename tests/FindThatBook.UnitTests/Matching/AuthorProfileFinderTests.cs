using FindThatBook.Application.Matching;
using FindThatBook.Domain;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Matching;

public sealed class AuthorProfileFinderTests
{
    [Fact]
    public void Find_QueriedAuthorIsPrimary_ReturnsTheirPage()
    {
        QueryHypothesis hypothesis = new("Nonexistent Title", "jrr tolkien", [], [], null);
        CatalogWork[] works = [OpenLibraryTestFactory.Work("/works/HOBBIT", "The Hobbit", ["J.R.R. Tolkien"], 482)];

        AuthorProfile? profile = AuthorProfileFinder.Find(hypothesis, works);

        Assert.Equal("J.R.R. Tolkien", profile!.Name);
        Assert.Equal("https://openlibrary.org/authors/OL-J.R.R.-TOLKIEN-A", profile.OpenLibraryUrl.ToString());
    }

    [Fact]
    public void Find_QueriedAuthorOnlyAContributor_ReturnsNull()
    {
        QueryHypothesis hypothesis = new("The Hobbit", "Dixon", [], [], null);
        CatalogWork[] works = [OpenLibraryTestFactory.Work("/works/HOBBIT", "The Hobbit", ["J.R.R. Tolkien"], 482, contributors: ["Charles Dixon"])];

        Assert.Null(AuthorProfileFinder.Find(hypothesis, works));
    }

    [Fact]
    public void Find_NoAuthorInQuery_ReturnsNull()
    {
        CatalogWork[] works = [OpenLibraryTestFactory.Work("/works/HOBBIT", "The Hobbit", ["J.R.R. Tolkien"], 482)];

        Assert.Null(AuthorProfileFinder.Find(new QueryHypothesis("The Hobbit", null, [], [], null), works));
    }

    [Fact]
    public void Find_SameSurname_PrefersTheMostPublishedWorksAuthor()
    {
        QueryHypothesis hypothesis = new(null, "Austen", [], [], null);
        CatalogWork[] works =
        [
            OpenLibraryTestFactory.Work("/works/OBSCURE", "An Obscure Memoir", ["Roger Austen"], 1),
            OpenLibraryTestFactory.Work("/works/PRIDE", "Pride and Prejudice", ["Jane Austen"], 1500),
        ];

        Assert.Equal("Jane Austen", AuthorProfileFinder.Find(hypothesis, works)!.Name);
    }
}
