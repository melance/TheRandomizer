namespace TheRandomizer.Application.Models.Results;

public class RunGeneratorResult : BaseGeneratorResult
{
    public GeneratorResult Content { get; set; } = new();
}

