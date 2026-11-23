using System.Text.Json;
using System.Text.Json.Serialization;

namespace FindThatBook.Infrastructure.OpenLibrary.Models;

/// <summary>
/// Open Library writes references both as objects (<c>{"key": "/type/author_role"}</c>) and as bare strings
/// (<c>"/type/author_role"</c>) depending on the record's age. Accepts either.
/// </summary>
internal sealed class KeyReferenceJsonConverter : JsonConverter<KeyReference>
{
    private const string KeyProperty = "key";

    public override KeyReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return new KeyReference(reader.GetString() ?? string.Empty);
            case JsonTokenType.StartObject:
                using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.TryGetProperty(KeyProperty, out JsonElement key) && key.ValueKind == JsonValueKind.String
                        ? new KeyReference(key.GetString()!)
                        : null;
                }

            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, KeyReference value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(KeyProperty, value.Key);
        writer.WriteEndObject();
    }
}
