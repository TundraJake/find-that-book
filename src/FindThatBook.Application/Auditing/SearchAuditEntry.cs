namespace FindThatBook.Application.Auditing;

/// <summary>One line of the search audit log: who searched, from where, for what, and how it ended.</summary>
public sealed record SearchAuditEntry(
    DateTimeOffset TimestampUtc,
    string RequestId,
    string ClientIpAddress,
    ClientLocation Location,
    string UserAgent,
    string Query,
    SearchOutcome Outcome,
    int ResultCount,
    string? TopResultTitle,
    string? TopResultWorkKey,
    string? FailureReason,
    long DurationMilliseconds);
