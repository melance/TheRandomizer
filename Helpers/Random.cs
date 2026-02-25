namespace TheRandomizer.Helpers;

public class RandomGen
{
    private Int32? _seed;

    private Random Random { get; set; }
    public Int32? Seed { 
        get => _seed;
        set 
        {
            _seed = value;
            if (_seed.HasValue)
                Random = new Random(_seed.Value);
            else
                Random = new Random();
        }
    }

    public RandomGen()
    {
        Random = new Random(); 
    }

    public RandomGen(Int32 seed) : this()
    {
        _seed = seed;
        Random = new(seed);
    }

    public Int32 NextInt32(Int32 maxExclusive) => Random.Next(maxExclusive);
    public Int32 NextInt32(Int32 minInclusive, Int32 maxExclusive) => Random.Next(minInclusive, maxExclusive);
    public Int64 NextInt64(Int64 maxExclusive) => Random.NextInt64(maxExclusive);
}

