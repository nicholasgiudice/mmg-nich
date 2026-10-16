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
    /// <summary>Regras de geração de recurso por evento (T-010).</summary>
    public IReadOnlyList<ResourceRuleDefinition> ResourceRules { get; init; } = [];
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

    /// <summary>
    /// Se definido (T-012), os efeitos não são aplicados ao concluir a conjuração: nasce um telegráfo e os efeitos
    /// são aplicados a quem estiver dentro da forma no tick de resolução.
    /// </summary>
    public TelegraphDefinition? Telegraph { get; init; }
    public IReadOnlyList<EffectDefinition> Effects { get; init; } = [];
}

/// <summary>
/// Aviso de perigo (T-012). Circle: <see cref="Radius"/>; Cone: <see cref="Length"/> (alcance) e <see cref="Angle"/> (abertura total, graus);
/// Line: <see cref="Length"/> e <see cref="Width"/>. Forma e orientação ficam fixas desde o início.
/// </summary>
public sealed record TelegraphDefinition
{
    public required TelegraphShape Shape { get; init; }

    /// <summary>Tempo de preparação (JSON: "delay", segundos). Mínimo 1 tick.</summary>
    public required int DelayTicks { get; init; }
    public double Radius { get; init; }
    public double Length { get; init; }
    public double Width { get; init; }
    public double Angle { get; init; }
    public TelegraphAnchor Anchor { get; init; } = TelegraphAnchor.Source;

    /// <summary>Quem é atingido, relativo à facção da fonte.</summary>
    public FactionFilter Affects { get; init; } = FactionFilter.Enemy;

    /// <summary>Investida: ao resolver, depois dos efeitos, a fonte é deslocada ao longo da linha (só shape line).</summary>
    public bool Charge { get; init; }
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

/// <summary>Fase de boss (T-012). Entra quando a vida cai a <see cref="HealthPercent"/> ou menos; cada fase é entrada uma única vez, em ordem.</summary>
public sealed record PhaseDefinition
{
    public required string Id { get; init; }

    /// <summary>Limiar de vida em % (0..100]. A primeira fase deve ter 100; os limiares decrescem estritamente.</summary>
    public required double HealthPercent { get; init; }

    /// <summary>Efeitos aplicados ao entrar, com o boss como fonte e alvo (invocar, criar áreas, status em si).</summary>
    public IReadOnlyList<EffectDefinition> OnEnter { get; init; } = [];

    /// <summary>Habilidades que o boss pode usar nesta fase. Null = todas as suas habilidades.</summary>
    public IReadOnlyList<string>? Abilities { get; init; }

    /// <summary>Proíbe invocar enquanto nesta fase (bloqueia habilidades com efeito summon e o próprio efeito).</summary>
    public bool NoSummons { get; init; }
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

    /// <summary>Fases de boss (T-012). Vazio = sem fases.</summary>
    public IReadOnlyList<PhaseDefinition> Phases { get; init; } = [];
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
