using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FindThatBook.Application;

namespace FindThatBook.Infrastructure.Gemini.Quota;

/// <summary>What a Gemini HTTP 429 (RESOURCE_EXHAUSTED) body says about the quota that ran out.</summary>
internal sealed partial record GeminiQuotaError(AiQuotaScope Scope, TimeSpan? RetryDelay)
{
    private const string DailyQuotaMarker = "PerDay";
    private const string QuotaFailureType = "QuotaFailure";
    private const string RetryInfoType = "RetryInfo";

    /// <summary>
    /// Reads <c>error.details[]</c>: a <c>google.rpc.QuotaFailure</c> whose <c>quotaId</c> contains "PerDay" means the daily quota;
    /// <c>google.rpc.RetryInfo.retryDelay</c> (e.g. "17s") is how long Google asks us to wait. Unreadable bodies count as per-minute.
    /// </summary>
    public static GeminiQuotaError Parse(string responseBody)
    {
        JsonArray? details;
        try
        {
            details = (JsonNode.Parse(responseBody) as JsonObject)?["error"] is JsonObject error ? error["details"] as JsonArray : null;
        }
        catch (JsonException)
        {
            details = null;
        }

        bool isDaily = false;
        TimeSpan? retryDelay = null;
        foreach (JsonNode? detail in details ?? [])
        {
            if (detail is not JsonObject)
            {
                continue;
            }

            string type = ReadString(detail["@type"]) ?? string.Empty;
            if (type.EndsWith(QuotaFailureType, StringComparison.Ordinal))
            {
                isDaily |= (detail["violations"] as JsonArray ?? [])
                    .Any(violation => violation is JsonObject && ReadString(violation["quotaId"])?.Contains(DailyQuotaMarker, StringComparison.Ordinal) == true);
            }
            else if (type.EndsWith(RetryInfoType, StringComparison.Ordinal))
            {
                retryDelay = ParseDuration(ReadString(detail["retryDelay"]));
            }
        }

        return new GeminiQuotaError(isDaily ? AiQuotaScope.Daily : AiQuotaScope.PerMinute, retryDelay);
    }

    /// <summary>Protobuf Duration JSON form: decimal seconds with an "s" suffix.</summary>
    internal static TimeSpan? ParseDuration(string? value)
    {
        Match match = value is null ? Match.Empty : DurationPattern().Match(value);
        return match.Success && double.TryParse(match.Groups["seconds"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }

    private static string? ReadString(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    [GeneratedRegex(@"^(?<seconds>\d+(\.\d+)?)s$")]
    private static partial Regex DurationPattern();
}
