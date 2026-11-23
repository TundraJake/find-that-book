using FindThatBook.Application.Matching;
using FindThatBook.Domain;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Matching;

public sealed class CandidateRankerTests
{
    private static readonly QueryHypothesis _tolkienHobbit = new("The Hobbit", "J.R.R. Tolkien", ["illustrated", "deluxe"], [], 1937);

    /// <summary>Same works and edition counts as the live Open Library search for title=the hobbit&amp;author=tolkien.</summary>
    private static readonly CatalogWork[] _hobbitWorks =
    [
        OpenLibraryTestFactory.Work("/works/OL219602W", "The Hobbit", ["Charles Dixon", "Sean Deming", "J.R.R. Tolkien"], 10, 1990),
        OpenLibraryTestFactory.Work("/works/OL14933471W", "Poems from The hobbit", ["J.R.R. Tolkien"], 3, 1999),
        OpenLibraryTestFactory.Work("/works/OL27482W", "The Hobbit", ["J.R.R. Tolkien"], 482, 1937),
        OpenLibraryTestFactory.Work("/works/OL14926019W", "The Hobbit & The Lord of the Rings [collection/set]", ["J.R.R. Tolkien"], 27, 1979),
        OpenLibraryTestFactory.Work("/works/OL27513W", "The Fellowship of the Ring", ["J.R.R. Tolkien"], 319, 1954),
        OpenLibraryTestFactory.Work("/works/HIST", "The History of the Hobbit", ["John D. Rateliff"], 4, 2007, contributors: ["J.R.R. Tolkien"]),
    ];

    [Fact]
    public void Rank_CanonicalWorkWithSoleAuthorComesFirst()
    {
        IReadOnlyList<BookCandidate> ranked = CandidateRanker.Rank(_tolkienHobbit, _hobbitWorks, 5);

        Assert.Equal("/works/OL27482W", ranked[0].WorkKey);
        Assert.Equal(MatchTier.ExactTitlePrimaryAuthor, ranked[0].MatchTier);
        Assert.Equal("/works/OL219602W", ranked[1].WorkKey);
        Assert.Equal(MatchTier.ExactTitlePrimaryAuthor, ranked[1].MatchTier);
    }

    [Fact]
    public void Rank_OrdersTiersStrongestFirstAndCapsAtMaximum()
    {
        IReadOnlyList<BookCandidate> ranked = CandidateRanker.Rank(_tolkienHobbit, _hobbitWorks, 5);

        Assert.Equal(5, ranked.Count);
        Assert.Equal(ranked.Select(candidate => candidate.MatchTier).OrderBy(tier => tier), ranked.Select(candidate => candidate.MatchTier));
        Assert.Equal(MatchTier.NearTitleAuthor, ranked.Single(candidate => candidate.WorkKey == "/works/HIST").MatchTier);
        Assert.DoesNotContain(ranked, candidate => candidate.WorkKey == "/works/OL27513W");
    }

    [Fact]
    public void ClassifyTier_ExactTitleWithContributorOnlyAuthor_IsTierB()
    {
        AuthorMatch contributor = new(AuthorMatchKind.Contributor, int.MaxValue);

        Assert.Equal(MatchTier.ExactTitleContributorAuthor, CandidateRanker.ClassifyTier(_tolkienHobbit, TitleMatch.Exact, contributor));
    }

    [Fact]
    public void Rank_AuthorOnlyQuery_ReturnsTopWorksByEditions()
    {
        QueryHypothesis austen = new(null, "Jane Austen", [], ["Bennet"], null);
        CatalogWork[] works =
        [
            OpenLibraryTestFactory.Work("/works/OL66512W", "Novels (Emma / Mansfield Park)", ["Jane Austen"], 75, 1818),
            OpenLibraryTestFactory.Work("/works/OL66554W", "Pride and Prejudice", ["Jane Austen"], 4042, 1813),
        ];

        IReadOnlyList<BookCandidate> ranked = CandidateRanker.Rank(austen, works, 5);

        Assert.Equal("Pride and Prejudice", ranked[0].Title);
        Assert.All(ranked, candidate => Assert.Equal(MatchTier.AuthorOnly, candidate.MatchTier));
    }

    [Fact]
    public void DeduplicateToCanonical_KeepsRecordWithMostEditions()
    {
        CatalogWork[] works =
        [
            OpenLibraryTestFactory.Work("/works/OL15165350W", "Pride and Prejudice", ["Jane Austen"], 132),
            OpenLibraryTestFactory.Work("/works/OL66554W", "Pride and Prejudice", ["Jane Austen"], 4042),
            OpenLibraryTestFactory.Work("/works/OL66554W", "Pride and Prejudice", ["Jane Austen"], 4042),
        ];

        CatalogWork single = Assert.Single(CandidateRanker.DeduplicateToCanonical(works));

        Assert.Equal("/works/OL66554W", single.WorkKey);
    }
}
