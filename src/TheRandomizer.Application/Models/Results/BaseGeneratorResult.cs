namespace TheRandomizer.Application.Models.Results;

public class BaseGeneratorResult()
{
    public static T Exception<T>(Exception ex) where T : BaseGeneratorResult, new()
    {
        return new()
        {
            Success = false,
            Diagnostics =
            [
                new (ex)
            ]
        };
    }

    public static T Error<T>(String message) where T : BaseGeneratorResult, new()
    {
        return new()
        {
            Success = false,
            Diagnostics = 
            [
                new (Enumerators.Severity.Error, message)
            ]
        };
    }

    public static T Warning<T>(String message) where T : BaseGeneratorResult, new()
    {
        return new()
        {
            Success = false,
            Diagnostics =
            [
                new (Enumerators.Severity.Warning, message)
            ]
        };
    }

    public virtual Boolean Success { get; set; }
    public virtual List<AppDiagnostic> Diagnostics { get; set; } = [];
}

