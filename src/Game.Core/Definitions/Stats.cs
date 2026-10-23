namespace Game.Core.Definitions;

/// <summary>Os cinco atributos (T-201, <c>design/progression-v0.1.md</c> §3). JSON: <c>str</c>, <c>dex</c>, <c>agi</c>, <c>int</c>, <c>vit</c>.</summary>
public sealed record AttributeSet
{
    public int Str { get; init; }
    public int Dex { get; init; }
    public int Agi { get; init; }
    public int Int { get; init; }
    public int Vit { get; init; }
}

/// <summary>Arma inicial da classe (T-201) até existirem itens (T-204): dano de arma (ARMA) e INT extra (cajado).</summary>
public sealed record StartingWeaponDefinition
{
    public double Damage { get; init; }
    public int Int { get; init; }
}

/// <summary>
/// Números das estatísticas derivadas (T-201, data/rules.json, seção <c>stats</c>; §3–§4 do design de progressão).
/// Os padrões são os do design, então valem também sem a seção.
/// </summary>
public sealed record StatRules
{
    /// <summary>Vida por ponto de VIT.</summary>
    public double HealthPerVit { get; init; } = 10;

    /// <summary>Mana máxima = <see cref="ManaBase"/> + <see cref="ManaPerInt"/> × INT.</summary>
    public double ManaBase { get; init; } = 80;
    public double ManaPerInt { get; init; } = 2;

    /// <summary>Mana por segundo a mais por ponto de INT (soma à regeneração da classe).</summary>
    public double ManaRegenPerInt { get; init; } = 0.05;

    /// <summary>Crítico: chance = base + AGI × porPonto (teto <see cref="CritMax"/>); valor × <see cref="CritMultiplier"/>.</summary>
    public double CritBase { get; init; } = 0.05;
    public double CritPerAgi { get; init; } = 0.0025;
    public double CritMax { get; init; } = 0.40;
    public double CritMultiplier { get; init; } = 1.5;

    /// <summary>Recarga do ataque básico ÷ (1 + AGI × porPonto), com bônus até <see cref="AttackSpeedMax"/>.</summary>
    public double AttackSpeedPerAgi { get; init; } = 0.005;
    public double AttackSpeedMax { get; init; } = 0.5;

    /// <summary>Redução de defesa = D / (D + base + porNível × nível do atacante), teto <see cref="DefenseMax"/>.</summary>
    public double DefenseBase { get; init; } = 40;
    public double DefensePerAttackerLevel { get; init; } = 8;
    public double DefenseMax { get; init; } = 0.6;
}
