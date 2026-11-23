using FindThatBook.Application;

namespace FindThatBook.Infrastructure.Gemini.Quota;

/// <summary>Thrown by <see cref="GeminiClient"/> when every configured model is over quota.</summary>
internal sealed class GeminiQuotaExhaustedException(AiQuotaScope scope, TimeSpan retryAfter)
    : Exception($"All Gemini models are over quota ({scope}).")
{
    public AiQuotaScope Scope { get; } = scope;

    public TimeSpan RetryAfter { get; } = retryAfter;

    public AiQuotaExhaustedException ToApplicationException() => new(Scope, RetryAfter, this);
}
