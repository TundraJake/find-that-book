using System.Text.Json.Nodes;

namespace FindThatBook.Infrastructure.Gemini.Models;

/// <summary>Body of <c>POST models/{model}:generateContent</c> (Gemini wire format).</summary>
/// <summary><see cref="SafetySettings"/> is omitted from the JSON when null.</summary>
internal sealed record GenerateContentRequest(
    IReadOnlyList<RequestContent> Contents,
    GenerationConfig GenerationConfig,
    IReadOnlyList<SafetySetting>? SafetySettings);

/// <summary>e.g. <c>{ "category": "HARM_CATEGORY_HARASSMENT", "threshold": "BLOCK_MEDIUM_AND_ABOVE" }</c>.</summary>
internal sealed record SafetySetting(string Category, string Threshold);

internal sealed record RequestContent(IReadOnlyList<ContentPart> Parts);

internal sealed record ContentPart(string Text);

/// <summary><see cref="ThinkingConfig"/> is omitted from the JSON when null, so the model default applies.</summary>
internal sealed record GenerationConfig(ResponseFormat ResponseFormat, ThinkingConfig? ThinkingConfig);

internal sealed record ThinkingConfig(string ThinkingLevel);

internal sealed record ResponseFormat(TextResponseFormat Text);

internal sealed record TextResponseFormat(string MimeType, JsonNode Schema);
