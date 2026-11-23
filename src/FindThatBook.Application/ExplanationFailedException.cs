namespace FindThatBook.Application;

/// <summary>Writing "why it matched" explanations failed.</summary>
public sealed class ExplanationFailedException(string reason, Exception? innerException = null)
    : UpstreamServiceFailedException(reason, innerException)
{
    public override string Title => "Gemini could not explain the matches";
}
