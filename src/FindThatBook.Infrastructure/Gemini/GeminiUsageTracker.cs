using FindThatBook.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.Gemini;

/// <summary>
/// Counts Gemini requests over a rolling 60-second window (Gemini's "requests per minute" quota window).
/// Every attempt counts, including ones that fail or are cancelled, because Google counts them too.
/// </summary>
internal sealed class GeminiUsageTracker(TimeProvider timeProvider, IOptions<GeminiOptions> options) : IAiUsageReporter
{
    private const int WindowSeconds = 60;
    private static readonly TimeSpan _window = TimeSpan.FromSeconds(WindowSeconds);

    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly GeminiOptions _options = options.Value;
    private readonly Queue<DateTimeOffset> _requestTimes = new();
    private readonly Lock _gate = new();

    public void RecordRequest()
    {
        lock (_gate)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            DropExpired(now);
            _requestTimes.Enqueue(now);
        }
    }

    public AiUsageSnapshot GetUsage()
    {
        lock (_gate)
        {
            DropExpired(_timeProvider.GetUtcNow());
            return new AiUsageSnapshot(_requestTimes.Count, _options.RequestsPerMinuteLimit, WindowSeconds);
        }
    }

    private void DropExpired(DateTimeOffset now)
    {
        while (_requestTimes.Count > 0 && now - _requestTimes.Peek() >= _window)
        {
            _requestTimes.Dequeue();
        }
    }
}
