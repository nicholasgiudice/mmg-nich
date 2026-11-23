using Game.Core.Definitions;

namespace Game.Data;

/// <summary>
/// Validação dos campos da T-422 (Inquisidor): <c>damage.selfHeal</c>, <c>damage.allyHeal</c>, <c>attackerLifesteal</c> e
/// <c>basicAttack</c> no status.
/// </summary>
internal static partial class GameDataValidator
{
    private static void ValidateDamageHeals(DamageEffect d, Ctx c, Refs refs)
    {
        if (d.SelfHeal is < 0 or > 1) { c.Error("selfHeal", "Deve estar em 0..1."); }
        if (d.AllyHeal is { } ah)
        {
            if (!(ah.Fraction > 0 && ah.Fraction <= 1)) { c.Error("allyHeal.fraction", "Deve estar em (0, 1]."); }
            c.Positive("allyHeal.radius", ah.Radius);
            if (ah.Ability is { } a) { c.Exists("allyHeal.ability", a, refs.Abilities, "habilidade"); }
        }
    }

    private static void ValidateInquisitorStatus(StatusDefinition x, Ctx ctx, Refs refs)
    {
        if (x.AttackerLifesteal is { } ls)
        {
            if (!(ls.Fraction > 0 && ls.Fraction <= 1)) { ctx.Error("attackerLifesteal.fraction", "Deve estar em (0, 1]."); }
            if (ls.BelowHealth is < 0 or > 1) { ctx.Error("attackerLifesteal.belowHealth", "Deve estar em 0..1."); }
            if (!(ls.BelowHealthMultiplier >= 1)) { ctx.Error("attackerLifesteal.belowHealthMultiplier", "Deve ser pelo menos 1."); }
            if (ls.KillHeal is < 0 or > 1) { ctx.Error("attackerLifesteal.killHeal", "Deve estar em 0..1."); }
            if (ls.Ability is { } la) { ctx.Exists("attackerLifesteal.ability", la, refs.Abilities, "habilidade"); }
        }

        if (x.BasicAttack is { } b) { ctx.Exists("basicAttack", b, refs.Abilities, "habilidade"); }
    }

    /// <summary>A habilidade que troca o ataque básico (<c>basicAttack</c> no status) não pode ser passiva nem ter telegráfo.</summary>
    private static void ValidateBasicAttackSwaps(RawContent c, Dictionary<string, AbilityDefinition> abilities, List<LoadError> errors)
    {
        foreach (var e in c.Statuses)
        {
            if (e.Value.BasicAttack is not { } id || !abilities.TryGetValue(id, out var ability))
            {
                continue;
            }

            var ctx = new Ctx(e.File, e.Path, errors);
            if (ability.Passive) { ctx.Error("basicAttack", $"'{id}' é passiva e não pode virar o ataque básico."); }
            if (ability.Telegraph is not null || ability.Ranks.Any(r => r.Telegraph is not null))
            {
                ctx.Error("basicAttack", $"'{id}' tem telegráfo, que a troca do ataque básico não suporta.");
            }
        }
    }
}
