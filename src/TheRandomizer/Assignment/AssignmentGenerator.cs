using DiceRoller;
using DiceRoller.Evaluator;
using LB.Utility.Collections;
using LB.Utility.Extensions;
using LB.Utility.Random;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TheRandomizer.Assignment.Parser;
using TheRandomizer.Parameters;
using TheRandomizer.Pluralize;
using TheRandomizer.Utility;

// ToDo: Potential upgrades
// Precompiled AST caching for performance

namespace TheRandomizer.Assignment;

public partial class AssignmentGenerator : BaseGenerator
{
    private readonly Dictionary<String, Func<List<Node>, Object?>>? _functions;

    private static Dictionary<String, Func<List<Node>, Object?>> BuildFunctionRegistry(AssignmentGenerator target)
    {
        var result = new Dictionary<String, Func<List<Node>, Object?>>(StringComparer.InvariantCultureIgnoreCase);
        var methods = typeof(AssignmentGenerator).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<FunctionAttribute>();
            if (attr != null)
            {
                var parameters = Expression.Parameter(typeof(List<Node>), "nodes");
                var instance = Expression.Constant(target);
                var call = Expression.Call(instance, method, parameters);

                var body = Expression.Convert(call, typeof(Object));
                var lambda = Expression.Lambda<Func<List<Node>, Object?>>(body, parameters).Compile();
                result[attr.Name] = lambda;
            }
        }
        

