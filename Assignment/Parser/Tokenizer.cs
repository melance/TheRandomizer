using LB.Utility.Extensions;
using System.Text;

namespace TheRandomizer.Assignment.Parser;

internal sealed class Tokenizer(String expression)
{
    private static Boolean IsIndentifierStart(Char c) => Char.IsLetter(c) || c == '_';
    private static Boolean IsIndentiferPart(Char c) => Char.IsLetterOrDigit(c) || c == '_' || c == '.';

    public String Expression { get; } = expression;
    private Int32 Index { get; set; }
    
    public IEnumerable<Token> Tokenize()
    {
        Token t;
        do
        {
            t = NextToken();
            yield return t;
        } while (t.TokenType != TokenTypes.EOF);
    }

    public Token NextToken()
    {
        SkipWhitespace();

        Int32 position = Index;
        if (Eof())
            return new Token(TokenTypes.EOF, String.Empty, position);

        var c = Peek();

        switch (c)
        {
            case '(': // Open Parenthesis
                Index++;
                return new Token(TokenTypes.OpenParenthesis, "(", position);
            case ')': // Close Parenthesis
                Index++;
                return new Token(TokenTypes.CloseParenthesis, ")", position);
            case ',': // Comma
                Index++;
                return new Token(TokenTypes.Comma, ",", position);
            case '&': // Union or And
                return ReadUnionOrAnd();
            case '|': // OneOf or Or
                return ReadOneOfOrOr();
            case '+': // KeyOf
                Index++;
                return new Token(TokenTypes.KeyOf, "+", position);
            case '*': // Weight
                Index++;
                return new Token(TokenTypes.Weight, "*", position);
            case '^': // Repeat
                Index++;
                return new Token(TokenTypes.Repeat, "^", position);
            case '!': // Negate
                return ReadNegateOrNotEqual();
            case '=': // Equal To
                return ReadEqualTo();
            case '>': // Greater Than
                return ReadGreaterThan();
            case '<': // Less Than
                return ReadLessThan();
            case '@': // Variable
                return ReadVariable();
            case '"': // String
            case '\'': // String Single Quote
                return ReadString();
            case ':': // Assignment if followed by =
                return ReadAssignment();
            case '-':
                return ReadNumber();
        }

        if (char.IsDigit(c))
            return ReadNumber();

        if (IsIndentifierStart(c))
            return ReadIdentifierOrKeyword();

        throw Error($"Unexpected character '{c}'.");
    }

    
    private Token ReadAssignment()
    {
        var position = Index;
        if (Peek(1) == '=')
        {
            Index += 2;
            return new Token(TokenTypes.Assignment, ":=", position);
        }
        if (Peek(1) == '>')
        {
            Index += 2;
            return new Token(TokenTypes.AssignAndEmit, ":>", position);
        }

        throw Error("Unexpected ':'; did you mean ':='?");
    }

    private Token ReadEqualTo()
    {
        var position = Index;
        if (Peek(1) == '=')
        {
            Index += 2;
            return new Token(TokenTypes.EqualTo, "==", position);
        }
        
        throw Error("Unexpected '='; did you mean '=='?");
    }

    private Token ReadGreaterThan()
    {
        var position = Index;
        if (Peek(1) == '=')
        {
            Index += 2;
            return new Token(TokenTypes.GreaterThanOrEqual, ">=", position);
        }

        Index++;
        return new Token(TokenTypes.GreaterThan, ">", position);
    }

    private Token ReadIdentifierOrKeyword()
    {
        var position = Index;
        var name = new StringBuilder();

        while (!Eof() && IsIndentiferPart(Peek()))
            name.Append(Next());

        if (name.ToString() == "true")
            return new Token(TokenTypes.Boolean, name.ToString(), position, true);

        if (name.ToString() == "false")
            return new Token(TokenTypes.Boolean, name.ToString(), position, false);

        return new Token(TokenTypes.Identifier, name.ToString(), position, name.ToString());
    }

