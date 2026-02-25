namespace TheRandomizer.Assignment.Parser;

internal class Token(TokenTypes tokenType, String text, Int32 position, Object? value = null)
{
    public TokenTypes TokenType { get; set; } = tokenType;
    public String Text { get; set; } = text;
    public Int32 Position {  get; set; } = position;
    public Object? Value { get; set; } = value;
}