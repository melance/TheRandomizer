using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Application.Interfaces;

public interface IGeneratorRunner
{
    RunGeneratorResult Run(BaseGenerator generator, Dictionary<String, Object?>? parameters);
}
