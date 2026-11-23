using FindThatBook.Application.Abstractions;
using FindThatBook.Infrastructure.Gemini;
using FindThatBook.Infrastructure.Gemini.Quota;
using FindThatBook.Infrastructure.Gemini.Extraction;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Gemini;

public sealed class GeminiUsageTrackerTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetUsage_CountsRequestsInRollingSixtySeconds()
    {
        ManualTimeProvider clock = new(_start);
        GeminiUsageTracker tracker = GeminiTestFactory.CreateUsageTracker(GeminiTestFactory.CreateOptions(), clock);

        tracker.RecordRequest();
        clock.Advance(TimeSpan.FromSeconds(30));
        tracker.RecordRequest();
        clock.Advance(TimeSpan.FromSeconds(31));

        AiUsageSnapshot usage = tracker.GetUsage();

        Assert.Equal(1, usage.RequestsInWindow);
        Assert.Equal(10, usage.RequestsPerMinuteLimit);
        Assert.Equal(60, usage.WindowSeconds);
    }

    [Fact]
    public void GetUsage_AllRequestsExpireAfterAMinute()
    {
        ManualTimeProvider clock = new(_start);
        GeminiUsageTracker tracker = GeminiTestFactory.CreateUsageTracker(GeminiTestFactory.CreateOptions(), clock);

        tracker.RecordRequest();
        tracker.RecordRequest();
        clock.Advance(TimeSpan.FromSeconds(60));

        Assert.Equal(0, tracker.GetUsage().RequestsInWindow);
    }

    [Fact]
    public async Task GeminiClient_RecordsEveryAttemptIncludingFailures()
    {
        GeminiOptions options = GeminiTestFactory.CreateOptions();
        GeminiUsageTracker tracker = GeminiTestFactory.CreateUsageTracker(options);
        GeminiClient client = GeminiTestFactory.CreateClient(GeminiTestFactory.RespondWith("{}", System.Net.HttpStatusCode.TooManyRequests), options, tracker);

        await Assert.ThrowsAsync<GeminiQuotaExhaustedException>(() => client.GenerateJsonAsync("prompt", ExtractionSchema.Create(), CancellationToken.None));

        Assert.Equal(options.Models.Count, tracker.GetUsage().RequestsInWindow);
    }
}
