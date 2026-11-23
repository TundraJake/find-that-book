using FindThatBook.Domain;

namespace FindThatBook.Infrastructure.OpenLibrary.Enrichment;

/// <summary>Enriched works, plus how many lookups failed or timed out (missing works that no longer exist do not count).</summary>
internal sealed record EnrichmentResult(IReadOnlyList<CatalogWork> Works, int FailedLookups);
