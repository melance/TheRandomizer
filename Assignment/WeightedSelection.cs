using TheRandomizer.Helpers;

namespace TheRandomizer.Assignment;

internal static class WeightedSelection
{
    public static LineItem Select(WeightedPool pool)
    {
        if (pool.Sources.Count == 0)
            throw new InvalidOperationException("Cannot select from an empty pool");

        var total = 0UL;
        foreach (var source in pool.Sources)
            total += EffectiveWeight(source.List, source.Multiplier);

        if (total == 0)
            throw new InvalidOperationException("Total weight is zero.");

        var roll = (UInt64)RandomGen.NextInt64((Int64)total);
        var sum = 0UL;

        foreach (var source in pool.Sources)
        {
            var weight = EffectiveWeight(source.List, source.Multiplier);

            if (roll < sum + weight)
            {
                return source.List.IsDeck
                        ? source.List.DrawRandomItem()
                        : SelectFromList(source.List, source.Multiplier, roll);
            }

            sum += weight;
        }
        return pool.Sources.Last().List.SelectRandomLineItem();
    }

    private static UInt64 EffectiveWeight(LineItemList list, UInt32 multiplier)
    {
        var total = 0UL;
        foreach(LineItem item in list) 
            total += item.Weight * multiplier;
        return total;
    }

    private static LineItem SelectFromList(LineItemList list, UInt32 mult, UInt64 roll)
    {
        var sum = 0UL;

        foreach (var item in list)
        {
            sum += (UInt64)item.Weight * mult;
            if (roll < sum)
                return item;
        }

        return list.Last();
    }
}

