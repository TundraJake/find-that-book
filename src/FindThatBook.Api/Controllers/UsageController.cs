using FindThatBook.Api.Contracts;
using FindThatBook.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FindThatBook.Api.Controllers;

[ApiController]
[Route("api/usage")]
[Produces("application/json")]
public sealed class UsageController(IAiUsageReporter usageReporter) : ControllerBase
{
    private readonly IAiUsageReporter _usageReporter = usageReporter;

    /// <summary>Gemini requests made by this API in the last 60 seconds, against the configured per-minute limit.</summary>
    [HttpGet]
    [ProducesResponseType<AiUsageResponse>(StatusCodes.Status200OK)]
    public ActionResult<AiUsageResponse> Get() => Ok(AiUsageResponse.FromSnapshot(_usageReporter.GetUsage()));
}
