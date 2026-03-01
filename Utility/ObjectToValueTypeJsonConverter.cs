using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheRandomizer.Utility;

internal class ObjectToValueTypeJsonConverter : JsonConverter<Object>
{
    public override Object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out Int64 l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out DateTime dt) => dt,
            JsonTokenType.String => reader.GetString(),
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };
    }

    public override void Write(Utf8JsonWriter writer, Object value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}

