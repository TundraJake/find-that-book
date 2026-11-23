using FindThatBook.Api.Contracts;
using FindThatBook.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FindThatBook.Api.Controllers;

[ApiController]
[Route("api/status")]
[Produces("application/json")]
public sealed class SystemStatusController(IIntegrationStatus integrationStatus) : ControllerBase
{
    private const string MissingGeminiKeyProblem = "Gemini API key is not configured.";

    private readonly IIntegrationStatus _integrationStatus = integrationStatus;

    /// <summary>Reports whether the app is configured well enough to run searches.</summary>
    [HttpGet]
    [ProducesResponseType<SystemStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemStatusResponse> Get()
    {
        bool geminiConfigured = _integrationStatus.IsQueryExtractionConfigured;
        List<string> problems = geminiConfigured ? [] : [MissingGeminiKeyProblem];
        return Ok(new SystemStatusResponse(Ready: problems.Count == 0, geminiConfigured, problems));
    }
}
