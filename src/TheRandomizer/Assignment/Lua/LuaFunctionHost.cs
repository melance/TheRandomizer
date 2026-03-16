using LB.Utility.Extensions;
using MoonSharp.Interpreter;

namespace TheRandomizer.Assignment.Lua;

internal sealed class LuaFunctionHost
{
    #region Members
    public Script Script { get; }
    private readonly Dictionary<String, LuaRegisteredFunction> _functions = new(StringComparer.OrdinalIgnoreCase);
    private String? _currentFile; 
    #endregion

    #region Public Methods
    public LuaFunctionHost()
    {
        Script = new Script();
        Script.Globals["Register"] = (Action<String, DynValue>)Register;
        Script.Globals["RegisterAll"] = (Action<DynValue>)RegisterAll;
    }

    public void LoadFile(String path)
    {
        var code = File.ReadAllText(path);
        _currentFile = path;
        Script.DoString(code);
        _currentFile = null;
    }

    public Boolean TryInvoke(String name, IReadOnlyList<Object?> arguments, out Object? result)
    {
        if (!_functions.TryGetValue(name, out var function))
        {
            result = null;
            return false;
        }

        result = Invoke(function, arguments);
        return true;
    }
    #endregion

    #region Private Methods
    private void Register(String name, DynValue fn)
    {
        if (String.IsNullOrWhiteSpace(name))
            throw new AssignmentLuaException("Lua function name cannot be empty.");

        if (fn.Type != DataType.Function)
            throw new AssignmentLuaException($"Register('{name}', ...) requires a Lua function.");

        if (_functions.ContainsKey(name))
            throw new AssignmentLuaException($"A Lua function named '{name}' is already registered.");

        _functions[name] = new()
        {
            Name = name,
            SourceFile = _currentFile ?? "<unknown>",
            Callback = fn
        };
    }

    private void RegisterAll(DynValue prefixValue)
    {
        String? prefix;
        if (prefixValue.Type == DataType.String)
            prefix = prefixValue.String;
        else
            prefix = Path.GetFileNameWithoutExtension(_currentFile);

        var exports = new List<(String Name, DynValue Function)>();

        foreach(var pair in Script.Globals.Pairs)
        {
            if (pair.Key.Type == DataType.String && pair.Value.Type == DataType.Function)
            {
                var localName = pair.Key.String;

                if (IsValidName(localName))
                {
                    var exportedName = prefix.IsNullOrWhitespace()
                                        ? localName
                                        : $"{prefix}.{localName}";

                    exports.Add((exportedName, pair.Value));
                }
            }
        }

        foreach (var (Name, Function) in exports)
            Register(Name, Function);
    }

    private static Boolean IsValidName(String name)
    {
        return !(name.IsNullOrWhitespace() 
                    || name.StartsWith('_')
                    || name.Equals("Register", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("RegisterAll", StringComparison.OrdinalIgnoreCase));
    }

    private Object? Invoke(LuaRegisteredFunction function, IReadOnlyList<Object?> arguments)
    {
        var luaArguments = arguments.Select(ToLuaValue).ToArray();
        var result = Script.Call(function.Callback, luaArguments);
        return FromLuaValue(result);
    } 

    private static DynValue ToLuaValue(Object? value) => 
        value switch
        {
            null => DynValue.Nil,
            String s => DynValue.NewString(s),
            Boolean b => DynValue.NewBoolean(b),
            Int32 i => DynValue.NewNumber(i),
            Int64 l => DynValue.NewNumber(l),
            Single s => DynValue.NewNumber(s),
            Double d => DynValue.NewNumber(d),
            Decimal m => DynValue.NewNumber((Double)m),
            _ => DynValue.NewString(value.ToString() ?? string.Empty),
        };

    private static Object? FromLuaValue(DynValue value) =>
        value.Type switch
        {
            DataType.Nil => null,
            DataType.Void => null,
            DataType.Boolean => value.Boolean,
            DataType.Number => value.Number,
            DataType.String => value.String,
            _ => value.ToString()
        };
    #endregion
}

