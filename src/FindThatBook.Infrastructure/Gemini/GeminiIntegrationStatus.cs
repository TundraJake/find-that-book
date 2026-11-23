using FindThatBook.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.Gemini;

internal sealed class GeminiIntegrationStatus(IOptions<GeminiOptions> options) : IIntegrationStatus
{
    private readonly GeminiOptions _options = options.Value;

    public bool IsQueryExtractionConfigured => _options.HasApiKey;
}
