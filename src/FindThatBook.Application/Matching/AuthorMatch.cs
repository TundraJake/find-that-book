namespace FindThatBook.Application.Matching;

/// <summary>How the queried author relates to a work.</summary>
/// <param name="Kind">Primary, contributor-only, or absent.</param>
/// <param name="PrimaryPosition">Zero-based position among primary authors when <paramref name="Kind"/> is Primary.</param>
public sealed record AuthorMatch(AuthorMatchKind Kind, int PrimaryPosition)
{
    public static AuthorMatch None { get; } = new(AuthorMatchKind.None, int.MaxValue);
}

public enum AuthorMatchKind
{
    None = 0,
    Contributor = 1,
    Primary = 2,
}
