namespace Game.Core.Simulation;

/// <summary>PCG32 (XSH-RR) com seed. Único gerador permitido no Core; vive em <see cref="World.Random"/>.</summary>
public sealed class SimRandom
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong Increment = 1442695040888963407UL;
    private ulong _state;

    public SimRandom(ulong seed)
    {
        _state = 0;
        NextUInt();
        _state += seed;
        NextUInt();
    }

    internal ulong State => _state;

    public uint NextUInt()
    {
        var old = _state;
        _state = unchecked((old * Multiplier) + Increment);
        var xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        var rot = (int)(old >> 59);
        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    /// <summary>Inteiro em [0, maxExclusive), sem viés (rejeição).</summary>
    public int NextInt(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        var bound = (uint)maxExclusive;
        var threshold = (uint)(-(int)bound) % bound;
        while (true)
        {
            var r = NextUInt();
            if (r >= threshold)
            {
                return (int)(r % bound);
            }
        }
    }

    /// <summary>Float em [0, 1).</summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / (1 << 24));
}
