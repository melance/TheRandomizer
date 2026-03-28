namespace TheRandomizer.Application.Models.Results;

public sealed class LoadGeneratorResult() : BaseGeneratorResult()
{
    public LoadGeneratorResult(BaseGenerator? generator) : this()
    {
        Success = true;
        Generator = generator;
    }

    public BaseGenerator? Generator { get; set; }
}

