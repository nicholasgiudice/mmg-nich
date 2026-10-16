using System.Numerics;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat;

public sealed record AbilityCheck(AbilityRejectReason? Reason, AbilityDefinition? Ability, TargetResolution? Resolution);

/// <summary>Validação de uma tentativa de uso. Ordem: vivo, conjurando, gates, conhecida, recarga, recurso, alvo, alcance, LoS.</summary>
public static class AbilityValidator
{
    public static AbilityCheck Validate(World world, Entity caster, string abilityId, EntityId? target, Vector2? aim)
    {
        if (!caster.IsAlive)
        {
            return new AbilityCheck(AbilityRejectReason.Dead, null, null);
        }

        var st = caster.TryGet<AbilityState>();
        if (st is { IsCasting: true })
        {
            return new AbilityCheck(AbilityRejectReason.AlreadyCasting, null, null);
        }

        world.Content.Abilities.TryGetValue(abilityId, out var ability);
        var isBasic = st?.IsBasicAttack(abilityId) ?? false;
        var gate = world.Combat.CheckGates(world, caster, abilityId, ability, isBasic);
        if (gate is not null)
        {
            return new AbilityCheck(gate, ability, null);
        }

        if (st is null || ability is null || !st.Knows(abilityId))
        {
            return new AbilityCheck(AbilityRejectReason.UnknownAbility, ability, null);
        }

        if (st.CooldownRemaining(abilityId, world.Tick) > 0)
        {
            return new AbilityCheck(AbilityRejectReason.OnCooldown, ability, null);
        }

        if (ability.Cost is { } cost)
        {
            var pool = caster.TryGet<ResourcePool>();
            if (pool is null || pool.Type != cost.Resource || pool.Current < cost.Amount)
            {
                return new AbilityCheck(AbilityRejectReason.NotEnoughResource, ability, null);
            }
        }

        var res = TargetResolver.Resolve(world, caster, ability, target, aim);
        return new AbilityCheck(res.Reason, ability, res);
    }
}

/// <summary>
/// Pipeline de habilidades (ordem <see cref="SystemOrder.Abilities"/>): expira escudos, avança conjurações e
/// processa <see cref="UseAbilityIntent"/> do tick. Custo e recarga são aplicados ao CONCLUIR a conjuração
/// (conjuração cancelada/interrompida não custa nada; o recurso é revalidado ao concluir).
/// </summary>
public sealed class AbilitySystem : ISimulationSystem
{
    public void Update(World world)
    {
        ExpireShields(world);
        AdvanceCasts(world);
        foreach (var intent in world.TickIntents)
        {
            if (intent is UseAbilityIntent use)
            {
                HandleUse(world, use);
            }
        }
    }

    private static void ExpireShields(World world)
    {
        foreach (var e in world.Entities)
        {
            var shields = e.TryGet<ShieldState>();
            if (shields is null || shields.Shields.Count == 0)
            {
                continue;
            }

            for (var i = 0; i < shields.Shields.Count; i++)
            {
                var s = shields.Shields[i];
                if (world.Tick >= s.ExpiresAtTick)
                {
                    shields.Shields.RemoveAt(i--);
                    world.Events.Publish(new ShieldExpired(e.Id, s.Id, s.Remaining, ShieldEndReason.Expired));
                }
            }
        }
    }

    private static void AdvanceCasts(World world)
    {
        foreach (var e in world.Entities)
        {
            var st = e.TryGet<AbilityState>();
            if (st?.Cast is not { } cast || e.IsRemoved)
            {
                continue;
            }

            world.Content.Abilities.TryGetValue(cast.AbilityId, out var ability);
            if (!e.IsAlive)
            {
                CastControl.Cancel(world, e, CastInterruptReason.Died);
            }
            else if (ability is { CastWhileMoving: false } && e.TryGet<Movement>() is { } mv && mv.Direction != Vector2.Zero)
            {
                CastControl.Cancel(world, e, CastInterruptReason.Moved);
            }
            else if (world.Tick >= cast.EndTick)
            {
                st.Cast = null;
                if (cast.LockedFacing && e.TryGet<Movement>() is { } m2)
                {
                    m2.LockFacing = false;
                }

                var check = AbilityValidator.Validate(world, e, cast.AbilityId, cast.Target, cast.AimPoint);
                if (check.Reason is { } reason)
                {
                    world.Events.Publish(new AbilityRejected(e.Id, cast.AbilityId, reason));
                }
                else
                {
                    Execute(world, e, st, check, cast.Target, cast.AimPoint);
                }
            }
        }
    }

    private static void HandleUse(World world, UseAbilityIntent use)
    {
        var caster = world.Find(use.Entity);
        if (caster is null)
        {
            return;
        }

        var check = AbilityValidator.Validate(world, caster, use.AbilityId, use.Target, use.AimPoint);
        if (check.Reason is { } reason)
        {
            world.Events.Publish(new AbilityRejected(caster.Id, use.AbilityId, reason));
            return;
        }

        var st = caster.Get<AbilityState>();
        var ability = check.Ability!;
        var dir = check.Resolution!.Direction;
        caster.Get<Transform>().Facing = dir;

        if (ability.CastTimeTicks > 0)
        {
            var mv = caster.TryGet<Movement>();
            var lockFacing = ability.CastWhileMoving && mv is not null;
            if (lockFacing)
            {
                mv!.LockFacing = true;
            }

            st.Cast = new CastState
            {
                AbilityId = ability.Id,
                StartTick = world.Tick,
                EndTick = world.Tick + ability.CastTimeTicks,
                Target = use.Target,
                AimPoint = use.AimPoint,
                LockedFacing = lockFacing,
            };
            world.Events.Publish(new AbilityStarted(caster.Id, ability.Id, ability.CastTimeTicks, st.Cast.EndTick, use.Target, use.AimPoint));
            return;
        }

        world.Events.Publish(new AbilityStarted(caster.Id, ability.Id, 0, world.Tick, use.Target, use.AimPoint));
        Execute(world, caster, st, check, use.Target, use.AimPoint);
    }

    private static void Execute(World world, Entity caster, AbilityState st, AbilityCheck check, EntityId? target, Vector2? aim)
    {
        var ability = check.Ability!;
        var res = check.Resolution!;
        string? costResource = null;
        var costAmount = 0f;
        if (ability.Cost is { } cost)
        {
            caster.Get<ResourcePool>().Current -= (float)cost.Amount;
            costResource = cost.Resource;
            costAmount = (float)cost.Amount;
        }

        var ready = world.Tick + ability.CooldownTicks;
        st.ReadyAtTick[ability.Id] = ready;
        caster.Get<Transform>().Facing = res.Direction;
        world.Events.Publish(new AbilityExecuted(
            caster.Id, ability.Id, res.Targets.Select(t => t.Id).ToList(), costResource, costAmount,
            st.IsBasicAttack(ability.Id), ready));
        EffectApplier.Apply(world, ability.Effects, caster, res.Targets, ability.Id, ability, aim, res.Direction);
    }
}
