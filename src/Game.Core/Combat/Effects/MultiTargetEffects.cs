using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Effects;

/// <summary>Um salto de <see cref="ChainEffect"/> (T-061): de <paramref name="From"/> para <paramref name="To"/>. Jump = 1..N.</summary>
public sealed record ChainJumped(EntityId Source, EntityId From, EntityId To, string SourceId, int Jump) : GameEvent;

/// <summary>Efeitos da T-061 que escolhem alvos novos: chain (saltos), extra_targets (alvos extras) e revive (levantar caídos).</summary>
public static class MultiTargetEffects
{
    public static void Register(CombatServices s)
    {
        s.Effects
            .Register<ChainEffect>(OnChain)
            .Register<ExtraTargetsEffect>(OnExtraTargets)
            .Register<ReviveEffect>(OnRevive, EffectScope.PerCast);
    }

    private static void OnChain(EffectContext c, ChainEffect e)
    {
        if (c.Source is not { } source)
        {
            return;
        }

        var hit = new HashSet<EntityId>(c.Targets.Select(t => t.Id));
        var from = c.Target!;
        for (var jump = 1; jump <= e.Jumps; jump++)
        {
            var fromPos = from.Get<Transform>().Position;
            var next = c.World.Spatial.Nearest(
                fromPos,
                (float)e.Radius,
                x => !hit.Contains(x.Id) && x.Has<Health>() && TargetResolver.MatchesFaction(source, x, e.Affects)
                    && HasLos(c.World, fromPos, x));
            if (next is null)
            {
                return;
            }

            hit.Add(next.Id);
            c.World.Events.Publish(new ChainJumped(source.Id, from.Id, next.Id, c.SourceId, jump));
            Nested(c, e.Effects, [next], e.Multiplier);
            from = next;
        }
    }

    private static void OnExtraTargets(EffectContext c, ExtraTargetsEffect e)
    {
        if (c.Source is not { } source)
        {
            return;
        }

        var center = c.Target!.Get<Transform>().Position;
        var primary = new HashSet<EntityId>(c.Targets.Select(t => t.Id));
        var candidates = c.World.Spatial.InRadius(
            center,
            (float)e.Radius,
            x => !primary.Contains(x.Id) && x.Has<Health>() && TargetResolver.MatchesFaction(source, x, e.Affects)
                && HasLos(c.World, center, x));
        if (candidates.Count == 0)
        {
            return;
        }

        float Distance(Entity x) => Vector2.Distance(center, x.Get<Transform>().Position);
        var ordered = e.Select == TargetSelect.MostInjured
            ? candidates.OrderBy(HealthFraction).ThenBy(Distance).ThenBy(x => x.Id.Value)
            : candidates.OrderBy(Distance).ThenBy(x => x.Id.Value);
        var chosen = (e.Count > 0 ? ordered.Take(e.Count) : ordered).ToList();
        Nested(c, e.Effects, chosen, e.Multiplier);
    }

    private static void OnRevive(EffectContext c, ReviveEffect e)
    {
        if (c.Source is not { } source || source.TryGet<FactionTag>() is not { } faction)
        {
            return;
        }

        var center = c.Targets.FirstOrDefault(t => t.IsAlive && t.Has<Transform>())?.Get<Transform>().Position
            ?? c.AimPoint
            ?? source.Get<Transform>().Position;
        var fallen = c.World.Entities
            .Where(x => !x.IsRemoved && x.TryGet<Health>() is { IsAlive: false }
                && x.TryGet<FactionTag>()?.Faction == faction.Faction && x.Has<Transform>())
            .Select(x => (Entity: x, Distance: Vector2.Distance(center, x.Get<Transform>().Position)))
            .Where(x => x.Distance <= e.Radius + (x.Entity.TryGet<Collider>()?.Radius ?? 0f))
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Entity.Id.Value)
            .Take(Math.Max(1, e.Count))
            .ToList();
        foreach (var (entity, _) in fallen)
        {
            var health = entity.Get<Health>();
            if (c.World.Revive(entity, Math.Max(1, CoreEffects.Round(health.Max * e.HealthFraction)), source.Id, c.SourceId))
            {
                Status.StatusManager.RemoveAll(c.World, entity, source.Id); // volta sem os status de antes da queda
            }
        }
    }

    /// <summary>Aplica efeitos aninhados a novos alvos com a potência multiplicada (mesma fonte, habilidade e acerto).</summary>
    private static void Nested(EffectContext c, IReadOnlyList<EffectDefinition> effects, IReadOnlyList<Entity> targets, double multiplier) =>
        EffectApplier.Apply(c.World, effects, c.Source, targets, c.SourceId, c.Ability, c.AimPoint, c.AimDirection, c.Power * multiplier, c.HitIndex);

    private static float HealthFraction(Entity x) => x.Get<Health>() is { Max: > 0 } h ? h.Current / h.Max : 1f;

    private static bool HasLos(World world, Vector2 from, Entity to) =>
        world.Map?.HasLineOfSight(from, to.Get<Transform>().Position) ?? true;
}
