using FindThatBook.Application.Matching;
using FindThatBook.Domain;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Matching;

public sealed class WorkMatcherTests
{
    [Theory]
    [InlineData("tale two cities", "A Tale of Two Cities", TitleMatch.Exact)]
    [InlineData("A Tale of Two Cities", "A Tale of Two Cities [adaptation]", TitleMatch.Near)]
    [InlineData("The Hobbit", "Poems from The hobbit", TitleMatch.Near)]
    [InlineData("The Hobbit", "The Two Towers", TitleMatch.None)]
    public void MatchTitle_ComparesSignificantWords(string queryTitle, string workTitle, TitleMatch expected)
    {
        QueryHypothesis hypothesis = new(queryTitle, null, [], [], null);

        Assert.Equal(expected, WorkMatcher.MatchTitle(hypothesis, OpenLibraryTestFactory.Work("/works/X", workTitle, ["Someone"], 1)));
    }

    [Fact]
    public void MatchTitle_SubtitleVariant_IsExact()
    {
        QueryHypothesis hypothesis = new("The Hobbit or There and Back Again", null, [], [], null);
        CatalogWork work = OpenLibraryTestFactory.Work("/works/X", "The Hobbit", ["J.R.R. Tolkien"], 1, subtitle: "or There and Back Again");

        Assert.Equal(TitleMatch.Exact, WorkMatcher.MatchTitle(hypothesis, work));
    }

    [Fact]
    public void MatchTitle_CharacterHintInTitle_IsNear()
    {
        QueryHypothesis hypothesis = new(null, "Mark Twain", [], ["Huckleberry Finn"], null);
        CatalogWork work = OpenLibraryTestFactory.Work("/works/OL53908W", "Adventures of Huckleberry Finn", ["Mark Twain"], 2623);

        Assert.Equal(TitleMatch.Near, WorkMatcher.MatchTitle(hypothesis, work));
    }

    [Fact]
    public void MatchAuthor_PrimaryPositionAndContributor()
    {
        QueryHypothesis hypothesis = new(null, "J.R.R. Tolkien", [], [], null);
        CatalogWork graphicNovel = OpenLibraryTestFactory.Work("/works/OL219602W", "The Hobbit", ["Charles Dixon", "Sean Deming", "J.R.R. Tolkien"], 10);
        CatalogWork annotated = OpenLibraryTestFactory.Work("/works/Y", "The History of the Hobbit", ["John D. Rateliff"], 5, contributors: ["J.R.R. Tolkien"]);

        Assert.Equal(new AuthorMatch(AuthorMatchKind.Primary, 2), WorkMatcher.MatchAuthor(hypothesis, graphicNovel));
        Assert.Equal(AuthorMatchKind.Contributor, WorkMatcher.MatchAuthor(hypothesis, annotated).Kind);
    }
}
