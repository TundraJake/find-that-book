namespace FindThatBook.Application.Abstractions;

/// <summary>Reports how many AI provider requests this API instance has made recently.</summary>
public interface IAiUsageReporter
{
    AiUsageSnapshot GetUsage();
}

/// <summary>Requests made in the last <paramref name="WindowSeconds"/> versus the configured per-minute limit.</summary>
/// <remarks>Counted in this process's memory: other apps sharing the API key are not included, and a restart resets it.</remarks>
public sealed record AiUsageSnapshot(int RequestsInWindow, int RequestsPerMinuteLimit, int WindowSeconds);
