namespace Game.Core.Definitions;

/// <summary>Efeito de habilidade/status/área. Discriminador JSON: <c>type</c>. Durações em ticks.</summary>
public abstract record EffectDefinition
{
    /// <summary>JSON: "target" (<c>targets</c> | <c>self</c>). <c>self</c> aplica o efeito ao conjurador, não aos alvos (T-017).</summary>
    public EffectTarget Target { get; init; } = EffectTarget.Targets;

    /// <summary>
    /// JSON: "onHit". Em habilidades com vários acertos (T-060), aplica o efeito só neste acerto (1 = o primeiro).
    /// Null = em todos. Ex.: a área da Chuva de Flechas nasce só na primeira onda (T-061).
    /// </summary>
    public int? OnHit { get; init; }
}

/// <summary>
/// Escala por estatística da fonte (T-201): valor = base + Σ coeficiente × estatística. Chaves: <c>weapon</c> (ARMA),
/// <c>str</c>, <c>dex</c>, <c>int</c>, <c>vit</c> (AGI não entra: atua na recarga e no crítico). Fonte sem estatísticas = só a base.
/// </summary>
public static class ScalingKeys
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal) { "weapon", "str", "dex", "int", "vit" };
}

public sealed record DamageEffect : EffectDefinition
{
    public required double Base { get; init; }

    /// <summary>Escala pelas estatísticas da fonte (T-201). Vazio = valor fixo.</summary>
    public IReadOnlyDictionary<string, double> Scaling { get; init; } = new Dictionary<string, double>();
    public DamageSchool School { get; init; } = DamageSchool.Physical;

    /// <summary>Bônus por tag do alvo (T-017): <c>{ "undead": 0.5 }</c> = +50% contra alvos com a tag. Bônus de tags diferentes somam.</summary>
    public IReadOnlyDictionary<string, double> BonusVsTags { get; init; } = new Dictionary<string, double>();
}

public sealed record HealEffect : EffectDefinition
{
    public required double Base { get; init; }

    /// <summary>Escala pelas estatísticas da fonte (T-201).</summary>
    public IReadOnlyDictionary<string, double> Scaling { get; init; } = new Dictionary<string, double>();
}

public sealed record ShieldEffect : EffectDefinition
{
    public required double Amount { get; init; }
    public required int DurationTicks { get; init; }

    /// <summary>Escala pelas estatísticas da fonte (T-201).</summary>
    public IReadOnlyDictionary<string, double> Scaling { get; init; } = new Dictionary<string, double>();

    /// <summary>Ao acabar o tempo, devolve a quem criou o escudo uma fração do que sobrou, em recurso (Barreira NH3+, T-061).</summary>
    public ShieldRefundDefinition? Refund { get; init; }
}

/// <summary>Devolução de recurso quando o escudo expira com valor sobrando: <c>round(sobra × fraction)</c> de <c>resource</c>.</summary>
public sealed record ShieldRefundDefinition
{
    public required string Resource { get; init; }
    public required double Fraction { get; init; }
}

public sealed record ApplyStatusEffect : EffectDefinition
{
    public required string Status { get; init; }
    /// <summary>Null = usa a duração padrão do status.</summary>
    public int? DurationTicks { get; init; }
}

public sealed record RemoveStatusEffect : EffectDefinition
{
    public string Tag { get; init; } = "";

    /// <summary>Alternativa a <see cref="Tag"/> (T-017): remove status que tenham qualquer uma destas tags (ex.: poison ou curse).</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Quantos status remover (do mais antigo ao mais novo). 0 = todos os que casarem (T-061: Muralha Viva).</summary>
    public int Count { get; init; } = 1;
}

public sealed record DisplacementEffect : EffectDefinition
{
    public required DisplacementKind Kind { get; init; }
    public required double Distance { get; init; }
    public DisplacementDirection Direction { get; init; } = DisplacementDirection.Aim;
}

public sealed record InterruptEffect : EffectDefinition;

public sealed record ThreatEffect : EffectDefinition
{
    public double Amount { get; init; }
    /// <summary>Se definido, é uma provocação com esta duração (ticks).</summary>
    public int? TauntDurationTicks { get; init; }
}

public sealed record ResourceEffect : EffectDefinition
{
    public required string Resource { get; init; }
    public required double Amount { get; init; }
}

public sealed record SpawnAreaEffect : EffectDefinition
{
    public required string Area { get; init; }
}

public sealed record SummonEffect : EffectDefinition
{
    public required string Monster { get; init; }
    public int Count { get; init; } = 1;
    public double Radius { get; init; }

    /// <summary>Máximo de invocações vivas deste monstro por fonte (0 = sem limite). Só nasce o que cabe no limite.</summary>
    public int MaxAlive { get; init; }
}

/// <summary>Como escolher alvos extras (T-061).</summary>
public enum TargetSelect
{
    /// <summary>Mais perto do alvo de origem.</summary>
    Nearest,

    /// <summary>Menor fração de vida (empate: mais perto).</summary>
    MostInjured,
}

/// <summary>
/// Saltos (T-061, Corrente Sagrada): a partir de cada alvo, salta até <see cref="Jumps"/> vezes para a entidade mais
/// próxima do último atingido (a até <see cref="Radius"/>, com linha de visão, sem repetir) e aplica <see cref="Effects"/>
/// com a potência multiplicada por <see cref="Multiplier"/>.
/// </summary>
public sealed record ChainEffect : EffectDefinition
{
    public required int Jumps { get; init; }
    public required double Radius { get; init; }
    public double Multiplier { get; init; } = 1;

    /// <summary>Facção dos saltos em relação a quem usou.</summary>
    public FactionFilter Affects { get; init; } = FactionFilter.Enemy;
    public required IReadOnlyList<EffectDefinition> Effects { get; init; }
}

/// <summary>
/// Alvos extras em volta de cada alvo (T-061): "também o inimigo mais próximo do alvo" (Marca), "também o aliado mais
/// ferido" (Cura Direta, Escudo Sagrado), "aliado mais próximo" (Barreira) ou todos a até <see cref="Radius"/>
/// (<see cref="Count"/> 0, Prisão de Gelo). Os alvos originais nunca são escolhidos. Use em habilidades de alvo único.
/// </summary>
public sealed record ExtraTargetsEffect : EffectDefinition
{
    public required double Radius { get; init; }
    public TargetSelect Select { get; init; } = TargetSelect.Nearest;

    /// <summary>Quantos alvos extras (0 = todos dentro do raio).</summary>
    public int Count { get; init; } = 1;
    public double Multiplier { get; init; } = 1;
    public FactionFilter Affects { get; init; } = FactionFilter.Enemy;
    public required IReadOnlyList<EffectDefinition> Effects { get; init; }
}

/// <summary>
/// Levanta aliados caídos (T-061, Milagre NH3): até <see cref="Count"/> aliados mortos a até <see cref="Radius"/> do alvo
/// (o mais perto primeiro) voltam com <see cref="HealthFraction"/> da vida máxima.
/// </summary>
public sealed record ReviveEffect : EffectDefinition
{
    public required double Radius { get; init; }
    public required double HealthFraction { get; init; }
    public int Count { get; init; } = 1;
}
