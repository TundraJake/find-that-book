namespace FindThatBook.Application.Abstractions;

/// <summary>Reports whether external integrations have the configuration they need.</summary>
public interface IIntegrationStatus
{
    bool IsQueryExtractionConfigured { get; }
}
