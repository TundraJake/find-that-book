using System.Globalization;
using FindThatBook.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace FindThatBook.Api.ErrorHandling;

/// <summary>
/// Turns any <see cref="UpstreamServiceFailedException"/> (Gemini or Open Library) into ProblemDetails the client can display:
/// 429 with <c>quota_scope</c> and <c>retry_after_seconds</c> when every AI model is over quota, 503 with <c>retry_after_seconds</c> when the
/// model stays overloaded, 503 with <c>source: "open_library"</c> when Open Library times out or fails (usually temporary),
/// 422 when the search is rejected as unsafe, otherwise 502.
/// </summary>
public sealed class UpstreamServiceFailedExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const string QuotaScopeExtension = "quota_scope";
    private const string RetryAfterSecondsExtension = "retry_after_seconds";
    private const string DailyQuotaScope = "daily";
    private const string PerMinuteQuotaScope = "per_minute";
    /// <summary>Names the failing upstream service so the client can offer the right retry.</summary>
    private const string SourceExtension = "source";
    private const string OpenLibrarySource = "open_library";

    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not UpstreamServiceFailedException upstreamFailure)
        {
            return false;
        }

        int statusCode = upstreamFailure switch
        {
            AiQuotaExhaustedException => StatusCodes.Status429TooManyRequests,
            AiServiceBusyException => StatusCodes.Status503ServiceUnavailable,
            SearchRejectedException => StatusCodes.Status422UnprocessableEntity,
            BookCatalogUnavailableException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status502BadGateway,
        };
        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = upstreamFailure.Title,
            Detail = upstreamFailure.Reason,
        };

        if (upstreamFailure is AiQuotaExhaustedException quotaFailure)
        {
            int retryAfterSeconds = (int)Math.Ceiling(quotaFailure.RetryAfter.TotalSeconds);
            problemDetails.Extensions[QuotaScopeExtension] = quotaFailure.Scope == AiQuotaScope.Daily ? DailyQuotaScope : PerMinuteQuotaScope;
            problemDetails.Extensions[RetryAfterSecondsExtension] = retryAfterSeconds;
            httpContext.Response.Headers[HeaderNames.RetryAfter] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        if (upstreamFailure is BookCatalogUnavailableException)
        {
            problemDetails.Extensions[SourceExtension] = OpenLibrarySource;
        }

        if (upstreamFailure is AiServiceBusyException busyFailure)
        {
            int retryAfterSeconds = (int)Math.Ceiling(busyFailure.RetryAfter.TotalSeconds);
            problemDetails.Extensions[RetryAfterSecondsExtension] = retryAfterSeconds;
            httpContext.Response.Headers[HeaderNames.RetryAfter] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = statusCode;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }
}
