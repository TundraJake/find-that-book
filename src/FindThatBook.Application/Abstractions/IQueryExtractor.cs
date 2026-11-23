using FindThatBook.Domain;

namespace FindThatBook.Application.Abstractions;

/// <summary>Turns a messy free-text query into structured search fields.</summary>
public interface IQueryExtractor
{
    /// <exception cref="QueryExtractionFailedException">The extractor could not produce a usable result.</exception>
    Task<QueryHypothesis> ExtractAsync(string query, CancellationToken cancellationToken);
}
