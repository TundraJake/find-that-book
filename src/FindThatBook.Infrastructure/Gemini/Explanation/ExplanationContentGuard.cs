using System.Text.RegularExpressions;
using FindThatBook.Application.Matching;
using FindThatBook.Domain;

namespace FindThatBook.Infrastructure.Gemini.Explanation;

/// <summary>
/// Output checks on "why it matched" text (second line of defence after the safety filters). An explanation must be plain prose
/// about its own book: no links, no markup, no code, and it must name the book's title or one of its authors. Anything else
/// usually means the query tried to make the model say something, so the whole search is rejected.
/// </summary>
internal static partial class ExplanationContentGuard
{
    private const string CodeFence = "```";

    /// <summary>Null when the explanation is acceptable, otherwise a short reason for the audit log.</summary>
    public static string? FindProblem(string explanation, BookCandidate candidate)
    {
        if (LinkPattern().IsMatch(explanation))
        {
            return "contains a link";
        }

        if (MarkupPattern().IsMatch(explanation) || explanation.Contains(CodeFence, StringComparison.Ordinal))
        {
            return "contains markup or code";
        }

        return MentionsBook(explanation, candidate) ? null : "does not mention the book's title or author";
    }

    private static bool MentionsBook(string explanation, BookCandidate candidate)
    {
        HashSet<string> explanationWords = TextNormalizer.SignificantWords(explanation).ToHashSet(StringComparer.Ordinal);

        string mainTitle = candidate.Title.Split(':', 2)[0];
        IReadOnlyList<string> titleWords = TextNormalizer.SignificantWords(mainTitle);
        if (titleWords.Count > 0 && titleWords.All(explanationWords.Contains))
        {
            return true;
        }

        return candidate.PrimaryAuthors.Concat(candidate.Contributors)
            .Select(TextNormalizer.Surname)
            .Any(surname => surname is not null && explanationWords.Contains(surname));
    }

    [GeneratedRegex(@"https?://|www\.|\b[a-z0-9-]+\.(com|net|org|io|ru|xyz|ly|me|co)\b", RegexOptions.IgnoreCase)]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"<\s*/?\s*[a-z!]", RegexOptions.IgnoreCase)]
    private static partial Regex MarkupPattern();
}
