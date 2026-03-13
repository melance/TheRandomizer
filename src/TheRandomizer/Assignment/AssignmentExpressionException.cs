namespace TheRandomizer.Assignment;

public class AssignmentExpressionException(String message) : Exception(message)
{
    public AssignmentExpressionException(String functionName, String message) : this($"{message} (function '{functionName}')")
    {        
        Data.Add("Function", functionName);
    }
}

