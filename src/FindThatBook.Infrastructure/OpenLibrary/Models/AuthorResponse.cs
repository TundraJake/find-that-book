namespace FindThatBook.Infrastructure.OpenLibrary.Models;

/// <summary>Response of <c>GET /authors/{id}.json</c> (only the fields we read).</summary>
internal sealed record AuthorResponse(string Key, string? Name);
