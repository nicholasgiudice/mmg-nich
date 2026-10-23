namespace Game.Core.Definitions;

/// <summary>Atributo que recebe pontos livres (T-203). JSON e rede: <c>str</c>, <c>dex</c>, <c>agi</c>, <c>int</c>, <c>vit</c>.</summary>
public enum AttributeKind { Str, Dex, Agi, Int, Vit }

/// <summary>Multiplicador da XP de um monstro pela categoria (<see cref="MonsterRank"/>, T-203).</summary>
public sealed record XpByRank
{
    public double Normal { get; init; } = 1;
    public double Elite { get; init; } = 5;
    public double Boss { get; init; } = 30;

    public double For(MonsterRank rank) => rank switch
    {
        MonsterRank.Elite => Elite,
        MonsterRank.Boss => Boss,
        _ => Normal,
    };
}

/// <summary>
/// Níveis, XP e pontos (T-203, data/rules.json, seção <c>progression</c>; §2, §3 e §7 do design de progressão).
/// Os padrões são os números do design, então valem também sem a seção.
/// </summary>
public sealed record ProgressionRules
{
    public int MaxLevel { get; init; } = 15;

    /// <summary>XP para passar do nível L para L+1 = round(<see cref="XpCurveBase"/> × L^<see cref="XpCurveExponent"/>).</summary>
    public double XpCurveBase { get; init; } = 50;
    public double XpCurveExponent { get; init; } = 1.8;

    /// <summary>XP de um monstro = porNível × nível do monstro × categoria × modificador de diferença de nível.</summary>
    public double XpPerMonsterLevel { get; init; } = 6;
    public XpByRank XpByRank { get; init; } = new();

    /// <summary>Monstro acima do personagem: + bônus por nível, até o máximo.</summary>
    public double XpBonusPerLevelAbove { get; init; } = 0.1;
    public double XpMaxBonus { get; init; } = 0.3;

    /// <summary>Monstro abaixo do personagem: − penalidade por nível; com <see cref="XpZeroLevelsBelow"/> ou mais níveis abaixo, 0.</summary>
    public double XpPenaltyPerLevelBelow { get; init; } = 0.2;
    public int XpZeroLevelsBelow { get; init; } = 5;

    /// <summary>Em grupo, a XP é dividida entre os participantes e ganha este bônus por participante além do primeiro.</summary>
    public double GroupBonusPerMember { get; init; } = 0.1;

    /// <summary>
    /// Quem causou dano ao monstro, ou curou/protegeu quem causou, nesta janela antes da morte participa da XP.
    /// JSON: "participationWindow" (segundos).
    /// </summary>
    public int ParticipationWindowTicks { get; init; } = 30 * 20;

    /// <summary>Sem dano causado ou recebido nesta janela (e fora da tabela de ameaça dos monstros) = fora de combate. JSON: "combatTimeout".</summary>
    public int CombatTimeoutTicks { get; init; } = 5 * 20;

    /// <summary>Pontos de atributo ganhos a cada nível acima do 1.</summary>
    public int AttributePointsPerLevel { get; init; } = 5;

    /// <summary>Pontos de habilidade por nível (o nível 1 já dá os seus).</summary>
    public int SkillPointsPerLevel { get; init; } = 1;

    /// <summary>Nível do personagem exigido por NH: índice 0 = NH1. Normais: 2n − 1.</summary>
    public IReadOnlyList<int> SkillRankLevels { get; init; } = [1, 3, 5, 7, 9];

    /// <summary>Nível exigido por NH da assinatura.</summary>
    public IReadOnlyList<int> SignatureRankLevels { get; init; } = [12, 13, 15];

    /// <summary>XP para sair do nível <paramref name="level"/>; 0 no nível máximo.</summary>
    public int XpToNext(int level) =>
        level >= MaxLevel ? 0 : (int)Math.Round(XpCurveBase * Math.Pow(level, XpCurveExponent), MidpointRounding.AwayFromZero);

    /// <summary>Nível exigido para o NH <paramref name="rank"/> (1..); int.MaxValue se a lista não cobre o NH.</summary>
    public int RequiredLevel(bool signature, int rank)
    {
        var levels = signature ? SignatureRankLevels : SkillRankLevels;
        return rank >= 1 && rank <= levels.Count ? levels[rank - 1] : int.MaxValue;
    }

    /// <summary>Pontos de atributo ganhos até o nível (sem os gastos).</summary>
    public int AttributePointsAt(int level) => (level - 1) * AttributePointsPerLevel;

    /// <summary>Pontos de habilidade ganhos até o nível (sem os gastos).</summary>
    public int SkillPointsAt(int level) => level * SkillPointsPerLevel;

    /// <summary>
    /// Modificador de diferença de nível: acima +bônus por nível (teto), abaixo −penalidade por nível, e 0 a partir de
    /// <see cref="XpZeroLevelsBelow"/> abaixo.
    /// </summary>
    public double LevelDifferenceModifier(int monsterLevel, int characterLevel)
    {
        var diff = monsterLevel - characterLevel;
        if (diff >= 0)
        {
            return 1 + Math.Min(XpMaxBonus, diff * XpBonusPerLevelAbove);
        }

        var below = -diff;
        return below >= XpZeroLevelsBelow ? 0 : Math.Max(0, 1 - (below * XpPenaltyPerLevelBelow));
    }

    /// <summary>XP do monstro para um personagem, antes da divisão em grupo.</summary>
    public double MonsterXp(int monsterLevel, MonsterRank rank, int characterLevel) =>
        XpPerMonsterLevel * monsterLevel * XpByRank.For(rank) * LevelDifferenceModifier(monsterLevel, characterLevel);

    /// <summary>Parte de cada um entre <paramref name="participants"/>: XP × (1 + bônus × (n − 1)) / n, arredondada.</summary>
    public int Share(double xp, int participants) =>
        participants <= 0 ? 0 : (int)Math.Round(xp * (1 + (GroupBonusPerMember * (participants - 1))) / participants, MidpointRounding.AwayFromZero);
}
