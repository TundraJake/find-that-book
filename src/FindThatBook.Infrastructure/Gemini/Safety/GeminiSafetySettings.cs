using FindThatBook.Infrastructure.Gemini.Models;

namespace FindThatBook.Infrastructure.Gemini.Safety;

/// <summary>Builds the generateContent <c>safetySettings</c> list and recognizes safety finish reasons.</summary>
internal static class GeminiSafetySettings
{
    /// <summary>Candidate finish reasons that mean the response was withheld for safety or policy, not truncated or malformed.</summary>
    private static readonly HashSet<string> _blockedFinishReasons = new(StringComparer.Ordinal)
    {
        "SAFETY",
        "PROHIBITED_CONTENT",
        "BLOCKLIST",
        "SPII",
        "IMAGE_SAFETY",
    };

    /// <summary>One setting per configured category, all at the configured threshold; null (field omitted) when none are configured.</summary>
    public static IReadOnlyList<SafetySetting>? Create(GeminiOptions options) =>
        options.SafetyCategories.Count == 0
            ? null
            : options.SafetyCategories.Select(category => new SafetySetting(category, options.SafetyThreshold)).ToList();

    public static bool IsBlockedFinishReason(string? finishReason) => finishReason is not null && _blockedFinishReasons.Contains(finishReason);
}