        return result;
    }

    public AssignmentGenerator() : base() 
    {
        _functions = BuildFunctionRegistry(this);
    }

    #region Enumerators
    #endregion

    #region Constants
    /// <summary>The preprocess line item</summary>
    private const String PRE_PROCESS_ITEM = "PreProcess";
    /// <summary>The starting lineitem for generation</summary>
    private const String START_ITEM = "Start";
    /// <summary>The maximum level of recursion to allow before aborting the generation process</summary>
    private const Int32 MAX_RECURSION_DEPTH = 1000;
    /// <summary>The maximum number of loops to allow before aborting the generation process</summary>
    private const Int32 MAX_LOOP_COUNT = 10000000;
    #endregion

    #region Public Properties
    public static TextInfo TextInfo => CultureInfo.CurrentCulture.TextInfo;
    [JsonIgnore]
    public override Boolean SupportsParameters => true;
    public Boolean RemoveEmptyLines { get; set; }
    public LineItemDictionary LineItems { get; set; } = [];
    public List<String> Libraries { get; set; } = [];
    [JsonIgnore]
    public Func<String, List<Object>, Object?>? FunctionHandler { get; set; }
    #endregion

    #region Private Properties
    private InsensitiveDictionary<Object?> Variables { get; set; } = [];
    private Int32 LoopCount { get; set; }
    private Int32 RecursionDepth { get; set; }
    #endregion

    #region Public Methods
    public override GeneratorResult Generate(params BaseParameter[] parameters)
    {
        if (LineItems.Sum(li => li.Value.Count) == 0) throw new DefinitionException($"Assignment definition \"{Name}\" has no line items.");
        LoopCount = 0;
        RecursionDepth = 0;

        Variables.Clear();
        LoadLibraries();
        if (!PreProcessParameters())
            throw new ParameterValidationException(Parameters.ErrorList);

        if (LineItems.ContainsKey(PRE_PROCESS_ITEM))
            PreProcess();

        var startItem = SelectLineItem(START_ITEM) 
                        ?? throw new Exception("Start list is empty");
        var output = Render(startItem.Content);

        if (RemoveEmptyLines)
            output = RemoveEmptyLinesFrom(output);

        output = UnescapeBrackets(output);

        return new GeneratorResult() { Text = output, Format = OutputFormat };
    }

    public override List<String> VerifyDefinition()
    {
        var result = new List<String>();
        foreach(var lineItemList in LineItems)
        {
            foreach(var lineItem in lineItemList.Value)
            {
                try
                {
                    var tokenizer = new Tokenizer(lineItem.Content);
                    var parser = new Parser.Parser(tokenizer);
                    parser.Parse();
                }
                catch (ParseException ex)
                {
                    result.Add($"Parser Error: {ex.Message} in group '{lineItemList.Key}");
                }
                catch (TokenizerException ex)
                {
                    result.Add($"Tokenizer Error: {ex.Message} in group '{lineItemList.Key}'");
                }
            }
        }
        return result;
    }
    #endregion

    #region Private Methods
    private void PreProcess()
    {
        foreach(var lineItem in LineItems[PRE_PROCESS_ITEM])
        {
            Render(lineItem.Content);
        }
    }

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
                var current = Path.GetFullPath(Path.Combine(filePath, resolved));
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

    private LineItem? SelectLineItem(String name)
    {
        var list = ResolveList(name);
        return SelectFromList(list);
    }

    private LineItem? SelectFromList(LineItemList list)
    {
        if (list.Count == 0) return null;
        var totalWeight = (UInt32)list.Sum(i => Math.Max(i.Weight, 1));

        var roll = RNG.NextUInt32(totalWeight);
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
        try
        {
            if (IsBareIdentifier(expression))
            {
                var item = SelectLineItem(expression);
                if (item == null) return String.Empty;
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
                throw new AssignmentExpressionException($"Expression '{expression}' evaluated to an ItemList. Did you forget to wrap it in Select(...)?");

            return value.ToString() ?? String.Empty;
        }
        catch (Exception ex)
        {
            ex.Data.Add(nameof(expression), expression);
            throw;
        }
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
        throw new AssignmentExpressionException($"Expected Boolean. Got {value?.GetType()} : {(value ?? "null")}");
    }

    private Object? EvalCall(CallNode node)
    {
        if (!_functions!.TryGetValue(node.Name, out var function))
        {
            if (FunctionHandler == null)
                throw new AssignmentExpressionException($"Unknown function '{node.Name}'.");
            else
            {
                var parameters = new List<Object>();
                foreach (var argument in node.Arguments)
                {
                    parameters.Add(Evaluate(argument) ?? throw new AssignmentExpressionException("EvalCall", "Parameter cannot be null"));
                }
                return FunctionHandler.Invoke(node.Name, parameters);
            }
        }
        return function!.Invoke(node.Arguments);
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

        throw new AssignmentExpressionException($"== and != require matching types. Got {FormatVar(l)} and {FormatVar(r)}.");

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
            _ => throw new AssignmentExpressionException($"Expected ItemListName or ItemList, got {FormatVar(value)}")
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
    [Function("Concat", "Concat(a, b, ...)", MinArguments = 1)]
    private String FuncConcat(List<Node> nodes)
    {
        CheckArity(nodes);
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
    [Function("Calc", "Calc(formula)", MinArguments = 1, MaxArguments = 1)]
    private Object FuncCalc(List<Node> nodes)
    {
        CheckArity(nodes);

        var expression = EvalAs<String>(nodes, 0);

        var dice = new Dice();
        foreach(var variable in Variables)
        {
            if (variable.Value is Int32 i32)
                dice.Variables[variable.Key] = i32;
            if (variable.Value is Int64 i64)
                dice.Variables[variable.Key] = i64;
            if (Int32.TryParse(variable.Value?.ToString(), out var i))
                dice.Variables[variable.Key] = i;
            if (Int64.TryParse(variable.Value?.ToString(), out var l))
                dice.Variables[variable.Key] = l;
        }
        var result = dice.Evaluate(expression);
        if (result is IntegerValue iResult) return iResult.Value;
        if (result is BooleanValue bResult) return bResult.Value;
        throw new AssignmentExpressionException($"Invalid type returned from calculation '{expression}' : '{result.GetType().Name}'");
    }

    [Function("ListExists", "ListExists(string)", MinArguments = 1, MaxArguments = 1)]
    private Boolean FuncListExists(List<Node> nodes)
    {
        CheckArity(nodes);
        var name = EvalAs<String>(nodes, 0);
        return LineItems.ContainsKey(name);
    }

    [Function("VarExists", "VarExists(string)", MinArguments = 1, MaxArguments = 1)]
    private Boolean FuncVarExists(List<Node> nodes)
    {
        CheckArity(nodes);
        var name = EvalAs<String>(nodes, 0);
        return Variables.ContainsKey(name);
    }

    /// <summary>
    /// Runs an external generator and returns the resulting text
    /// </summary>
    [Function("Generate", "Generate(path[,param_name,param_value,...]", MinArguments = 1)]
    private String FuncGenerate(List<Node> nodes)
    {
        CheckArity(nodes);

        var filePath = EvalAs<String>(nodes, 0);
        var parameters = new ParameterList();

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

        var generator = Deserialize(filePath)
                        ?? throw new AssignmentExpressionException("Generate(path[,param_name,param_value,...])", $"Unable to load generator, '{filePath}'.");

        generator.Parameters = parameters;
        return generator.Generate().Text;
    }

    /// <summary>
    /// Concatenates the list of strings into a Item List Name
    /// </summary>
    [Function("KeyOf", "KeyOf(a,b,...)", MinArguments = 1)]
    private String FuncKeyOf(List<Node> nodes)
    {
        CheckArity(nodes);

        var builder = new StringBuilder();
        for (var i = 0; i < nodes.Count; i++)
        {
            var name = EvalAs<String>(nodes, i);
            var item = SelectLineItem(name);
            if (item == null) return String.Empty;
            builder.Append(Render(item.Content));
        }
        return builder.ToString();
    }

    [Function("Max", "Max(a,b,...)", MinArguments = 1)]
    private Int32 FuncMax(List<Node> nodes)
    {
        CheckArity(nodes);
        var values = EvalAllAs<Int32>(nodes);
        return values.Max();
    }

    [Function("Min", "Min(a,b,...)", MinArguments = 1)]
    private Int32 FuncMin(List<Node> nodes)
    {
        CheckArity(nodes);
        var values = EvalAllAs<Int32>(nodes);
        return values.Min();
    }

    /// <summary>
    /// Selects one string from the provided list
    /// </summary>
    [Function("Pick", "Pick(a,b,...)", MinArguments = 1)]
    private String FuncPick(List<Node> nodes)
    {
        CheckArity(nodes);
        
        var values = new List<String>();        
        for (var i = 0; i < nodes.Count; i++)
        {
            var value = EvalAs<String>(nodes, i);
            values.Add(value);
        }

        return values[(Int32)(RNG.NextUInt32((UInt32)values.Count))];
    }

    [Function("Random", "Random([min, ] max)", MinArguments = 1)]
    private Int32 FuncRandom(List<Node> nodes)
    {
        CheckArity(nodes);

        var min = nodes.Count == 1 ? 0 : EvalAs<Int32>(nodes, 0);
        var max = nodes.Count == 1 ? EvalAs<Int32>(nodes, 0) : EvalAs<Int32>(nodes, 1);

        return RNG.NextInt32(min, max);
    }

    /// <summary>
    /// Selects an item from the provided Item List Count times and returns them as a string
    /// </summary>
    [Function("Repeat", "Repeat(list, count[, seperator])", MinArguments = 2, MaxArguments = 3)]
    private String FuncRepeat(List<Node> nodes)
    {
        CheckArity(nodes);
        
        var list = EvalListArgument(nodes[0]);

        var count = EvalAs<Int32>(nodes,1);

        var seperator = nodes.Count == 3 ? EvalAs<String>(nodes, 2) : String.Empty;
        
        var items = new List<String>();

        for (Int32 i = 0; i < count; i++)
        {
            var item = list.IsDeck ? list.DrawRandomItem() : SelectFromList(list);
            if (item == null) return String.Empty;
            items.Add(Render(item.Content));
        }

        return String.Join(seperator, items.ToArray());
    }

    /// <summary>
    /// Selects an item from the Line Item List provided
    /// </summary>
    [Function("Select", "Select(x)", MinArguments = 1, MaxArguments = 1)]
    private String FuncSelect(List<Node> nodes)
    {
        CheckArity(nodes);

        var value = Evaluate(nodes[0]);

        if (value is String listName)
        {
            var item = SelectLineItem(listName);
            if (item == null) return String.Empty;
            return Render(item.Content);
        }

        if (value is LineItemList list)
        {
            var item = SelectFromList(list);
            if (item == null) return String.Empty;
            return Render(item.Content);
        }

        throw new AssignmentExpressionException(FunctionAttribute.GetDefinition(), $"Expected ItemListName or ItemList, got {value?.GetType().Name ?? "null"}.");
    }

    /// <summary>
    /// Selects a portion of a string defined by the start and end values
    /// </summary>
    [Function("SubString", "SubString(string[, start], end)", MinArguments = 2, MaxArguments = 3)]
    private String FuncSubString(List<Node> nodes)
    {
        CheckArity(nodes);

        var value = EvalAs<String>(nodes, 0);
                
        var start = nodes.Count == 2 ? EvalAs<Int32>(nodes, 1) : 0;
        var end = EvalAs<Int32>(nodes, nodes.Count - 1);

        if (start < 0 || start > value.Length)
            throw new AssignmentExpressionException(FunctionAttribute.GetDefinition(), "Start value is out of range.");
        if (end < 0 || end > value.Length)
            throw new AssignmentExpressionException(FunctionAttribute.GetDefinition(), "End value is out of range.");
        if (end < start)
            throw new AssignmentExpressionException(FunctionAttribute.GetDefinition(), "End value is smaller than start value.");

        var range = new Range(start, end);
        return value[range];
    }

    [Function("Switch","Switch(value, case, result, [case, result,] default)", MinArguments = 4)]
    private Object? FuncSwitch(List<Node> nodes)
    {
        CheckArity(nodes);
        if (nodes.Count.IsOdd()) throw new AssignmentExpressionException(FunctionAttribute.GetName(nameof(FuncSwitch)), "Expects an even number of arguments.");

        var value = Evaluate(nodes[0]);

        for(var i = 1; i < nodes.Count; i+=2)
        {
            var caseValue = Evaluate(nodes[i]);

            if (ValuesEqual(value, caseValue))
                return Evaluate(nodes[i + 1]);
        }
        return Evaluate(nodes[^1]);
    }

    /// <summary>
    /// Selects the left portion of a string
    /// </summary>
    [Function("Left", "Left(string, length)", MinArguments = 2, MaxArguments = 2)]
    private String FuncLeft(List<Node> nodes)
    {
        CheckArity(nodes);        
        var value = EvalAs<String>(nodes, 0);
        var length = EvalAs<Int32>(nodes, 1);
        if (length == 0) return String.Empty;
        if (length > value.Length) return value;
        return value[length..];
    }

    /// <summary>
    /// Selects the right portion of a string
    /// </summary>
    [Function("Right", "Right(string, length)", MinArguments = 2, MaxArguments = 2)]
    private String FuncRight(List<Node> nodes)
    {
        CheckArity(nodes);
        var value = EvalAs<String>(nodes, 0);        
        var length = EvalAs<Int32>(nodes, 1);
        if (length == 0) return String.Empty;
        if (length > value.Length) return value;
        return value.Right(length);
    }

    /// <summary>
    /// Returns the length of a string
    /// </summary>
    [Function("Length", "Length(string)", MinArguments = 1, MaxArguments = 1)]
    private Int32 FuncLength(List<Node> nodes)
    {
        CheckArity(nodes);
        var value = EvalAs<String>(nodes, 0);
        return value.Length;
    }
    #endregion

    #region Line Item List Result Functions
    /// <summary>
    /// Retrieves the Item List for the provided Item List Name
    /// </summary>
    [Function("From", "From(key)", MinArguments = 1, MaxArguments = 1)]
    private LineItemList FuncFrom(List<Node> nodes)
    {
        CheckArity(nodes);
        return ResolveList(EvalAs<String>(nodes, 0));
    }

    /// <summary>
    /// Chooses one of the provided Item lists and selects a value from it.
    /// </summary>
    [Function("OneOf", "OneOf(a,b,...)", MinArguments = 1)]
    private LineItemList FuncOneOf(List<Node> nodes)
    {
        CheckArity(nodes);

        var lists = nodes.Select(EvalListArgument).ToList();

        var total = (UInt32)lists.Sum(l => (Int32)Math.Max(l.TotalWeight, 1U));
        var roll = PseudoRNG.Instance?.NextUInt32((UInt32)nodes.Count)!;

        return lists[(Int32)roll];
    }

    /// <summary>
    /// Joins two Item Lists into a single list
    /// </summary>
    [Function("Union", "Union(a,b,...)", MinArguments = 1)]
    private LineItemList FuncUnion(List<Node> nodes)
    {
        CheckArity(nodes);

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
    [Function("Weight", "Weight(list, factor)", MinArguments = 2, MaxArguments = 2)]
    private LineItemList FuncWeight(List<Node> nodes)
    {
        CheckArity(nodes);

        var list = EvalListArgument(nodes[0]);

        var weight = EvalAs<Int32>(nodes, 1);
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
    [Function("Contains", "Contains(haystack, needle)", MinArguments = 2, MaxArguments = 2)]
    private Boolean FuncContains(List<Node> nodes)
    {
        CheckArity(nodes);
        var haystack = EvalAs<String>(nodes, 0);
        var needle = EvalAs<String>(nodes, 1);
        return haystack.Contains(needle);
    }

    [Function("Bool", "Bool(value)", MinArguments = 1, MaxArguments = 1)]
    private Boolean FuncBool(List<Node> nodes)
    {
        CheckArity(nodes);
        
        var valueObj = Evaluate(nodes[0]) ?? false;

        if (valueObj is Boolean b) return b;
        if (valueObj is String s)
        {
            if (Boolean.TryParse(s, out var result)) return result;
            else throw new AssignmentExpressionException(FunctionAttribute.GetDefinition(), $"Could not convert '{valueObj}' to a boolean.");
        }

        return false;
    }

    [Function("In","In(value, a[, b, ...])", MinArguments = 2)]
    private Boolean FuncIn(List<Node> nodes)
    {
        CheckArity(nodes);

        var value = Evaluate(nodes[0]);

        if (value == null) return false;

        for(var i = 1; i < nodes.Count; i++)
        {
            var comp = Evaluate(nodes[1]);
            if (value.GetType() != comp?.GetType()) throw new AssignmentExpressionException("In", "All arguments must be of the same type.");
            if (value.Equals(comp)) return true;
        }
        return false;
    }

    [Function("Int", "Int(value)", MinArguments = 1, MaxArguments = 1)]
    private Int32 FuncInt(List<Node> nodes)
    {
        CheckArity(nodes);

        var valueObj = Evaluate(nodes[0]) ?? 0;

        if (valueObj is Int32 i) return i;
        if (valueObj is String s)
        {
            if (Int32.TryParse(s, out var result)) return result;
            else throw new AssignmentExpressionException(FunctionAttribute.GetDefinition(), $"Could not convert '{valueObj}' to an integer.");
        }

        return 0;
    }

    /// <summary>
    /// Performs an If branch
    /// </summary>
    [Function("If", "If(condition,then,else)", MinArguments = 2, MaxArguments = 3)]
    private Object? FuncIf(List<Node> nodes)
    {
        CheckArity(nodes);

        var cond = EvalAs<Boolean>(nodes, 0);

        if (nodes.Count == 3)
            return cond ? Evaluate(nodes[1]) : Evaluate(nodes[2]);
        else
            return cond ? Evaluate(nodes[1]) : null;
    }

    /// <summary>
    /// If the first list exists, select from it otherwise use the second list
    /// </summary>
    /// <param name="nodes"></param>
    [Function("SelectIf", "SelectIf(ListOne,ListTwo)", MinArguments = 2, MaxArguments = 2)]
    private String? FuncSelectIf(List<Node> nodes)
    {
        CheckArity(nodes);

        var listName1 = EvalAs<String>(nodes, 0);

        if (LineItems.ContainsKey(listName1))
            return FuncSelect([nodes[0]]);
        else if (nodes.Count == 2)
            return FuncSelect([nodes[1]]);
        else 
            return null;
    }
    #endregion

    #region String Manipulation Functions
    /// <summary>
    /// Converts the provided string to lower case
    /// </summary>
    [Function("Lower", "Lower(string)", MinArguments = 1, MaxArguments = 1)]
    private String FuncLower(List<Node> nodes)
    {
        CheckArity(nodes);
        return TextInfo.ToLower(EvalAs<String>(nodes, 0));
    }

    /// <summary>
    /// Converts the string to plural
    /// </summary>
    [Function("Plural", "Plural(string)", MinArguments = 1, MaxArguments = 1)]
    private String FuncPlural(List<Node> nodes)
    {
        CheckArity(nodes);
        return Pluralizer.Pluralize(EvalAs<String>(nodes, 0));
    }

    [GeneratedRegex("[?.!]\\s+[a-z]")]
    private static partial Regex SentenceRegex();

    /// <summary>
    /// Converts the provided string to sentence case
    /// </summary>
    [Function("Sentence", "Sentence(string)", MinArguments = 1, MaxArguments = 1)]
    private String FuncSentence(List<Node> nodes)
    {
        CheckArity(nodes);

        var value = EvalAs<String>(nodes, 0);
        if (value == String.Empty) return value;
        var r = SentenceRegex();
        value = TextInfo.ToUpper(value[0]) + (value.Length > 1 ? value[1..] : "");
        value = r.Replace(value, m => m.Value.ToUpper());

        return value;
    }

    /// <summary>
    /// Converts the string to singluar
    /// </summary>
    [Function("Singular", "Singular(string)", MinArguments = 1, MaxArguments = 1)]
    private String FuncSingular(List<Node> nodes)
    {
        CheckArity(nodes);
        return Pluralizer.Singularize(EvalAs<String>(nodes, 0));
    }

    /// <summary>
    /// Converts the string to title case
    /// </summary>
    [Function("Title", "Title(string)", MinArguments = 1, MaxArguments = 1)]
    private String FuncTitle(List<Node> nodes)
    {
        CheckArity(nodes);
        var words = TextInfo.ToTitleCase(EvalAs<String>(nodes, 0)).Split(' ');
        for(var i = 1; i < words.Length; i++)
        {
            var word = words[i];
            if (word.In(StringComparer.InvariantCultureIgnoreCase,
                        "the", "a", "an",
                        "and", "or", "but",
                        "at", "by", "to", "of", "in", "on"))
                words[i] = word.ToLowerInvariant();
        }
        return String.Join(' ', words);
    }

    /// <summary>
    /// Converts the string to upper case
    /// </summary>
    [Function("Upper", "Upper(string)", MinArguments = 1, MaxArguments = 1)]
    private String FuncUpper(List<Node> nodes)
    {
        CheckArity(nodes);
        return TextInfo.ToUpper(EvalAs<String>(nodes, 0));
    }
    #endregion

    #endregion

    /// <summary>
    /// Attempts to retrieve the value of the named variable
    /// </summary>
    private Object? ReadVariable(String name)
    {
        return Variables.GetValue(name, null);
    }

    /// <summary>
    /// Attempts to get a list with the provided name
    /// </summary>
    private LineItemList ResolveList(String name)
    {
        if (!LineItems.TryGetValue(name, out var list))
            throw new KeyNotFoundException($"Item list '{name}' was not found.");

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
    private static String FormatVar(Object? v) => $"{v?.GetType()} ({v ?? "null"})";

    private static Boolean ValuesEqual(Object? left, Object? right)
    {
        if (left is Int32 li && right is Int32 ri) return li == ri;
        if (left is Boolean lb && right is Boolean rb) return lb == rb;
        if (left is String ls && right is String rs)
            return ls.Equals(rs, StringComparison.InvariantCultureIgnoreCase);
        return false;
    }
    #endregion
    #endregion

    #region Validation Helpers
    private static void CheckArity(List<Node> nodes, [CallerMemberName] String name = "")
    {
        var (min, max) = FunctionAttribute.GetArity(name);
        if (nodes.Count < min || nodes.Count > max)
            throw new AssignmentExpressionException(name, $"Expects {DescribeArityError(min, max)} arguments.");
    }

    private static String DescribeArityError(Int32 min, Int32 max)
    {
        if (min == 0 && max == 0)
            return "no";
        if (min == max)
            return $"exactly {min}";
        if (max == Int32.MaxValue)
            return $"at least {min}";
        return $"between {min} and {max}";
    }

    private Object? EvalArgument(String name, List<Node> nodes, Int32 index)
    {
        if (index >= nodes.Count)
            throw new AssignmentExpressionException(name, $"Missing argument {index + 1}.");
        return Evaluate(nodes[index]);
    }

    private T EvalAs<T>(List<Node> nodes, Int32 index, [CallerMemberName] String name = "")
    {
        name = FunctionAttribute.GetDefinition(name);
        return EvalAs<T>(nodes, index, false, name)!;
    }

    private T? EvalAs<T>(List<Node> nodes, Int32 index, Boolean allowNull, [CallerMemberName] String name = "")
    {
        var v = EvalArgument(name, nodes, index);
        if (v == null && allowNull) return default;
        if (v == null) throw new AssignmentExpressionException(name, $"Argument {index + 1} cannot be null");
        if (v is not T t)
            throw new AssignmentExpressionException(name, $"Argument {index + 1} expects a {typeof(T)} got {v.GetType()}");
        return t;
    }

    private IEnumerable<T> EvalAllAs<T>(List<Node> nodes, [CallerMemberName] String name = "")
    {
        return EvalRangeAs<T>(nodes, 0, nodes.Count - 1, name);            
    }

    private IEnumerable<T?> EvalAllAs<T>(List<Node> nodes, Boolean allowNull, [CallerMemberName] String name = "")
    {
        return EvalRangeAs<T>(nodes, 0, nodes.Count - 1, allowNull, name);
    }

    private IEnumerable<T> EvalRangeAs<T>(List<Node> nodes, Int32 start, Int32 end, [CallerMemberName] String name = "")
    {
        for (var i = start; i <= end; i++)
            yield return EvalAs<T>(nodes, i, name);
    }

    private IEnumerable<T?> EvalRangeAs<T>(List<Node> nodes, Int32 start, Int32 end, Boolean allowNull, [CallerMemberName] String name = "")
    {
        for(var i = start; i <= end; i++)
            yield return EvalAs<T>(nodes, i, allowNull, name);
    }
    #endregion
}
