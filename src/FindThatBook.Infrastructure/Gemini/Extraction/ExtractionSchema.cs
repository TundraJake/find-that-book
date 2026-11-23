using System.Text.Json.Nodes;

namespace FindThatBook.Infrastructure.Gemini.Extraction;

/// <summary>JSON Schema sent with the extraction request; Gemini constrains its output to this shape.</summary>
internal static class ExtractionSchema
{
    private const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "title":          { "type": "string",  "description": "Book title, if present." },
            "author":         { "type": "string",  "description": "Author name, if present." },
            "keywords":       { "type": "array", "items": { "type": "string" }, "description": "Other meaningful words." },
            "characterHints": { "type": "array", "items": { "type": "string" }, "description": "Character names mentioned." },
            "yearHint":       { "type": "integer", "description": "Four-digit year, if present." }
          },
          "required": ["keywords", "characterHints"]
        }
        """;

    /// <summary>A fresh copy per call, because a <see cref="JsonNode"/> can only have one parent.</summary>
    public static JsonNode Create() => JsonNode.Parse(SchemaJson)!;
}
