using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Application.Interfaces;

public interface IGeneratorLoader
{
    LoadGeneratorResult Load(String definition, String? seed = null);
    LoadGeneratorResult LoadFromFile(String filePath, String? seed = null);
}
