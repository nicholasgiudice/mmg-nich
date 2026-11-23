using System.Numerics;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Stats;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat;

/// <summary>
/// Crítico vindo dos status de quem ataca (T-431, <see cref="StatusDefinition.Crit"/>): chance a mais contra alvos longe
/// (Mira Firme), multiplicador próprio (Mira Firme NH5) e crítico garantido (Olho de Falcão). Vale só para efeitos com
/// habilidade de origem (habilidades e ataque básico); dano periódico de status e de área segue <see cref="StatMath.RollCrit"/>.
/// Sem status que se aplique, o sorteio é o mesmo de antes (o mesmo consumo do RNG da simulação).
/// </summary>
public static class StatusCrit
{
    /// <summary>O que os status de quem ataca somam contra um alvo.</summary>
    public readonly record struct Modifiers(bool Any, double Bonus, double? Multiplier, bool Guaranteed);

    /// <summary>
    /// Sorteia o crítico de um dano e devolve o fator (1 sem crítico; o multiplicador da regra, ou o do status, com crítico).
    /// Crítico garantido não consome o RNG; os outros casos consomem um número, como <see cref="StatMath.RollCrit"/>.
    /// </summary>
    public static (bool Critical, double Factor) Roll(World world, Entity? source, Entity target, AbilityDefinition? ability)
    {
        var rules = world.Content.Rules.Stats;
        var m = ability is null || source is null ? default : For(world, source, target);
        if (!m.Any)
        {
            var normal = StatMath.RollCrit(world, source);
            return (normal, normal ? rules.CritMultiplier : 1);
        }

        var crit = source!.TryGet<CharacterStats>() is { } s
            && (m.Guaranteed || world.Random.NextFloat() < StatMath.CritChance(s, rules, m.Bonus));
        return (crit, crit ? m.Multiplier ?? rules.CritMultiplier : 1);
    }

    /// <summary>
    /// Chance de crítico de <paramref name="source"/> contra <paramref name="target"/> com esta habilidade (1 com crítico
    /// garantido). Só quem tem estatísticas de personagem pode critar (0 sem elas).
    /// </summary>
    public static double Chance(World world, Entity? source, Entity target, AbilityDefinition? ability)
    {
        if (source?.TryGet<CharacterStats>() is not { } s)
        {
            return 0;
        }

        var m = ability is null ? default : For(world, source, target);
        return m.Guaranteed ? 1 : StatMath.CritChance(s, world.Content.Rules.Stats, m.Bonus);
    }

    /// <summary>
    /// Soma dos blocos <c>crit</c> dos status ativos de <paramref name="source"/> que valem contra <paramref name="target"/>
    /// agora: bônus × acúmulos, o maior multiplicador e se algum garante o crítico.
    /// </summary>
    public static Modifiers For(World world, Entity source, Entity target)
    {
        var any = false;
        var bonus = 0.0;
        double? multiplier = null;
        var guaranteed = false;
        double? distance = null;
        foreach (var s in StatusManager.Active(world, source))
        {
            if (StatusManager.Def(world, s).Crit is not { } crit)
            {
                continue;
            }

            if (crit.MinDistance > 0 && (distance ??= EdgeDistance(source, target)) < crit.MinDistance)
            {
                continue;
            }

            any = true;
            bonus += crit.Bonus * s.Stacks;
            guaranteed |= crit.Guaranteed;
            if (crit.Multiplier is { } mult)
            {
                multiplier = Math.Max(multiplier ?? mult, mult);
            }
        }

        return new Modifiers(any, bonus, multiplier, guaranteed);
    }

    /// <summary>Distância como a do alcance: do centro de quem ataca à borda do alvo (distância − raio do alvo), mínimo 0.</summary>
    public static double EdgeDistance(Entity source, Entity target)
    {
        if (source.TryGet<Transform>() is not { } a || target.TryGet<Transform>() is not { } b)
        {
            return 0;
        }

        return Math.Max(0, Vector2.Distance(a.Position, b.Position) - (target.TryGet<Collider>()?.Radius ?? 0f));
    }
}

/// <summary>
/// Alcance a mais vindo de status (T-431, <see cref="StatusDefinition.RangeBonus"/>): só para habilidades com a tag
/// <see cref="Tag"/>, para não alongar golpes corpo a corpo. O cliente pode usar <see cref="Of"/> com os status do snapshot
/// (que já vão na rede) para desenhar a mira com o alcance somado.
/// </summary>
public static class RangeBonus
{
    /// <summary>Tag das habilidades à distância (o Disparo e as habilidades do Arqueiro já a têm).</summary>
    public const string Tag = "ranged";

    /// <summary>Tiles a mais no alcance de <paramref name="ability"/> para <paramref name="caster"/> agora.</summary>
    public static double For(World world, Entity caster, AbilityDefinition ability) =>
        Of(world.Content, caster.TryGet<StatusEffects>(), world.Tick, ability);

    /// <summary>Soma de <c>rangeBonus</c> × acúmulos dos status ativos no tick; 0 se a habilidade não tem a tag.</summary>
    public static double Of(ContentCatalog content, StatusEffects? statuses, long tick, AbilityDefinition ability)
    {
        if (statuses is null || !ability.Tags.Contains(Tag))
        {
            return 0;
        }

        var bonus = 0.0;
        foreach (var s in statuses.Active)
        {
            if (s.ExpiresAtTick > tick && content.Statuses.TryGetValue(s.StatusId, out var def) && def.RangeBonus > 0)
            {
                bonus += def.RangeBonus * s.Stacks;
            }
        }

        return bonus;
    }
}

/// <summary>
/// Dano menor em quem está atrás (T-431, <see cref="DamageEffect.BehindFraction"/>, Flecha Perfurante): entre os alvos do
/// uso, o mais perto de quem usou (empate: menor id) leva o dano inteiro; os outros, a fração.
/// </summary>
public static class PierceFalloff
{
    /// <summary>1 para o primeiro alvo (ou sem <c>behindFraction</c>, ou com um alvo só); a fração para os outros.</summary>
    public static double Factor(EffectContext c, DamageEffect e) =>
        e.BehindFraction is { } fraction && c.Target is { } target && c.Targets.Count > 1 && First(c) != target ? fraction : 1;

    /// <summary>
    /// O alvo mais perto da fonte entre todos os do uso (vivos ou não: quem morreu no acerto continua sendo o primeiro).
    /// Sem fonte com posição, o primeiro da lista.
    /// </summary>
    public static Entity First(EffectContext c)
    {
        if (c.Source?.TryGet<Transform>() is not { } src)
        {
            return c.Targets[0];
        }

        Entity? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var t in c.Targets)
        {
            var d = t.TryGet<Transform>() is { } tr ? Vector2.DistanceSquared(src.Position, tr.Position) : float.PositiveInfinity;
            if (best is null || d < bestDistance || (d == bestDistance && t.Id.Value < best.Id.Value))
            {
                best = t;
                bestDistance = d;
            }
        }

        return best!;
    }
}
