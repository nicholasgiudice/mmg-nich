using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Objectives;

/// <summary>
/// Objetivo a proteger de um encontro (T-351: os selos do Vigia). <see cref="Destroyed"/> marca que a destruição já foi
/// publicada (<see cref="ObjectiveDestroyed"/>), para cada objetivo contar uma vez só.
/// </summary>
public sealed class EncounterObjective : IComponent, IReplicated
{
    public bool Destroyed { get; set; }

    public IComponent Clone() => new EncounterObjective { Destroyed = Destroyed };

    public void WriteHash(StateHasher hasher) => hasher.Add(Destroyed ? 1 : 0);
}

/// <summary>O objetivo <paramref name="Entity"/> (definição <paramref name="MonsterId"/>) foi destruído; restam <paramref name="Remaining"/>.</summary>
public sealed record ObjectiveDestroyed(EntityId Entity, string MonsterId, int Remaining) : GameEvent;

public enum EncounterFailReason
{
    /// <summary>Todos os objetivos do encontro foram destruídos (T-351: os três selos do Vigia).</summary>
    ObjectivesDestroyed,
}

/// <summary>O encontro terminou em falha (T-351), mesmo com jogadores vivos. Publicado uma vez.</summary>
public sealed record EncounterFailed(EncounterFailReason Reason) : GameEvent;

public static class ObjectiveService
{
    /// <summary>Faz nascer os objetivos do encontro (facção dos jogadores, sem IA). Nível: o do objetivo, o do encontro ou o da definição.</summary>
    public static List<Entity> SpawnAll(World world, EncounterDefinition encounter)
    {
        var list = new List<Entity>();
        foreach (var o in encounter.Objectives)
        {
            var def = world.Content.Monsters[o.Monster];
            list.Add(Spawn(world, def, new Vector2((float)o.Position.X, (float)o.Position.Y), o.Level ?? encounter.MonsterLevel));
        }

        return list;
    }

    /// <summary>Um objetivo: a unidade da definição de monstro, mas aliada dos jogadores e marcada com <see cref="EncounterObjective"/>.</summary>
    public static Entity Spawn(World world, MonsterDefinition def, Vector2 position, int? level = null)
    {
        var e = EntityFactory.SpawnMonster(world, def, position, level, Faction.Player);
        e.Add(new EncounterObjective());
        return e;
    }

    public static bool IsObjective(Entity e) => e.Has<EncounterObjective>();

    /// <summary>Objetivos vivos (ordem de id).</summary>
    public static IEnumerable<Entity> Alive(World world) => world.Entities.Where(e => e.IsAlive && IsObjective(e));
}

/// <summary>
/// Depois das mortes do tick (ordem <see cref="SystemOrder.Objectives"/>): publica <see cref="ObjectiveDestroyed"/> para cada
/// objetivo que morreu e, quando o último cai, <see cref="EncounterFailed"/>. Sem objetivos no mundo, não faz nada.
/// </summary>
public sealed class ObjectiveSystem : ISimulationSystem
{
    public void Update(World world)
    {
        var any = false;
        var changed = false;
        var remaining = 0;
        foreach (var e in world.Entities)
        {
            if (!e.Has<EncounterObjective>())
            {
                continue;
            }

            any = true;
            if (e.IsAlive)
            {
                remaining++;
            }
        }

        if (!any)
        {
            return;
        }

        foreach (var e in world.Entities)
        {
            if (e.TryGet<EncounterObjective>() is not { Destroyed: false } o || e.IsAlive)
            {
                continue;
            }

            o.Destroyed = true;
            changed = true;
            world.Events.Publish(new ObjectiveDestroyed(e.Id, e.TryGet<UnitInfo>()?.DefinitionId ?? "", remaining));
        }

        if (changed && remaining == 0)
        {
            world.Events.Publish(new EncounterFailed(EncounterFailReason.ObjectivesDestroyed));
        }
    }
}
