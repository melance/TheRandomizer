namespace TheRandomizer.Assignment.Parser;

public class ParseException(String message, String expression, Int32 position) : Exception($"{message}\n\t in {expression} at {position}.")
{
    public Int32 Position { get; } = position;
}

