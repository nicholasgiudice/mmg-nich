using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Combat;

public sealed record TargetResolution(AbilityRejectReason? Reason, IReadOnlyList<Entity> Targets, Vector2 Direction)
{
    public static TargetResolution Fail(AbilityRejectReason reason, Vector2 dir) => new(reason, [], dir);
}

/// <summary>
/// Resolve os alvos de uma habilidade para todos os tipos de <see cref="TargetingDefinition"/> usando
/// <c>world.Spatial</c> e <c>world.Map</c>. Ataques à distância são hitscan (instantâneos, bloqueados por paredes).
/// Regras: unit exige alvo válido (facção, vivo, alcance, LoS); tipos de área nunca falham por não acertar ninguém.
/// Em áreas, habilidades só com efeitos benéficos (heal/shield/remove_status) afetam aliados (inclui o conjurador);
/// as demais afetam inimigos.
/// </summary>
public static class TargetResolver
{
    private const float RangeEpsilon = 1e-4f;

    /// <summary>Só os efeitos com <c>target: targets</c> contam (efeitos em <c>self</c> não decidem a facção).</summary>
    public static bool IsBeneficial(AbilityDefinition ability)
    {
        var onTargets = ability.Effects.Where(e => e.Target == EffectTarget.Targets).ToList();
        return onTargets.Count > 0 && onTargets.All(e => e is HealEffect or ShieldEffect or RemoveStatusEffect);
    }

    public static Vector2 AimDirection(Entity caster, Entity? target, Vector2? aim)
    {
        var tr = caster.Get<Transform>();
        Vector2? to = aim ?? target?.TryGet<Transform>()?.Position;
        if (to.HasValue)
        {
            var d = to.Value - tr.Position;
            if (d.LengthSquared() > 1e-8f)
            {
                return Vector2.Normalize(d);
            }
        }

        return tr.Facing.LengthSquared() > 1e-8f ? Vector2.Normalize(tr.Facing) : new Vector2(0, 1);
    }

    public static TargetResolution Resolve(World world, Entity caster, AbilityDefinition ability, EntityId? targetId, Vector2? aim)
    {
        var pos = caster.Get<Transform>().Position;
        var target = targetId.HasValue ? world.Find(targetId.Value) : null;
        var dir = AimDirection(caster, target, aim);
        var beneficial = IsBeneficial(ability);

        bool AreaFilter(Entity e) => MatchesFaction(caster, e, beneficial ? FactionFilter.Ally : FactionFilter.Enemy);

        switch (ability.Targeting)
        {
            case SelfTargeting:
                return new TargetResolution(null, [caster], dir);

            case UnitTargeting u:
                if (target is null || !target.IsAlive || target.Has<Transform>() is false || !MatchesFaction(caster, target, u.Faction))
                {
                    return TargetResolution.Fail(AbilityRejectReason.InvalidTarget, dir);
                }

                var tpos = target.Get<Transform>().Position;
                var radius = target.TryGet<Collider>()?.Radius ?? 0f;
                if (Vector2.Distance(pos, tpos) - radius > u.Range + RangeEpsilon)
                {
                    return TargetResolution.Fail(AbilityRejectReason.OutOfRange, dir);
                }

                if (!HasLos(world, pos, tpos))
                {
                    return TargetResolution.Fail(AbilityRejectReason.NoLineOfSight, dir);
                }

                return new TargetResolution(null, [target], dir);

            case MeleeArcTargeting a:
                var arc = world.Spatial.InCone(pos, dir, (float)(a.Angle * Math.PI / 180.0), (float)a.Range, AreaFilter);
                return new TargetResolution(null, WithLos(world, pos, arc), dir);

            case LineTargeting l:
                var line = world.Spatial.InOrientedRect(pos, dir, (float)l.Length, (float)l.Width, AreaFilter);
                return new TargetResolution(null, WithLos(world, pos, line), dir);

            case CircleSelfTargeting cs:
                var around = world.Spatial.InRadius(pos, (float)cs.Radius, AreaFilter);
                return new TargetResolution(null, WithLos(world, pos, around), dir);

            case CircleAtPointTargeting cp:
                var point = aim ?? target?.TryGet<Transform>()?.Position;
                if (point is null)
                {
                    return TargetResolution.Fail(AbilityRejectReason.InvalidTarget, dir);
                }

                if (Vector2.Distance(pos, point.Value) > cp.Range + RangeEpsilon)
                {
                    return TargetResolution.Fail(AbilityRejectReason.OutOfRange, dir);
                }

                if (!HasLos(world, pos, point.Value))
                {
                    return TargetResolution.Fail(AbilityRejectReason.NoLineOfSight, dir);
                }

                var inCircle = world.Spatial.InRadius(point.Value, (float)cp.Radius, AreaFilter);
                return new TargetResolution(null, WithLos(world, point.Value, inCircle), dir);

            default:
                throw new NotSupportedException($"Targeting {ability.Targeting.GetType().Name} not supported");
        }
    }

    public static bool MatchesFaction(Entity caster, Entity other, FactionFilter filter)
    {
        var cf = caster.TryGet<FactionTag>();
        var of = other.TryGet<FactionTag>();
        if (of is null || !other.IsAlive)
        {
            return false;
        }

        return filter switch
        {
            FactionFilter.Any => true,
            FactionFilter.Ally => cf is not null && cf.Faction == of.Faction,
            FactionFilter.Enemy => cf is not null && cf.Faction != of.Faction,
            _ => false,
        };
    }

    private static bool HasLos(World world, Vector2 a, Vector2 b) => world.Map?.HasLineOfSight(a, b) ?? true;

    private static IReadOnlyList<Entity> WithLos(World world, Vector2 origin, IReadOnlyList<Entity> hits) =>
        world.Map is null
            ? hits
            : hits.Where(e => world.Map.HasLineOfSight(origin, e.Get<Transform>().Position)).ToList();
}
