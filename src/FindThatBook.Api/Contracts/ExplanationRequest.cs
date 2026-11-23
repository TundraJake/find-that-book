using System.ComponentModel.DataAnnotations;
using FindThatBook.Application;
using FindThatBook.Domain;

namespace FindThatBook.Api.Contracts;

/// <summary>Body of <c>POST /api/diagnostics/explain</c>: a query plus hand-supplied candidates to explain.</summary>
public sealed record ExplanationRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(BookSearchRequest.MaximumQueryLength, MinimumLength = 1)]
    public required string Query { get; init; }

    [Required]
    [Length(1, BookSearchRules.MaximumCandidates)]
    public required IReadOnlyList<CandidateInput> Candidates { get; init; }
}

/// <summary>One candidate as the caller would get it from Open Library.</summary>
public sealed record CandidateInput
{
    private const string OpenLibraryBaseUrl = "https://openlibrary.org";

    [Required(AllowEmptyStrings = false)]
    public required string WorkKey { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Title { get; init; }

    public IReadOnlyList<string> PrimaryAuthors { get; init; } = [];

    public IReadOnlyList<string> Contributors { get; init; } = [];

    public int? FirstPublishYear { get; init; }

    public MatchTier MatchTier { get; init; } = MatchTier.Fallback;

    public BookCandidate ToDomain() => new(
        Title,
        PrimaryAuthors,
        Contributors,
        FirstPublishYear,
        WorkKey,
        new Uri($"{OpenLibraryBaseUrl}{WorkKey}"),
        CoverUrl: null,
        MatchTier,
        Explanation: string.Empty);
}
