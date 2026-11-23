namespace FindThatBook.Application;

/// <summary>
/// The search was refused as unsafe: Gemini's safety filters blocked the query or response, or an explanation failed the
/// output checks (links, markup, or text not about the book), which usually means the query tried to steer the model.
/// </summary>
public sealed class SearchRejectedException(string reason, Exception? innerException = null)
    : UpstreamServiceFailedException(reason, innerException)
{
    public override string Title => "This search can't be processed";
}
