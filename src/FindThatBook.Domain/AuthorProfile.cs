namespace FindThatBook.Domain;

/// <summary>An author and their catalog page, e.g. J.R.R. Tolkien → https://openlibrary.org/authors/OL26320A.</summary>
public sealed record AuthorProfile(string Name, Uri OpenLibraryUrl);
