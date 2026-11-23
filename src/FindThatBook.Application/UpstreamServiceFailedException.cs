namespace FindThatBook.Application;

/// <summary>An external service the pipeline depends on (Gemini, Open Library) failed or returned unusable data.</summary>
public abstract class UpstreamServiceFailedException(string reason, Exception? innerException)
    : Exception($"Upstream service failed: {reason}", innerException)
{
    /// <summary>Short, user-facing summary of which step failed.</summary>
    public abstract string Title { get; }

    /// <summary>Technical reason, safe to show to the user (never contains secrets).</summary>
    public string Reason { get; } = reason;
}
