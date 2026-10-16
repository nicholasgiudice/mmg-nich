using System.Numerics;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Areas;

/// <summary>Área persistente (poça, teia, armadilha). Entidade sem vida: <c>Transform</c> (centro) + este componente.</summary>
public sealed class AreaState : IComponent
{
    public string AreaId { get; init; } = "";
    public EntityId Owner { get; init; }

    /// <summary>Facção do dono quando a área nasceu (o dono pode morrer/sumir; null = sem dono/facção).</summary>
    public Faction? OwnerFaction { get; init; }
    public float Radius { get; init; }
    public AreaMode Mode { get; init; }
    public FactionFilter Affects { get; init; }
    public long CreatedTick { get; init; }

    /// <summary>A área expira no primeiro tick com <c>Tick >= ExpiresAtTick</c> (após aplicar, se for tick de aplicação).</summary>
    public long ExpiresAtTick { get; init; }

    /// <summary>Próxima aplicação periódica.</summary>
    public long NextApplyTick { get; set; }

    public IComponent Clone() => new AreaState
    {
        AreaId = AreaId,
        Owner = Owner,
        OwnerFaction = OwnerFaction,
        Radius = Radius,
        Mode = Mode,
        Affects = Affects,
        CreatedTick = CreatedTick,
        ExpiresAtTick = ExpiresAtTick,
        NextApplyTick = NextApplyTick,
    };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(AreaId);
        hasher.Add(Owner.Value);
        hasher.Add(OwnerFaction.HasValue ? (int)OwnerFaction.Value : -1);
        hasher.Add(Radius);
        hasher.Add((int)Mode);
        hasher.Add((int)Affects);
        hasher.Add(CreatedTick);
        hasher.Add(ExpiresAtTick);
        hasher.Add(NextApplyTick);
    }
}

public enum AreaExpireReason
{
    /// <summary>Acabou a duração.</summary>
    Duration,

    /// <summary>Armadilha (<c>on_enter_once</c>) disparada.</summary>
    Triggered,
}

public sealed record AreaSpawned(EntityId Area, string AreaId, EntityId Owner, Vector2 Position, float Radius, long ExpiresAtTick) : GameEvent;

/// <summary>Os efeitos da área foram aplicados a <paramref name="Targets"/> (só é publicado quando há alvos).</summary>
public sealed record AreaTriggered(EntityId Area, string AreaId, EntityId Owner, IReadOnlyList<EntityId> Targets) : GameEvent;

public sealed record AreaExpired(EntityId Area, string AreaId, AreaExpireReason Reason) : GameEvent;

/// <summary>Criação de áreas (efeito <c>spawn_area</c>).</summary>
public static class AreaService
{
    public static Entity Spawn(World world, AreaDefinition def, Entity? owner, Vector2 position)
    {
        var interval = def.Mode == AreaMode.Periodic ? Math.Max(1, def.IntervalTicks) : 0;
        var state = new AreaState
        {
            AreaId = def.Id,
            Owner = owner?.Id ?? EntityId.None,
            OwnerFaction = owner?.TryGet<FactionTag>()?.Faction,
            Radius = (float)def.Radius,
            Mode = def.Mode,
            Affects = def.Affects,
            CreatedTick = world.Tick,
            ExpiresAtTick = world.Tick + def.DurationTicks,
            NextApplyTick = world.Tick + interval,
        };
        var entity = world.Spawn(new Transform { Position = position }, state);
        world.Events.Publish(new AreaSpawned(entity.Id, def.Id, state.Owner, position, state.Radius, state.ExpiresAtTick));
        return entity;
    }

    /// <summary>Registro do efeito <c>spawn_area</c>: a área nasce no ponto mirado, senão na posição do primeiro alvo, senão na fonte.</summary>
    public static void Register(CombatServices s) =>
        s.Effects.Register<SpawnAreaEffect>(
            (c, e) =>
            {
                var def = c.World.Content.Areas[e.Area];
                var pos = c.AimPoint
                    ?? c.Targets.FirstOrDefault(t => t.IsAlive && t.Has<Transform>())?.Get<Transform>().Position
                    ?? c.Source?.TryGet<Transform>()?.Position;
                if (pos is { } p)
                {
                    Spawn(c.World, def, c.Source, p);
                }
            },
            EffectScope.PerCast);
}

/// <summary>
/// Aplica e expira áreas (ordem <see cref="SystemOrder.Areas"/>, antes de Status/Recursos/IA para que o dano gere recurso e ameaça no mesmo tick).
/// Periódica: aplica os efeitos a quem está dentro a cada intervalo (primeira aplicação em criação + intervalo, inclusive no tick final).
/// <c>on_enter_once</c>: dispara no primeiro tick em que algum afetado está dentro, aplica a todos os que estão dentro e some.
/// Alvos: entidades vivas com facção e vida cujo corpo toca o círculo.
/// </summary>
public sealed class AreaSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var entity in world.Entities)
        {
            if (entity.IsRemoved || entity.TryGet<AreaState>() is not { } area)
            {
                continue;
            }

            var def = world.Content.Areas[area.AreaId];
            var pos = entity.Get<Transform>().Position;
            var owner = world.Find(area.Owner);

            if (area.Mode == AreaMode.OnEnterOnce)
            {
                var inside = Inside(world, area, pos);
                if (inside.Count > 0)
                {
                    Trigger(world, entity, area, def, owner, inside, pos);
                    Expire(world, entity, area, AreaExpireReason.Triggered);
                    continue;
                }
            }
            else if (world.Tick >= area.NextApplyTick)
            {
                area.NextApplyTick += Math.Max(1, def.IntervalTicks);
                var inside = Inside(world, area, pos);
                if (inside.Count > 0)
                {
                    Trigger(world, entity, area, def, owner, inside, pos);
                }
            }

            if (world.Tick >= area.ExpiresAtTick)
            {
                Expire(world, entity, area, AreaExpireReason.Duration);
            }
        }
    }

    private static void Trigger(World world, Entity entity, AreaState area, AreaDefinition def, Entity? owner, IReadOnlyList<Entity> inside, Vector2 pos)
    {
        world.Events.Publish(new AreaTriggered(entity.Id, area.AreaId, area.Owner, inside.Select(e => e.Id).ToList()));
        EffectApplier.Apply(world, def.Effects, owner, inside, area.AreaId, null, pos, null);
    }

    private static void Expire(World world, Entity entity, AreaState area, AreaExpireReason reason)
    {
        world.Events.Publish(new AreaExpired(entity.Id, area.AreaId, reason));
        world.Remove(entity.Id);
    }

    private static IReadOnlyList<Entity> Inside(World world, AreaState area, Vector2 pos)
    {
        bool Filter(Entity e) =>
            e.TryGet<FactionTag>() is { } f && e.Has<Health>() && e.IsAlive && area.Affects switch
            {
                FactionFilter.Any => true,
                FactionFilter.Ally => area.OwnerFaction == f.Faction,
                _ => area.OwnerFaction is { } of && of != f.Faction,
            };

        return world.Spatial.InRadius(pos, area.Radius, Filter);
    }
}
