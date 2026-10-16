namespace Game.Core.Definitions;

/// <summary>Efeito de habilidade/status/área. Discriminador JSON: <c>type</c>. Durações em ticks.</summary>
public abstract record EffectDefinition
{
    /// <summary>JSON: "target" (<c>targets</c> | <c>self</c>). <c>self</c> aplica o efeito ao conjurador, não aos alvos (T-017).</summary>
    public EffectTarget Target { get; init; } = EffectTarget.Targets;
}

public sealed record DamageEffect : EffectDefinition
{
    public required double Base { get; init; }
    public DamageSchool School { get; init; } = DamageSchool.Physical;

    /// <summary>Bônus por tag do alvo (T-017): <c>{ "undead": 0.5 }</c> = +50% contra alvos com a tag. Bônus de tags diferentes somam.</summary>
    public IReadOnlyDictionary<string, double> BonusVsTags { get; init; } = new Dictionary<string, double>();
}

public sealed record HealEffect : EffectDefinition
{
    public required double Base { get; init; }
}

public sealed record ShieldEffect : EffectDefinition
{
    public required double Amount { get; init; }
    public required int DurationTicks { get; init; }
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
