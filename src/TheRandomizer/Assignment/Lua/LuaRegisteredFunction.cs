using MoonSharp.Interpreter;

namespace TheRandomizer.Assignment.Lua;

internal sealed class LuaRegisteredFunction
{
    public required String Name { get; init; }
    public required String SourceFile { get; init; }
    public required DynValue Callback {  get; init; }
}

