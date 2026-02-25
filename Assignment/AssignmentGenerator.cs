using DiceRoller;
using LB.Utility.Collections;
using LB.Utility.Extensions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TheRandomizer.Assignment.Parser;
using TheRandomizer.Helpers;
using TheRandomizer.Parameters;

// ToDo: Potential upgrades
// Deterministic RNG injection (for unit tests)
// Precompiled AST caching for performance

namespace TheRandomizer.Assignment;

public partial class AssignmentGenerator : BaseGenerator
{
    #region Enumerators
    #endregion

    #region Constants
    /// <summary>The starting lineitem for generation</summary>
    private const String START_ITEM = "Start";
    /// <summary>The maximum level of recursion to allow before aborting the generation process</summary>
    private const Int32 MAX_RECURSION_DEPTH = 1000;
    /// <summary>The maximum number of loops to allow before aborting the generation process</summary>
    private const Int32 MAX_LOOP_COUNT = 10000000;
    #endregion

    #region Public Properties
    public static TextInfo TextInfo => CultureInfo.CurrentCulture.TextInfo;
    public override Boolean SupportsParameters => true;
    public Boolean RemoveEmptyLines { get; set; }
    public LineItemDictionary LineItems { get; set; } = [];
    public List<String> Libraries { get; set; } = [];
    #endregion

    #region Private Properties
    private InsensitiveDictionary<Object?> Variables { get; set; } = [];
    private Int32 LoopCount { get; set; }
    private Int32 RecursionDepth { get; set; }
    #endregion

    #region Public Methods
    public override GeneratorResult Generate(params BaseParameter[] parameters)
    {
        if (LineItems.Sum(li => li.Value.Count) == 0) throw new DefinitionException($"Markov definition \"{Name}\" has no line items.");
        LoopCount = 0;
        RecursionDepth = 0;

        Variables.Clear();
        LoadLibraries();
        if (!PreProcessParameters())
            throw new ParameterValidationException(Parameters.ErrorList);

        var startItem = SelectLineItem(START_ITEM);
        var output = Render(startItem.Content);

        if (RemoveEmptyLines)
            output = RemoveEmptyLinesFrom(output);

        output = UnescapeBrackets(output);

        return new GeneratorResult() { Text = output, Format = OutputFormat };
    }
    #endregion

    #region Private Methods
    private Boolean PreProcessParameters()
    {
        var valid = true;
        foreach(var parameter in Parameters)
        {
            var value = parameter.Value;
            if (!parameter.HasValue && !parameter.Default.IsNullOrWhitespace())
            {
                value = EvaluateExpression(parameter.Default);
            }
            Variables.Add(parameter.Name, value);
            valid &= parameter.Valid;
        }
        return valid;
    }

    private void LoadLibraries()
    {
        foreach(var library in Libraries)
        {
            var path = ResolveFilePath(library);
            var text = File.ReadAllText(path);
            var format = ExtensionToFormatType(path);
            var lineItemDictionary = Serialization.Deserialize<LineItemDictionary>(text, format) ??
                throw new AssignmentExpressionException($"Unable to load library {library}.");
            
            foreach(var lineItems in lineItemDictionary)
            {
                if (!LineItems.Contains(lineItems.Key))
                    LineItems.Add(lineItems.Key, lineItems.Value);
                else
                    LineItems[lineItems.Key].AddRange(lineItems.Value);
            }
        }
    }

    private String ResolveFilePath(String path)
    {
        var resolved = Environment.ExpandEnvironmentVariables(path);
        if (!Path.IsPathRooted(resolved))
        {
            var working = Path.Combine(Directory.GetCurrentDirectory(), resolved);
            if (File.Exists(working))
                return working;

            var filePath = Path.GetDirectoryName(FilePath);

            if (!String.IsNullOrWhiteSpace(filePath))
            {               
                var current = Path.Combine(filePath, resolved);
                if (File.Exists(current))
                    return current;
            }
        }
        return resolved;
    }

    private static String RemoveEmptyLinesFrom(String value)
    {
        var lines = value.Replace("\r\n", "\n").Split('\n');            
        return String.Join('\n', lines.Where(l => !String.IsNullOrWhiteSpace(l)));
    }

    private LineItem SelectLineItem(String name)
    {
        var list = ResolveList(name);
        return SelectFromList(list);
    }

