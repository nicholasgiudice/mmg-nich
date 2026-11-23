using Game.Core.Definitions;

namespace Game.Data;

/// <summary>
/// Validação dos campos do Atirador de Elite (T-431): <c>damage.behindFraction</c>, <c>damage.ignoreArmor</c>,
/// <c>status.crit</c> e <c>status.rangeBonus</c>.
/// </summary>
internal static partial class GameDataValidator
{
    private static void ValidateMarksmanDamage(DamageEffect d, Ctx c)
    {
        if (d.BehindFraction is { } bf && !(bf > 0 && bf <= 1)) { c.Error("behindFraction", "Deve estar em (0, 1]."); }
        if (!(d.IgnoreArmor >= 0 && d.IgnoreArmor <= 1)) { c.Error("ignoreArmor", "Deve estar em 0..1."); }
    }

    private static void ValidateMarksmanStatus(StatusDefinition x, Ctx ctx)
    {
        if (x.Crit is { } crit)
        {
            if (!(crit.Bonus >= 0 && crit.Bonus <= 1)) { ctx.Error("crit.bonus", "Deve estar em 0..1."); }
            ctx.NonNegative("crit.minDistance", crit.MinDistance);
            if (crit.Multiplier is { } m && !(m >= 1 && !double.IsInfinity(m))) { ctx.Error("crit.multiplier", "Deve ser pelo menos 1."); }

            if (crit.Bonus == 0 && crit.Multiplier is null && !crit.Guaranteed)
            {
                ctx.Error("crit", "Informe bonus, multiplier ou guaranteed.");
            }
        }

        ctx.NonNegative("rangeBonus", x.RangeBonus);
    }
}
