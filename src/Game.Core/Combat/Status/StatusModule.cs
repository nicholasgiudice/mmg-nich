using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Status;

/// <summary>Registro do módulo de status (T-009): efeitos apply_status/remove_status, gate e modificadores.</summary>
public static class StatusModule
{
    public static void Register(CombatServices s)
    {
        s.Effects
            .Register<ApplyStatusEffect>((c, e) => StatusManager.Apply(c.World, c.Source, c.Target!, e.Status, e.DurationTicks))
            .Register<RemoveStatusEffect>((c, e) => StatusManager.RemoveByTag(c.World, c.Target!, e.Tag, e.Count, c.SourceEntityId));
        s.Gates.Add(new StatusGate());
        s.Modifiers.Add(new StatusModifierSource());
    }
}

/// <summary>stunned: nada; silenced: só ataque básico.</summary>
public sealed class StatusGate : IActionGate
{
    public AbilityRejectReason? CanUse(World world, Entity actor, string abilityId, AbilityDefinition? ability, bool isBasicAttack)
    {
        if (StatusManager.IsStunned(world, actor))
        {
            return AbilityRejectReason.Stunned;
        }

        if (!isBasicAttack && StatusManager.IsSilenced(world, actor))
        {
            return AbilityRejectReason.Silenced;
        }

        return null;
    }
}

/// <summary>
/// Agregação: multiplicativa. Cada status contribui com mult^stacks; todos os status se multiplicam.
/// Velocidade é 0 se stunned ou rooted.
/// </summary>
public sealed class StatusModifierSource : IModifierSource
{
    public double SpeedMultiplier(World world, Entity entity)
    {
        var m = 1.0;
        foreach (var s in StatusManager.Active(world, entity))
        {
            var d = StatusManager.Def(world, s);
            if (d.Flags.Stunned || d.Flags.Rooted)
            {
                return 0;
            }

            m *= Math.Pow(d.Modifiers.SpeedMultiplier, s.Stacks);
        }

        return m;
    }

    public double DamageDealtMultiplier(World world, Entity source, Entity target, DamageSchool school)
    {
        var m = 1.0;
        foreach (var s in StatusManager.Active(world, source))
        {
            m *= Math.Pow(StatusManager.Def(world, s).Modifiers.DamageDealtMultiplier, s.Stacks);
        }

        return m;
    }

    public double DamageTakenMultiplier(World world, Entity target, Entity? source, DamageSchool school)
    {
        var m = 1.0;
        foreach (var s in StatusManager.Active(world, target))
        {
            var d = StatusManager.Def(world, s);
            m *= Math.Pow(d.Modifiers.DamageTakenMultiplier, s.Stacks);
            if (d.Frontal is { } f && source is not null && IsInFrontArc(target, source, f.ArcDegrees))
            {
                m *= Math.Pow(f.DamageTakenMultiplier, s.Stacks);
            }
        }

        return m;
    }

    /// <summary>A fonte está dentro do arco (total, em graus) centrado no facing do alvo? Mesma posição = fora.</summary>
    public static bool IsInFrontArc(Entity target, Entity source, double arcDegrees)
    {
        if (target.TryGet<Transform>() is not { } t || source.TryGet<Transform>() is not { } s)
        {
            return false;
        }

        var to = s.Position - t.Position;
        if (to.LengthSquared() < 1e-8f || t.Facing.LengthSquared() < 1e-8f)
        {
            return false;
        }

        var cos = Vector2.Dot(Vector2.Normalize(t.Facing), Vector2.Normalize(to));
        return cos >= Math.Cos(arcDegrees * Math.PI / 360.0) - 1e-6;
    }
}

/// <summary>Efeitos periódicos e expiração. Roda em <see cref="SystemOrder.Status"/>.</summary>
public sealed class StatusSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var entity in world.Entities)
        {
            if (!entity.IsAlive || entity.TryGet<StatusEffects>() is not { } fx || fx.Active.Count == 0)
            {
                continue;
            }

            foreach (var s in fx.Active.ToList())
            {
                var def = world.Content.Statuses[s.StatusId];
                for (var i = 0; i < def.Periodic.Count && entity.IsAlive; i++)
                {
                    if (s.NextPeriodicTick[i] <= world.Tick && s.NextPeriodicTick[i] <= s.ExpiresAtTick)
                    {
                        s.NextPeriodicTick[i] += def.Periodic[i].IntervalTicks;
                        var source = world.Find(s.Source);
                        for (var k = 0; k < s.Stacks && entity.IsAlive; k++)
                        {
                            Effects.EffectApplier.Apply(world, def.Periodic[i].Effects, source, [entity], s.StatusId);
                        }
                    }
                }

                if (!fx.Active.Contains(s))
                {
                    continue;
                }

                if (world.Tick >= s.ExpiresAtTick && fx.Active.Remove(s))
                {
                    world.Events.Publish(new StatusExpired(entity.Id, s.StatusId, s.Id));
                }
            }
        }
    }
}
