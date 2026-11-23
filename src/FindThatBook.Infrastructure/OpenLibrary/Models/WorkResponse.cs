using System.Text.Json.Serialization;

namespace FindThatBook.Infrastructure.OpenLibrary.Models;

/// <summary>Response of <c>GET /works/{id}.json</c>. A merged work comes back as <c>/type/redirect</c> with a <c>location</c>.</summary>
internal sealed record WorkResponse(
    string Key,
    string? Title,
    string? Subtitle,
    KeyReference? Type,
    string? Location,
    IReadOnlyList<WorkAuthor>? Authors,
    IReadOnlyList<int>? Covers)
{
    public const string RedirectType = "/type/redirect";

    public bool IsRedirect => string.Equals(Type?.Key, RedirectType, StringComparison.Ordinal) && !string.IsNullOrEmpty(Location);
}

/// <summary>Canonical author entry on a work. <c>type</c> and <c>role</c> are optional and inconsistently shaped, so only <c>author</c> is read.</summary>
internal sealed record WorkAuthor(KeyReference? Author);

/// <summary>A reference such as <c>{"key": "/authors/OL26320A"}</c>; may also arrive as a bare string.</summary>
[JsonConverter(typeof(KeyReferenceJsonConverter))]
internal sealed record KeyReference(string Key);
