using System.Text.Json;
using FindThatBook.Domain;
using FindThatBook.Infrastructure.OpenLibrary.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.OpenLibrary.Enrichment;

/// <summary>
/// Turns search documents into <see cref="CatalogWork"/>s: canonical work record (redirects followed),
/// primary authors from <c>works.authors</c>, and everyone else in <c>author_name</c> as contributors.
/// </summary>
internal sealed partial class WorkEnricher(OpenLibraryClient client, IOptions<OpenLibraryOptions> options, ILogger<WorkEnricher> logger)
{
    private const string CoverSize = "M";
    private const string NoPlaceholderCover = "?default=false";
    private const string CatalogSiteRoot = "https://openlibrary.org";

    private readonly OpenLibraryClient _client = client;
    private readonly OpenLibraryOptions _options = options.Value;
    private readonly ILogger<WorkEnricher> _logger = logger;

    public async Task<EnrichmentResult> EnrichAsync(IReadOnlyList<SearchDocument> documents, CancellationToken cancellationToken)
    {
        using SemaphoreSlim throttle = new(_options.MaximumConcurrentRequests);
        LookupOutcome[] outcomes = await Task.WhenAll(documents.Select(document => EnrichThrottledAsync(document, throttle, cancellationToken)));
        return new EnrichmentResult(
            outcomes.Select(outcome => outcome.Work).OfType<CatalogWork>().ToList(),
            outcomes.Count(outcome => outcome.Failed));
    }

    private sealed record LookupOutcome(CatalogWork? Work, bool Failed);

    private async Task<LookupOutcome> EnrichThrottledAsync(SearchDocument document, SemaphoreSlim throttle, CancellationToken cancellationToken)
    {
        await throttle.WaitAsync(cancellationToken);
        try
        {
            return new LookupOutcome(await EnrichAsync(document, cancellationToken), Failed: false);
        }
        catch (Exception exception) when (OpenLibraryFailures.IsOpenLibraryFailure(exception, cancellationToken))
        {
            // One malformed, failing, or slow work record must not sink the whole search; the caller fails only if every lookup fails.
            LogWorkSkipped(_logger, exception, document.Key);
            return new LookupOutcome(null, Failed: true);
        }
        finally
        {
            throttle.Release();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipped Open Library work {WorkKey}: unusable record, error, or timeout.")]
    private static partial void LogWorkSkipped(ILogger logger, Exception exception, string workKey);

    /// <summary>Null when the work no longer exists in the catalog.</summary>
    internal async Task<CatalogWork?> EnrichAsync(SearchDocument document, CancellationToken cancellationToken)
    {
        WorkResponse? work = await _client.GetWorkAsync(document.Key, cancellationToken);
        if (work is null)
        {
            return null;
        }

        Dictionary<string, string> namesFromSearch = PairAuthorKeysWithNames(document);
        List<string> primaryKeys = (work.Authors ?? [])
            .Select(entry => entry.Author?.Key)
            .OfType<string>()
            .Select(ToBareAuthorKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        List<string> primaryAuthors;
        List<string> contributors;
        List<AuthorProfile> primaryAuthorProfiles = [];
        if (primaryKeys.Count == 0)
        {
            // No canonical author data: fall back to the search document's authors, all treated as primary.
            primaryAuthors = [.. document.AuthorName ?? []];
            contributors = [];
        }
        else
        {
            primaryAuthors = [];
            foreach (string authorKey in primaryKeys)
            {
                string? name = namesFromSearch.GetValueOrDefault(authorKey) ?? (await _client.GetAuthorAsync($"/authors/{authorKey}", cancellationToken))?.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    primaryAuthors.Add(name);
                    primaryAuthorProfiles.Add(new AuthorProfile(name, new Uri($"{CatalogSiteRoot}/authors/{authorKey}")));
                }
            }

            HashSet<string> primaryKeySet = new(primaryKeys, StringComparer.Ordinal);
            contributors = namesFromSearch
                .Where(pair => !primaryKeySet.Contains(pair.Key))
                .Select(pair => pair.Value)
                .Where(name => !primaryAuthors.Contains(name, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        int? coverId = document.CoverId is > 0 ? document.CoverId : work.Covers?.FirstOrDefault(cover => cover > 0) is int workCover and > 0 ? workCover : null;

        return new CatalogWork(
            work.Key,
            work.Title ?? document.Title ?? work.Key,
            work.Subtitle ?? document.Subtitle,
            primaryAuthors,
            contributors,
            document.FirstPublishYear,
            document.EditionCount ?? 0,
            new Uri($"{CatalogSiteRoot}{work.Key}"),
            coverId is null ? null : new Uri($"{_options.CoversBaseUrl.TrimEnd('/')}/b/id/{coverId}-{CoverSize}.jpg{NoPlaceholderCover}"),
            primaryAuthorProfiles);
    }

    /// <summary>Search results list <c>author_key</c> and <c>author_name</c> in the same order.</summary>
    private static Dictionary<string, string> PairAuthorKeysWithNames(SearchDocument document)
    {
        IReadOnlyList<string> keys = document.AuthorKey ?? [];
        IReadOnlyList<string> names = document.AuthorName ?? [];
        Dictionary<string, string> pairs = new(StringComparer.Ordinal);
        for (int index = 0; index < Math.Min(keys.Count, names.Count); index++)
        {
            pairs.TryAdd(ToBareAuthorKey(keys[index]), names[index]);
        }

        return pairs;
    }

    /// <summary>"/authors/OL26320A" or "OL26320A" → "OL26320A".</summary>
    private static string ToBareAuthorKey(string key) => key[(key.LastIndexOf('/') + 1)..];
}
