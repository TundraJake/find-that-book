namespace FindThatBook.Application;

/// <summary>The book catalog (Open Library) could not be reached, timed out, or returned unusable data. Usually temporary.</summary>
public sealed class BookCatalogUnavailableException(string reason, Exception? innerException = null)
    : UpstreamServiceFailedException(reason, innerException)
{
    public override string Title => "Open Library could not be searched";
}
