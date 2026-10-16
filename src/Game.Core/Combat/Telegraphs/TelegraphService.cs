using System.Numerics;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Combat.Telegraphs;

/// <summary>Criação e resolução de telegráfos. A resolução roda em <see cref="TelegraphSystem"/> (antes de status/recursos/IA).</summary>
public static class TelegraphService
{
    /// <summary>
    /// Cria o telegráfo de uma habilidade com <see cref="AbilityDefinition.Telegraph"/> (chamado pelo pipeline ao concluir a conjuração).
    /// Origem: posição da fonte (<c>Source</c>) ou ponto mirado/posição do alvo (<c>Aim</c>).
    /// </summary>
    public static Entity Start(World world, Entity caster, AbilityDefinition ability, Vector2 direction, Vector2? aim, Entity? target)
    {
        var def = ability.Telegraph!;
        var casterPos = caster.Get<Transform>().Position;
        var origin = def.Anchor == TelegraphAnchor.Aim
            ? aim ?? target?.TryGet<Transform>()?.Position ?? casterPos
            : casterPos;
        var dir = direction.LengthSquared() > 1e-8f ? Vector2.Normalize(direction) : new Vector2(0, 1);
        var state = new TelegraphState
        {
            Source = caster.Id,
            AbilityId = ability.Id,
            Shape = def.Shape,
            Radius = (float)def.Radius,
            Length = (float)def.Length,
            Width = (float)def.Width,
            AngleDegrees = (float)def.Angle,
            Affects = def.Affects,
            Charge = def.Charge,
            SourceFaction = caster.TryGet<FactionTag>()?.Faction,
            StartTick = world.Tick,
            ResolveAtTick = world.Tick + Math.Max(1, def.DelayTicks),
        };
        var entity = world.Spawn(new Transform { Position = origin, Facing = dir }, state);
        world.Events.Publish(new TelegraphStarted(
            entity.Id, caster.Id, ability.Id, state.Shape, origin, dir, state.Radius, state.Length, state.Width,
            state.AngleDegrees, state.ResolveAtTick));
        return entity;
    }

    /// <summary>Entidades (vivas, com facção) dentro da forma agora, filtradas pela facção relativa à fonte. Paredes bloqueiam (linha de visão da origem).</summary>
    public static IReadOnlyList<Entity> Inside(World world, TelegraphState t, Vector2 origin, Vector2 dir)
    {
        bool Filter(Entity e) => e.TryGet<FactionTag>() is { } f && e.Has<Health>() && e.IsAlive && Matches(t, f.Faction);

        IReadOnlyList<Entity> hits = t.Shape switch
        {
            TelegraphShape.Circle => world.Spatial.InRadius(origin, t.Radius, Filter),
            TelegraphShape.Cone => world.Spatial.InCone(origin, dir, t.AngleDegrees * MathF.PI / 180f, t.Length, Filter),
            _ => world.Spatial.InOrientedRect(origin, dir, t.Length, t.Width, Filter),
        };
        return world.Map is null
            ? hits
            : hits.Where(e => world.Map.HasLineOfSight(origin, e.Get<Transform>().Position)).ToList();
    }

    private static bool Matches(TelegraphState t, Faction faction) => t.Affects switch
    {
        FactionFilter.Any => true,
        FactionFilter.Ally => t.SourceFaction == faction,
        _ => t.SourceFaction is { } sf && sf != faction,
    };

    internal static void Resolve(World world, Entity telegraph, TelegraphState t)
    {
        var tr = telegraph.Get<Transform>();
        var source = world.Find(t.Source);
        if (source is null || !source.IsAlive || !world.Content.Abilities.TryGetValue(t.AbilityId, out var ability))
        {
            world.Events.Publish(new TelegraphResolved(telegraph.Id, t.Source, t.AbilityId, [], true));
            world.Remove(telegraph.Id);
            return;
        }

        var targets = Inside(world, t, tr.Position, tr.Facing);
        world.Events.Publish(new TelegraphResolved(telegraph.Id, t.Source, t.AbilityId, targets.Select(e => e.Id).ToList(), false));
        EffectApplier.Apply(world, ability.Effects, source, targets, ability.Id, ability, tr.Position, tr.Facing);

        if (t.Charge && source.IsAlive && source.Has<Transform>())
        {
            var st = source.Get<Transform>();
            var from = st.Position;
            var moved = ForcedMovement.Displace(world, source, tr.Facing, t.Length);
            world.Events.Publish(new Displaced(source.Id, source.Id, ability.Id, DisplacementKind.Dash, from, st.Position, moved));
        }

        world.Remove(telegraph.Id);
    }
}

/// <summary>
/// Resolve os telegráfos cujo tick chegou. Roda em <see cref="SystemOrder.Telegraphs"/> (2500), ANTES de Status/Recursos/IA:
/// assim o dano gerado (DamageDealt) é visto no mesmo tick por recursos (4000) e ameaça (5000).
/// </summary>
public sealed class TelegraphSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var e in world.Entities)
        {
            if (!e.IsRemoved && e.TryGet<TelegraphState>() is { } t && world.Tick >= t.ResolveAtTick)
            {
                TelegraphService.Resolve(world, e, t);
            }
        }
    }
}
