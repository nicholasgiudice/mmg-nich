using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Effects;

/// <summary>
/// Alvo atual de uma entidade (T-442, Orbes Arcanos): o alvo de unidade da última intenção de usar habilidade dela (aceita
/// ou não), desde que seja outra entidade. Estado interno do servidor: o cliente já sabe o próprio alvo.
/// </summary>
public sealed class CurrentTarget : IComponent
{
    public EntityId Target { get; set; } = EntityId.None;

    public IComponent Clone() => new CurrentTarget { Target = Target };

    public void WriteHash(StateHasher hasher) => hasher.Add(Target.Value);
}

/// <summary>Efeito <c>current_target</c> (T-442): disparos no alvo atual de quem recebe o efeito.</summary>
public static class CurrentTargetEffects
{
    public static void Register(CombatServices s) => s.Effects.Register<CurrentTargetEffect>(OnCurrentTarget);

    /// <summary>Guarda o alvo escolhido numa intenção de usar habilidade (chamado pelo <c>AbilitySystem</c>).</summary>
    internal static void Remember(World world, Entity chooser, EntityId? target)
    {
        if (target is not { } id || id == chooser.Id || world.Find(id) is null)
        {
            return;
        }

        var current = chooser.TryGet<CurrentTarget>();
        if (current is null)
        {
            current = new CurrentTarget();
            chooser.Add(current);
        }

        current.Target = id;
    }

    /// <summary>Alvo atual válido para disparar a partir de <paramref name="holder"/>, ou null.</summary>
    public static Entity? Resolve(World world, Entity holder, Entity source, CurrentTargetEffect e)
    {
        if (holder.TryGet<CurrentTarget>() is not { } current || world.Find(current.Target) is not { IsAlive: true } target
            || !target.Has<Transform>() || !target.Has<Health>()
            || !TargetResolver.MatchesFaction(source, target, e.Affects) || !TargetResolver.CanBeHitBy(world, source, target))
        {
            return null;
        }

        var from = holder.Get<Transform>().Position;
        var to = target.Get<Transform>().Position;
        var radius = target.TryGet<Collider>()?.Radius ?? 0f;
        if (Vector2.Distance(from, to) - radius > e.Range + 1e-4f || !(world.Map?.HasLineOfSight(from, to) ?? true))
        {
            return null;
        }

        return target;
    }

    private static void OnCurrentTarget(EffectContext c, CurrentTargetEffect e)
    {
        var holder = c.Target!;
        var source = c.Source ?? holder;
        if (!source.IsAlive || Resolve(c.World, holder, source, e) is not { } target)
        {
            return;
        }

        IReadOnlyList<Entity> targets = [target];
        var dir = TargetResolver.AimDirection(holder, target, null);
        for (var shot = 0; shot < e.Count && target.IsAlive; shot++)
        {
            EffectApplier.Apply(c.World, e.Effects, source, targets, c.SourceId, c.Ability, null, dir, c.Power, c.HitIndex);
        }
    }
}
