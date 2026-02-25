using LB.Utility.Extensions;

namespace TheRandomizer.Assignment.Parser;

internal abstract record Node();

internal record NumberNode(Int32 Value) : Node;
internal record StringNode(String Value) : Node;
internal record BooleanNode(Boolean Value) : Node;
internal record VariableNode(String Name) : Node;
internal record IdentifierNode(String Name) : Node;
internal record CallNode(String Name, List<Node> Arguments) : Node;

internal enum BinaryOperators
{
    Union,              // &
    OneOf,              // |
    KeyOf,              // +
    Repeat,             // ^
    Weight,             // *
    EqualTo,            // ==
    NotEqualTo,         // !=
    LessThan,           // <
    LessThanOrEqual,    // <=
    GreaterThan,        // >
    GreaterThanOrEqual, // >=
    And,                // &&
    Or                  // ||
}

internal enum UnaryOperators
{
    Not         // !
}

internal record BinaryNode(BinaryOperators Operator, Node Left, Node Right) : Node;
internal record UnaryNode(UnaryOperators Operator, Node Operand) : Node;
internal record AssignmentNode(String Name, Node Value, Boolean Emit) : Node;

internal class Parser
{
    private List<Token> Tokens { get; }
    private Int32 Position { get; set; }
    public String Expression { get; }

    public Parser(String expression) : this(new Tokenizer(expression)) { }

    public Parser(Tokenizer tokenizer)
    {        
        Tokens = [..tokenizer.Tokenize()];
        Position = 0;
        Expression = tokenizer.Expression;

        if (Tokens.Count == 0 || Tokens[^1].TokenType != TokenTypes.EOF)
            Tokens.Add(new Token(TokenTypes.EOF, String.Empty, Tokens.Count > 0 ? Tokens[^1].Position : 0));
    }

    public Node Parse()
    {
        var node = ParseAssignment();
        Expect(TokenTypes.EOF, "Unexpected tokens after expression.");
        return node;
    }

    private Node ParseAssignment()
    {
        if (Check(TokenTypes.Identifier) && 
            (Peek(1).TokenType == TokenTypes.Assignment || Peek(1).TokenType == TokenTypes.AssignAndEmit))
        {
            var variableName = Next();
            var token = Next(); 

            var value = ParseAssignment();
            var emit = token.TokenType == TokenTypes.AssignAndEmit;
            return new AssignmentNode((String)variableName.Value!, value, emit);
        }

        return ParseOr();
    }

    private Node ParseOr()
    {
        var left = ParseAnd();
        
        while (Match(TokenTypes.Or))
        {
            var right = ParseAnd();
            left = new BinaryNode(BinaryOperators.Or, left, right);
        }

        return left;
    }

    private Node ParseAnd()
    {
        var left = ParseEqual();

        while (Match(TokenTypes.And))
        {
            var right = ParseEqual();
            left = new BinaryNode(BinaryOperators.And, left, right);
        }

        return left;
    }

    private Node ParseEqual()
    {
        var left = ParseComparison();

        while (Match(TokenTypes.EqualTo, TokenTypes.NotEqualTo))
        {
            var opToken = Previous();
            var right = ParseComparison();
            var op = opToken.TokenType switch
            {
                TokenTypes.EqualTo => BinaryOperators.EqualTo,
                TokenTypes.NotEqualTo => BinaryOperators.NotEqualTo,
                _ => throw new InvalidOperationException()
            };

            left = new BinaryNode(op, left, right);
        }

        return left;
    }

    private Node ParseComparison()
    {
        var left = ParseOneOf();

        while (Match(TokenTypes.LessThan, 
                     TokenTypes.LessThanOrEqual,
                     TokenTypes.GreaterThan,
                     TokenTypes.GreaterThanOrEqual))
        {
            var opToken = Previous();
            var right = ParseOneOf();

            var op = opToken.TokenType switch
            {
                TokenTypes.LessThan => BinaryOperators.LessThan,
                TokenTypes.LessThanOrEqual => BinaryOperators.LessThanOrEqual,
                TokenTypes.GreaterThan => BinaryOperators.GreaterThan,
                TokenTypes.GreaterThanOrEqual => BinaryOperators.GreaterThanOrEqual,
                _ => throw new InvalidOperationException()
            };

            left = new BinaryNode(op, left, right);
        }

        return left;
    }

