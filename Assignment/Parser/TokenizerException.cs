namespace TheRandomizer.Assignment.Parser;

public class TokenizerException(String message, String expression, Int32 position) : Exception($"{message}\n\tin {expression} at {position}")
{
    public Int32 Position { get; } = position;
}

