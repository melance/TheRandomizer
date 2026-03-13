namespace TheRandomizer.Table;

internal class TableRange
{
    public TableRange() { }
    public TableRange(String range)
    {
        if (String.IsNullOrWhiteSpace(range)) throw new ArgumentException("Range must have a value.");

        var parts = range.Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            if (!Int32.TryParse(parts[0], out var i))
                throw new FormatException($"Invalid range '{range}'");
            Min = i;
            Max = i;
        }
        else if (parts.Length == 2)
        {
            if (!Int32.TryParse(parts[0], out var min) ||
                !Int32.TryParse(parts[1], out var max))
                throw new FormatException($"Invalid range '{range}");
            if (min > max) throw new FormatException($"Range minimum cannot be greater than maximum '{range}");

            Min = min;
            Max = max;
        }
        else
            throw new FormatException($"Invalid range '{range}");
        
    }

    public Int32 Min { get; set; }
    public Int32 Max { get; set; }

    public Boolean Overlaps(TableRange other) => Min <= other.Max && Max >= other.Min;
    public Boolean Contains(Int32 value) => Min <= value && Max >= value;

    public override String ToString() => Min == Max ? Min.ToString() : $"{Min} - {Max}";

    public static explicit operator TableRange(String range)
    {
        return new TableRange(range);
    }
}

