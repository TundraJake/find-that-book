using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FindThatBook.Infrastructure.OpenLibrary.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.OpenLibrary;

/// <summary>
/// The only class that talks to openlibrary.org. Work and author records are cached in memory;
/// merged works (<c>/type/redirect</c>) are followed to their canonical record.
/// </summary>
internal sealed class OpenLibraryClient(HttpClient httpClient, IMemoryCache cache, IOptions<OpenLibraryOptions> options)
{
    /// <summary>Fields requested from <c>/search.json</c>; keeps responses small.</summary>
    internal const string SearchFields = "key,title,subtitle,author_name,author_key,first_publish_year,cover_i,edition_count";

    private const int MaximumRedirects = 3;
    private const string WorkCachePrefix = "openlibrary:work:";
    private const string AuthorCachePrefix = "openlibrary:author:";
    private const string AuthorWorksCachePrefix = "openlibrary:author-works:";
    private const string JsonExtension = ".json";

    internal static readonly JsonSerializerOptions WireSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly IMemoryCache _cache = cache;
    private readonly OpenLibraryOptions _options = options.Value;

    public async Task<IReadOnlyList<SearchDocument>> SearchAsync(IReadOnlyList<KeyValuePair<string, string>> parameters, CancellationToken cancellationToken)
    {
        IEnumerable<KeyValuePair<string, string>> allParameters = parameters.Concat(
        [
            new("fields", SearchFields),
            new("limit", _options.SearchLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        ]);
        string queryString = string.Join('&', allParameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        SearchResponse response = await GetRequiredAsync<SearchResponse>($"search.json?{queryString}", cancellationToken);
        return response.Docs ?? [];
    }

    /// <summary>Returns the canonical work for a key, following redirects; null when the work does not exist.</summary>
    public async Task<WorkResponse?> GetWorkAsync(string workKey, CancellationToken cancellationToken)
    {
        string currentKey = workKey;
        for (int hop = 0; hop <= MaximumRedirects; hop++)
        {
            WorkResponse? work = await GetCachedAsync<WorkResponse>(WorkCachePrefix + currentKey, ToPath(currentKey), cancellationToken);
            if (work is null || !work.IsRedirect)
            {
                return work;
            }

            currentKey = work.Location!;
        }

        throw new OpenLibraryException($"too many redirects starting at {workKey}");
    }

    /// <summary>Returns an author record; null when the author does not exist.</summary>
    public Task<AuthorResponse?> GetAuthorAsync(string authorKey, CancellationToken cancellationToken) =>
        GetCachedAsync<AuthorResponse>(AuthorCachePrefix + authorKey, ToPath(authorKey), cancellationToken);

    /// <summary>
    /// Works listing for an author (<c>/authors/{id}/works.json</c>, first <see cref="OpenLibraryOptions.AuthorWorksLimit"/> entries);
    /// null when the author does not exist.
    /// </summary>
    public Task<AuthorWorksResponse?> GetAuthorWorksAsync(string authorKey, CancellationToken cancellationToken)
    {
        string limit = _options.AuthorWorksLimit.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return GetCachedAsync<AuthorWorksResponse>(AuthorWorksCachePrefix + authorKey, $"{authorKey.TrimStart('/')}/works.json?limit={limit}", cancellationToken);
    }

    /// <summary>"/works/OL27482W" → "works/OL27482W.json" (relative to the base address).</summary>
    private static string ToPath(string key) => $"{key.TrimStart('/')}{JsonExtension}";

    private async Task<T?> GetCachedAsync<T>(string cacheKey, string path, CancellationToken cancellationToken)
        where T : class
    {
        if (_cache.TryGetValue(cacheKey, out T? cached))
        {
            return cached;
        }

        T? value = await GetOptionalAsync<T>(path, cancellationToken);
        if (value is not null)
        {
            _cache.Set(cacheKey, value, TimeSpan.FromMinutes(_options.CacheMinutes));
        }

        return value;
    }

    private async Task<T?> GetOptionalAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadAsync<T>(response, path, cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(path, cancellationToken);
        return await ReadAsync<T>(response, path, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, string path, CancellationToken cancellationToken)
        where T : class
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new OpenLibraryException($"HTTP {(int)response.StatusCode} for {path.Split('?')[0]}");
        }

        return await response.Content.ReadFromJsonAsync<T>(WireSerializerOptions, cancellationToken)
            ?? throw new OpenLibraryException($"empty body for {path.Split('?')[0]}");
    }
}
