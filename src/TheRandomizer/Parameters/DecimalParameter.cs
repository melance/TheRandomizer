namespace TheRandomizer.Parameters;

public class DecimalParameter : BaseParameter
{
    public Decimal? Min { get; set; }
    public Decimal? Max { get; set; }

    protected override void Validate()
    {
        if (!Decimal.TryParse(Value?.ToString(), out var d))
        {
            if (d < Min)
                ErrorList.Add($"Parameter {Name} must be greater than or equal to {Min}");
            if (d > Max)
                ErrorList.Add($"Parameter {Name} must be less than or equal to {Max}");
        }
    }
}

