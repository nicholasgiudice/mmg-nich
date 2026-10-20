using Game.Core.Definitions;

namespace Game.Core.Combat;

/// <summary>
/// Níveis de habilidade (NH, T-060): monta a definição efetiva de um NH aplicando, em ordem, as sobrescritas
/// de cada nível sobre a definição base. Usado pelo <see cref="ContentCatalog"/> (pré-calculado) e pela validação.
/// </summary>
public static class AbilityRanks
{
    /// <summary>Maior NH permitido para a habilidade (5, ou 3 na assinatura).</summary>
    public static int MaxAllowed(AbilityDefinition ability) =>
        ability.Signature ? AbilityDefinition.MaxSignatureRank : AbilityDefinition.MaxRank;

    /// <summary>
    /// Definição efetiva no NH <paramref name="rank"/> (limitado a 1..<see cref="AbilityDefinition.HighestRank"/>).
    /// O resultado não tem <c>Ranks</c> (é uma definição "achatada").
    /// </summary>
    public static AbilityDefinition Resolve(AbilityDefinition ability, int rank)
    {
        var target = Math.Clamp(rank, 1, ability.HighestRank);
        var current = ability with { Ranks = [] };
        foreach (var r in ability.Ranks.OrderBy(r => r.Rank))
        {
            if (r.Rank > target)
            {
                break;
            }

            current = current with
            {
                Cost = r.Cost ?? current.Cost,
                CooldownTicks = r.CooldownTicks ?? current.CooldownTicks,
                CastTimeTicks = r.CastTimeTicks ?? current.CastTimeTicks,
                Targeting = r.Targeting ?? current.Targeting,
                Telegraph = r.Telegraph ?? current.Telegraph,
                Effects = r.Effects ?? current.Effects,
                Hits = r.Hits ?? current.Hits,
                CooldownCharges = r.CooldownCharges ?? current.CooldownCharges,
            };
        }

        return current;
    }
}
