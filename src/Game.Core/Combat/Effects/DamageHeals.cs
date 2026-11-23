using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Effects;

/// <summary>
/// Cura pelo dano de um efeito <c>damage</c> (T-422, Inquisidor): <c>selfHeal</c> cura quem causou (Julgamento NH5) e
/// <c>allyHeal</c> cura os aliados perto dele (Ira Divina), numa fração do dano final do acerto (o <c>Amount</c>, depois da
/// defesa e antes dos escudos do alvo, sem a parte desviada a um protetor; o mesmo número da vida pelo dano da T-407). Cada
/// cura passa pela cura recebida de quem recebe, não é crítica e sai como <see cref="HealApplied"/> de quem causou o dano.
/// Cura sem efeito (vida cheia) não publica nada. Fonte morta ou dano em si mesmo não curam.
/// </summary>
public static class DamageHeals
{
    public static void Apply(EffectContext c, DamageEffect e, float dealt)
    {
        if (dealt <= 0 || c.Source is not { IsAlive: true } source || source == c.Target)
        {
            return;
        }

        if (e.SelfHeal > 0)
        {
            Heal(c.World, source, source, c.SourceId, dealt * e.SelfHeal);
        }

        if (e.AllyHeal is { } ah && source.TryGet<FactionTag>() is { } faction && source.TryGet<Transform>() is { } tr)
        {
            var allies = c.World.Spatial.InRadius(
                tr.Position,
                (float)ah.Radius,
                x => x.IsAlive && !x.IsRemoved && x.Has<Health>() && x.TryGet<FactionTag>()?.Faction == faction.Faction
                    && (ah.IncludeSelf || x != source));
            foreach (var ally in allies)
            {
                Heal(c.World, source, ally, ah.Ability ?? c.SourceId, dealt * ah.Fraction);
            }
        }
    }

    /// <summary>Cura <paramref name="raw"/> × cura recebida em <paramref name="target"/>, em nome de <paramref name="healer"/>; sem efeito, nada.</summary>
    public static void Heal(World world, Entity healer, Entity target, string sourceId, double raw)
    {
        if (target.TryGet<Health>() is not { IsAlive: true } health)
        {
            return;
        }

        var amount = Math.Max(0, CoreEffects.Round(raw * Status.StatusManager.HealingTakenMultiplier(world, target)));
        var effective = Math.Min(amount, health.Max - health.Current);
        if (effective <= 0)
        {
            return;
        }

        health.Current += effective;
        world.Events.Publish(new HealApplied(healer.Id, target.Id, sourceId, amount, effective, amount - effective, false));
    }
}
