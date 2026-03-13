using System.Diagnostics;
using TheRandomizer.Assignment;
using TheRandomizer.Assignment.Parser;
using TheRandomizer.Utility;

namespace TheRandomizer.Tests;

[TestClass]
public sealed class Assignment
{
    [TestMethod]
    [DataRow("(", null, TokenTypes.OpenParenthesis)]
    [DataRow(")", null, TokenTypes.CloseParenthesis)]
    [DataRow(",", null, TokenTypes.Comma)]
    [DataRow("&", null, TokenTypes.Union)]
    [DataRow("|", null, TokenTypes.OneOf)]
    [DataRow("*", null, TokenTypes.Weight)]
    [DataRow(":=", null, TokenTypes.Assignment)]
    [DataRow("\"Test\"", "Test", TokenTypes.String)]
    [DataRow("Ident","Ident",TokenTypes.Identifier)]
    [DataRow("123", 123d, TokenTypes.Number)]
    [DataRow("123.4", 123.4, TokenTypes.Number)]
    [DataRow("@variable.1", "variable.1", TokenTypes.Variable)]
    public void TokenizerSingleTokenTest(String expression, Object? value, TokenTypes type)
    {
        var t = new Tokenizer(expression);
        var token = t.Tokenize().FirstOrDefault();
        Assert.IsNotNull(token);
        Assert.AreEqual(expression, token.Text);
        Assert.AreEqual(value, token.Value);
        Assert.AreEqual(type, token.TokenType);
    }

    [TestMethod]
    [DynamicData(nameof(MultiTokenData))]
    public void TokenizerMultiTokenTest((String expression, List<TokenTypes> tokens) data)
    {
        var t = new Tokenizer(data.expression);
        var tokens = t.Tokenize().ToList();
        Assert.IsNotNull(tokens);
        Assert.HasCount(data.tokens.Count, tokens);
        for (int i = 0; i < data.tokens.Count; i++)
        {
            Assert.AreEqual(data.tokens[i], tokens[i].TokenType);
        }
    }

    internal static IEnumerable<(String expression, List<TokenTypes> tokens)> MultiTokenData()
    {
        yield return new("This&\"That\"", [TokenTypes.Identifier,
                                           TokenTypes.Union,
                                           TokenTypes.String,
                                           TokenTypes.EOF]);
    }

    [TestMethod]
    public void ParserTests()
    {
        var parser = new Parser("Test:=Func(a,b)");
        var parsed = parser.Parse();
        Assert.IsInstanceOfType<AssignmentNode>(parsed);
        Assert.AreEqual("Test", ((AssignmentNode)parsed).Name);
        Assert.IsInstanceOfType<CallNode>(((AssignmentNode)parsed).Value);
    }

    [TestMethod]
    [DynamicData(nameof(GeneratorTestData))]
    public void GeneratorTests((AssignmentGenerator generator, String expected) data)
    {        
        var value = data.generator.Generate();
        Assert.AreEqual(data.expected, value.Text);
    }

    internal static IEnumerable<(BaseGenerator Generator, String Expected)> GeneratorTestData()
    {
        var json =
@"Name: Test
  RemoveEmptyLines: true
  LineItems: {
    Start: [
      {
        Content: Test
		Weight: 1
      }
    ]
  }";
        var generator = BaseGenerator.Deserialize(json, Enumerators.FileFormatTypes.Json);

        yield return new (generator!, "Test");

        json =
@"Name: Test
  LineItems: {
    Start: [
        {
            Content: ""[concat(\""Hello\"",\""World\"")]""
        }
    ]
}
";
        generator = BaseGenerator.Deserialize(json, Enumerators.FileFormatTypes.Json);
        yield return new(generator!, "HelloWorld");

        json =
@"Name: Test
  LineItems: {
    Start: [
        {
            Content: ""Hello [World]""
        }
    ]
    World: [
        {
            Content: ""World""
        }
    ]
}
";
        generator = BaseGenerator.Deserialize(json, Enumerators.FileFormatTypes.Json);
        yield return new(generator!, "Hello World");

        json =
@"Name: Test
  LineItems: {
    Start: [
        {
            Content: ""[var:=\""Test\""][@var]""
        }
    ]
}
";
        generator = BaseGenerator.Deserialize(json, Enumerators.FileFormatTypes.Json);
        yield return new(generator!, "Test");
    }
}
