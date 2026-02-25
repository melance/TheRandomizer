using LB.Utility.Extensions;

namespace TheRandomizer.Parameters;

public class Option(String value, String? display)
{
    public String Value { get; } = value;
    public String Display { get; } = display ?? value;
    public override String ToString() => Display;
}

public class SelectParameter : BaseParameter
{
    public List<Option> Options { get; set; } = [];

    public override Boolean HasValue => Value?.ToString().HasValue() == true;

    protected override void Validate()
    {
        var s = Value?.ToString();
        if (s == null) return;
        if (!Options.Where(o => o.Value != null).Any(o => o.Value == s))
            ErrorList.Add($"Select parameter {Name} does not contain the option provided '{s}'.");
    }
}

