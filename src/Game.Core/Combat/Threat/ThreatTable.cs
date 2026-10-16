using Game.Core.Simulation;

namespace Game.Core.Combat.Threat;

/// <summary>Tabela de ameaça de um monstro (chave = id da entidade; ordenada para ser determinística) e provocação ativa.</summary>
public sealed class ThreatTable : IComponent
{
    public SortedDictionary<int, float> Entries { get; } = new();

    /// <summary>Alvo forçado pela provocação (None = sem provocação).</summary>
    public EntityId TauntTarget { get; set; }

    public long TauntUntilTick { get; set; }

    public IComponent Clone()
    {
        var c = new ThreatTable { TauntTarget = TauntTarget, TauntUntilTick = TauntUntilTick };
        foreach (var kv in Entries)
        {
            c.Entries[kv.Key] = kv.Value;
        }

        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(TauntTarget.Value);
        hasher.Add(TauntUntilTick);
        foreach (var kv in Entries)
        {
            hasher.Add(kv.Key);
            hasher.Add(kv.Value);
        }
    }
}
