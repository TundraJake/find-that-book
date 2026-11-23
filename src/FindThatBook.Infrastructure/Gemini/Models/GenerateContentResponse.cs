namespace FindThatBook.Infrastructure.Gemini.Models;

/// <summary>Response of <c>generateContent</c> (Gemini wire format). Only the fields we read are modeled.</summary>
internal sealed record GenerateContentResponse(IReadOnlyList<ResponseCandidate>? Candidates, PromptFeedback? PromptFeedback);

internal sealed record ResponseCandidate(ResponseContent? Content, string? FinishReason);

internal sealed record ResponseContent(IReadOnlyList<ContentPart>? Parts);

internal sealed record PromptFeedback(string? BlockReason);
