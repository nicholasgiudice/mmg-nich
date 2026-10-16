using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Combat.Effects;

/// <summary>Efeitos da T-008: damage, heal, shield, displacement, interrupt.</summary>
public static class CoreEffects
{
    public static void Register(CombatServices s)
    {
        s.Effects
            .Register<DamageEffect>(OnDamage)
            .Register<HealEffect>(OnHeal)
            .Register<ShieldEffect>(OnShield)
            .Register<InterruptEffect>(OnInterrupt)
            .Register<DisplacementEffect>(OnDisplacement, EffectScope.PerCast);
    }

    /// <summary>Arredondamento de valores de combate: inteiro, metades para longe do zero.</summary>
    public static float Round(double value) => (float)Math.Round(value, MidpointRounding.AwayFromZero);

    private static void OnDamage(EffectContext c, DamageEffect e) =>
        DamageResolver.Deal(c.World, c.Source, c.Target!, c.SourceId, e.Base * TagBonus(c.Target!, e), e.School);

    /// <summary>1 + soma dos bônus <c>bonusVsTags</c> das tags que o alvo possui (mínimo 0).</summary>
    public static double TagBonus(Entity target, DamageEffect e)
    {
        if (e.BonusVsTags.Count == 0 || target.TryGet<UnitInfo>() is not { } info)
        {
            return 1;
        }

        var bonus = 0.0;
        foreach (var kv in e.BonusVsTags)
        {
            if (info.HasTag(kv.Key))
            {
                bonus += kv.Value;
            }
        }

        return Math.Max(0, 1 + bonus);
    }

    private static void OnHeal(EffectContext c, HealEffect e)
    {
        var target = c.Target!;
        var health = target.TryGet<Health>();
        if (health is null || !health.IsAlive)
        {
            return;
        }

        var amount = Math.Max(0, Round(e.Base));
        var effective = Math.Min(amount, health.Max - health.Current);
        health.Current += effective;
        c.World.Events.Publish(new HealApplied(c.SourceEntityId, target.Id, c.SourceId, amount, effective, amount - effective));
    }

    private static void OnShield(EffectContext c, ShieldEffect e)
    {
        var target = c.Target!;
        var state = target.TryGet<ShieldState>();
        if (state is null)
        {
            state = new ShieldState();
            target.Add(state);
        }

        var amount = Math.Max(0, Round(e.Amount));
        var expires = c.World.Tick + e.DurationTicks;
        var id = state.NextShieldId++;
        state.Shields.Add(new ShieldInstance
        {
            Id = id,
            Remaining = amount,
            ExpiresAtTick = expires,
            Source = c.SourceEntityId,
            SourceId = c.SourceId,
        });
        c.World.Events.Publish(new ShieldApplied(c.SourceEntityId, target.Id, c.SourceId, id, amount, expires));
    }

    private static void OnInterrupt(EffectContext c, InterruptEffect e) =>
        CastControl.Cancel(c.World, c.Target!, CastInterruptReason.Interrupted, c.SourceEntityId);

    private static void OnDisplacement(EffectContext c, DisplacementEffect e)
    {
        var distance = (float)e.Distance;
        if (e.Kind == DisplacementKind.Dash)
        {
            if (c.Source is null || !c.Source.IsAlive)
            {
                return;
            }

            var first = c.Targets.FirstOrDefault(t => t.IsAlive && t.Id != c.Source.Id);
            var srcPos = c.Source.TryGet<Transform>()?.Position ?? Vector2.Zero;
            var toTarget = first?.TryGet<Transform>() is { } ft ? ft.Position - srcPos : Vector2.Zero;
            var dir = e.Direction switch
            {
                DisplacementDirection.Aim => c.AimDirection,
                DisplacementDirection.Backward => -c.AimDirection,
                DisplacementDirection.TowardTarget => Or(toTarget, c.AimDirection),
                DisplacementDirection.AwayFromSource => Or(-toTarget, -c.AimDirection),
                _ => c.AimDirection,
            };
            Move(c, c.Source, dir, distance, e.Kind);
            return;
        }

        var sourcePos = c.Source?.TryGet<Transform>()?.Position;
        foreach (var t in c.Targets)
        {
            if (!t.IsAlive || t.TryGet<Transform>() is not { } tr)
            {
                continue;
            }

            var away = sourcePos.HasValue ? tr.Position - sourcePos.Value : Vector2.Zero;
            var dir = e.Direction switch
            {
                DisplacementDirection.Aim => c.AimDirection,
                DisplacementDirection.Backward => -c.AimDirection,
                DisplacementDirection.TowardTarget => Or(-away, -c.AimDirection),
                DisplacementDirection.AwayFromSource => Or(away, c.AimDirection),
                _ => c.AimDirection,
            };
            Move(c, t, dir, distance, e.Kind);
        }
    }

