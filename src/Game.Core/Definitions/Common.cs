namespace Game.Core.Definitions;

/// <summary>Posição em unidades de mundo (1 unidade = 1 tile).</summary>
public sealed record PositionDefinition(double X, double Y);

public sealed record ResourceDefinition
{
    /// <summary>Id do recurso, ex.: "vigor", "focus", "mana", "faith".</summary>
    public required string Type { get; init; }
    public required double Max { get; init; }
    public double Initial { get; init; }
    /// <summary>Regeneração por segundo; pode ser negativa.</summary>
    public double RegenPerSecond { get; init; }
    /// <summary>Se true, a regeneração/decaimento passivo só vale fora de combate.</summary>
    public bool PassiveOnlyOutOfCombat { get; init; }
    /// <summary>Ticks sem dar/receber dano ou usar habilidade para contar como fora de combate (padrão 5 s).</summary>
    public int CombatTimeoutTicks { get; init; } = 100;
}

public sealed record ResourceCost
{
    public required string Resource { get; init; }
    public required double Amount { get; init; }
}
