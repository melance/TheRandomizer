using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheRandomizer.Assignment;

internal class LineItemJsonConverter : JsonConverter<LineItem>
{
    public override LineItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var content = reader.GetString();

                content ??= String.Empty;

                return new LineItem()
                {
                    Content = content,
                    Weight = 1
                };
            case JsonTokenType.StartObject:
                return SerializeObject(ref reader);
        }

        throw new JsonException($"Unexpected token {reader.TokenType}");
    }

    private static LineItem SerializeObject(ref Utf8JsonReader reader)
    {
        String? content = null;
        UInt32 weight = 1;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new LineItem { Content = content ?? String.Empty, Weight = weight };

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Unexpected token type {reader.TokenType}.");

            var property = reader.GetString();

            reader.Read();

            switch (property)
            {
                case "Content":
                    content = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    break;
                case "Weight":
                    weight = reader.GetUInt32();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, LineItem value, JsonSerializerOptions options)
    {
        if (value.Weight == 1)
            writer.WriteStringValue(value.Content);
        else
        {
            writer.WriteStartObject();
            writer.WriteString("Content", value.Content);
            writer.WriteNumber("Weight", value.Weight);
            writer.WriteEndObject();
        }
    }
}

