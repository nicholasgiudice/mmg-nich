using System.Buffers;
using System.Numerics;
using Game.Core.Simulation;
using MemoryPack;

namespace Game.Protocol;

/// <summary>
/// Primitivas de escrita/leitura dos tipos do Core (T-103). Inteiros e ids vão em varint (quase sempre pequenos);
/// floats e vetores em 4 bytes por número; strings no formato do MemoryPack.
/// </summary>
internal static class Wire
{
    // ---------- escrita ----------

    public static void I<TB>(ref this MemoryPackWriter<TB> w, int v)
        where TB : IBufferWriter<byte> => w.WriteVarInt(v);

    public static void L<TB>(ref this MemoryPackWriter<TB> w, long v)
        where TB : IBufferWriter<byte> => w.WriteVarInt(v);

    public static void F<TB>(ref this MemoryPackWriter<TB> w, float v)
        where TB : IBufferWriter<byte> => w.WriteUnmanaged(v);

    public static void D<TB>(ref this MemoryPackWriter<TB> w, double v)
        where TB : IBufferWriter<byte> => w.WriteUnmanaged(v);

    public static void B<TB>(ref this MemoryPackWriter<TB> w, bool v)
        where TB : IBufferWriter<byte> => w.WriteUnmanaged(v);

    public static void E<TB, TEnum>(ref this MemoryPackWriter<TB> w, TEnum v)
        where TB : IBufferWriter<byte>
        where TEnum : unmanaged, Enum => w.WriteVarInt(Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture));

    public static void S<TB>(ref this MemoryPackWriter<TB> w, string? v)
        where TB : IBufferWriter<byte> => w.WriteString(v);

    public static void Id<TB>(ref this MemoryPackWriter<TB> w, EntityId id)
        where TB : IBufferWriter<byte> => w.WriteVarInt(id.Value);

    /// <summary>Id opcional: -1 = ausente (ids válidos são ≥ 0).</summary>
    public static void IdOpt<TB>(ref this MemoryPackWriter<TB> w, EntityId? id)
        where TB : IBufferWriter<byte> => w.WriteVarInt(id?.Value ?? -1);

    public static void V<TB>(ref this MemoryPackWriter<TB> w, Vector2 v)
        where TB : IBufferWriter<byte> => w.WriteUnmanaged(v.X, v.Y);

    public static void VOpt<TB>(ref this MemoryPackWriter<TB> w, Vector2? v)
        where TB : IBufferWriter<byte>
    {
        w.B(v.HasValue);
        if (v is { } value)
        {
            w.V(value);
        }
    }

    public static void IOpt<TB>(ref this MemoryPackWriter<TB> w, int? v)
        where TB : IBufferWriter<byte>
    {
        w.B(v.HasValue);
        if (v is { } value)
        {
            w.I(value);
        }
    }

    public static void Ids<TB>(ref this MemoryPackWriter<TB> w, IReadOnlyList<EntityId> ids)
        where TB : IBufferWriter<byte>
    {
        w.I(ids.Count);
        foreach (var id in ids)
        {
            w.Id(id);
        }
    }

    public static void Strs<TB>(ref this MemoryPackWriter<TB> w, IReadOnlyList<string> list)
        where TB : IBufferWriter<byte>
    {
        w.I(list.Count);
        foreach (var s in list)
        {
            w.S(s);
        }
    }

    // ---------- leitura ----------

    public static int I(ref this MemoryPackReader r) => r.ReadVarIntInt32();

    public static long L(ref this MemoryPackReader r) => r.ReadVarIntInt64();

    public static float F(ref this MemoryPackReader r) => r.ReadUnmanaged<float>();

    public static double D(ref this MemoryPackReader r) => r.ReadUnmanaged<double>();

    public static bool B(ref this MemoryPackReader r) => r.ReadUnmanaged<bool>();

    public static TEnum E<TEnum>(ref this MemoryPackReader r)
        where TEnum : unmanaged, Enum
    {
        var raw = r.ReadVarIntInt32();
        var value = (TEnum)Enum.ToObject(typeof(TEnum), raw);
        return Enum.IsDefined(value) ? value : throw new MemoryPackSerializationException($"{typeof(TEnum).Name} inválido: {raw}");
    }

    public static string S(ref this MemoryPackReader r) => r.ReadString() ?? "";

    public static string? SOpt(ref this MemoryPackReader r) => r.ReadString();

    public static EntityId Id(ref this MemoryPackReader r) => new(r.ReadVarIntInt32());

    public static EntityId? IdOpt(ref this MemoryPackReader r)
    {
        var v = r.ReadVarIntInt32();
        return v < 0 ? null : new EntityId(v);
    }

    public static Vector2 V(ref this MemoryPackReader r) => new(r.ReadUnmanaged<float>(), r.ReadUnmanaged<float>());

    public static Vector2? VOpt(ref this MemoryPackReader r) => r.B() ? r.V() : null;

    public static int? IOpt(ref this MemoryPackReader r) => r.B() ? r.I() : null;

    public static EntityId[] Ids(ref this MemoryPackReader r)
    {
        var n = Count(ref r);
        var ids = new EntityId[n];
        for (var i = 0; i < n; i++)
        {
            ids[i] = r.Id();
        }

        return ids;
    }

    public static string[] Strs(ref this MemoryPackReader r)
    {
        var n = Count(ref r);
        var list = new string[n];
        for (var i = 0; i < n; i++)
        {
            list[i] = r.S();
        }

        return list;
    }

    /// <summary>Tamanho de lista lido da rede, limitado (mensagem corrompida ou maliciosa não aloca à toa).</summary>
    public static int Count(ref MemoryPackReader r)
    {
        var n = r.ReadVarIntInt32();
        return n is >= 0 and <= MaxListLength ? n : throw new MemoryPackSerializationException($"Lista com tamanho inválido: {n}");
    }

    public const int MaxListLength = 4096;
}
