using Game.Core.Combat.Ai;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Threat;

/// <summary>Operações sobre tabelas de ameaça (usadas pelo efeito <c>threat</c> e pelo <see cref="AiSystem"/>).</summary>
public static class ThreatService
{
    /// <summary>Adiciona ameaça de <paramref name="source"/> na tabela de <paramref name="monster"/>. Ignora inimigos inválidos (mortos, mesma facção, não-IA).</summary>
    public static void Add(World world, Entity monster, Entity source, double amount, AggroReason reason, bool pullGroup = true)
    {
        if (!monster.IsAlive || !source.IsAlive || monster.Id == source.Id || SameFaction(monster, source))
        {
            return;
        }

        var ai = AiState.Ensure(world, monster);
        var table = monster.TryGet<ThreatTable>();
        if (ai is null || table is null)
        {
            return;
        }

        var wasEngaged = table.Entries.Count > 0;
        var isNew = !table.Entries.TryGetValue(source.Id.Value, out var current);
        table.Entries[source.Id.Value] = (float)Math.Max(0, current + amount);
        if (isNew)
        {
            world.Events.Publish(new AggroGained(monster.Id, source.Id, reason));
        }

        if (!wasEngaged && pullGroup)
        {
            PullGroup(world, monster, ai, source);
        }
    }

    private static void PullGroup(World world, Entity monster, AiState ai, Entity source)
    {
        var rules = world.Content.Threat;
        foreach (var other in world.Entities)
        {
            if (other.Id == monster.Id || !other.IsAlive || AiState.Ensure(world, other) is not { } oai || oai.GroupId != ai.GroupId)
            {
                continue;
            }

            if (other.TryGet<ThreatTable>() is { Entries.Count: 0 })
            {
                Add(world, other, source, rules.GroupAggroThreat, AggroReason.Group, pullGroup: false);
            }
        }
    }

    /// <summary>Provoca: força o alvo do monstro por <paramref name="durationTicks"/> ticks.</summary>
    public static void Taunt(World world, Entity monster, Entity source, int durationTicks)
    {
        if (durationTicks <= 0 || !monster.IsAlive || !source.IsAlive || SameFaction(monster, source))
        {
            return;
        }

        Add(world, monster, source, 0, AggroReason.Taunt);
        var table = monster.TryGet<ThreatTable>();
        if (table is null)
        {
            return;
        }

        table.TauntTarget = source.Id;
        table.TauntUntilTick = world.Tick + durationTicks;
        world.Events.Publish(new Taunted(source.Id, monster.Id, table.TauntUntilTick));
    }

    /// <summary>Remove entradas de entidades mortas/removidas e provocação inválida/expirada.</summary>
    public static void Prune(World world, ThreatTable table)
    {
        List<int>? remove = null;
        foreach (var id in table.Entries.Keys)
        {
            var e = world.Find(new EntityId(id));
            if (e is null || !e.IsAlive)
            {
                (remove ??= []).Add(id);
            }
        }

        if (remove is not null)
        {
            foreach (var id in remove)
            {
                table.Entries.Remove(id);
            }
        }

        if (!table.TauntTarget.IsNone)
        {
            var t = world.Find(table.TauntTarget);
            if (world.Tick >= table.TauntUntilTick || t is null || !t.IsAlive)
            {
                table.TauntTarget = EntityId.None;
                table.TauntUntilTick = 0;
            }
        }
    }

    /// <summary>
    /// Escolha pela tabela: maior ameaça; o alvo atual só é trocado se outro tiver mais que
    /// <c>atual x OvertakeRatio</c>. Vazio = null.
    /// </summary>
    public static EntityId? SelectFromTable(ThreatTable table, EntityId current, double overtakeRatio)
    {
        if (table.Entries.Count == 0)
        {
            return null;
        }

        var best = 0;
        var bestValue = float.NegativeInfinity;
        foreach (var kv in table.Entries)
        {
            if (kv.Value > bestValue)
            {
                best = kv.Key;
                bestValue = kv.Value;
            }
        }

        if (!current.IsNone && current.Value != best && table.Entries.TryGetValue(current.Value, out var cur)
            && bestValue <= cur * overtakeRatio)
        {
            return current;
        }

        return new EntityId(best);
    }

    private static bool SameFaction(Entity a, Entity b) =>
        a.TryGet<FactionTag>()?.Faction == b.TryGet<FactionTag>()?.Faction;
}
