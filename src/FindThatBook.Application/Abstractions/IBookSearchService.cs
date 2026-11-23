using FindThatBook.Domain;

namespace FindThatBook.Application.Abstractions;

/// <summary>Turns a messy reader query into an ordered list of likely books.</summary>
public interface IBookSearchService
{
    Task<BookSearchResult> SearchAsync(string query, CancellationToken cancellationToken);
}
