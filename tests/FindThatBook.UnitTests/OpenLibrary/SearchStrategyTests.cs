using FindThatBook.Domain;
using FindThatBook.Infrastructure.OpenLibrary.Search;

namespace FindThatBook.UnitTests.OpenLibrary;

public sealed class SearchStrategyTests
{
    private static string Describe(IReadOnlyList<KeyValuePair<string, string>> strategy) =>
        string.Join('&', strategy.Select(pair => $"{pair.Key}={pair.Value}"));

    [Fact]
    public void Build_TitleAndAuthor_RunsCombinedTitleAndAuthorSearches()
    {
        QueryHypothesis hypothesis = new("The Hobbit", "J.R.R. Tolkien", [], [], null);

        IEnumerable<string> strategies = SearchStrategy.Build("tolkien hobbit", hypothesis).Select(Describe);

        Assert.Equal<string>(new[] { "title=The Hobbit&author=J.R.R. Tolkien", "title=The Hobbit", "author=J.R.R. Tolkien&sort=editions" }, strategies);
    }

    [Fact]
    public void Build_CharacterHints_AddsFreeTextSearch()
    {
        QueryHypothesis hypothesis = new(null, "Mark Twain", [], ["Huckleberry Finn"], null);

        IEnumerable<string> strategies = SearchStrategy.Build("mark huckleberry", hypothesis).Select(Describe);

        Assert.Equal<string>(new[] { "author=Mark Twain&sort=editions", "q=Huckleberry Finn Mark Twain" }, strategies);
    }

    [Fact]
    public void Build_NothingExtracted_SearchesOriginalQuery()
    {
        IEnumerable<string> strategies = SearchStrategy.Build("some messy words", QueryHypothesis.Empty).Select(Describe);

        Assert.Equal<string>(new[] { "q=some messy words" }, strategies);
    }
}
