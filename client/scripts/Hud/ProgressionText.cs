using System.Globalization;
using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;

namespace MmoRpgClient.Hud;

/// <summary>Nomes, motivos e contas de exibição das telas de personagem e de habilidades (T-221). Só apresentação.</summary>
public static class ProgressionText
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    public static readonly AttributeKind[] Attributes = [AttributeKind.Str, AttributeKind.Dex, AttributeKind.Agi, AttributeKind.Int, AttributeKind.Vit];

    public static string Short(AttributeKind a) => a switch
    {
        AttributeKind.Str => "FOR",
        AttributeKind.Dex => "DES",
        AttributeKind.Agi => "AGI",
        AttributeKind.Int => "INT",
        _ => "VIT",
    };

    public static string Name(AttributeKind a) => a switch
    {
        AttributeKind.Str => "Força",
        AttributeKind.Dex => "Destreza",
        AttributeKind.Agi => "Agilidade",
        AttributeKind.Int => "Inteligência",
        _ => "Vitalidade",
    };

    /// <summary>Chave de escala dos dados (<c>scaling</c>) do atributo.</summary>
    public static string ScalingKey(AttributeKind a) => a switch
    {
        AttributeKind.Str => "str",
        AttributeKind.Dex => "dex",
        AttributeKind.Agi => "agi",
        AttributeKind.Int => "int",
        _ => "vit",
    };

    public static int Get(CharacterStats s, AttributeKind a) => a switch
    {
        AttributeKind.Str => s.Str,
        AttributeKind.Dex => s.Dex,
        AttributeKind.Agi => s.Agi,
        AttributeKind.Int => s.Int,
        _ => s.Vit,
    };

    /// <summary>Cópia das estatísticas com <paramref name="amount"/> pontos a mais no atributo (prévia).</summary>
    public static CharacterStats With(CharacterStats s, AttributeKind a, int amount)
    {
        var copy = (CharacterStats)s.Clone();
        switch (a)
        {
            case AttributeKind.Str: copy.Str += amount; break;
            case AttributeKind.Dex: copy.Dex += amount; break;
            case AttributeKind.Agi: copy.Agi += amount; break;
            case AttributeKind.Int: copy.Int += amount; break;
            default: copy.Vit += amount; break;
        }

        return copy;
    }

    /// <summary>
    /// Atributo sugerido da classe: o que mais escala as habilidades dela (soma dos coeficientes de <c>scaling</c> do
    /// ataque básico e das habilidades, sem a arma). Derivado dos dados para não repetir a tabela do design no cliente.
    /// </summary>
    public static AttributeKind? Suggested(ClassDefinition cls, ContentCatalog content)
    {
        var sums = new Dictionary<AttributeKind, double>();
        foreach (var id in cls.Abilities.Prepend(cls.BasicAttack))
        {
            if (!content.Abilities.TryGetValue(id, out var ability))
            {
                continue;
            }

            foreach (var scaling in Scalings(ability.Effects))
            {
                foreach (var a in Attributes)
                {
                    if (scaling.TryGetValue(ScalingKey(a), out var coef))
                    {
                        sums[a] = sums.GetValueOrDefault(a) + coef;
                    }
                }
            }
        }

        return sums.Count == 0 ? null : sums.MaxBy(kv => kv.Value).Key;
    }

    private static IEnumerable<IReadOnlyDictionary<string, double>> Scalings(IEnumerable<EffectDefinition> effects)
    {
        foreach (var e in effects)
        {
            switch (e)
            {
                case DamageEffect d: yield return d.Scaling; break;
                case HealEffect h: yield return h.Scaling; break;
                case ShieldEffect s: yield return s.Scaling; break;
                case ChainEffect c:
                    foreach (var inner in Scalings(c.Effects))
                    {
                        yield return inner;
                    }

                    break;
                case ExtraTargetsEffect x:
                    foreach (var inner in Scalings(x.Effects))
                    {
                        yield return inner;
                    }

                    break;
            }
        }
    }

    public static string Reason(ProgressionRejectReason reason) => reason switch
    {
        ProgressionRejectReason.NoProgression => "Este personagem não tem progressão (encontro de teste).",
        ProgressionRejectReason.NoAttributePoints => "Sem pontos de atributo livres.",
        ProgressionRejectReason.NoSkillPoints => "Sem pontos de habilidade livres.",
        ProgressionRejectReason.NotLearnable => "Essa habilidade não recebe pontos.",
        ProgressionRejectReason.MaxRank => "Já está no NH máximo.",
        ProgressionRejectReason.LevelTooLow => "Nível do personagem insuficiente para esse NH.",
        ProgressionRejectReason.InCombat => "Só dá para redistribuir fora de combate (5 s sem lutar nem conjurar).",
        _ => reason.ToString(),
    };

    /// <summary>Intervalo do ataque básico em segundos (mesma conta do <c>AbilitySystem</c>: recarga ÷ fator da AGI).</summary>
    public static double BasicAttackSeconds(AbilityDefinition? basic, CharacterStats s, StatRules r) =>
        basic is null ? 0 : Math.Round(basic.CooldownTicks / StatMath.AttackSpeedFactor(s, r), MidpointRounding.AwayFromZero) / Game.Core.Simulation.World.TickRate;

    /// <summary>Redução que a defesa dá contra um atacante do mesmo nível (mesma conta do <c>StatMath.DefenseReduction</c>).</summary>
    public static double Reduction(double defense, int level, StatRules r) =>
        defense <= 0 ? 0 : Math.Min(r.DefenseMax, defense / (defense + r.DefenseBase + (r.DefensePerAttackerLevel * level)));

    public static string Percent(double fraction, string format = "0.#") => (fraction * 100).ToString(format, Pt) + "%";

    public static string Number(double v, string format = "0.##") => v.ToString(format, Pt);
}
