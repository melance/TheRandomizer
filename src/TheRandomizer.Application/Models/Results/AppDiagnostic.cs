using TheRandomizer.Application.Enumerators;

namespace TheRandomizer.Application.Models.Results;

public class AppDiagnostic(Severity severity, String message)
{
    public AppDiagnostic(Exception ex) : this(Severity.Error, ex.Message) { }

    public Severity Severity { get; set; } = severity;
    public String Message { get; set; } = message;
}

