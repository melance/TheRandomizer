using TheRandomizer.Application.Interfaces;
using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Application.Services;

public class GeneratorLoader : BaseService, IGeneratorLoader
{
    public LoadGeneratorResult Load(String definition, String? seed = null)
    {
        try
        {
            if (String.IsNullOrWhiteSpace(definition))
            {
                return new()
                {
                    Success = false,
                    Diagnostics = Error("Definition is empty.")
                };
            }
            var generator = BaseGenerator.Deserialize(definition, TheRandomizer.Enumerators.FileFormatTypes.Jsonh, seed);
            if (generator == null)
                return new()
                {
                    Success = false,
                    Diagnostics = Error("Failed to deserialize generator.")
                };
            return new()
            {
                Success = true,
                Generator = generator
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                Success = false,
                Diagnostics = Exception(ex)
            };
        }
    }

    public LoadGeneratorResult LoadFromFile(String filePath, String? seed = null)
    {
        try
        {
            if (String.IsNullOrEmpty(filePath))
            {
                return new()
                {
                    Success = false,
                    Diagnostics = Error("File path was not provided.")
                };
            }
            var generator = BaseGenerator.Deserialize(filePath, seed);
            if (generator == null)
            {
                return new()
                {
                    Success = false,
                    Diagnostics = Error($"Unabled to deserialize {filePath}.")
                };
            }
            return new(generator);
        }
        catch (Exception ex)
        {
            return new()
            {
                Success = false,
                Diagnostics = Exception(ex)
            };
        }
    }
}

