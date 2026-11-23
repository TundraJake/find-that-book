namespace FindThatBook.Application;

/// <summary>The AI model stayed "overloaded / high demand" (HTTP 503) through its retries. Temporary: the client should retry shortly.</summary>
public sealed class AiServiceBusyException(TimeSpan retryAfter, Exception? innerException = null)
    : UpstreamServiceFailedException(BusyReason, innerException)
{
    private const string BusyReason = "Gemini is experiencing high demand and did not recover after retrying.";

    public override string Title => "Gemini is busy right now";

    public TimeSpan RetryAfter { get; } = retryAfter;
}
