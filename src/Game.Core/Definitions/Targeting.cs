namespace Game.Core.Definitions;

/// <summary>Como uma habilidade escolhe alvos. Discriminador JSON: <c>type</c>.</summary>
public abstract record TargetingDefinition;

public sealed record SelfTargeting : TargetingDefinition;

public sealed record UnitTargeting : TargetingDefinition
{
    public required double Range { get; init; }
    public FactionFilter Faction { get; init; } = FactionFilter.Enemy;
}

public sealed record MeleeArcTargeting : TargetingDefinition
{
    public required double Range { get; init; }
    /// <summary>Abertura do arco em graus.</summary>
    public required double Angle { get; init; }
}

public sealed record LineTargeting : TargetingDefinition
{
    public required double Length { get; init; }
    public required double Width { get; init; }

    // ---- T-441 (Piromante) ----

    /// <summary>
    /// Explodir no primeiro que acerta (T-441, Bola de Fogo): o disparo para no primeiro alvo da linha (o mais à frente de
    /// quem usou) e os alvos passam a ser todos a até este raio (tiles) dele. Sem alvo na linha, não explode (nenhum alvo).
    /// Null = linha comum (todos na linha).
    /// </summary>
    public double? ExplodeRadius { get; init; }

    // ---- fim T-441 ----
}

public sealed record CircleAtPointTargeting : TargetingDefinition
{
    public required double Range { get; init; }
    public required double Radius { get; init; }
}

public sealed record CircleSelfTargeting : TargetingDefinition
{
    public required double Radius { get; init; }
}
