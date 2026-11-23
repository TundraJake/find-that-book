using FindThatBook.Application;

namespace FindThatBook.Infrastructure.Gemini.Quota;

/// <summary>Thrown by <see cref="GeminiClient"/> when no model could answer and at least one was overloaded (HTTP 503).</summary>
internal sealed class GeminiBusyException(TimeSpan retryAfter) : Exception("All available Gemini models are overloaded.")
{
    public TimeSpan RetryAfter { get; } = retryAfter;

    public AiServiceBusyException ToApplicationException() => new(RetryAfter, this);
}
