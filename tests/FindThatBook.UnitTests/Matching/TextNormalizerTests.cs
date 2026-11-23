using FindThatBook.Application.Matching;

namespace FindThatBook.UnitTests.Matching;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("García Márquez: The Novel!", "garcia marquez the novel")]
    [InlineData("  The   Hobbit,  or There and Back Again ", "the hobbit or there and back again")]
    [InlineData("J.R.R. Tolkien", "j r r tolkien")]
    public void Normalize_StripsCaseDiacriticsAndPunctuation(string input, string expected)
    {
        Assert.Equal(expected, TextNormalizer.Normalize(input));
    }

    [Fact]
    public void SignificantWords_DropsFillerWords()
    {
        Assert.Equal<string>(new[] { "tale", "two", "cities" }, TextNormalizer.SignificantWords("A Tale of Two Cities"));
    }

    [Theory]
    [InlineData("J.R.R. Tolkien", "tolkien")]
    [InlineData("Jane Austen", "austen")]
    [InlineData("Gabriel García Márquez", "marquez")]
    public void Surname_IsLastMultiLetterWord(string name, string expected)
    {
        Assert.Equal(expected, TextNormalizer.Surname(name));
    }
}
