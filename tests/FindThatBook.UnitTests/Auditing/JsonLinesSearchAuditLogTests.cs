using System.Text.Json;
using FindThatBook.Application.Auditing;
using FindThatBook.Infrastructure.Auditing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FindThatBook.UnitTests.Auditing;

public sealed class JsonLinesSearchAuditLogTests
{
    private static readonly SearchAuditEntry _entry = new(
        new DateTimeOffset(2026, 11, 23, 8, 15, 40, TimeSpan.Zero),
        RequestId: "trace-1",
        ClientIpAddress: "203.0.113.42",
        Location: new ClientLocation("CH", ClientLocation.NotAvailable, "Zürich", ClientLocation.NotAvailable, "cloudflare"),
        UserAgent: "test-agent",
        Query: "tolkien hobbit",
        Outcome: SearchOutcome.QuotaExhausted,
        ResultCount: 0,
        TopResultTitle: null,
        TopResultWorkKey: null,
        FailureReason: "Daily usage limits reached",
        DurationMilliseconds: 29);

    [Fact]
    public void ToLine_WritesOneSnakeCaseJsonObjectWithReadableUnicode()
    {
        string line = JsonLinesSearchAuditLog.ToLine(_entry);

        Assert.False(line.Contains('\n', StringComparison.Ordinal));
        Assert.Contains("\"city\":\"Zürich\"", line, StringComparison.Ordinal);
        using JsonDocument document = JsonDocument.Parse(line);
        Assert.Equal("quota_exhausted", document.RootElement.GetProperty("outcome").GetString());
        Assert.Equal("203.0.113.42", document.RootElement.GetProperty("client_ip_address").GetString());
        Assert.Equal(29, document.RootElement.GetProperty("duration_milliseconds").GetInt64());
        Assert.Equal(ClientLocation.NotAvailable, document.RootElement.GetProperty("location").GetProperty("region").GetString());
    }

    [Fact]
    public async Task Record_AppendsToDailyFileUnderContentRoot()
    {
        string contentRoot = Path.Combine(Path.GetTempPath(), $"audit-test-{Guid.NewGuid():N}");
        JsonLinesSearchAuditLog auditLog = new(
            Options.Create(new SearchAuditOptions { Directory = "logs", FileNamePrefix = "searches", QueueCapacity = 10 }),
            new TestHostEnvironment(contentRoot),
            NullLogger<JsonLinesSearchAuditLog>.Instance);

        await auditLog.StartAsync(CancellationToken.None);
        auditLog.Record(_entry);
        auditLog.Record(_entry with { RequestId = "trace-2" });
        string expectedPath = Path.Combine(contentRoot, "logs", "searches-2026-11-23.jsonl");
        for (int attempt = 0; attempt < 50 && (!File.Exists(expectedPath) || File.ReadAllLines(expectedPath).Length < 2); attempt++)
        {
            await Task.Delay(20);
        }

        await auditLog.StopAsync(CancellationToken.None);

        Assert.Equal(2, File.ReadAllLines(expectedPath).Length);
        Directory.Delete(contentRoot, recursive: true);
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "FindThatBook.UnitTests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
