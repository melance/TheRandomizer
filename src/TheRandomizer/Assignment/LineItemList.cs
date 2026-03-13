using LB.Utility.Random;
using System.Security.Cryptography;
using TheRandomizer.Utility;

namespace TheRandomizer.Assignment;

public class LineItemList : List<LineItem>
{
    private LineItemList? _deck;

    /// <summary>
    /// Treat this list like a card deck, removing entries after they are chosen
    /// </summary>
    public Boolean IsDeck { get; set; }
    public String Variable { get; set; } = String.Empty;
    public UInt32 TotalWeight => (UInt32)this.Sum(i => i.Weight);

    public LineItem SelectRandomLineItem()
    {
        if (IsDeck) return DrawRandomItem();
        if (Count == 1) return this.First();

        var value = PseudoRNG.Instance?.NextUInt32(1, TotalWeight);
        var sum = 0u;

        foreach(var item in this)
        {
            sum += item.Weight;
            if (sum >= value) return item;
        }

        return new();
    }
        
    public LineItem DrawRandomItem()
    {
        _deck ??= [.. this];

        if (_deck.Count == 1)
        {
            var item = _deck.First();
            _deck.Clear();
            return item;
        }
        else
        {
            var value = PseudoRNG.Instance?.NextUInt32(1, _deck.TotalWeight);
            var sum = 0u;
            for (Int32 i = 0; i < _deck.Count; i++)
            {
                var item = _deck[i];
                sum += item.Weight;
                if (sum >= value)
                {
                    _deck.Remove(item);
                    return item;
                }
            }
        }
        return new();
    }
}

