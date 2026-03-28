using System.Text.Json.Serialization;
using TheRandomizer.Application.Enumerators;

namespace TheRandomizer.Application.Models;

public class ThemeDefinition
{
    public String Id { get; set; } = String.Empty;
    public String Name { get; set; } = String.Empty;
    public String Author { get; set; } = String.Empty;
    public String Path { get; set; } = String.Empty; 
    public Dictionary<String, String> Colors { get; set; } = [];
    [JsonIgnore]
    public Boolean IsBuiltIn { get; set; }

    public ThemeDefinition Copy()
    {
        var theme = new ThemeDefinition()
        {
            Id = Id,
            Name = Name,
            Author = Author,
            Path = Path,
            IsBuiltIn = IsBuiltIn
        };

        foreach(var color in Colors)
        {
            theme.Colors.Add(color.Key, color.Value);
        }
        return theme;
    }
}

