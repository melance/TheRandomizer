using System.Text.Json;
using System.Text.Json.Serialization;
using TheRandomizer.Assignment;

namespace TheRandomizer.Parameters;

public class SelectOptionJsonConverter : JsonConverter<Option>
{
    public override Option? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var content = reader.GetString();

                content ??= String.Empty;

                return new Option(content, null);
            case JsonTokenType.StartObject:
                return SerializeObject(ref reader);
        }

        throw new JsonException($"Unexpected token {reader.TokenType}");
    }

    private static Option SerializeObject(ref Utf8JsonReader reader)
    {
        String? display = null;
        String value = String.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Option(value, display);

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Unexpected token type {reader.TokenType}.");

            var property = reader.GetString();

            reader.Read();

            switch (property)
            {
                case "Value":
                    value = reader.TokenType == JsonTokenType.String ? reader.GetString() ?? String.Empty : String.Empty;
                    break;
                case "Weight":
                    display = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Option value, JsonSerializerOptions options)
    {
        if (String.IsNullOrEmpty(value.Display))
            writer.WriteStringValue(value.Value);
        else
        {
            writer.WriteStartObject();
            writer.WriteString("Value", value.Value);
            writer.WriteString("Display", value.Display);
            writer.WriteEndObject();
        }
    }
}

