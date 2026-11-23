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

    private static void OnDamage(EffectContext c, DamageEffect e)
    {
        // T-201: base + escala pelas estatísticas da fonte; crítico só para quem tem estatísticas.
        var crit = Stats.StatMath.RollCrit(c.World, c.Source);
        var value = Stats.StatMath.Scaled(e.Base, e.Scaling, c.Source) * Stats.StatMath.MonsterPower(c.World, c.Source)
            * TagBonus(c.Target!, e) * LowHealthBonus(c.Target!, e) * c.Power * CritFactor(c.World, crit);
        var dealt = DamageResolver.Deal(
            c.World, c.Source, c.Target!, c.SourceId, value, e.School, melee: c.Ability?.IsMelee ?? false, critical: crit,
            threatMultiplier: e.ThreatMultiplier);
        DamageHeals.Apply(c, e, dealt); // T-422: selfHeal e allyHeal
        if (e.ResetCooldownOnKill && !c.Target!.IsAlive)
        {
            ResetCooldown(c);
        }
    }

    /// <summary>
    /// T-407 (Execução): 1 + <c>lowHealthBonus.bonus</c> se o alvo está abaixo de <c>belowHealth</c> da vida máxima agora
    /// (antes deste dano); senão 1.
    /// </summary>
    public static double LowHealthBonus(Entity target, DamageEffect e) =>
        e.LowHealthBonus is { } b && target.TryGet<Health>() is { Max: > 0 } h && h.Current < b.BelowHealth * h.Max
            ? 1 + b.Bonus
            : 1;

    /// <summary>
    /// T-407 (Execução NH5): o golpe matou, a recarga da habilidade de origem volta a zero (pronta neste tick). Com cargas,
    /// a carga em recarga volta no tick seguinte (<c>RechargeCooldownCharges</c>). Sem habilidade (status, área) não faz nada.
    /// O cliente vê a recarga nova pelo <c>AbilityState</c> do snapshot.
    /// </summary>
    private static void ResetCooldown(EffectContext c)
    {
        if (c.Ability is not { } ability || c.Source?.TryGet<AbilityState>() is not { } st
            || !st.ReadyAtTick.TryGetValue(ability.Id, out var ready) || ready <= c.World.Tick)
        {
            return;
        }

        st.ReadyAtTick[ability.Id] = c.World.Tick;
    }

    private static double CritFactor(World world, bool crit) => crit ? world.Content.Rules.Stats.CritMultiplier : 1;

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

        var crit = Stats.StatMath.RollCrit(c.World, c.Source); // crítico vale para cura (design §10)
        var raw = (Stats.StatMath.Scaled(e.Base, e.Scaling, c.Source) * Stats.StatMath.MonsterPower(c.World, c.Source))
            + (e.MaxHealthFraction * health.Max); // T-421: fração da vida máxima de quem recebe
        var amount = Math.Max(0, Round(raw * c.Power * CritFactor(c.World, crit) * Status.StatusManager.HealingTakenMultiplier(c.World, target)));
        var effective = Math.Min(amount, health.Max - health.Current);
        health.Current += effective;
        c.World.Events.Publish(new HealApplied(c.SourceEntityId, target.Id, c.SourceId, amount, effective, amount - effective, crit));
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

        var crit = Stats.StatMath.RollCrit(c.World, c.Source); // e para escudo
        var amount = Math.Max(0, Round(Stats.StatMath.Scaled(e.Amount, e.Scaling, c.Source) * Stats.StatMath.MonsterPower(c.World, c.Source)
            * c.Power * CritFactor(c.World, crit)));
        var expires = c.World.Tick + e.DurationTicks;
        var id = state.NextShieldId++;
        state.Shields.Add(new ShieldInstance
        {
            Id = id,
            Remaining = amount,
            ExpiresAtTick = expires,
            Source = c.SourceEntityId,
            SourceId = c.SourceId,
            RefundResource = e.Refund?.Resource,
            RefundFraction = (float)(e.Refund?.Fraction ?? 0),
        });
        c.World.Events.Publish(new ShieldApplied(c.SourceEntityId, target.Id, c.SourceId, id, amount, expires, crit));
    }

    /// <summary>Cancela a conjuração do alvo e quebra os status dele com <c>breaks.interrupt</c> (T-341, Escudo Espectral).</summary>
    private static void OnInterrupt(EffectContext c, InterruptEffect e)
    {
        CastControl.Cancel(c.World, c.Target!, CastInterruptReason.Interrupted, c.SourceEntityId);
        Status.StatusManager.BreakOnInterrupt(c.World, c.Target!, c.SourceEntityId);
    }

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
            if (e.Direction == DisplacementDirection.TowardTarget && first is not null && toTarget.LengthSquared() > 1e-8f)
            {
                // T-406: avançar até o alvo (inimigo ou aliado) para no contato, sem passar dele.
                var contact = (c.Source.TryGet<Collider>()?.Radius ?? 0f) + (first.TryGet<Collider>()?.Radius ?? 0f);
                distance = Math.Min(distance, Math.Max(0f, toTarget.Length() - contact));
            }

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

            if (e.Kind == DisplacementKind.Knockback && Status.StatusManager.IsKnockbackImmune(c.World, t) && !IsAlly(c.Source, t))
            {
                continue; // T-406: imune a empurrão de inimigo (Bastião NH5)
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

    private static bool IsAlly(Entity? source, Entity target) =>
        source is not null && source.TryGet<FactionTag>() is { } a && target.TryGet<FactionTag>() is { } b && a.Faction == b.Faction;

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
    /// final = round(base × causado × recebido × (1 − resistência) × (1 − defesa)), mínimo 0; escudos absorvem antes da vida.
    /// Defesa (T-201): armadura contra físico, resistência contra o resto, pela fórmula das <c>StatRules</c>.
    /// Publica DamageDealt e, se matar, CastInterrupted(Died) + EntityDied. Retorna o dano final (Amount) ou 0 se o alvo é inválido.
    /// <paramref name="melee"/>: golpe corpo a corpo (reflexo da Guarda Firme, T-061). <paramref name="applyModifiers"/> false =
    /// ignora multiplicadores de causado/recebido (dano devolvido, que já vem calculado).
    /// Depois do dano, avisa as reações registradas (<see cref="IDamageReaction"/>).
    /// <para>Desvio (T-406): se o alvo tem status com <c>redirect</c>, a fração desviada sai do valor base antes de tudo
    /// (antes dos modificadores e escudos do alvo) e é aplicada no protetor como um novo dano do mesmo atacante, com a defesa
    /// e os modificadores do protetor e a redução do status; esse dano não é desviado de novo. Dano sem modificadores
    /// (<paramref name="applyModifiers"/> false, já calculado) não é desviado.</para>
    /// <paramref name="threatMultiplier"/>: ameaça = este número × dano, no lugar de <c>rules.threat.damageMultiplier</c>.
    /// Retorna o dano final no alvo (sem a parte desviada).
    /// </summary>
    public static float Deal(
        World world, Entity? source, Entity target, string sourceId, double baseAmount, DamageSchool school,
        bool melee = false, bool applyModifiers = true, bool critical = false, double? threatMultiplier = null)
    {
        var health = target.TryGet<Health>();
        if (health is null || !health.IsAlive || target.IsRemoved)
        {
            return 0;
        }

        var shares = applyModifiers && baseAmount > 0 ? Status.DamageRedirect.Shares(world, target) : [];
        if (shares.Count == 0)
        {
            return Apply(world, source, target, health, sourceId, baseAmount, school, melee, applyModifiers, critical, threatMultiplier);
        }

        var kept = baseAmount * (1 - shares.Sum(x => x.Fraction));
        var amount = Apply(world, source, target, health, sourceId, kept, school, melee, applyModifiers, critical, threatMultiplier);
        foreach (var share in shares)
        {
            if (share.Protector.TryGet<Health>() is not { IsAlive: true } ph || share.Protector.IsRemoved)
            {
                continue;
            }

            var part = baseAmount * share.Fraction * (1 - share.Redirect.Reduction);
            var taken = Apply(
                world, source, share.Protector, ph, sourceId, part, school, melee: false, applyModifiers: true, critical, threatMultiplier,
                redirected: true);
            world.Events.Publish(new Status.DamageRedirected(
                source?.Id ?? EntityId.None, target.Id, share.Protector.Id, sourceId, share.Status.StatusId, taken));
        }

        return amount;
    }

    /// <summary>
    /// O dano num alvo, sem desvio: modificadores, defesa, escudos, vida, eventos e reações. <paramref name="redirected"/>:
    /// a parte desviada para um protetor (T-406); ela e o dano sem modificadores (reflexos) vão às reações como indiretos
    /// (<see cref="DamageReport.Indirect"/>, T-407: não geram vida).
    /// </summary>
    private static float Apply(
        World world, Entity? source, Entity target, Health health, string sourceId, double baseAmount, DamageSchool school,
        bool melee, bool applyModifiers, bool critical, double? threatMultiplier, bool redirected = false)
    {
        var svc = world.Combat;
        var dealt = source is null || !applyModifiers ? 1.0 : svc.DamageDealtMultiplier(world, source, target, school);
        var taken = applyModifiers ? svc.DamageTakenMultiplier(world, target, source, school) : 1.0;
        var resist = target.TryGet<UnitInfo>()?.ResistanceTo(school) ?? 0;
        var defense = applyModifiers ? Stats.StatMath.DefenseReduction(world, target, source, school) : 0;
        var amount = Math.Max(0, CoreEffects.Round(baseAmount * dealt * taken * Math.Max(0, 1 - resist) * (1 - defense)));

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
        if (applied >= health.Current && Status.StatusManager.IsUndying(world, target))
        {
            applied = Math.Max(0, health.Current - 1); // T-421 (Santuário): fica com 1 de vida
            overkill = 0;
        }
        health.Current -= applied;
        var killed = health.Current <= 0;
        world.Events.Publish(new DamageDealt(
            source?.Id ?? EntityId.None, target.Id, sourceId, school, amount, absorbed, applied, overkill, killed, critical)
        {
            ThreatMultiplier = threatMultiplier,
        });
        if (killed)
        {
            CastControl.Cancel(world, target, CastInterruptReason.Died, source?.Id);
            world.Kill(target);
        }

        if (amount > 0)
        {
            svc.AfterDamage(world, new DamageReport(source, target, sourceId, school, amount, melee, Indirect: redirected || !applyModifiers));
        }

        return amount;
    }
}

/// <summary>Cancelamento de conjuração (interrupt, morte, movimento, stun de T-009...).</summary>
public static class CastControl
{
    /// <summary>
    /// Cancela a conjuração da entidade (sem custo nem recarga, salvo <c>cooldownOnInterrupt</c> numa interrupção) e publica
    /// CastInterrupted. Retorna false se não conjurava.
    /// </summary>
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

        // T-351: habilidade com cooldownOnInterrupt entra na recarga cheia quando interrompida.
        if (reason == CastInterruptReason.Interrupted
            && world.Content.AbilityAt(cast.AbilityId, st.RankOf(cast.AbilityId)) is { CooldownOnInterrupt: true, CooldownTicks: > 0 } def)
        {
            st.ReadyAtTick[cast.AbilityId] = world.Tick + def.CooldownTicks;
        }

        world.Events.Publish(new CastInterrupted(entity.Id, cast.AbilityId, reason, by));
        return true;
    }
}