    private LineItem SelectFromList(LineItemList list)
    {
        var totalWeight = (Int32)list.Sum(i => Math.Max(i.Weight, 1));

        var roll = RNG.NextInt32(totalWeight);
        var sum = 0;

        foreach(var item in list)
        {
            sum += (Int32)Math.Max(item.Weight, 1U);
            if (roll < sum) return item;
        }

        return list[^1];
    }

    private String Render(String content)
    {
        RecursionDepth++;

        if (RecursionDepth > MAX_RECURSION_DEPTH)
            throw new AssignmentExpressionException($"Max recursion depth reached ({MAX_RECURSION_DEPTH}).");

        try
        {
            var builder = new StringBuilder(content);

            while (true)
            {
                LoopCount++;
                if (LoopCount > MAX_LOOP_COUNT) 
                    throw new AssignmentExpressionException($"Max loop count reached ({MAX_LOOP_COUNT}).");

                var (start, end) = FindNextBracketSpan(builder);

                if (start < 0) break;

                var expression = builder.ToString(start + 1, end - start - 1).Trim();
                var result = EvaluateExpression(expression);
                builder.Remove(start, end - start + 1);
                builder.Insert(start, result);
            }

            return builder.ToString();
        }
        finally
        {
            RecursionDepth--;
        }
    }

    #region Expression Evaluation
    #region Evaluation
    private String EvaluateExpression(String expression)
    {
        if (IsBareIdentifier(expression))
        {
            var item = SelectLineItem(expression);
            return Render(item.Content);
        }

        var ast = new Parser.Parser(expression).Parse();
        var value = Evaluate(ast);

        if (value is null)
            return String.Empty;

        if (value is String str) return str;
        if (value is Int32 n) return n.ToString();
        if (value is Boolean b) return b.ToString().ToLowerInvariant();

        if (value is LineItemList)
            throw new AssignmentExpressionException("Expression evaluated to an ItemList. Did you forget to wrap it in Select(...)?");

        return value.ToString() ?? String.Empty;
    }

    private Object? Evaluate(Node node)
    {
        return node switch
        {
            NumberNode n => n.Value,
            StringNode s => s.Value,
            BooleanNode b => b.Value,
            VariableNode v => ReadVariable(v.Name),
            IdentifierNode id => id.Name,
            AssignmentNode a => EvalAssignment(a),
            CallNode c => EvalCall(c),
            BinaryNode b => EvalBinary(b),
            _ => throw new NotSupportedException($"Unknown node type: {node.GetType().Name}")
        };
    }

    private Object? EvalAssignment(AssignmentNode node)
    {
        var right = Evaluate(node.Value) ?? throw new AssignmentExpressionException($"Cannot assign void to '{node.Name}'.");
        if (!right.GetType().In(typeof(String), typeof(Int32), typeof(Boolean)))
            throw new AssignmentExpressionException($"Variables can only store primitives (String/Number/Boolean). '{node.Name}' got {right.GetType().Name}");

        Variables[node.Name] = right;
        return node.Emit ? right : null;
    }

    private Object? EvalBinary(BinaryNode node)
    {
        return node.Operator switch
        {
            // Functions
            BinaryOperators.Union => FuncUnion([node.Left, node.Right]),
            BinaryOperators.OneOf => FuncOneOf([node.Left, node.Right]),
            BinaryOperators.KeyOf => FuncKeyOf([node.Left, node.Right]),
            BinaryOperators.Repeat => FuncRepeat([node.Left, node.Right]),
            BinaryOperators.Weight => FuncWeight([node.Left, node.Right]),
            // Comparers
            BinaryOperators.EqualTo => EvalEqualTo(node.Left, node.Right),
            BinaryOperators.NotEqualTo => !EvalEqualTo(node.Left, node.Right),
            BinaryOperators.LessThan => EvalRelation(node.Left, node.Right, node.Operator),
            BinaryOperators.LessThanOrEqual => EvalRelation(node.Left, node.Right, node.Operator),
            BinaryOperators.GreaterThan => EvalRelation(node.Left, node.Right, node.Operator),
            BinaryOperators.GreaterThanOrEqual => EvalRelation(node.Left, node.Right, node.Operator),
            // Boolean
            BinaryOperators.And => EvalAnd(node.Left,node.Right),
            BinaryOperators.Or => EvalBoolean(node.Left) && EvalBoolean(node.Right),
            // Throw
            _ => throw new NotSupportedException($"Unknown operator {node.Operator}.")
        };
    }

