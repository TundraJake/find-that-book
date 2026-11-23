namespace FindThatBook.Infrastructure.OpenLibrary;

/// <summary>Open Library returned an error or data we cannot use.</summary>
public sealed class OpenLibraryException(string reason) : Exception($"Open Library response unusable: {reason}")
{
    public string Reason { get; } = reason;
}
