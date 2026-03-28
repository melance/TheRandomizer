namespace TheRandomizer.Application.Models;

public class GeneratorFileItem
{
    public GeneratorFileItem(String filePath)
    {
        var summary = DefinitionSummary.Deserialize(filePath);
        Name = summary.Name;
        Summary = summary;
        FullPath = filePath;
    }

    public String Name { get; }
    public DefinitionSummary Summary { get; }
    public String FullPath {  get; }
}

