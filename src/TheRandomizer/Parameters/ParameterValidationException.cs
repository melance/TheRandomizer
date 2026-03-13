namespace TheRandomizer.Parameters;

public class ParameterValidationException(List<String> messages) : Exception($"The follow validation errors occured:\n{String.Join("\n\t", messages)}")
{
}

