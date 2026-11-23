using FindThatBook.Api.Auditing;
using FindThatBook.Api.Contracts;
using FindThatBook.Application.Abstractions;
using FindThatBook.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FindThatBook.Api.Controllers;

[ApiController]
[Route("api/books")]
[Produces("application/json")]
public sealed class BookSearchController(IBookSearchService bookSearchService, SearchAuditRecorder auditRecorder, TimeProvider timeProvider)
    : ControllerBase
{
    /// <summary>Finds the books most likely meant by a messy free-text query. Every search is written to the search audit log.</summary>
    [HttpPost("search")]
    [ProducesResponseType<BookSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BookSearchResponse>> Search(BookSearchRequest request, CancellationToken cancellationToken)
    {
        string query = request.Query.Trim();
        long startTimestamp = timeProvider.GetTimestamp();
        BookSearchResult result;
        try
        {
            result = await bookSearchService.SearchAsync(query, cancellationToken);
        }
        catch (Exception exception)
        {
            auditRecorder.RecordFailure(HttpContext, query, exception, startTimestamp);
            throw;
        }

        auditRecorder.RecordSuccess(HttpContext, query, result, startTimestamp);
        return Ok(BookSearchResponse.FromDomain(result));
    }
}
