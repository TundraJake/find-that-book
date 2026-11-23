using FindThatBook.Application.Abstractions;
using FindThatBook.Application.Auditing;
using FindThatBook.Infrastructure.Auditing;
using FindThatBook.Infrastructure.Gemini;
using FindThatBook.Infrastructure.Gemini.Explanation;
using FindThatBook.Infrastructure.Gemini.Extraction;
using FindThatBook.Infrastructure.Gemini.Quota;
using FindThatBook.Infrastructure.OpenLibrary;
using FindThatBook.Infrastructure.OpenLibrary.Enrichment;
using FindThatBook.Infrastructure.OpenLibrary.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers infrastructure adapters: Gemini (extraction, explanations), Open Library (candidate search), and the search audit log.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<GeminiClient>((serviceProvider, httpClient) =>
        {
            GeminiOptions geminiOptions = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            httpClient.BaseAddress = new Uri(geminiOptions.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(geminiOptions.TimeoutSeconds);
        });
        services.AddTransient<IQueryExtractor, GeminiQueryExtractor>();
        services.AddTransient<IExplanationWriter, GeminiExplanationWriter>();
        services.AddSingleton<IIntegrationStatus, GeminiIntegrationStatus>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<GeminiUsageTracker>();
        services.AddSingleton<GeminiModelAvailability>();
        services.AddSingleton<IAiUsageReporter>(serviceProvider => serviceProvider.GetRequiredService<GeminiUsageTracker>());

        services.AddOptions<SearchAuditOptions>()
            .Bind(configuration.GetSection(SearchAuditOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<JsonLinesSearchAuditLog>();
        services.AddSingleton<ISearchAuditLog>(serviceProvider => serviceProvider.GetRequiredService<JsonLinesSearchAuditLog>());
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<JsonLinesSearchAuditLog>());

        services.AddMemoryCache();
        services.AddOptions<OpenLibraryOptions>()
            .Bind(configuration.GetSection(OpenLibraryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<OpenLibraryClient>((serviceProvider, httpClient) =>
        {
            OpenLibraryOptions openLibraryOptions = serviceProvider.GetRequiredService<IOptions<OpenLibraryOptions>>().Value;
            httpClient.BaseAddress = new Uri(openLibraryOptions.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(openLibraryOptions.TimeoutSeconds);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(openLibraryOptions.UserAgent);
        });
        services.AddTransient<WorkEnricher>();
        services.AddTransient<IBookCatalog, OpenLibraryBookCatalog>();
        return services;
    }
}