    private Boolean EvalAnd(Node left, Node right)
    {
        var l = EvalBoolean(left);
        if (!l) return false;
        var r = EvalBoolean(right);
        return l && r;
    }

    private Boolean EvalBoolean(Node n)
    {
        var value = Evaluate(n);
        if (value is Boolean b) return b;
        throw new AssignmentExpressionException("Excpected Boolean.");
    }

    private Object? EvalCall(CallNode node)
    {
        return node.Name.ToLowerInvariant() switch
        {
            "select" => FuncSelect(node.Arguments),
            "union" => FuncUnion(node.Arguments),
            "oneof" => FuncOneOf(node.Arguments),
            "concat" => FuncConcat(node.Arguments),
            "pick" => FuncPick(node.Arguments),
            "from" => FuncFrom(node.Arguments),
            "keyof" => FuncKeyOf(node.Arguments),
            "eval" => FuncEval(node.Arguments),
            "repeat" => FuncRepeat(node.Arguments),
            "weight" => FuncWeight(node.Arguments),
            "if" => FuncIf(node.Arguments),
            // String Manipulation
            "lower" => FuncLower(node.Arguments),
            "title" => FuncTitle(node.Arguments),
            "sentence" => FuncSentence(node.Arguments),
            "upper" => FuncUpper(node.Arguments),
            "substring" => FuncSubString(node.Arguments),
            "contains" => FuncContains(node.Arguments),
            "left" => FuncLeft(node.Arguments),
            "right" => FuncRight(node.Arguments),
            "length" => FuncLength(node.Arguments),
            // 
            "generate" => FuncGenerate(node.Arguments),
            _ => throw new AssignmentExpressionException($"Unknown function '{node.Name}'."),
        };
    }

    /// <summary>
    /// Evaluates an equal to operation
    /// </summary>
    private Boolean EvalEqualTo(Node left, Node right)
    {
        var l = Evaluate(left);
        var r = Evaluate(right);

        if (l is Int32 li && r is Int32 ri) return li == ri;
        if (l is Boolean lb && r is Boolean rb) return lb == rb;
        if (l is String ls && r is String rs)
            return ls.Equals(rs, StringComparison.InvariantCultureIgnoreCase);

        throw new AssignmentExpressionException("== and != require matching types.");

    }

    /// <summary>
    /// If node is an Item List Name, returns the Item List
    /// </summary>
    private LineItemList EvalListArgument(Node node)
    {
        var value = Evaluate(node);

        return value switch
        {
            String name => ResolveList(name),
            LineItemList list => list,
            _ => throw new AssignmentExpressionException($"Expected ItemListName or ItemList, got {value?.GetType()}")
        };
    }

    /// <summary>
    /// Evaluates a number or converts a string to a number
    /// </summary>
    private static Boolean EvalNumber(Object? value, out Int32 number)
    {
        if (value is Int32 i)
        {
            number = i;
            return true;
        }
        else if (value is String s && Int32.TryParse(s, out var n))
        {
            number = n;
            return true;
        }
        number = 0;
        return false;
    }

    /// <summary>
    /// Evaluates relational operators
    /// </summary>
    private Boolean EvalRelation(Node left, Node right, BinaryOperators op)
    {
        var l = Evaluate(left);
        var r = Evaluate(right);

        if (l is Int32 li && r is Int32 ri)
        {
            return op switch
            {
                BinaryOperators.LessThan => li < ri,
                BinaryOperators.GreaterThan => li > ri,
                BinaryOperators.LessThanOrEqual => li <= ri,
                BinaryOperators.GreaterThanOrEqual => li >= ri,
                _ => false
            };
        }
        if (l is String ls && r is String rs)
        {
            return op switch
            {
                BinaryOperators.LessThan => String.Compare(ls, rs) == -1,
                BinaryOperators.GreaterThan => String.Compare(ls, rs) == 1,
                BinaryOperators.LessThanOrEqual => String.Compare(ls, rs).In(-1,0),
                BinaryOperators.GreaterThanOrEqual => String.Compare(ls, rs).In(0,1),
                _ => false
            };
        }

        throw new AssignmentExpressionException("Relational operators require two numbers or two strings");
    }
    #endregion

