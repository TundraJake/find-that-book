using FindThatBook.Application;
using FindThatBook.Infrastructure.Gemini.Quota;
using FindThatBook.UnitTests.TestDoubles;

namespace FindThatBook.UnitTests.Gemini;

public sealed class GeminiQuotaTests
{
    private static readonly DateTimeOffset _pacificAfternoon = new(2026, 11, 23, 15, 0, 0, TimeSpan.FromHours(-8));

    [Fact]
    public void Parse_DailyQuotaFailure_ReturnsDailyScopeAndRetryDelay()
    {
        GeminiQuotaError quotaError = GeminiQuotaError.Parse(GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-daily.json"));

        Assert.Equal(AiQuotaScope.Daily, quotaError.Scope);
        Assert.Equal(TimeSpan.FromSeconds(17), quotaError.RetryDelay);
    }

    [Fact]
    public void Parse_PerMinuteQuotaFailure_ReturnsPerMinuteScope()
    {
        GeminiQuotaError quotaError = GeminiQuotaError.Parse(GeminiTestFactory.ReadFixture("Gemini/quota-exhausted-per-minute.json"));

        Assert.Equal(AiQuotaScope.PerMinute, quotaError.Scope);
        Assert.Equal(TimeSpan.FromSeconds(42.5), quotaError.RetryDelay);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"error\":\"quota\"}")]
    public void Parse_UnreadableBody_TreatedAsPerMinuteWithoutDelay(string body)
    {
        GeminiQuotaError quotaError = GeminiQuotaError.Parse(body);

        Assert.Equal(AiQuotaScope.PerMinute, quotaError.Scope);
        Assert.Null(quotaError.RetryDelay);
    }

    [Theory]
    [InlineData("17s", 17.0)]
    [InlineData("0.5s", 0.5)]
    public void ParseDuration_ProtobufDuration_ReturnsSeconds(string value, double expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), GeminiQuotaError.ParseDuration(value));
    }

    [Fact]
    public void NextQuotaReset_IsNextMidnightPacific()
    {
        DateTimeOffset reset = GeminiModelAvailability.NextQuotaReset(_pacificAfternoon);

        Assert.Equal(new DateTimeOffset(2026, 11, 24, 0, 0, 0, TimeSpan.FromHours(-8)), reset);
    }

    [Fact]
    public void MarkExhausted_Daily_BlocksUntilMidnightPacific()
    {
        ManualTimeProvider clock = new(_pacificAfternoon);
        GeminiModelAvailability availability = new(clock);

        availability.MarkExhausted("model", new GeminiQuotaError(AiQuotaScope.Daily, TimeSpan.FromSeconds(17)));
        clock.Advance(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(59));
        bool availableBeforeMidnight = availability.IsAvailable("model");
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.False(availableBeforeMidnight);
        Assert.True(availability.IsAvailable("model"));
    }

    [Fact]
    public void MarkExhausted_PerMinute_BlocksForRetryDelay()
    {
        ManualTimeProvider clock = new(_pacificAfternoon);
        GeminiModelAvailability availability = new(clock);

        availability.MarkExhausted("model", new GeminiQuotaError(AiQuotaScope.PerMinute, TimeSpan.FromSeconds(30)));
        bool availableImmediately = availability.IsAvailable("model");
        clock.Advance(TimeSpan.FromSeconds(30));

        Assert.False(availableImmediately);
        Assert.True(availability.IsAvailable("model"));
    }

    [Fact]
    public void CreateExhaustedException_ReportsEarliestRecovery()
    {
        ManualTimeProvider clock = new(_pacificAfternoon);
        GeminiModelAvailability availability = new(clock);
        availability.MarkExhausted("first", new GeminiQuotaError(AiQuotaScope.Daily, RetryDelay: null));
        availability.MarkExhausted("second", new GeminiQuotaError(AiQuotaScope.Daily, RetryDelay: null));

        GeminiQuotaExhaustedException exception = availability.CreateExhaustedException(["first", "second"]);

        Assert.Equal(AiQuotaScope.Daily, exception.Scope);
        Assert.Equal(TimeSpan.FromHours(9), exception.RetryAfter);
    }
}
