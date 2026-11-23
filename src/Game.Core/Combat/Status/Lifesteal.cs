using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Status;

/// <summary>
/// Vida pelo dano causado (T-407: Sede de Sangue; T-422: Selo da Luz, pelo <c>attackerLifesteal</c> no alvo). Depois de cada dano,
/// cada status com <c>lifesteal</c> em quem causou cura <c>fraction × acúmulos</c> do dano final (o <c>Amount</c>, antes dos
/// escudos do alvo). Com vida abaixo de <c>belowHealth</c> (conferida antes da cura), a fração é multiplicada por
/// <c>belowHealthMultiplier</c>; se o dano matou um inimigo, soma <c>killHeal</c> × vida máxima. A cura passa pela cura
/// recebida (<see cref="StatusManager.HealingTakenMultiplier"/>), não é crítica e sai como <see cref="HealApplied"/> de quem
/// causou nele mesmo, com a habilidade <c>lifesteal.ability</c> (ou o status) como origem. Cura sem efeito (vida cheia) não
/// publica nada. Dano indireto (refletido ou desviado), dano em si mesmo e dano sem fonte viva não geram vida.
/// </summary>
public sealed class LifestealReaction : IDamageReaction
{
    public void AfterDamage(World world, DamageReport damage)
    {
        if (damage.Indirect || damage.Source is not { IsAlive: true } attacker || attacker == damage.Target
            || attacker.TryGet<Health>() is not { Max: > 0 } health)
        {
            return;
        }

        var killedEnemy = !damage.Target.IsAlive && IsEnemy(attacker, damage.Target);
        var current = health.Current;
        foreach (var s in StatusManager.Active(world, attacker).ToList())
        {
            if (StatusManager.Def(world, s).Lifesteal is not { } ls)
            {
                continue;
            }

            var fraction = ls.Fraction * s.Stacks;
            if (ls.BelowHealth > 0 && current < ls.BelowHealth * health.Max)
            {
                fraction *= ls.BelowHealthMultiplier;
            }

            var raw = (damage.Amount * fraction) + (killedEnemy ? ls.KillHeal * health.Max : 0);
            Heal(world, attacker, health, ls.Ability ?? s.StatusId, raw);
        }

        AttackerLifesteal(world, damage, attacker, health, current, killedEnemy);
    }

    /// <summary>
    /// T-422 (Selo da Luz): status com <c>attackerLifesteal</c> em quem levou o dano curam quem bateu, se ele é aliado (mesma
    /// facção) de quem aplicou o status. Mesmas regras e números da vida pelo dano acima, com os acúmulos do status no alvo.
    /// </summary>
    private static void AttackerLifesteal(World world, DamageReport damage, Entity attacker, Health health, float current, bool killedEnemy)
    {
        if (attacker.TryGet<FactionTag>() is not { } faction)
        {
            return;
        }

        foreach (var s in StatusManager.Active(world, damage.Target).ToList())
        {
            if (StatusManager.Def(world, s).AttackerLifesteal is not { } ls
                || world.Find(s.Source)?.TryGet<FactionTag>() is not { } sealer || sealer.Faction != faction.Faction)
            {
                continue;
            }

            var fraction = ls.Fraction * s.Stacks;
            if (ls.BelowHealth > 0 && current < ls.BelowHealth * health.Max)
            {
                fraction *= ls.BelowHealthMultiplier;
            }

            var raw = (damage.Amount * fraction) + (killedEnemy ? ls.KillHeal * health.Max : 0);
            Heal(world, attacker, health, ls.Ability ?? s.StatusId, raw);
        }
    }

    private static void Heal(World world, Entity attacker, Health health, string sourceId, double raw)
    {
        var amount = Math.Max(0, Effects.CoreEffects.Round(raw * StatusManager.HealingTakenMultiplier(world, attacker)));
        var effective = Math.Min(amount, health.Max - health.Current);
        if (effective <= 0 || !health.IsAlive)
        {
            return;
        }

        health.Current += effective;
        world.Events.Publish(new HealApplied(attacker.Id, attacker.Id, sourceId, amount, effective, amount - effective, false));
    }

    private static bool IsEnemy(Entity a, Entity b) =>
        a.TryGet<FactionTag>() is { } fa && b.TryGet<FactionTag>() is { } fb && fa.Faction != fb.Faction;
}
