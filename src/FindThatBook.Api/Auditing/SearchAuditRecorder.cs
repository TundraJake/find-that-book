using System.Diagnostics;
using FindThatBook.Application;
using FindThatBook.Application.Auditing;
using FindThatBook.Domain;

namespace FindThatBook.Api.Auditing;

/// <summary>Builds a <see cref="SearchAuditEntry"/> from the HTTP request and the search's result or failure, and hands it to the audit log.</summary>
public sealed class SearchAuditRecorder(ISearchAuditLog auditLog, ClientContextResolver clientContextResolver, TimeProvider timeProvider)
{
    private readonly ISearchAuditLog _auditLog = auditLog;
    private readonly ClientContextResolver _clientContextResolver = clientContextResolver;
    private readonly TimeProvider _timeProvider = timeProvider;

    public void RecordSuccess(HttpContext httpContext, string query, BookSearchResult result, long startTimestamp)
    {
        BookCandidate? topResult = result.Candidates.Count > 0 ? result.Candidates[0] : null;
        Record(
            httpContext,
            query,
            topResult is null ? SearchOutcome.NoResults : SearchOutcome.Succeeded,
            result.Candidates.Count,
            topResult,
            failureReason: null,
            startTimestamp);
    }

    public void RecordFailure(HttpContext httpContext, string query, Exception exception, long startTimestamp)
    {
        (SearchOutcome outcome, string reason) = exception switch
        {
            SearchRejectedException rejection => (SearchOutcome.Rejected, $"{rejection.Title}: {rejection.Reason}"),
            AiServiceBusyException busyFailure => (SearchOutcome.Busy, $"{busyFailure.Title}: {busyFailure.Reason}"),
            AiQuotaExhaustedException quotaFailure => (SearchOutcome.QuotaExhausted, $"{quotaFailure.Title}: {quotaFailure.Reason}"),
            UpstreamServiceFailedException upstreamFailure => (SearchOutcome.UpstreamFailed, $"{upstreamFailure.Title}: {upstreamFailure.Reason}"),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (SearchOutcome.Cancelled, "Client cancelled the request"),
            _ => (SearchOutcome.Failed, exception.GetType().Name),
        };
        Record(httpContext, query, outcome, resultCount: 0, topResult: null, reason, startTimestamp);
    }

    private void Record(
        HttpContext httpContext,
        string query,
        SearchOutcome outcome,
        int resultCount,
        BookCandidate? topResult,
        string? failureReason,
        long startTimestamp) =>
        _auditLog.Record(new SearchAuditEntry(
            TimestampUtc: _timeProvider.GetUtcNow(),
            RequestId: Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
            ClientIpAddress: _clientContextResolver.ResolveIpAddress(httpContext),
            Location: _clientContextResolver.ResolveLocation(httpContext),
            UserAgent: ClientContextResolver.ResolveUserAgent(httpContext),
            Query: query,
            Outcome: outcome,
            ResultCount: resultCount,
            TopResultTitle: topResult?.Title,
            TopResultWorkKey: topResult?.WorkKey,
            FailureReason: failureReason,
            DurationMilliseconds: (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds));
}
