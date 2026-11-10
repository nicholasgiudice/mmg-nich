using Game.Core.Definitions;

namespace Game.Server.Zones;

/// <summary>
/// Uma cópia de uma zona do tipo <see cref="ZoneKind.Instance"/> (T-350), criada sob demanda pelo <see cref="WorldServer"/>
/// quando alguém entra pelo portão. É dona de um grupo (<see cref="Owner"/>): <c>party:&lt;id&gt;</c> ou, sem grupo,
/// <c>solo:&lt;personagem&gt;</c>. Tem <see cref="OpenZone"/> próprio (mundo com seed derivada do id), que só avança com
/// jogador dentro, como as zonas abertas; os monstros não renascem.
/// </summary>
public sealed class DungeonInstance
{
    internal DungeonInstance(int id, OpenZone zone, string owner, long createdAt)
    {
        Id = id;
        Zone = zone;
        Owner = owner;
        CreatedAt = createdAt;
    }

    /// <summary>Id da cópia no processo (≥ 1; vai no <c>Welcome</c>).</summary>
    public int Id { get; }

    public OpenZone Zone { get; }

    public ZoneDefinition Definition => Zone.Definition;

    /// <summary>Grupo dono: <c>party:&lt;id&gt;</c> ou <c>solo:&lt;personagem&gt;</c>.</summary>
    public string Owner { get; internal set; }

    /// <summary>Tick do servidor em que a cópia foi criada.</summary>
    public long CreatedAt { get; }

    /// <summary>Tick do servidor em que a cópia ficou vazia; null = tem alguém dentro.</summary>
    public long? EmptySince { get; internal set; }

    /// <summary>
    /// Seed do mundo da cópia: derivada da seed do servidor e do id da instância (SplitMix64), para cópias diferentes não
    /// repetirem a mesma sequência e a mesma cópia ser reproduzível.
    /// </summary>
    public static ulong SeedFor(ulong serverSeed, int instanceId)
    {
        var z = serverSeed + (0x9E3779B97F4A7C15UL * (ulong)(uint)instanceId);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
