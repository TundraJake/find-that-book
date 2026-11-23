using FindThatBook.Api.Contracts;
using FindThatBook.Application.Abstractions;
using FindThatBook.Application.Services;
using FindThatBook.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FindThatBook.Api.Controllers;

/// <summary>Development-only endpoints for exercising pipeline stages in isolation.</summary>
[ApiController]
[Route("api/diagnostics")]
[Produces("application/json")]
public sealed class DiagnosticsController(IQueryExtractor queryExtractor, CandidateExplainer candidateExplainer, IWebHostEnvironment environment)
    : ControllerBase
{
    private readonly IQueryExtractor _queryExtractor = queryExtractor;
    private readonly CandidateExplainer _candidateExplainer = candidateExplainer;
    private readonly IWebHostEnvironment _environment = environment;

    /// <summary>Runs Gemini query extraction without searching for books.</summary>
    [HttpPost("extract")]
    [ProducesResponseType<QueryExtractionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<QueryExtractionResponse>> Extract(BookSearchRequest request, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        string query = request.Query.Trim();
        QueryHypothesis hypothesis = await _queryExtractor.ExtractAsync(query, cancellationToken);
        return Ok(QueryExtractionResponse.FromDomain(query, hypothesis));
    }

    /// <summary>Runs Gemini extraction on the query, then Gemini explanations for the supplied candidates.</summary>
    [HttpPost("explain")]
    [ProducesResponseType<ExplanationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ExplanationResponse>> Explain(ExplanationRequest request, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        string query = request.Query.Trim();
        QueryHypothesis hypothesis = await _queryExtractor.ExtractAsync(query, cancellationToken);
        List<BookCandidate> candidates = request.Candidates.Select(candidate => candidate.ToDomain()).ToList();
        IReadOnlyList<BookCandidate> explained = await _candidateExplainer.ExplainAsync(query, hypothesis, candidates, cancellationToken);

        return Ok(new ExplanationResponse(
            query,
            QueryHypothesisResponse.FromDomain(hypothesis),
            explained.Select(BookCandidateResponse.FromDomain).ToList()));
    }
}
