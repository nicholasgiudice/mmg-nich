namespace Game.Core.Combat.Threat;

/// <summary>Regras globais de ameaça (seção <c>"threat"</c> de <c>data/rules.json</c>).</summary>
public sealed record ThreatRules
{
    /// <summary>Ameaça gerada por ponto de dano causado (1:1 por padrão).</summary>
    public double DamageMultiplier { get; init; } = 1.0;

    /// <summary>
    /// Ameaça gerada por ponto de cura efetiva, para cada monstro engajado com o alvo curado
    /// (reduzida em relação ao dano).
    /// </summary>
    public double HealMultiplier { get; init; } = 0.5;

    /// <summary>Um alvo só toma a atenção do monstro se tiver mais que (atual x esta razão) de ameaça.</summary>
    public double OvertakeRatio { get; init; } = 1.1;

    /// <summary>Ameaça inicial dada a membros do mesmo grupo quando um deles entra em combate.</summary>
    public double GroupAggroThreat { get; init; } = 1.0;

    /// <summary>Ameaça inicial por aggro de proximidade.</summary>
    public double ProximityThreat { get; init; } = 1.0;
}
