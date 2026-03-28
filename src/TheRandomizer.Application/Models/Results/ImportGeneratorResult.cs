namespace TheRandomizer.Application.Models.Results;

public class ImportGeneratorResult : BaseGeneratorResult
{
    public String? ImportedPath { get; init; }
    public String? StoredFilename { get; init; }
}

