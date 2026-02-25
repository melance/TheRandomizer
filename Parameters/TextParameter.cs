using LB.Utility.Extensions;
using System.Text.RegularExpressions;

namespace TheRandomizer.Parameters;

public class TextParameter : BaseParameter
{
    public Int32? MinLength { get; set; }
    public Int32? MaxLength { get; set; }
    public String? Pattern { get; set; }

    public override Boolean HasValue => Value?.ToString().HasValue() == true;

    protected override void Validate()
    {
        var s = Value?.ToString();
        if (s is null) return;
        if (MinLength != null && s.Length < MinLength)
            ErrorList.Add($"Parameter {Name} must be more than {MinLength} characters.");
        if (MaxLength != null && s.Length > MaxLength)
            ErrorList.Add($"Parameter {Name} must be less than {MaxLength} characters.");
        if (!String.IsNullOrWhiteSpace(Pattern) && Regex.IsMatch(s, Pattern))
            ErrorList.Add($"Parameter {Name} failed pattern validation.");
    }
}