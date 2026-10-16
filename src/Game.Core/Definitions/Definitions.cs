namespace Game.Core.Definitions;

// Todos os tempos (sufixo Ticks) estão em ticks de simulação (20/s).
// No JSON ficam em segundos, sem o sufixo (cooldownTicks <-> "cooldown").

public sealed record ClassDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required double Health { get; init; }
    public required double Speed { get; init; }
    public required double Radius { get; init; }
    public required ResourceDefinition Resource { get; init; }
    public required string BasicAttack { get; init; }
    public IReadOnlyList<string> Abilities { get; init; } = [];
}

public sealed record AbilityDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public ResourceCost? Cost { get; init; }
    public int CooldownTicks { get; init; }
    public int CastTimeTicks { get; init; }
    public bool CastWhileMoving { get; init; }
    public required TargetingDefinition Targeting { get; init; }
    public IReadOnlyList<EffectDefinition> Effects { get; init; } = [];
}

public sealed record StatModifiers
{
    public double SpeedMultiplier { get; init; } = 1;
    public double DamageTakenMultiplier { get; init; } = 1;
    public double DamageDealtMultiplier { get; init; } = 1;
}

/// <summary>Redução de dano recebido só quando a fonte está dentro de um arco à frente do alvo (Guarda Firme, Guarda Esquelético).</summary>
public sealed record FrontalModifier
{
    /// <summary>Abertura total do arco em graus, centrado no facing do alvo (180 = semicírculo frontal).</summary>
    public required double ArcDegrees { get; init; }

    /// <summary>Multiplicador de dano recebido quando a fonte está no arco (0.5 = -50%).</summary>
    public required double DamageTakenMultiplier { get; init; }
}

public sealed record StatusFlags
{
    public bool Stunned { get; init; }
    public bool Rooted { get; init; }
    public bool Silenced { get; init; }
}

public sealed record PeriodicEffectDefinition
{
    public required int IntervalTicks { get; init; }
    public required IReadOnlyList<EffectDefinition> Effects { get; init; }
}

public sealed record StatusDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public required int DefaultDurationTicks { get; init; }
    public StackingMode Stacking { get; init; } = StackingMode.Refresh;
    /// <summary>Máximo de acúmulos (relevante quando Stacking = Stack).</summary>
    public int MaxStacks { get; init; } = 1;
    public IReadOnlyList<PeriodicEffectDefinition> Periodic { get; init; } = [];
    public StatModifiers Modifiers { get; init; } = new();
    public StatusFlags Flags { get; init; } = new();
    public FrontalModifier? Frontal { get; init; }
}

public sealed record AreaDefinition
{
    public required string Id { get; init; }
    public required double Radius { get; init; }
    public required int DurationTicks { get; init; }
    /// <summary>Intervalo entre aplicações (modo periodic).</summary>
    public int IntervalTicks { get; init; }
    public AreaMode Mode { get; init; } = AreaMode.Periodic;
    public FactionFilter Affects { get; init; } = FactionFilter.Enemy;
    public IReadOnlyList<EffectDefinition> Effects { get; init; } = [];
}

public sealed record AiProfileDefinition
{
    public required string Id { get; init; }
    public IReadOnlyDictionary<string, double> Parameters { get; init; } = new Dictionary<string, double>();
}

public sealed record MonsterDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required double Health { get; init; }
    public required double Speed { get; init; }
    public required double Radius { get; init; }
    public MonsterRank Rank { get; init; } = MonsterRank.Normal;
    public IReadOnlyList<string> Tags { get; init; } = [];
    /// <summary>Resistência por escola (chave = nome da escola em snake_case; 0.25 = -25% de dano).</summary>
    public IReadOnlyDictionary<string, double> Resistances { get; init; } = new Dictionary<string, double>();
    public IReadOnlyList<string> Abilities { get; init; } = [];
    public required AiProfileDefinition Ai { get; init; }
}

public sealed record ArenaDefinition
{
    public required string Id { get; init; }
    /// <summary>'#' parede, '.' chão; outros caracteres reservados. Linha 0 = y 0.</summary>
    public required IReadOnlyList<string> Lines { get; init; }
    public double Scale { get; init; } = 1;
}

public sealed record PlayerSpawnDefinition
{
    public required string Class { get; init; }
    public required PositionDefinition Position { get; init; }
}

public sealed record EnemySpawnDefinition
{
    public required string Monster { get; init; }
    public required PositionDefinition Position { get; init; }
}

public sealed record DefeatDefinition
{
    public DefeatCondition Condition { get; init; } = DefeatCondition.AllPlayersDead;
    public int? TimeLimitTicks { get; init; }
}

public sealed record EncounterDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Arena { get; init; }
    public required IReadOnlyList<PlayerSpawnDefinition> Players { get; init; }
    public required IReadOnlyList<EnemySpawnDefinition> Enemies { get; init; }
    public VictoryCondition Victory { get; init; } = VictoryCondition.AllEnemiesDead;
    public DefeatDefinition Defeat { get; init; } = new();
}
