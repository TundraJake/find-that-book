namespace FindThatBook.Domain;

/// <summary>Strength of a candidate match, strongest first (match hierarchy tiers a–e).</summary>
public enum MatchTier
{
    ExactTitlePrimaryAuthor = 0,
    ExactTitleContributorAuthor = 1,
    NearTitleAuthor = 2,
    AuthorOnly = 3,
    Fallback = 4,
}
