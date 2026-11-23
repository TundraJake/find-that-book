using FindThatBook.Application.Abstractions;

namespace FindThatBook.Api.Contracts;

/// <summary>Response of <c>GET /api/usage</c>.</summary>
public sealed record AiUsageResponse(int RequestsInWindow, int RequestsPerMinuteLimit, int WindowSeconds)
{
    public static AiUsageResponse FromSnapshot(AiUsageSnapshot snapshot) =>
        new(snapshot.RequestsInWindow, snapshot.RequestsPerMinuteLimit, snapshot.WindowSeconds);
}
