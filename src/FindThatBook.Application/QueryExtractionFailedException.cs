namespace FindThatBook.Application;

/// <summary>Query extraction failed.</summary>
public sealed class QueryExtractionFailedException(string reason, Exception? innerException = null)
    : UpstreamServiceFailedException(reason, innerException)
{
    public override string Title => "Gemini could not interpret the query";
}
