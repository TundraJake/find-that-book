using System.Text.Json.Serialization;

namespace FindThatBook.Infrastructure.OpenLibrary.Models;

/// <summary>Response of <c>GET /search.json</c> (only requested fields are modeled).</summary>
internal sealed record SearchResponse(int NumFound, IReadOnlyList<SearchDocument>? Docs);

/// <summary>One work in search results. <c>author_name</c> can include contributors (illustrators, adaptors, editors).</summary>
internal sealed record SearchDocument(
    string Key,
    string? Title,
    string? Subtitle,
    IReadOnlyList<string>? AuthorName,
    IReadOnlyList<string>? AuthorKey,
    int? FirstPublishYear,
    [property: JsonPropertyName("cover_i")] int? CoverId,
    int? EditionCount);
