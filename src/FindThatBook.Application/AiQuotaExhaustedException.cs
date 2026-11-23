namespace FindThatBook.Application;

/// <summary>Every configured AI model has hit its usage quota, so no result can be produced until one resets.</summary>
public sealed class AiQuotaExhaustedException(AiQuotaScope scope, TimeSpan retryAfter, Exception? innerException = null)
    : UpstreamServiceFailedException(DescribeReason(scope), innerException)
{
    private const string DailyReason = "Every Gemini model has used its daily request allowance. Limits reset at midnight Pacific time.";
    private const string PerMinuteReason = "Every Gemini model is at its per-minute request limit.";

    public override string Title => Scope == AiQuotaScope.Daily ? "Daily usage limits reached" : "Usage limits reached";

    public AiQuotaScope Scope { get; } = scope;

    /// <summary>Time until the earliest model becomes available again.</summary>
    public TimeSpan RetryAfter { get; } = retryAfter;

    private static string DescribeReason(AiQuotaScope scope) => scope == AiQuotaScope.Daily ? DailyReason : PerMinuteReason;
}