    private Node ParseOneOf()
    {
        var left = ParseUnion();
        while (Match(TokenTypes.OneOf))
        {
            var right = ParseUnion();
            left = new BinaryNode(BinaryOperators.OneOf, left, right);
        }
        return left;
    }

    private Node ParseUnion()
    {
        var left = ParseKeyOf();
        while (Match(TokenTypes.Union))
        {
            var right = ParseKeyOf();
            left = new BinaryNode(BinaryOperators.Union, left, right);
        }
        return left;
    }

    private Node ParseKeyOf()
    {
        var left = ParseWeight();
        while (Match(TokenTypes.KeyOf))
        {
            var right = ParseWeight();
            left = new BinaryNode(BinaryOperators.KeyOf, left, right);
        }
        return left;
    }

    private Node ParseWeight()
    {
        var left = ParseRepeat();
        while (Match(TokenTypes.Weight))
        {
            var right = ParseRepeat();
            left = new BinaryNode(BinaryOperators.Weight, left, right);
        }
        return left;
    }

    private Node ParseRepeat()
    {
        var left = ParseUnary();
        while (Match(TokenTypes.Repeat))
        {
            var right = ParseUnary();
            left = new BinaryNode(BinaryOperators.Repeat, left, right);
        }
        return left;
    }

    private Node ParseUnary()
    {
        if (Match(TokenTypes.Negate))
        {
            var right = ParseUnary();
            return new UnaryNode(UnaryOperators.Not, right);
        }
        return ParsePrimary();
    }

    private Node ParsePrimary()
    {
        if (Match(TokenTypes.Number))
            return new NumberNode((Int32)Previous().Value!);
        if (Match(TokenTypes.String))
            return new StringNode((String)Previous().Value!);
        if (Match(TokenTypes.Boolean))
            return new BooleanNode((Boolean)Previous().Value!);
        if (Match(TokenTypes.Variable))
            return new VariableNode((String)Previous().Value!);

        if (Match(TokenTypes.Identifier))
        {
            var name = (String)Previous().Value!;

            if (Match(TokenTypes.OpenParenthesis))
            {
                var arguments = new List<Node>();

                if (!Check(TokenTypes.CloseParenthesis))
                {
                    do
                    {
                        arguments.Add(ParseAssignment());
                    } while (Match(TokenTypes.Comma));
                }

                Expect(TokenTypes.CloseParenthesis, "Expected ')' after function arguments.");
                return new CallNode(name, arguments);
            }

            return new IdentifierNode(name);
        }

        if (Match(TokenTypes.OpenParenthesis))
        {
            var inner = ParseAssignment();
            Expect(TokenTypes.CloseParenthesis, "Expected ')'.");
            return inner;
        }

        throw ErrorHere("Expected an expression.");
    }

    #region Helper Methods
    private Token Next()
    {
        var token = Tokens[Position];
        if (Position < Tokens.Count) Position++;
        return token;
    }

    private Token Previous() => Tokens[Math.Max(0, Position - 1)];

    private Token Peek(int lookAhead = 0)
    {
        Int32 index = Position + lookAhead;
        if (index < 0) index = 0;
        if (index >= Tokens.Count) return Tokens[^1];
        return Tokens[index];
    }

    private Boolean Check(TokenTypes type) => Peek().TokenType == type;

    private Boolean Match(TokenTypes type)
    {
        if (Check(type))
        {
            Position++;
            return true;
        }
        return false;
    }

    private Boolean Match(params TokenTypes[] types)
    {
        foreach(var type in types)
        {
            if (Check(type))
            {
                Position++;
                return true;
            }
        }
        return false;
    }

    private void Expect(TokenTypes types, string message)
    {
        if (!Match(types))
            throw ErrorHere(message);
    }

    private ParseException ErrorHere(string message)
    {
        var t = Peek();
        var near = t.TokenType == TokenTypes.EOF ? "end of input" : $"'{t.Text}'";
        return new ParseException($"{message} Near {near}.", Expression, t.Position);
    } 
    #endregion
}

