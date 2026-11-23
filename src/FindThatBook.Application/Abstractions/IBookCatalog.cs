using FindThatBook.Domain;

namespace FindThatBook.Application.Abstractions;

/// <summary>Finds candidate works for an interpreted query.</summary>
public interface IBookCatalog
{
    /// <exception cref="BookCatalogUnavailableException">The catalog could not be reached or returned unusable data.</exception>
    Task<IReadOnlyList<CatalogWork>> FindCandidatesAsync(string query, QueryHypothesis hypothesis, CancellationToken cancellationToken);
}
