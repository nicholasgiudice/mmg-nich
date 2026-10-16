using System.Numerics;
using System.Text;

namespace Game.Core.Simulation;

/// <summary>FNV-1a 64 bits sobre os bytes do estado. Só para testes de determinismo (não criptográfico).</summary>
public sealed class StateHasher
{
    private const ulong Prime = 1099511628211UL;
    private ulong _hash = 14695981039346656037UL;

    public ulong Value => _hash;

    public void Add(ulong v)
    {
        for (var i = 0; i < 8; i++)
        {
            _hash ^= (byte)(v >> (i * 8));
            _hash *= Prime;
        }
    }

    public void Add(long v) => Add((ulong)v);

    public void Add(int v) => Add((ulong)(uint)v);

    public void Add(bool v) => Add(v ? 1 : 0);

    /// <summary>Normaliza -0 para 0.</summary>
    public void Add(float v) => Add(BitConverter.SingleToInt32Bits(v == 0f ? 0f : v));

    public void Add(Vector2 v)
    {
        Add(v.X);
        Add(v.Y);
    }

    public void Add(string s)
    {
        Add(s.Length);
        foreach (var b in Encoding.UTF8.GetBytes(s))
        {
            _hash ^= b;
            _hash *= Prime;
        }
    }
}
