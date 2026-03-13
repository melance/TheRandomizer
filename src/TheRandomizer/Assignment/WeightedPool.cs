namespace TheRandomizer.Assignment;

internal record class WeightedList(LineItemList List, UInt32 Multiplier);

internal sealed class WeightedPool
{
    public readonly List<WeightedList> Sources = [];

    public WeightedPool() { }
    public WeightedPool(LineItemList list, UInt32 multiplier = 1)
    {
        Sources.Add(new WeightedList(list, multiplier));
    }

    public WeightedPool Add(WeightedPool other)
    {
        Sources.AddRange(other.Sources);
        return this;
    }

    public WeightedPool Multiply(UInt32 factor)
    {
        if (factor == 0)
            throw new InvalidCastException("Weight must be greater than zero");

        for (Int32 i = 0; i < Sources.Count; i++)
        {
            var list = Sources[i];
            Sources[i] = new WeightedList(list.List, list.Multiplier * factor);
        }

        return this;
    }
}

