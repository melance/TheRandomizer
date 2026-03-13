using System.Reflection;
using System.Runtime.CompilerServices;

namespace TheRandomizer.Assignment;

[AttributeUsage(AttributeTargets.Method)]
internal class FunctionAttribute(String name, String definition) : Attribute
{
    public static String GetName([CallerMemberName] String methodName = "")
    {
        var attr = GetFunction(methodName);
        if (attr == null) return methodName.Replace("func", "");
        return attr.Name;
    }

    public static String GetDefinition([CallerMemberName] String methodName = "")
    {
        var attr = GetFunction(methodName);
        if (attr == null) return $"{methodName.Replace("func", "")}()";
        return attr.Definition;
    }

    public static FunctionAttribute? GetFunction([CallerMemberName] String methodName = "")
    {
        return typeof(AssignmentGenerator).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetCustomAttribute<FunctionAttribute>();
    }
        
    public static (Int32 Min, Int32 Max) GetArity([CallerMemberName] String methodName = "")
    {
        var attr = GetFunction(methodName);
        if (attr == null) return (0, 0);
        return (attr.MinArguments, attr.MaxArguments);
    }

    public String Name { get; set; } = name;
    public String Definition { get; set; } = definition;
    public String Description { get; set; } = "";
    public Int32 MinArguments { get; set; } = 0;
    public Int32 MaxArguments { get; set; } = Int32.MaxValue;
}

