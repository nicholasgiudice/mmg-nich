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

    /// <summary>Tick em que a área fica armada (T-017); antes disso não dispara. Igual a CreatedTick = armada desde o início.</summary>
    public long ArmedAtTick { get; init; }

    /// <summary>Já publicou <see cref="AreaArmed"/> (só áreas com tempo de armar).</summary>
    public bool ArmedAnnounced { get; set; }

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
        ArmedAtTick = ArmedAtTick,
        ArmedAnnounced = ArmedAnnounced,
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
        hasher.Add(ArmedAtTick);
        hasher.Add(ArmedAnnounced);
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

/// <summary>ArmedAtTick (T-017): tick em que a área passa a disparar (= tick de criação se não há tempo de armar).</summary>
public sealed record AreaSpawned(
    EntityId Area, string AreaId, EntityId Owner, Vector2 Position, float Radius, long ExpiresAtTick, long ArmedAtTick = 0) : GameEvent;

/// <summary>A área terminou de armar e passa a disparar (T-017). Só é publicado para áreas com <c>armTime</c> &gt; 0.</summary>
public sealed record AreaArmed(EntityId Area, string AreaId, EntityId Owner) : GameEvent;

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
            ArmedAtTick = world.Tick + def.ArmTimeTicks,
            ArmedAnnounced = def.ArmTimeTicks <= 0,
            ExpiresAtTick = world.Tick + def.DurationTicks,
            NextApplyTick = world.Tick + interval,
        };
        var entity = world.Spawn(new Transform { Position = position }, state);
        world.Events.Publish(new AreaSpawned(entity.Id, def.Id, state.Owner, position, state.Radius, state.ExpiresAtTick, state.ArmedAtTick));
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
/// Áreas com <c>armTime</c> só disparam depois de armadas (evento <see cref="AreaArmed"/>); a primeira aplicação periódica só ocorre depois de armada.
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

            var armed = world.Tick >= area.ArmedAtTick;
            if (armed && !area.ArmedAnnounced)
            {
                area.ArmedAnnounced = true;
                world.Events.Publish(new AreaArmed(entity.Id, area.AreaId, area.Owner));
            }

            if (!armed)
            {
                // Ainda armando: não dispara (a duração continua correndo).
                if (world.Tick >= area.ExpiresAtTick)
                {
                    Expire(world, entity, area, AreaExpireReason.Duration);
                }

                continue;
            }

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
