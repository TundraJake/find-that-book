using FindThatBook.Application;

namespace FindThatBook.Infrastructure.Gemini.Quota;

/// <summary>
/// Remembers which models are over quota and until when, so later requests skip straight to a model that can answer.
/// Daily quotas block a model until the next midnight Pacific; per-minute quotas block it for Google's retryDelay.
/// In-memory and per process: a restart forgets the blocks and simply re-learns them from the next 429.
/// </summary>
internal sealed class GeminiModelAvailability(TimeProvider timeProvider)
{
    private const string QuotaResetTimeZoneId = "America/Los_Angeles";
    /// <summary>Used if the host has no time zone database; Pacific Standard Time, so the reset is at worst an hour late.</summary>
    private static readonly TimeSpan _fallbackPacificOffset = TimeSpan.FromHours(-8);
    private static readonly TimeSpan _defaultPerMinuteBlock = TimeSpan.FromSeconds(60);
    private static readonly TimeZoneInfo _quotaResetTimeZone = ResolveQuotaResetTimeZone();

    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly Dictionary<string, (DateTimeOffset BlockedUntil, AiQuotaScope Scope)> _blockedModels = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    public bool IsAvailable(string model)
    {
        lock (_lock)
        {
            return !_blockedModels.TryGetValue(model, out var block) || block.BlockedUntil <= _timeProvider.GetUtcNow();
        }
    }

    public void MarkExhausted(string model, GeminiQuotaError quotaError)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset blockedUntil = quotaError.Scope == AiQuotaScope.Daily
            ? NextQuotaReset(now)
            : now + (quotaError.RetryDelay ?? _defaultPerMinuteBlock);

        lock (_lock)
        {
            _blockedModels[model] = (blockedUntil, quotaError.Scope);
        }
    }

    /// <summary>Builds the "everything is exhausted" error: daily only if every model is blocked for the day.</summary>
    public GeminiQuotaExhaustedException CreateExhaustedException(IReadOnlyCollection<string> models)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            var blocks = models.Where(_blockedModels.ContainsKey).Select(model => _blockedModels[model]).ToList();
            AiQuotaScope scope = blocks.Count > 0 && blocks.All(block => block.Scope == AiQuotaScope.Daily) ? AiQuotaScope.Daily : AiQuotaScope.PerMinute;
            TimeSpan retryAfter = blocks.Count > 0 ? blocks.Min(block => block.BlockedUntil) - now : _defaultPerMinuteBlock;
            return new GeminiQuotaExhaustedException(scope, retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero);
        }
    }

    internal static DateTimeOffset NextQuotaReset(DateTimeOffset now)
    {
        DateTimeOffset pacificNow = TimeZoneInfo.ConvertTime(now, _quotaResetTimeZone);
        DateTime nextMidnight = pacificNow.Date.AddDays(1);
        return new DateTimeOffset(nextMidnight, _quotaResetTimeZone.GetUtcOffset(nextMidnight));
    }

    private static TimeZoneInfo ResolveQuotaResetTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(QuotaResetTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(QuotaResetTimeZoneId, _fallbackPacificOffset, QuotaResetTimeZoneId, QuotaResetTimeZoneId);
        }
    }
}
