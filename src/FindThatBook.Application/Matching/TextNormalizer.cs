using System.Globalization;
using System.Text;

namespace FindThatBook.Application.Matching;

/// <summary>Normalizes titles and names for comparison: lowercase, no diacritics, no punctuation, no filler words.</summary>
public static class TextNormalizer
{
    private static readonly HashSet<string> _fillerWords = new(StringComparer.Ordinal) { "the", "a", "an", "of", "and" };

    /// <summary>"García Márquez: The Novel!" → "garcia marquez the novel".</summary>
    public static string Normalize(string text)
    {
        string decomposed = text.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(decomposed.Length);
        foreach (char character in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Normalized words with filler words ("the", "a", "of", …) removed: "A Tale of Two Cities" → [tale, two, cities].</summary>
    public static IReadOnlyList<string> SignificantWords(string text) =>
        Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(word => !_fillerWords.Contains(word)).ToList();

    /// <summary>The last normalized word of a name, ignoring single-letter initials: "J.R.R. Tolkien" → "tolkien".</summary>
    public static string? Surname(string name) =>
        Normalize(name).Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault(word => word.Length > 1);
}
