using System.Text.Json;
using System.Text.Json.Serialization;
using FindThatBook.Application;
using FindThatBook.Application.Abstractions;
using FindThatBook.Domain;
using FindThatBook.Infrastructure.Gemini.Prompts;
using FindThatBook.Infrastructure.Gemini.Quota;
using FindThatBook.Infrastructure.Gemini.Safety;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindThatBook.Infrastructure.Gemini.Explanation;

/// <summary>
/// Writes "why it matched" explanations for all candidates in one Gemini call.
/// Validation: every candidate must get exactly one non-empty explanation; unknown ids are rejected.
/// </summary>
internal sealed partial class GeminiExplanationWriter(GeminiClient geminiClient, IOptions<GeminiOptions> options, ILogger<GeminiExplanationWriter> logger)
    : IExplanationWriter
{
    private const int MaximumExplanationLength = 400;
    private const string Ellipsis = "…";
    private const string NotConfiguredReason = "Gemini API key is not configured.";

    private static readonly JsonSerializerOptions _strictSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly GeminiClient _geminiClient = geminiClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly ILogger<GeminiExplanationWriter> _logger = logger;

    public async Task<IReadOnlyDictionary<string, CandidateAssessment>> ExplainAsync(
        string query,
        QueryHypothesis hypothesis,
        IReadOnlyList<BookCandidate> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return new Dictionary<string, CandidateAssessment>();
        }

        if (!_options.HasApiKey)
        {
            throw new ExplanationFailedException(NotConfiguredReason);
        }

        try
        {
            string prompt = CandidateExplanationPrompt.Build(ExplanationInput.Create(query, hypothesis, candidates));
            string json = await _geminiClient.GenerateJsonAsync(prompt, ExplanationSchema.Create(), cancellationToken);
            Dictionary<string, CandidateAssessment> assessments = ParseAndValidate(json, candidates.Select(candidate => candidate.WorkKey).ToList());
            EnsureSafe(assessments, candidates);
            if (assessments.Values.Any(assessment => assessment.AiRank is null))
            {
                LogRanksUnusable(_logger, CandidateExplanationPrompt.Version);
            }

            return assessments;
        }
        catch (GeminiQuotaExhaustedException exception)
        {
            throw exception.ToApplicationException();
        }
        catch (GeminiBusyException exception)
        {
            throw exception.ToApplicationException();
        }
        catch (GeminiContentBlockedException exception)
        {
            LogContentBlocked(_logger, exception.Reason);
            throw exception.ToApplicationException();
        }
        catch (Exception exception) when (GeminiFailures.IsGeminiFailure(exception, cancellationToken))
        {
            LogExplanationFailed(_logger, exception, CandidateExplanationPrompt.Version);
            throw new ExplanationFailedException(GeminiFailures.DescribeReason(exception), exception);
        }
    }

    /// <summary>Explanations are strict (every candidate exactly once); ranks are kept only if they are exactly 1..N, otherwise all are dropped.</summary>
    internal static Dictionary<string, CandidateAssessment> ParseAndValidate(string json, IReadOnlyList<string> expectedIds)
    {
        ExplanationResult result = JsonSerializer.Deserialize<ExplanationResult>(json, _strictSerializerOptions)
            ?? throw new GeminiException("JSON was null");

        HashSet<string> expected = new(expectedIds, StringComparer.Ordinal);
        Dictionary<string, string> explanations = new(StringComparer.Ordinal);
        Dictionary<string, int?> ranks = new(StringComparer.Ordinal);
        foreach (CandidateExplanation item in result.Explanations)
        {
            if (!expected.Contains(item.Id))
            {
                throw new GeminiException($"explanation for unknown candidate '{item.Id}'");
            }

            string text = item.Explanation.Trim();
            if (text.Length == 0)
            {
                throw new GeminiException($"empty explanation for '{item.Id}'");
            }

            if (!explanations.TryAdd(item.Id, Shorten(text)))
            {
                throw new GeminiException($"duplicate explanation for '{item.Id}'");
            }

            ranks[item.Id] = item.Rank;
        }

        string? missing = expectedIds.FirstOrDefault(id => !explanations.ContainsKey(id));
        if (missing is not null)
        {
            throw new GeminiException($"missing explanation for '{missing}'");
        }

        bool ranksUsable = IsPermutationOfOneToCount(ranks.Values, expectedIds.Count);
        return explanations.ToDictionary(
            entry => entry.Key,
            entry => new CandidateAssessment(entry.Value, ranksUsable ? ranks[entry.Key] : null),
            StringComparer.Ordinal);
    }

    internal static bool IsPermutationOfOneToCount(IEnumerable<int?> ranks, int count)
    {
        List<int?> values = ranks.ToList();
        return values.Count == count && values.All(rank => rank is >= 1 && rank <= count) && values.Distinct().Count() == count;
    }

    /// <summary>Runs <see cref="ExplanationContentGuard"/> on every explanation; one unsafe explanation rejects the search.</summary>
    internal static void EnsureSafe(IReadOnlyDictionary<string, CandidateAssessment> assessments, IReadOnlyList<BookCandidate> candidates)
    {
        foreach (BookCandidate candidate in candidates)
        {
            if (ExplanationContentGuard.FindProblem(assessments[candidate.WorkKey].Explanation, candidate) is { } problem)
            {
                throw new GeminiContentBlockedException($"explanation for '{candidate.WorkKey}' {problem}");
            }
        }
    }

    private static string Shorten(string text) =>
        text.Length <= MaximumExplanationLength ? text : string.Concat(text.AsSpan(0, MaximumExplanationLength - Ellipsis.Length), Ellipsis);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini explanation failed (prompt {PromptVersion}).")]
    private static partial void LogExplanationFailed(ILogger logger, Exception exception, string promptVersion);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini content blocked: {Reason}")]
    private static partial void LogContentBlocked(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini ranks were not 1..N exactly once (prompt {PromptVersion}); AI ranking omitted for this search.")]
    private static partial void LogRanksUnusable(ILogger logger, string promptVersion);
}
