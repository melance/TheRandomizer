using JsonhCs;
using LB.Utility.Extensions;
using System.Text.Json;

namespace TheRandomizer;

public class DefinitionSummary
{
    public static DefinitionSummary Deserialize(String filePath)
    {
        var jsonh = File.ReadAllText(filePath);
        var json = JsonhReader.ParseNode(jsonh).Value
                    ?? throw new Exception($"Unable to parse json in {filePath}");
        var definition = JsonSerializer.Deserialize<DefinitionSummary>(json)
                    ?? throw new Exception($"Unable to deserialize {filePath}");
        return definition;
    }

    public String Name { get; set; } = String.Empty;
    public Version Version { get; set; } = new Version(1,0);
    public String? Author { get; set; }
    public String? Description { get; set; }
    public String Type { get; set; } = String.Empty;
    public List<String>? Tags { get; set; } = [];
    public String? TagList => Tags == null ? String.Empty : String.Join(", ", Tags);
    public Boolean Show { get; set; } = true;
}

