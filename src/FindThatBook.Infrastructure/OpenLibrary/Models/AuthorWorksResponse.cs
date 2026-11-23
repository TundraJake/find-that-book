namespace FindThatBook.Infrastructure.OpenLibrary.Models;

/// <summary>
/// Response of <c>GET /authors/{id}/works.json</c>: works whose canonical <c>authors</c> include this author.
/// Ordered newest record first (not by popularity), so it is used to confirm authorship, not to pick "top works".
/// </summary>
internal sealed record AuthorWorksResponse(int Size, IReadOnlyList<AuthorWorkEntry>? Entries);

internal sealed record AuthorWorkEntry(string Key, string? Title);
