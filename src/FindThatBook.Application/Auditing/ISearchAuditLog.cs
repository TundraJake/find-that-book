namespace FindThatBook.Application.Auditing;

/// <summary>Records searches for later review. Must not block or fail the request it describes.</summary>
public interface ISearchAuditLog
{
    void Record(SearchAuditEntry entry);
}
