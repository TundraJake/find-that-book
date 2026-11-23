using System.ComponentModel.DataAnnotations;
using FindThatBook.Infrastructure.Gemini;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Gemini;

public sealed class GeminiOptionsTests
{
    [Fact]
    public void Validate_MissingApiKey_StillValidSoTheAppStarts()
    {
        GeminiOptions options = GeminiTestFactory.CreateOptions(apiKey: string.Empty);

        bool isValid = Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true);

        Assert.True(isValid);
        Assert.False(options.HasApiKey);
    }

    [Fact]
    public void Validate_NoModels_Fails()
    {
        GeminiOptions options = GeminiTestFactory.CreateOptions();
        options.Models = [];

        bool isValid = Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true);

        Assert.False(isValid);
    }

    [Fact]
    public void Validate_UnknownThinkingLevel_Fails()
    {
        GeminiOptions options = GeminiTestFactory.CreateOptions();
        options.ThinkingLevel = "extreme";

        bool isValid = Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true);

        Assert.False(isValid);
    }
}
