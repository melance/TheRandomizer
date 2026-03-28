using TheRandomizer.Application.Interfaces;
using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Application.Services;

public class GeneratorRunner : IGeneratorRunner
{
    public RunGeneratorResult Run(BaseGenerator generator, Dictionary<String, Object?>? parameters)
    {
        try
        {
            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                {
                    generator.Parameters[parameter.Key]?.Value = parameter.Value;
                }
            }
            var result = generator.Generate();
            return new()
            {
                Success = true,
                Content = new() { Text = result.Text, Format = generator.OutputFormat }
            };

        }
        catch (Exception ex)
        {
            return new()
            {
                Success = false,
                Diagnostics =
                [
                    new(Enumerators.Severity.Error, ex.Message)
                ]
            };
        }
    }
}

