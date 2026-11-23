namespace FindThatBook.Infrastructure.Gemini;

/// <summary>Gemini returned something we cannot use (HTTP error, block, truncation, empty output).</summary>
public sealed class GeminiException(string reason) : Exception($"Gemini response unusable: {reason}")
{
    public string Reason { get; } = reason;
}
