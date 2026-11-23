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
    public static Entity Start(World world, Entity caster, AbilityDefinition ability, Vector2 direction, Vector2? aim, Entity? target, int rank = 1,
        double power = 1, int hitIndex = 1)
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
            Rank = rank,
            Power = power,
            HitIndex = hitIndex,
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

    /// <summary>Tentativas de sortear um ponto andável e visível da fonte, por telegráfo aleatório da salva.</summary>
    private const int VolleyPointAttempts = 16;

    /// <summary>
    /// Salva (<see cref="TelegraphDefinition.Volley"/>): de Min a Max telegráfos. O primeiro no ponto mirado; depois um em
    /// cada outro jogador inimigo vivo e alvejável a até <c>radius</c> da fonte (ordem embaralhada se faltar telegráfo);
    /// o resto em pontos andáveis aleatórios no mesmo raio, com linha de visão da fonte. Sorteios só no RNG do mundo.
    /// </summary>
    public static List<Entity> StartVolley(World world, Entity caster, AbilityDefinition ability, Vector2 direction, Vector2? aim, Entity? target,
        int rank = 1, double power = 1, int hitIndex = 1)
    {
        var volley = ability.Telegraph!.Volley!;
        var rng = world.Random;
        var count = volley.Min + rng.NextInt(volley.Max - volley.Min + 1);
        var casterPos = caster.Get<Transform>().Position;
        var radius = (float)volley.Radius;
        var points = new List<Vector2> { aim ?? target?.TryGet<Transform>()?.Position ?? casterPos };

        var players = VolleyPlayers(world, caster, casterPos, radius, target?.Id);
        for (var i = players.Count - 1; i > 0; i--)
        {
            var j = rng.NextInt(i + 1);
            (players[i], players[j]) = (players[j], players[i]);
        }

        foreach (var p in players)
        {
            if (points.Count >= count)
            {
                break;
            }

            points.Add(p.Get<Transform>().Position);
        }

        while (points.Count < count && RandomPoint(world, casterPos, radius) is { } point)
        {
            points.Add(point);
        }

        var list = new List<Entity>(points.Count);
        foreach (var point in points)
        {
            var d = point - casterPos;
            list.Add(Start(world, caster, ability, d.LengthSquared() > 1e-6f ? d : direction, point, null, rank, power, hitIndex));
        }

        return list;
    }

    /// <summary>Jogadores (com <see cref="Stats.CharacterStats"/>) inimigos da fonte, vivos e alvejáveis, no raio; ordem de id.</summary>
    private static List<Entity> VolleyPlayers(World world, Entity caster, Vector2 casterPos, float radius, EntityId? exclude)
    {
        var faction = caster.TryGet<FactionTag>()?.Faction;
        var list = new List<Entity>();
        foreach (var e in world.Entities)
        {
            if (e.IsAlive && e.Id != exclude && e.Has<Stats.CharacterStats>() && e.TryGet<FactionTag>() is { } f && f.Faction != faction
                && !Status.StatusManager.IsUntargetable(world, e)
                && Vector2.Distance(casterPos, e.Get<Transform>().Position) <= radius)
            {
                list.Add(e);
            }
        }

        return list;
    }

    /// <summary>Ponto uniforme no disco de raio <paramref name="radius"/>, andável e visível de <paramref name="center"/> (sem mapa: qualquer um).</summary>
    private static Vector2? RandomPoint(World world, Vector2 center, float radius)
    {
        for (var i = 0; i < VolleyPointAttempts; i++)
        {
            var angle = world.Random.NextFloat() * MathF.Tau;
            var dist = MathF.Sqrt(world.Random.NextFloat()) * radius;
            var p = center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist);
            if (world.Map is not { } map || (!map.IsBlocked(p) && map.HasLineOfSight(center, p)))
            {
                return p;
            }
        }

        return null;
    }

    /// <summary>Entidades (vivas, com facção) dentro da forma agora, filtradas pela facção relativa à fonte. Paredes bloqueiam (linha de visão da origem).</summary>
    public static IReadOnlyList<Entity> Inside(World world, TelegraphState t, Vector2 origin, Vector2 dir)
    {
        bool Filter(Entity e) => e.TryGet<FactionTag>() is { } f && e.Has<Health>() && e.IsAlive && Matches(t, f.Faction)
            && (e.Id == t.Source || !Status.StatusManager.IsUntargetable(world, e));

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
        if (source is null || !source.IsAlive || world.Content.AbilityAt(t.AbilityId, t.Rank) is not { } ability)
        {
            world.Events.Publish(new TelegraphResolved(telegraph.Id, t.Source, t.AbilityId, [], true));
            world.Remove(telegraph.Id);
            return;
        }

        var targets = Inside(world, t, tr.Position, tr.Facing);
        world.Events.Publish(new TelegraphResolved(telegraph.Id, t.Source, t.AbilityId, targets.Select(e => e.Id).ToList(), false));
        EffectApplier.Apply(world, ability.Effects, source, targets, ability.Id, ability, tr.Position, tr.Facing, t.Power, t.HitIndex);

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
