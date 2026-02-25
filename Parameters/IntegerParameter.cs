namespace TheRandomizer.Parameters;

public class IntegerParameter : BaseParameter
{
    public Int32? Min { get; set; }
    public Int32? Max { get; set; }

    protected override void Validate()
    {
        if (Int32.TryParse(Value?.ToString(), out var i))
        {
            if (i < Min)
                ErrorList.Add($"Parameter {Name} must be greater than or equal to {Min}");
            if (i > Max)
                ErrorList.Add($"Parameter {Name} must be less than or equal to {Max}");
        }
    }
}

