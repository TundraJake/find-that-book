using FindThatBook.Application;
using FindThatBook.Application.Abstractions;
using FindThatBook.Domain;
using FindThatBook.Infrastructure.OpenLibrary.Enrichment;
using FindThatBook.Infrastructure.OpenLibrary.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.OpenLibrary.Search;

/// <summary>
/// Finds candidate works on Open Library: runs the search strategies in parallel, merges, puts works the queried author wrote first
/// (confirmed with <c>/authors/{id}/works.json</c>), then enriches the top ones.
/// </summary>
internal sealed partial class OpenLibraryBookCatalog(
    OpenLibraryClient client,
    WorkEnricher enricher,
    IOptions<OpenLibraryOptions> options,
    ILogger<OpenLibraryBookCatalog> logger) : IBookCatalog
{
    private readonly OpenLibraryClient _client = client;
    private readonly WorkEnricher _enricher = enricher;
    private readonly OpenLibraryOptions _options = options.Value;
    private readonly ILogger<OpenLibraryBookCatalog> _logger = logger;

    public async Task<IReadOnlyList<CatalogWork>> FindCandidatesAsync(string query, QueryHypothesis hypothesis, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<IReadOnlyList<KeyValuePair<string, string>>> strategies = SearchStrategy.Build(query, hypothesis);
            IReadOnlyList<SearchDocument> merged = await RunStrategiesAsync(strategies, cancellationToken);

            List<SearchDocument> prioritized = merged.Count > _options.EnrichLimit
                ? await PrioritizeQueriedAuthorsWorksAsync(hypothesis, merged, cancellationToken)
                : [.. merged];
            List<SearchDocument> toEnrich = prioritized.Take(_options.EnrichLimit).ToList();

            EnrichmentResult enrichment = await _enricher.EnrichAsync(toEnrich, cancellationToken);
            if (enrichment.Works.Count == 0 && enrichment.FailedLookups > 0)
            {
                throw new OpenLibraryException("every work lookup failed or timed out");
            }

            return enrichment.Works;
        }
        catch (Exception exception) when (OpenLibraryFailures.IsOpenLibraryFailure(exception, cancellationToken))
        {
            LogSearchFailed(_logger, exception);
            throw new BookCatalogUnavailableException(OpenLibraryFailures.DescribeReason(exception), exception);
        }
    }

    /// <summary>
    /// Runs the search strategies in parallel and merges what comes back. A strategy that fails or times out is logged and skipped;
    /// the search fails only when every strategy fails (Open Library is slow at times, and one slow query should not sink the rest).
    /// </summary>
    private async Task<IReadOnlyList<SearchDocument>> RunStrategiesAsync(
        IReadOnlyList<IReadOnlyList<KeyValuePair<string, string>>> strategies,
        CancellationToken cancellationToken)
    {
        StrategyOutcome[] outcomes = await Task.WhenAll(strategies.Select(strategy => RunStrategyAsync(strategy, cancellationToken)));

        List<Exception> failures = outcomes.Select(outcome => outcome.Failure).OfType<Exception>().ToList();
        if (failures.Count == outcomes.Length)
        {
            throw failures[0];
        }

        foreach (Exception failure in failures)
        {
            LogStrategyFailed(_logger, failure, failures.Count, outcomes.Length);
        }

        return outcomes
            .SelectMany(outcome => outcome.Documents)
            .DistinctBy(document => document.Key, StringComparer.Ordinal)
            .ToList();
    }

    private sealed record StrategyOutcome(IReadOnlyList<SearchDocument> Documents, Exception? Failure);

    /// <summary>One strategy; an Open Library failure or timeout becomes a failed outcome instead of an exception. Caller cancellation still throws.</summary>
    private async Task<StrategyOutcome> RunStrategyAsync(IReadOnlyList<KeyValuePair<string, string>> strategy, CancellationToken cancellationToken)
    {
        try
        {
            return new StrategyOutcome(await _client.SearchAsync(strategy, cancellationToken), Failure: null);
        }
        catch (Exception exception) when (OpenLibraryFailures.IsOpenLibraryFailure(exception, cancellationToken))
        {
            return new StrategyOutcome([], exception);
        }
    }

    /// <summary>
    /// Optional step, run only when there are more results than <see cref="OpenLibraryOptions.EnrichLimit"/> (otherwise the order cannot
    /// change which works are enriched). A failure here is logged and the merged order is kept, never failing the search.
    /// </summary>
    private async Task<List<SearchDocument>> PrioritizeQueriedAuthorsWorksAsync(
        QueryHypothesis hypothesis,
        IReadOnlyList<SearchDocument> merged,
        CancellationToken cancellationToken)
    {
        string? authorKey = hypothesis.Author is { } author ? AuthorWorksPrioritizer.ResolveAuthorKey(author, merged) : null;
        if (authorKey is null)
        {
            return [.. merged];
        }

        try
        {
            AuthorWorksResponse? authorWorks = await _client.GetAuthorWorksAsync($"/authors/{authorKey}", cancellationToken);
            HashSet<string> confirmed = (authorWorks?.Entries ?? []).Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
            List<SearchDocument> prioritized = AuthorWorksPrioritizer.PrioritizeConfirmed(merged, confirmed);
            int confirmedResults = merged.Count(document => confirmed.Contains(document.Key));
            LogAuthorWorksChecked(_logger, authorKey, confirmed.Count, authorWorks?.Size ?? 0, confirmedResults);
            return prioritized;
        }
        catch (Exception exception) when (OpenLibraryFailures.IsOpenLibraryFailure(exception, cancellationToken))
        {
            LogAuthorWorksFailed(_logger, exception, authorKey);
            return [.. merged];
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Author {AuthorKey}: {Fetched} of {Total} works fetched; {Confirmed} search results confirmed")]
    private static partial void LogAuthorWorksChecked(ILogger logger, string authorKey, int fetched, int total, int confirmed);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read works for author {AuthorKey}; keeping search order.")]
    private static partial void LogAuthorWorksFailed(ILogger logger, Exception exception, string authorKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "An Open Library search strategy failed ({Failed} of {Total}); continuing with the others.")]
    private static partial void LogStrategyFailed(ILogger logger, Exception exception, int failed, int total);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Open Library search failed.")]
    private static partial void LogSearchFailed(ILogger logger, Exception exception);
}
