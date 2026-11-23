using System.Text.Json;
using System.Text.Json.Serialization;

namespace FindThatBook.Api;

/// <summary>
/// The API's JSON convention: snake_case for property names, dictionary keys, and enum values (e.g. <c>first_publish_year</c>,
/// <c>exact_title_primary_author</c>), matching the requested result format. Applied to controllers, minimal
/// endpoints, ProblemDetails, and validation error keys so every response follows it.
/// </summary>
internal static class ApiJsonConventions
{
    /// <summary>ASP.NET Core adds the request trace id under this camelCase key by default.</summary>
    private const string DefaultTraceIdExtension = "traceId";
    private const string TraceIdExtension = "trace_id";

    public static JsonNamingPolicy NamingPolicy => JsonNamingPolicy.SnakeCaseLower;

    public static void Apply(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = NamingPolicy;
        options.DictionaryKeyPolicy = NamingPolicy;
        options.Converters.Add(new JsonStringEnumConverter(NamingPolicy));
    }

    /// <summary>ProblemDetails extensions are written verbatim, so the built-in <c>traceId</c> is renamed here.</summary>
    public static void RenameTraceId(ProblemDetailsContext context)
    {
        IDictionary<string, object?> extensions = context.ProblemDetails.Extensions;
        if (extensions.Remove(DefaultTraceIdExtension, out object? traceId))
        {
            extensions[TraceIdExtension] = traceId;
        }
    }
}
