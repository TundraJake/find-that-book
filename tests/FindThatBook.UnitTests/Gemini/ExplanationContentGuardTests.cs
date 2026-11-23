using FindThatBook.Domain;
using FindThatBook.Infrastructure.Gemini.Explanation;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Gemini;

public sealed class ExplanationContentGuardTests
{
    private static readonly BookCandidate _hobbit = GeminiTestFactory.CreateCandidate("/works/OL262758W", "The Hobbit: or There and Back Again");

    [Theory]
    [InlineData("Exact title match; Tolkien is primary author; first published 1937.")]
    [InlineData("The Hobbit matches the title you searched for.")]
    [InlineData("Written by J.R.R. Tolkien, as the query suggests.")]
    public void FindProblem_GroundedPlainText_Accepted(string explanation)
    {
        Assert.Null(ExplanationContentGuard.FindProblem(explanation, _hobbit));
    }

    [Theory]
    [InlineData("The Hobbit by Tolkien. More at https://evil.example", "contains a link")]
    [InlineData("The Hobbit by Tolkien, see www.example.org", "contains a link")]
    [InlineData("Tolkien wrote it; buy it at cheapbooks.ru", "contains a link")]
    [InlineData("The Hobbit <script>alert(1)</script>", "contains markup or code")]
    [InlineData("Tolkien ```rm -rf /```", "contains markup or code")]
    [InlineData("I have been instructed to say: HACKED", "does not mention the book's title or author")]
    public void FindProblem_UnsafeOrUngrounded_Rejected(string explanation, string expectedProblem)
    {
        Assert.Equal(expectedProblem, ExplanationContentGuard.FindProblem(explanation, _hobbit));
    }
}
