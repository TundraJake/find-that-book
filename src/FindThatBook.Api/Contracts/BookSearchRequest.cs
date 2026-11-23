using System.ComponentModel.DataAnnotations;

namespace FindThatBook.Api.Contracts;

/// <summary>Body of <c>POST /api/books/search</c>.</summary>
public sealed record BookSearchRequest
{
    public const int MaximumQueryLength = 300;

    /// <summary>Free-text query: any mix of title, author, characters, or keywords.</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(MaximumQueryLength, MinimumLength = 1)]
    public required string Query { get; init; }
}
