using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using FindThatBook.Application.Auditing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.Auditing;

/// <summary>
/// Appends each search as one JSON object per line to <c>{Directory}/{FileNamePrefix}-{yyyy-MM-dd}.jsonl</c>, and mirrors it to the
/// application log (so hosted runtime logs carry it too). Writes happen on a background queue: requests never wait on disk,
/// and a write failure is logged, never thrown.
/// </summary>
internal sealed partial class JsonLinesSearchAuditLog : BackgroundService, ISearchAuditLog
{
    private const string FileExtension = ".jsonl";
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>snake_case, same convention as the API responses.</summary>
    private static readonly JsonSerializerOptions _lineSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly SearchAuditOptions _options;
    private readonly string _directory;
    private readonly ILogger<JsonLinesSearchAuditLog> _logger;
    private readonly Channel<SearchAuditEntry> _queue;

    public JsonLinesSearchAuditLog(IOptions<SearchAuditOptions> options, IHostEnvironment environment, ILogger<JsonLinesSearchAuditLog> logger)
    {
        _options = options.Value;
        _directory = Path.GetFullPath(_options.Directory, environment.ContentRootPath);
        _logger = logger;
        _queue = Channel.CreateBounded<SearchAuditEntry>(new BoundedChannelOptions(_options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });
    }

    public void Record(SearchAuditEntry entry)
    {
        if (!_options.Enabled)
        {
            return;
        }

        LogSearch(_logger, entry.Outcome, entry.ClientIpAddress, entry.Location.City, entry.Location.Region, entry.Location.Country, entry.Query);
        if (!_queue.Writer.TryWrite(entry))
        {
            LogEntryDropped(_logger, entry.RequestId);
        }
    }

    internal string FilePathFor(DateTimeOffset timestampUtc) =>
        Path.Combine(_directory, $"{_options.FileNamePrefix}-{timestampUtc.UtcDateTime.ToString(DateFormat, System.Globalization.CultureInfo.InvariantCulture)}{FileExtension}");

    internal static string ToLine(SearchAuditEntry entry) => JsonSerializer.Serialize(entry, _lineSerializerOptions);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (SearchAuditEntry entry in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await WriteAsync(entry);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down: flush whatever is already queued.
            while (_queue.Reader.TryRead(out SearchAuditEntry? entry))
            {
                await WriteAsync(entry);
            }
        }
    }

    private async Task WriteAsync(SearchAuditEntry entry)
    {
        try
        {
            System.IO.Directory.CreateDirectory(_directory);
            await File.AppendAllTextAsync(FilePathFor(entry.TimestampUtc), ToLine(entry) + Environment.NewLine, CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogWriteFailed(_logger, exception, _directory);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Search {Outcome} from {ClientIpAddress} ({City}, {Region}, {Country}): {Query}")]
    private static partial void LogSearch(ILogger logger, SearchOutcome outcome, string clientIpAddress, string city, string region, string country, string query);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Search audit queue is full; dropped entry for request {RequestId}")]
    private static partial void LogEntryDropped(ILogger logger, string requestId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not write the search audit log under {Directory}")]
    private static partial void LogWriteFailed(ILogger logger, Exception exception, string directory);
}
