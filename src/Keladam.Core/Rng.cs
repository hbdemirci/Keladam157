namespace Keladam.Core;

/// <summary>Tohumlu, platformdan bağımsız rastgele sayı üreteci (SplitMix64).</summary>
public sealed class Rng
{
    private ulong _state;

    public Rng(ulong seed) => _state = seed;

    public ulong State => _state;

    public ulong NextULong()
    {
        ulong z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>[min, max) aralığında.</summary>
    public int Next(int min, int max)
    {
        if (max <= min) return min;
        return min + (int)(NextULong() % (ulong)(max - min));
    }

    /// <summary>1000 üzerinden olasılık.</summary>
    public bool Chance(int permille) => Next(0, 1000) < permille;
}