    private static Vector2 Or(Vector2 v, Vector2 fallback) => v.LengthSquared() > 1e-8f ? Vector2.Normalize(v) : fallback;

    private static void Move(EffectContext c, Entity mover, Vector2 dir, float distance, DisplacementKind kind)
    {
        var tr = mover.Get<Transform>();
        var from = tr.Position;
        var moved = ForcedMovement.Displace(c.World, mover, dir, distance);
        c.World.Events.Publish(new Displaced(c.SourceEntityId, mover.Id, c.SourceId, kind, from, tr.Position, moved));
    }
}

/// <summary>Cálculo e aplicação de dano. Use também para dano periódico/áreas (T-009/T-012).</summary>
public static class DamageResolver
{
    /// <summary>
    /// final = round(base × causado × recebido × (1 − resistência)), mínimo 0; escudos absorvem antes da vida.
    /// Publica DamageDealt e, se matar, CastInterrupted(Died) + EntityDied. Retorna o dano final (Amount) ou 0 se o alvo é inválido.
    /// </summary>
    public static float Deal(World world, Entity? source, Entity target, string sourceId, double baseAmount, DamageSchool school)
    {
        var health = target.TryGet<Health>();
        if (health is null || !health.IsAlive || target.IsRemoved)
        {
            return 0;
        }

        var svc = world.Combat;
        var dealt = source is null ? 1.0 : svc.DamageDealtMultiplier(world, source, target, school);
        var taken = svc.DamageTakenMultiplier(world, target, source, school);
        var resist = target.TryGet<UnitInfo>()?.ResistanceTo(school) ?? 0;
        var amount = Math.Max(0, CoreEffects.Round(baseAmount * dealt * taken * Math.Max(0, 1 - resist)));

        var remaining = amount;
        var absorbed = 0f;
        var shields = target.TryGet<ShieldState>();
        if (shields is not null)
        {
            for (var i = 0; i < shields.Shields.Count && remaining > 0; i++)
            {
                var sh = shields.Shields[i];
                var take = Math.Min(sh.Remaining, remaining);
                sh.Remaining -= take;
                remaining -= take;
                absorbed += take;
                if (sh.Remaining <= 0)
                {
                    shields.Shields.RemoveAt(i--);
                    world.Events.Publish(new ShieldExpired(target.Id, sh.Id, 0, ShieldEndReason.Depleted));
                }
            }
        }

        var applied = Math.Min(health.Current, remaining);
        var overkill = remaining - applied;
        health.Current -= applied;
        var killed = health.Current <= 0;
        world.Events.Publish(new DamageDealt(
            source?.Id ?? EntityId.None, target.Id, sourceId, school, amount, absorbed, applied, overkill, killed));
        if (killed)
        {
            CastControl.Cancel(world, target, CastInterruptReason.Died, source?.Id);
            world.Kill(target);
        }

        return amount;
    }
}

/// <summary>Cancelamento de conjuração (interrupt, morte, movimento, stun de T-009...).</summary>
public static class CastControl
{
    /// <summary>Cancela a conjuração da entidade (sem custo nem recarga) e publica CastInterrupted. Retorna false se não conjurava.</summary>
    public static bool Cancel(World world, Entity entity, CastInterruptReason reason, EntityId? by = null)
    {
        var st = entity.TryGet<AbilityState>();
        if (st?.Cast is not { } cast)
        {
            return false;
        }

        st.Cast = null;
        if (cast.LockedFacing && entity.TryGet<Movement>() is { } mv)
        {
            mv.LockFacing = false;
        }

        world.Events.Publish(new CastInterrupted(entity.Id, cast.AbilityId, reason, by));
        return true;
    }
}
