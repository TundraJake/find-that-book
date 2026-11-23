using System.ComponentModel.DataAnnotations;

namespace FindThatBook.Infrastructure.Gemini;

/// <summary>Gemini settings, bound from the <c>Gemini</c> configuration section.</summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Root of the Gemini REST API, e.g. <c>https://generativelanguage.googleapis.com/</c>.</summary>
    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API version path segment, e.g. <c>v1beta</c>.</summary>
    [Required]
    public string ApiVersion { get; set; } = string.Empty;

    /// <summary>
    /// Models in preference order, e.g. <c>gemini-3.5-flash-lite</c> then <c>gemini-3.1-flash-lite</c>.
    /// When a model hits its quota (HTTP 429) the same request is retried on the next one; the search fails only when all are exhausted.
    /// </summary>
    [Required, MinLength(1)]
    public List<string> Models { get; set; } = [];

    /// <summary>Gemini 3 <c>thinkingLevel</c> sent with every call: minimal, low, medium or high. Empty uses the model default.</summary>
    [AllowedValues("", "minimal", "low", "medium", "high")]
    public string ThinkingLevel { get; set; } = string.Empty;

    /// <summary>API key: user-secrets locally, the Gemini__ApiKey environment variable when deployed.
    /// The app starts without it, but reports itself as not configured and rejects searches.</summary>
    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; }

    /// <summary>Your model's requests-per-minute quota, copied from AI Studio; Google does not report it, so it is display-only.</summary>
    [Range(1, 100000)]
    public int RequestsPerMinuteLimit { get; set; }

    /// <summary>Harm categories filtered on every call, e.g. HARM_CATEGORY_HARASSMENT, HARM_CATEGORY_JAILBREAK. Empty sends no safety settings.</summary>
    public List<string> SafetyCategories { get; set; } = [];

    /// <summary>Block threshold applied to every category in <see cref="SafetyCategories"/>.</summary>
    [AllowedValues("BLOCK_LOW_AND_ABOVE", "BLOCK_MEDIUM_AND_ABOVE", "BLOCK_ONLY_HIGH", "BLOCK_NONE", "OFF")]
    public string SafetyThreshold { get; set; } = "BLOCK_MEDIUM_AND_ABOVE";

    /// <summary>Extra attempts on the same model when it answers "high demand" (HTTP 503). The model is never switched for this, so results stay consistent.</summary>
    [Range(0, 10)]
    public int BusyRetryAttempts { get; set; }

    /// <summary>Wait between those attempts. Each attempt has its own <see cref="TimeoutSeconds"/>.</summary>
    [Range(0, 60)]
    public int BusyRetryDelaySeconds { get; set; }

    /// <summary>Seconds the client is told to wait once the retries are used up and the model is still overloaded.</summary>
    [Range(1, 300)]
    public int BusyRetrySeconds { get; set; }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public bool HasThinkingLevel => !string.IsNullOrEmpty(ThinkingLevel);
}
