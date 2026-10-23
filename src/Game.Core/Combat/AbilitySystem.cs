using System.Numerics;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat;

public sealed record AbilityCheck(AbilityRejectReason? Reason, AbilityDefinition? Ability, TargetResolution? Resolution);

/// <summary>Validação de uma tentativa de uso. Ordem: vivo, conjurando, gates, conhecida, cargas, recarga, recurso, alvo, alcance, LoS.</summary>
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

        // T-060: a definição efetiva depende do NH que a entidade tem na habilidade.
        var ability = world.Content.AbilityAt(abilityId, st?.RankOf(abilityId) ?? 1);
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

        if (st.ChargesRemaining(ability) is 0)
        {
            return new AbilityCheck(AbilityRejectReason.NoCharges, ability, null);
        }

        if (st.IsOnCooldown(ability, world.Tick))
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
/// Pipeline de habilidades (ordem <see cref="SystemOrder.Abilities"/>): expira escudos, devolve cargas por recarga,
/// avança conjurações, aplica acertos extras agendados e processa <see cref="UseAbilityIntent"/> do tick. Custo e
/// recarga são aplicados ao CONCLUIR a conjuração (conjuração cancelada/interrompida não custa nada; o recurso é
/// revalidado ao concluir). Tudo usa a definição do NH da entidade (T-060).
/// </summary>
public sealed class AbilitySystem : ISimulationSystem
{
    public void Update(World world)
    {
        ExpireShields(world);
        RechargeCooldownCharges(world);
        AdvanceCasts(world);
        ProcessPendingHits(world);
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
                    RefundShield(world, s);
                }
            }
        }
    }

    /// <summary>T-061 (Barreira NH3+): escudo que expira com sobra devolve uma fração dela, em recurso, a quem o criou.</summary>
    private static void RefundShield(World world, ShieldInstance s)
    {
        if (s.RefundResource is not { } resource || s.RefundFraction <= 0 || s.Remaining <= 0
            || world.Find(s.Source) is not { IsAlive: true } owner)
        {
            return;
        }

        var amount = CoreEffects.Round(s.Remaining * s.RefundFraction);
        if (amount > 0)
        {
            Resources.ResourceModule.Change(world, owner, resource, amount, Resources.ResourceChangeReason.Effect, s.SourceId);
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

            var ability = world.Content.AbilityAt(cast.AbilityId, st.RankOf(cast.AbilityId));
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

        long ready;
        if (ability.CooldownCharges > 1)
        {
            // Cargas por recarga (T-060): a recarga só começa quando a primeira carga é gasta; as outras voltam uma a uma.
            var spent = st.CooldownChargesSpent.TryGetValue(ability.Id, out var s) ? s : 0;
            if (spent == 0)
            {
                st.ReadyAtTick[ability.Id] = world.Tick + ability.CooldownTicks;
            }

            st.CooldownChargesSpent[ability.Id] = spent + 1;
            ready = st.ReadyAtTick[ability.Id];
        }
        else
        {
            ready = world.Tick + CooldownFor(world, caster, st, ability);
            st.ReadyAtTick[ability.Id] = ready;
        }

        if (ability.Charges is not null)
        {
            st.ChargesUsed[ability.Id] = (st.ChargesUsed.TryGetValue(ability.Id, out var used) ? used : 0) + 1;
        }

        world.Events.Publish(new AbilityExecuted(
            caster.Id, ability.Id, res.Targets.Select(t => t.Id).ToList(), costResource, costAmount,
            st.IsBasicAttack(ability.Id), ready, st.ChargesRemaining(ability)));

        var rank = st.RankOf(ability.Id);
        var power = world.Combat.ConsumeAbilityPower(world, caster, ability.Id); // T-061: fortalecer o próximo uso
        ApplyHit(world, caster, ability, res, target, aim, rank, power, 1);
        ScheduleExtraHits(world, caster, ability, rank, target, aim, power);
    }

    /// <summary>Recarga do uso: a do ataque básico é dividida pelo fator de AGI (T-201); as outras ficam como estão.</summary>
    private static long CooldownFor(World world, Entity caster, AbilityState st, AbilityDefinition ability)
    {
        if (!st.IsBasicAttack(ability.Id) || caster.TryGet<Stats.CharacterStats>() is not { } stats)
        {
            return ability.CooldownTicks;
        }

        return (long)Math.Round(ability.CooldownTicks / Stats.StatMath.AttackSpeedFactor(stats, world.Content.Rules.Stats), MidpointRounding.AwayFromZero);
    }

    /// <summary>Efeitos de um acerto: direto nos alvos resolvidos ou, com telegráfo (T-012), no tick de resolução.</summary>
    private static void ApplyHit(
        World world, Entity caster, AbilityDefinition ability, TargetResolution res, EntityId? target, Vector2? aim, int rank,
        double power, int hitIndex)
    {
        caster.Get<Transform>().Facing = res.Direction;
        if (ability.Telegraph is not null)
        {
            Telegraphs.TelegraphService.Start(
                world, caster, ability, res.Direction, aim, target.HasValue ? world.Find(target.Value) : null, rank, power, hitIndex);
            return;
        }

        EffectApplier.Apply(world, ability.Effects, caster, res.Targets, ability.Id, ability, aim, res.Direction, power, hitIndex);
    }

    /// <summary>Acertos 2..N (T-060): agora mesmo se o intervalo é zero, senão agendados no <see cref="PendingHits"/> da fonte.</summary>
    private static void ScheduleExtraHits(
        World world, Entity caster, AbilityDefinition ability, int rank, EntityId? target, Vector2? aim, double power)
    {
        var count = ability.HitCount;
        if (count <= 1)
        {
            return;
        }

        var interval = ability.Hits!.IntervalTicks;
        for (var i = 2; i <= count; i++)
        {
            var hit = new PendingHit(ability.Id, rank, i, count, world.Tick + ((long)(i - 1) * interval), target, aim, power);
            if (interval == 0)
            {
                ExtraHit(world, caster, hit);
                continue;
            }

            var pending = caster.TryGet<PendingHits>();
            if (pending is null)
            {
                pending = new PendingHits();
                caster.Add(pending);
            }

            pending.Items.Add(hit);
        }
    }

    private static void ProcessPendingHits(World world)
    {
        foreach (var e in world.Entities)
        {
            var pending = e.TryGet<PendingHits>();
            if (pending is null || pending.Items.Count == 0)
            {
                continue;
            }

            if (!e.IsAlive)
            {
                pending.Items.Clear();
                continue;
            }

            for (var i = 0; i < pending.Items.Count; i++)
            {
                var hit = pending.Items[i];
                if (hit.DueTick > world.Tick)
                {
                    continue;
                }

                pending.Items.RemoveAt(i--);
                ExtraHit(world, e, hit);
            }
        }
    }

    /// <summary>
    /// Um acerto extra. Alvo único que morreu, sumiu ou saiu do alcance passa para o inimigo (ou aliado, conforme a
    /// habilidade) mais próximo ao alcance; sem ninguém, o acerto se perde. Não custa recurso nem mexe na recarga.
    /// </summary>
    private static void ExtraHit(World world, Entity caster, PendingHit hit)
    {
        if (world.Content.AbilityAt(hit.AbilityId, hit.Rank) is not { } ability)
        {
            return;
        }

        var target = hit.Target;
        var aim = hit.AimPoint;
        var res = TargetResolver.Resolve(world, caster, ability, target, aim);
        if (res.Reason is not null && ability.Targeting is UnitTargeting unit)
        {
            var origin = caster.Get<Transform>().Position;
            var next = world.Spatial.Nearest(
                origin,
                (float)unit.Range + 1f,
                x => x.Id != hit.Target && x.Has<Transform>() && TargetResolver.MatchesFaction(caster, x, unit.Faction));
            if (next is null)
            {
                return;
            }

            target = next.Id;
            aim = null;
            res = TargetResolver.Resolve(world, caster, ability, target, aim);
        }

        if (res.Reason is not null)
        {
            return;
        }

        world.Events.Publish(new AbilityHit(caster.Id, ability.Id, hit.HitIndex, hit.HitCount, res.Targets.Select(t => t.Id).ToList()));
        ApplyHit(world, caster, ability, res, target, aim, hit.Rank, hit.Power, hit.HitIndex);
    }

    /// <summary>Devolve as cargas por recarga (T-060), uma por vez, reiniciando a recarga enquanto faltar alguma.</summary>
    private static void RechargeCooldownCharges(World world)
    {
        foreach (var e in world.Entities)
        {
            var st = e.TryGet<AbilityState>();
            if (st is null || !st.HasCooldownChargesSpent)
            {
                continue;
            }

            foreach (var id in st.CooldownChargesSpent.Keys.ToList())
            {
                if (world.Tick < (st.ReadyAtTick.TryGetValue(id, out var t) ? t : 0))
                {
                    continue;
                }

                var spent = st.CooldownChargesSpent[id] - 1;
                if (spent <= 0)
                {
                    st.CooldownChargesSpent.Remove(id);
                }
                else
                {
                    st.CooldownChargesSpent[id] = spent;
                    var cooldown = world.Content.AbilityAt(id, st.RankOf(id))?.CooldownTicks ?? 0;
                    st.ReadyAtTick[id] = world.Tick + cooldown;
                }
            }
        }
    }
}