    private Token ReadLessThan()
    {
        var position = Index;
        if (Peek(1) == '=')
        {
            Index += 2;
            return new Token(TokenTypes.LessThanOrEqual, "<=", position);
        }

        Index++;
        return new Token(TokenTypes.LessThan, "<", position);
    }

    private Token ReadNegateOrNotEqual()
    {
        var position = Index;
        if (Peek(1) == '=')
        {
            Index += 2;
            return new Token(TokenTypes.NotEqualTo, "!=", position);
        }
        else
        {
            Index++;
            return new Token(TokenTypes.Negate, "!", position);
        }
            
    }

    private Token ReadNumber()
    {
        var position = Index;
        var value = new StringBuilder();
        var multiplier = 1;
        Char? coin = null;

        if (Peek() == '-')
            value.Append(Next());

        while (!Eof() && (Char.IsDigit(Peek()) || Peek() == '_'))
        {
            if (Peek() != '_')
                value.Append(Next());
        }

        if (Char.ToLowerInvariant(Peek()).In('c','s','e','g','p') 
            && Char.ToLowerInvariant(Peek(1)) == 'p')
        {
            coin = Next();
            Next();
            multiplier = Char.ToLowerInvariant(coin.Value) switch
            {
                's' => 10,
                'e' => 50,
                'g' => 100,
                'p' => 1000,
                _ => 1
            };
        }

        if (!Int32.TryParse(value.ToString(), out Int32 n))
            throw Error($"Invalid number '{value}'.", position);
        if (coin != null)
            value.Append($"{coin}p");
        return new Token(TokenTypes.Number, value.ToString(), position, n * multiplier);
    }

    private Token ReadOneOfOrOr()
    {
        var position = Index;

        if (Peek(1) == '|')
        {
            Index += 2;
            return new Token(TokenTypes.Or, "||", position);
        }

        Index++;
        return new Token(TokenTypes.OneOf, "|", position);
    }

    private Token ReadString()
    {
        var position = Index;
        var value = new StringBuilder();
        var endQuote = Peek(); 
        Index++; // Consume quote

        while (!Eof())
        {
            char c = Next();
            if (c == endQuote)
            {
                var raw = Expression[position..Index];
                return new Token(TokenTypes.String, raw, position, value.ToString());
            }

            if (c == '\\')
            {
                if (Eof())
                    throw Error("Unterminated escape sequence in string literal.");

                char esc = Next();
                value.Append(esc switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => throw Error($"Unknown escape sequence '\\{esc}'.")
                });
                continue;
            }

            value.Append(c);
        }
        throw Error("Unterminated string literal.");
    }

    private Token ReadVariable()
    {
        var position = Index;
        Index++; // Consume @
        var name = new StringBuilder();

        if (Eof() || !IsIndentifierStart(Peek()))
            throw Error("Invalid variable name character after '@'.");
        
        while (!Eof() && IsIndentiferPart(Peek()))
            name.Append(Next());

        if (name.ToString().Contains("..")) throw Error("Variable segment cannot be empty (did you use '..'?).");
        if (name.ToString().EndsWith('.')) throw Error("Variable names cannot end in '.'.");

        return new(TokenTypes.Variable, '@' + name.ToString(), position, name.ToString());
    }

    private Token ReadUnionOrAnd()
    {
        var position = Index;
        if (Peek(1) == '&')
        {
            Index += 2;
            return new Token(TokenTypes.And, "&&", position);
        }

        Index++;
        return new Token(TokenTypes.Union, "&", position);
    }

    private void SkipWhitespace()
    {
        while (!Eof() && char.IsWhiteSpace(CurrentChar()))
            Index++;
    }

    private TokenizerException Error(string message, Int32? position = null) => new(message, Expression, position ?? Index);

    private Boolean Eof() => Index >= Expression.Length;
    private Char CurrentChar() => Expression[Index];
    private Char Peek(Int32 lookahead = 0)
    {
        int i = Index + lookahead;
        return (i >= 0 && i < Expression.Length) ? Expression[i] : '\0';
    }
    private Char Next() => Expression[Index++];
    
}

