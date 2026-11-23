using System.ComponentModel.DataAnnotations;

namespace FindThatBook.Infrastructure.OpenLibrary;

/// <summary>Open Library settings, bound from the <c>OpenLibrary</c> configuration section. No API key is needed.</summary>
public sealed class OpenLibraryOptions
{
    public const string SectionName = "OpenLibrary";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, Url]
    public string CoversBaseUrl { get; set; } = string.Empty;

    /// <summary>Open Library asks clients to identify themselves with an app name and contact.</summary>
    [Required]
    public string UserAgent { get; set; } = string.Empty;

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; }

    /// <summary>Results requested per search strategy.</summary>
    [Range(1, 100)]
    public int SearchLimit { get; set; }

    /// <summary>Maximum distinct works enriched with a <c>/works/{id}.json</c> lookup per query.</summary>
    [Range(1, 50)]
    public int EnrichLimit { get; set; }

    /// <summary>Entries requested from <c>/authors/{id}/works.json</c> (one page) to confirm which results the queried author wrote.</summary>
    [Range(1, 1000)]
    public int AuthorWorksLimit { get; set; }

    /// <summary>Concurrent requests to Open Library per query (be polite).</summary>
    [Range(1, 10)]
    public int MaximumConcurrentRequests { get; set; }

    /// <summary>How long work and author records stay in the in-memory cache.</summary>
    [Range(1, 1440)]
    public int CacheMinutes { get; set; }
}
