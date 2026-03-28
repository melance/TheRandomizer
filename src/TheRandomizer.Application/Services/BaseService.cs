using TheRandomizer.Application.Models.Results;

namespace TheRandomizer.Application.Services;

public abstract class BaseService
{
    protected static List<AppDiagnostic> Error(String message) => [ new(Enumerators.Severity.Error, message)];
    protected static List<AppDiagnostic> Exception(Exception ex) => [new(ex)];
    protected static T ErrorResult<T>(String message) where T : BaseGeneratorResult, new() => new()
    {
        Success = false,
        Diagnostics = Error(message)
    };
    protected static T ExceptionResult<T>(Exception ex) where T : BaseGeneratorResult, new() => new()
    {
        Success = false,
        Diagnostics = Exception(ex)
    };
}

