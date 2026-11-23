using System.Text.Json.Nodes;

namespace FindThatBook.Infrastructure.Gemini.Explanation;

/// <summary>JSON Schema sent with the explanation request; Gemini constrains its output to this shape.</summary>
internal static class ExplanationSchema
{
    private const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "explanations": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "id":          { "type": "string", "description": "The candidate id, copied unchanged." },
                  "explanation": { "type": "string", "description": "One or two sentences citing the candidate's fields." },
                  "rank":        { "type": "integer", "description": "1 = most likely the book the reader means; each rank from 1 to the number of candidates used exactly once." }
                },
                "required": ["id", "explanation", "rank"]
              }
            }
          },
          "required": ["explanations"]
        }
        """;

    /// <summary>A fresh copy per call, because a <see cref="JsonNode"/> can only have one parent.</summary>
    public static JsonNode Create() => JsonNode.Parse(SchemaJson)!;
}
