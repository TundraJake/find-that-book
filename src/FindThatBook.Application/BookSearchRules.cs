namespace FindThatBook.Application;

/// <summary>Fixed business rules for book search, taken from the project requirements.</summary>
public static class BookSearchRules
{
    /// <summary>Match hierarchy: when there is no clear winner, return up to 5 ordered candidates with explanations.</summary>
    public const int MaximumCandidates = 5;
}
