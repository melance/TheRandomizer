using JsonhCs;
using System.Text.Json;
using System.Text.Json.Serialization;
using TheRandomizer.Assignment;
using TheRandomizer.Enumerators;
using TheRandomizer.Parameters;

namespace TheRandomizer.Helpers;

internal static class Serialization
{
    private static JsonSerializerOptions? _jsonOptions;
    private static JsonSerializerOptions JsonOptions
    {

        get
        {
            _jsonOptions ??= new()
            {
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                Converters =
                    {
                        new JsonStringEnumConverter(),
                        new LineItemJsonConverter(),
                        new SelectOptionJsonConverter(),
                        new ObjectToValueTypeJsonConverter()
                    }
            };
            return _jsonOptions;
        }
    }

    public static String Serialize<T>(T obj, FileFormatTypes type)
    {
        return type switch
        {
            FileFormatTypes.Json => JsonSerializer.Serialize(obj, JsonOptions),
            _ => throw new Exception($"Unrecognized file format {type}."),
        };
    }

    public static T? Deserialize<T>(String text, FileFormatTypes type)
    {
        return type switch
        {
            FileFormatTypes.Json => JsonSerializer.Deserialize<T>(text, JsonOptions),
            _ => throw new Exception($"Unrecognized file format {type}."),
        };
    }

    public static Object? Deserialize(String text, Type returnType, FileFormatTypes type)
    {
        return type switch {
            FileFormatTypes.Json => DeserializeJson(text, returnType),
            _ => throw new Exception($"Unrecognized file format {type}."),
        };
    }

    private static Object? DeserializeJson(String text, Type returnType)
    {
        var json = JsonhReader.ParseNode(text).Value!.ToString();
        return JsonSerializer.Deserialize(json, returnType, JsonOptions);
    }
}

