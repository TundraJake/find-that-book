namespace FindThatBook.Application.Auditing;

/// <summary>How a search request ended, as recorded in the search audit log.</summary>
public enum SearchOutcome
{
    Succeeded,
    NoResults,
    QuotaExhausted,
    Busy,
    Rejected,
    UpstreamFailed,
    Cancelled,
    Failed,
}