    #region Functions
    
    #region String Result Methods
    /// <summary>
    /// Concatenates one or more primitive values into a single string
    /// </summary>
    private String FuncConcat(List<Node> nodes)
    {
        var builder = new StringBuilder();
        foreach (var node in nodes)
        {
            var value = Evaluate(node);
            if (value is not null)
            {
                builder.Append(value switch
                {
                    String s => s,
                    Double d => d.ToString(),
                    Int32 n => n.ToString(),
                    Boolean b => b.ToString(),
                    _ => throw new AssignmentExpressionException("Concat(a, b, ...)", "Only accepts primitives.")
                });
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Evaluates the provided Dice Roller expression
    /// </summary>
    private String FuncEval(List<Node> nodes)
    {
        if (nodes.Count != 1)
            throw new AssignmentExpressionException("Eval(formula)", "Takes exactly one argument.");

        var value = Evaluate(nodes[0]);
        if (value is not String expression)
            throw new AssignmentExpressionException("Eval(formula)", "Expects a string.");

        var dice = new Dice();
        foreach(var variable in Variables)
        {
            if (variable.Value is Int32 i)
                dice.Variables.Add(variable.Key, i);
        }
        return dice.Roll(expression).ToString();
    }

    /// <summary>
    /// Runs an external generator and returns the resulting text
    /// </summary>
    private String FuncGenerate(List<Node> nodes)
    {
        if (nodes.Count < 1)
            throw new AssignmentExpressionException("Generate(path[,param_name,param_value,...])", "Takes one or more arguments.");

        var filePathObj = Evaluate(nodes[0]);
        var parameters = new ParameterList();

        if (filePathObj is not String filePath)
            throw new AssignmentExpressionException("Generate(path[,param_name,param_value,...])", "Expects a first argument of type string.");

        filePath = ResolveFilePath(filePath);

        if (!File.Exists(filePath))
            throw new AssignmentExpressionException("Generate(path[,param_name,param_value,...])", $"Could not fine definition file '{filePath}'.");

        for(var index = 1; index < nodes.Count; index+=2)
        {
            var paramNameObj = Evaluate(nodes[index]);
            var paramValue = Evaluate(nodes[index + 1]);
            if (paramNameObj is not String paramName)
                throw new AssignmentExpressionException("Generate(path[,param_name,param_value,...])", "Parameters arguments must be in the format 'Name','Value'.");
            parameters.Add(paramName, paramValue);
        }

        var generator = BaseGenerator.Deserialize(filePath) ?? throw new AssignmentExpressionException("Generate(path[,param_name,param_value,...])", $"Unable to load generator, '{filePath}'.");

        generator.Parameters = parameters;
        return generator.Generate().Text;
    }

    /// <summary>
    /// Concatenates the list of strings into a Item List Name
    /// </summary>
    private String FuncKeyOf(List<Node> nodes)
    {
        if (nodes.Count == 0)
            throw new AssignmentExpressionException("KeyOf(a,b,...)", "Requires at least one argument.");

        var builder = new StringBuilder();
        foreach (var node in nodes)
        {
            var value = Evaluate(node);
            if (value is not String name)
                throw new AssignmentExpressionException("KeyOf(a,b,...)", "Expects ItemListName arguments.");

            var item = SelectLineItem(name);
            builder.Append(Render(item.Content));
        }
        return builder.ToString();
    }

    /// <summary>
    /// Selects one string from the provided list
    /// </summary>
    private String FuncPick(List<Node> nodes)
    {
        if (nodes.Count == 0)
            throw new AssignmentExpressionException("Pick(a, b, ...)", "Requires at least one argument.");
        var values = new List<String>();
        foreach (var node in nodes)
        {
            var value = Evaluate(node);
            if (value is not string str)
                throw new AssignmentExpressionException("Pick(a, b, ...)", "Only accepts string arguments.");
            values.Add(str);
        }

        return values[RNG.NextInt32(values.Count)];
    }

    /// <summary>
    /// Selects an item from the provided Item List Count times and returns them as a string
    /// </summary>
    private String FuncRepeat(List<Node> nodes)
    {
        if (nodes.Count < 2 || nodes.Count > 3)
            throw new AssignmentExpressionException("Repeat(list, count[, seperator])", "Expects two or three arguments.");

        var list = EvalListArgument(nodes[0]);

        var countObj = Evaluate(nodes[1]);

        if (countObj is not Int32 count || count <= 0)
            throw new AssignmentExpressionException("Repeat(list count[, seperator])", "Count must be a positive integer.");

        String seperator = String.Empty;
        if (nodes.Count == 3)
        {
            var seperatorObj = Evaluate(nodes[2]);
            if (seperatorObj is not String s)
                throw new AssignmentExpressionException("Repeat(list count[, seperator])", "Seperator must be a string.");
            seperator = s;
        }

        var items = new List<String>();

        for (Int32 i = 0; i < count; i++)
        {
            var item = list.IsDeck ? list.DrawRandomItem() : SelectFromList(list);
            items.Add(Render(item.Content));
        }

        return String.Join(seperator, items.ToArray());
    }

    /// <summary>
    /// Selects an item from the Line Item List provided
    /// </summary>
    private String FuncSelect(List<Node> nodes)
    {
        if (nodes.Count != 1)
            throw new AssignmentExpressionException("Select(x)", "Takes exactly one argument.");

        var value = Evaluate(nodes[0]);

        if (value is String listName)
        {
            var item = SelectLineItem(listName);
            return Render(item.Content);
        }

        if (value is LineItemList list)
        {
            var item = SelectFromList(list);
            return Render(item.Content);
        }

        throw new AssignmentExpressionException("Select(x)", $"Expected ItemListName or ItemList, got {value?.GetType().Name ?? "null"}.");
    }

    /// <summary>
    /// Selects a portion of a string defined by the start and end values
    /// </summary>
    private String FuncSubString(List<Node> nodes)
    {
        if (nodes.Count < 2 || nodes.Count > 3)
            throw new AssignmentExpressionException("SubString(s,n[,n])", "Takes two or three arguments.");

        var value = Evaluate(nodes[0]);

        if (value is not String s)
            throw new AssignmentExpressionException("Substring(s,n[,n])", "Expects a first argument type of string.");

        var startObj = nodes.Count == 2 ? Evaluate(nodes[1]) : 0;
        var endObj = Evaluate(nodes.Last());

        if (!EvalNumber(startObj, out var start))
            throw new AssignmentExpressionException("Substring(s,n[,n])", "Expects a start value of type number.");
        if (!EvalNumber(endObj,out var end))
            throw new AssignmentExpressionException("Substring(s,n[,n])", "Expects an end value of type number.");
        if (start < 0 || start > s.Length)
            throw new AssignmentExpressionException("Substring(s,n[,n])", "Start value is out of range.");
        if (end < 0 || end > s.Length)
            throw new AssignmentExpressionException("Substring(s,n[,n])", "End value is out of range.");
        if (end < start)
            throw new AssignmentExpressionException("Substring(s,n[,n])", "End value is smaller than start value.");

        var range = new Range(start, end);
        return s[range];
    }

    /// <summary>
    /// Selects the left portion of a string
    /// </summary>
    private String FuncLeft(List<Node> nodes)
    {
        if (nodes.Count != 2)
            throw new AssignmentExpressionException("Left(s,n)", "Takes exactly two arguments.");
        
        var value = Evaluate(nodes[0]);
        if (value is not String s)
            throw new AssignmentExpressionException("Left(s,n)", "Expects a first argument of type string.");

        var lengthObj = Evaluate(nodes[1]);

        if (!EvalNumber(lengthObj, out var length))
            throw new AssignmentExpressionException("Left(s,n)", "Expects a second argument of number.");

        return s[length..];
    }

    /// <summary>
    /// Selects the left portion of a string
    /// </summary>
    private String FuncRight(List<Node> nodes)
    {
        if (nodes.Count != 2)
            throw new AssignmentExpressionException("Right(s,n)", "Takes exactly two arguments.");

        var value = Evaluate(nodes[0]);
        if (value is not String s)
            throw new AssignmentExpressionException("Right(s,n)", "Expects a first argument of type string.");

        var lengthObj = Evaluate(nodes[1]);

        if (!EvalNumber(lengthObj, out var length))
            throw new AssignmentExpressionException("Right(s,n)", "Expects a second argument of number.");

        return s.Right(length);
    }

    /// <summary>
    /// Returns the length of a string
    /// </summary>
    private Int32 FuncLength(List<Node> nodes)
    {
        if (nodes.Count != 1)
            throw new AssignmentExpressionException("Length(s)", "Takes exactly one argument.");

        var value = Evaluate(nodes[0]);
        if (value is not String s)
            throw new AssignmentExpressionException("Length(s)", "Expects a first argument of type string.");

        return s.Length;
    }
    #endregion

    #region Line Item List Result Functions
    /// <summary>
    /// Retrieves the Item List for the provided Item List Name
    /// </summary>
    private LineItemList FuncFrom(List<Node> nodes)
    {
        if (nodes.Count != 1)
            throw new AssignmentExpressionException("From(key)", "Takes exactly one argument.");

        var value = Evaluate(nodes[0]);
        if (value is not String key)
            throw new AssignmentExpressionException("From(key)", "Expects a string.");

        return ResolveList(key);
    }

    /// <summary>
    /// Chooses one of the provided Item lists and selects a value from it.
    /// </summary>
    private LineItemList FuncOneOf(List<Node> nodes)
    {
        if (nodes.Count == 0)
            throw new AssignmentExpressionException("OneOf(a,b,...)", "Requires at least 1 argument.");

        var lists = nodes.Select(EvalListArgument).ToList();

        var total = lists.Sum(l => (Int32)Math.Max(l.TotalWeight, 1U));
        var roll = RNG.NextInt32(total);

        var sum = 0;
        foreach (var list in lists)
        {
            sum += (Int32)Math.Max(list.TotalWeight, 1U);
            if (roll < sum) return list;
        }
        return lists.Last();
    }

    /// <summary>
    /// Joins two Item Lists into a single list
    /// </summary>
    private LineItemList FuncUnion(List<Node> nodes)
    {
        if (nodes.Count == 0)
            throw new AssignmentExpressionException("Union(a,b,...)", "Requires at least one argument.");

        var result = new LineItemList();

        foreach (var node in nodes)
        {
            var list = EvalListArgument(node);
            result.AddRange(list);
        }

        return result;
    }

    /// <summary>
    /// Adds weight to the Lists provided
    /// </summary>
    private LineItemList FuncWeight(List<Node> nodes)
    {
        if (nodes.Count != 2)
            throw new AssignmentExpressionException("Weight", "Function requires exactly two operands.");

        var list = EvalListArgument(nodes[0]);

        var weightObj = Evaluate(nodes[1]);
        if (weightObj is not Int32 weight)
            throw new AssignmentExpressionException("Weight", "Must be a number.");

        var result = new LineItemList
        {
            IsDeck = list.IsDeck,
            Variable = list.Variable
        };

        checked
        {
            foreach (var item in list)
            {
                result.Add(new LineItem
                {
                    Content = item.Content,
                    Weight = (UInt32)((UInt64)item.Weight * (UInt32)weight)
                });
            }
        }

        return result;
    }
    #endregion

    #region Boolean Functions
    /// <summary>
    /// Returns true if haystack contains needle
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns></returns>
    private Boolean FuncContains(List<Node> nodes)
    {
        if (nodes.Count != 2)
            throw new AssignmentExpressionException("Contains(haystack, needle)", "Expects exactly two arguments.");
        
        var haystackObj = Evaluate(nodes[0]);

        if (haystackObj is not String haystack)
            throw new AssignmentExpressionException("Contains(haystack, needle)", "Haystack must be a string.");

        var needleObj = Evaluate(nodes[1]);

        if (needleObj is not String needle)
            throw new AssignmentExpressionException("Contains(haystack, needle)", "Needle must be a string.");

        return haystack.Contains(needle);
    }

    /// <summary>
    /// Performs an If branch
    /// </summary>
    private Object? FuncIf(List<Node> nodes)
    {
        if (nodes.Count != 3)
            throw new AssignmentExpressionException("If(condition,then,else)", "Expects exactly three arguments.");

        var cond = Evaluate(nodes[0]);
        if (cond is not Boolean b)
            throw new AssignmentExpressionException("If(condition, then, else)", "Condition must be a boolean.");

        return b ? Evaluate(nodes[1]) : Evaluate(nodes[2]);
    }
    #endregion

    #region String Manipulation Functions
    /// <summary>
    /// Converts the provided string to lower case
    /// </summary>
    private String FuncLower(List<Node> nodes)
    {
        if (nodes.Count != 1)
            throw new AssignmentExpressionException("Lower(s)", "Requires one argument.");

        var value = Evaluate(nodes[0]);

        if (value is not String s)
            throw new AssignmentExpressionException("Lower(s)", "Expects a string.");

        return TextInfo.ToLower(s);
    }

    [GeneratedRegex("[?.!]\\s+[a-z]")]
    private static partial Regex SentenceRegex();

    /// <summary>
    /// Converts the provided string to sentence case
    /// </summary>
    private String FuncSentence(List<Node> nodes)
    {
        if (nodes.Count != 1)
            throw new AssignmentExpressionException("Sentence(s)", "Requires exactly one argument.");

        var value = Evaluate(nodes[0]);

        if (value is not String s)
            throw new AssignmentExpressionException("Sentence(s)", "Expects a string.");

        s = TextInfo.ToUpper(s[0]) + s[1..];
        var r = SentenceRegex();
        s = r.Replace(s, m => m.Value.ToUpper());

        return s;
    }

    /// <summary>
    /// Converts the string to title case
    /// </summary>
    private String FuncTitle(List<Node> nodes)
    {
        if (nodes.Count != 1)
            throw new AssignmentExpressionException("Title(s)", "Requires exactly one argument.");

        var value = Evaluate(nodes[0]);

        if (value is not String s)
            throw new AssignmentExpressionException("Title(s)", "Expects a string.");

        return TextInfo.ToTitleCase(s);
    }

    /// <summary>
    /// Converts the string to upper case
    /// </summary>
    private String FuncUpper(List<Node> nodes)
    {
        if (nodes.Count != 1)
            throw new AssignmentExpressionException("Upper(s)", "Requires exactly one argument.");

        var value = Evaluate(nodes[0]);

        if (value is not String s)
            throw new AssignmentExpressionException("Upper(s)", "Expects a string.");

        return TextInfo.ToUpper(s);
    } 
    #endregion

    #endregion
    /// <summary>
    /// Attempts to retrieve the value of the named variable
    /// </summary>
    private Object? ReadVariable(String name)
    {
        if (!Variables.TryGetValue(name, out var value))
            throw new AssignmentExpressionException($"Variable '@{name}' is not defined.");
        return value;
    }

    /// <summary>
    /// Attempts to get a list with the provided name
    /// </summary>
    private LineItemList ResolveList(String name)
    {
        if (!LineItems.TryGetValue(name, out var list))
            throw new KeyNotFoundException($"Item list '{name}' was not found.");

        if (list is null || list.Count == 0)
            throw new AssignmentExpressionException($"Item list '{name}' contains no items.");

        return list;
    }

    private static bool IsBareIdentifier(string text)
    {
        // Must be a dotted identifier with no spaces and no operators/parentheses/quotes.
        // This is a cheap check; you can tighten later.
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (text.Contains(' ') || text.Contains('(') || text.Contains(')') || text.Contains('"') || text.Contains('@') ||
            text.Contains('&') || text.Contains('|') || text.Contains('*') || text.Contains(',') || text.Contains(':'))
            return false;

        // allow dotted identifiers
        foreach (var ch in text)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '.'))
                return false;
        }
        return true;
    }

    private static (int start, int end) FindNextBracketSpan(StringBuilder builder)
    {
        Boolean escaped = false;

        for (Int32 i = 0; i < builder.Length; i++)
        {
            Char c = builder[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c=='\\')
            {
                escaped = true;
                continue;
            }

            if (c != '[') continue;

            escaped = false;
            for (Int32 j = i + 1; j < builder.Length; j++)
            {
                char d = builder[j];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (d == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (d == '[')
                    throw new AssignmentExpressionException("Nested '[' is not allowed inside a bracket expression.");

                if (d == ']')
                    return (i, j);
            }
            throw new AssignmentExpressionException("Unterminated '[' bracket expression.");
        }

        return (-1, -1);
    }

    private static String UnescapeBrackets(String value) => value.Replace(@"\[", "[").Replace(@"\]", "]");        
    #endregion
    #endregion
}
