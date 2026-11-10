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
            .Register<ApplyStatusEffect>((c, e) => StatusManager.Apply(c.World, c.Source, c.Target!, e.Status, e.DurationTicks, c.AimPoint))
            .Register<RemoveStatusEffect>((c, e) => StatusManager.RemoveByTags(c.World, c.Target!, e.Tags.Count > 0 ? e.Tags : [e.Tag], e.Count, c.SourceEntityId));
        s.Gates.Add(new StatusGate());
        s.Modifiers.Add(new StatusModifierSource());
        s.DamageReactions.Add(new FrontalReflectReaction());
        s.DamageReactions.Add(new ReflectAndBreakReaction());
    }
}

/// <summary>
/// T-341 (Escudo Espectral): status com <c>reflect</c> devolvem a quem atacou <c>fraction</c> do dano direto recebido (mesma
/// escola, sem multiplicadores); dano vindo de status (veneno, outros reflexos) não volta. Depois, status com
/// <c>breaks.damageFraction</c> somam o dano recebido e quebram ao chegar a essa fração da vida máxima.
/// </summary>
public sealed class ReflectAndBreakReaction : IDamageReaction
{
    public void AfterDamage(World world, DamageReport damage)
    {
        var target = damage.Target;
        var active = StatusManager.Active(world, target).ToList();
        if (active.Count == 0)
        {
            return;
        }

        var direct = damage.Source is { } attacker && attacker != target && !world.Content.Statuses.ContainsKey(damage.SourceId);
        foreach (var s in active)
        {
            var def = StatusManager.Def(world, s);
            if (direct && def.Reflect is { Fraction: > 0 } r && damage.Source!.IsAlive)
            {
                var reflected = Effects.CoreEffects.Round(damage.Amount * r.Fraction);
                if (reflected > 0)
                {
                    Effects.DamageResolver.Deal(world, target, damage.Source, s.StatusId, reflected, damage.School, applyModifiers: false);
                }
            }

            if (def.Breaks is { DamageFraction: > 0 } b && target.TryGet<Health>() is { Max: > 0 } h)
            {
                s.DamageTaken += damage.Amount;
                if (s.DamageTaken >= b.DamageFraction * h.Max)
                {
                    StatusManager.Remove(world, target, s.StatusId, damage.Source?.Id ?? EntityId.None);
                }
            }
        }
    }
}

/// <summary>
/// Guarda Firme NH5 (T-061): devolve a quem atacou corpo a corpo, pela frente, <c>reflectMeleeFraction</c> do dano que o
/// arco bloqueou. Bloqueado = dano sem a redução frontal − dano recebido. O dano devolvido não passa por multiplicadores.
/// </summary>
public sealed class FrontalReflectReaction : IDamageReaction
{
    public void AfterDamage(World world, DamageReport damage)
    {
        if (!damage.Melee || damage.Source is not { IsAlive: true } attacker || damage.Source == damage.Target)
        {
            return;
        }

        foreach (var s in StatusManager.Active(world, damage.Target).ToList())
        {
            var def = StatusManager.Def(world, s);
            if (def.Frontal is not { ReflectMeleeFraction: > 0 } f
                || f.DamageTakenMultiplier >= 1
                || !StatusModifierSource.IsInFrontArc(damage.Target, attacker, f.ArcDegrees))
            {
                continue;
            }

            var mult = Math.Pow(f.DamageTakenMultiplier, s.Stacks);
            var blocked = mult <= 0 ? damage.Amount : (damage.Amount / mult) - damage.Amount;
            var reflected = Effects.CoreEffects.Round(blocked * f.ReflectMeleeFraction);
            if (reflected > 0 && attacker.IsAlive)
            {
                Effects.DamageResolver.Deal(world, damage.Target, attacker, s.StatusId, reflected, damage.School, applyModifiers: false);
            }
        }
    }
}

/// <summary>stunned: nada; feared (T-351): nada; silenced: só ataque básico.</summary>
public sealed class StatusGate : IActionGate
{
    public AbilityRejectReason? CanUse(World world, Entity actor, string abilityId, AbilityDefinition? ability, bool isBasicAttack)
    {
        if (StatusManager.IsStunned(world, actor))
        {
            return AbilityRejectReason.Stunned;
        }

        if (StatusManager.Fear(world, actor) is not null)
        {
            return AbilityRejectReason.CannotAct;
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
    /// <summary>Medo (T-351): foge da origem, ignorando a intenção de movimento.</summary>
    public Vector2? ForcedDirection(World world, Entity entity) => StatusManager.FleeDirection(world, entity);

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

    /// <summary>Status com <c>empower</c> para esta habilidade (T-061): multiplicam a potência do uso e são consumidos.</summary>
    public double ConsumeAbilityPower(World world, Entity caster, string abilityId)
    {
        var m = 1.0;
        foreach (var s in StatusManager.Active(world, caster).ToList())
        {
            if (StatusManager.Def(world, s).Empower is { } emp && emp.Abilities.Contains(abilityId))
            {
                m *= emp.Multiplier;
                StatusManager.Remove(world, caster, s.StatusId, caster.Id);
            }
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
            if (d.Modifiers.DamageTakenBySchool.Count > 0
                && d.Modifiers.DamageTakenBySchool.TryGetValue(UnitInfo.SchoolName(school), out var bySchool))
            {
                m *= Math.Pow(bySchool, s.Stacks);
            }

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
