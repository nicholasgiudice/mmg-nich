namespace Game.Core.Definitions;

// Todos os tempos (sufixo Ticks) estão em ticks de simulação (20/s).
// No JSON ficam em segundos, sem o sufixo (cooldownTicks <-> "cooldown").

public sealed record ClassDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Vida máxima fixa. Com <see cref="Attributes"/>, a vida vem de <see cref="BaseHealth"/> + VIT (T-201) e este campo é ignorado.</summary>
    public double Health { get; init; }
    public required double Speed { get; init; }
    public required double Radius { get; init; }
    public required ResourceDefinition Resource { get; init; }
    public required string BasicAttack { get; init; }

    /// <summary>Perfil inicial (nível 1) dos atributos (T-201). Null = classe sem atributos (vida e recurso fixos, sem escala nem crítico).</summary>
    public AttributeSet? Attributes { get; init; }

    /// <summary>Vida base da classe: vida máxima = base + VIT × <c>healthPerVit</c> + (nível − 1) × <see cref="HealthGrowth"/>.</summary>
    public double BaseHealth { get; init; }
    public double HealthGrowth { get; init; }

    /// <summary>Itens com que a classe começa equipada (T-204; nível do item 2 calibra o protótipo). Comuns, sem afixos.</summary>
    public IReadOnlyList<StartingItemDefinition> StartingEquipment { get; init; } = [];
    public IReadOnlyList<string> Abilities { get; init; } = [];

    /// <summary>
    /// Consumíveis (T-017): ids de habilidade com uso limitado por <see cref="AbilityDefinition.Charges"/>. Ficam separados de
    /// <see cref="Abilities"/>; o jogador os conhece mas eles não entram em <c>AbilityState.Known</c> (ver <c>AbilityState.Consumables</c>).
    /// </summary>
    public IReadOnlyList<string> Consumables { get; init; } = [];

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

    /// <summary>Usos por encontro (T-017). Null = ilimitado. Esgotadas: <c>AbilityRejectReason.NoCharges</c>.</summary>
    public int? Charges { get; init; }
    public required TargetingDefinition Targeting { get; init; }

    /// <summary>
    /// Se definido (T-012), os efeitos não são aplicados ao concluir a conjuração: nasce um telegráfo e os efeitos
    /// são aplicados a quem estiver dentro da forma no tick de resolução.
    /// </summary>
    public TelegraphDefinition? Telegraph { get; init; }
    public IReadOnlyList<EffectDefinition> Effects { get; init; } = [];

    /// <summary>Habilidade de assinatura (T-060): no máximo <see cref="MaxSignatureRank"/> níveis em vez de <see cref="MaxRank"/>.</summary>
    public bool Signature { get; init; }

    /// <summary>
    /// Acertos por uso (T-060). Null = 1. Os acertos extras repetem os efeitos (ou o telegráfo) a cada intervalo;
    /// alvo único morto passa para o inimigo mais próximo.
    /// </summary>
    public AbilityHitsDefinition? Hits { get; init; }

    /// <summary>Usos guardados que recarregam um a um (T-060; ex.: Recuo Rápido com 2 cargas). 1 = recarga normal.</summary>
    public int CooldownCharges { get; init; } = 1;

    /// <summary>
    /// Níveis de habilidade a partir do NH2 (T-060). Cada nível sobrescreve só o que declara e herda o resto do nível
    /// anterior. A definição efetiva de um NH vem de <c>ContentCatalog.AbilityAt</c>.
    /// </summary>
    public IReadOnlyList<AbilityRankDefinition> Ranks { get; init; } = [];

    public const int MaxRank = 5;
    public const int MaxSignatureRank = 3;

    /// <summary>Maior NH desta habilidade (1 + níveis declarados).</summary>
    public int HighestRank => 1 + Ranks.Count;

    /// <summary>Número de acertos efetivo (pelo menos 1).</summary>
    public int HitCount => Math.Max(1, Hits?.Count ?? 1);

    /// <summary>Corpo a corpo (arco curto). Usado pelo reflexo da Guarda Firme (T-061).</summary>
    public bool IsMelee => Targeting is MeleeArcTargeting;
}

/// <summary>Acertos de um uso (T-060). <see cref="IntervalTicks"/> = tempo entre acertos (JSON "interval", segundos).</summary>
public sealed record AbilityHitsDefinition
{
    public int Count { get; init; } = 1;
    public int IntervalTicks { get; init; }
}

/// <summary>
/// Um nível de habilidade (NH2 em diante). Campos nulos herdam do nível anterior; <see cref="Effects"/> declarado
/// substitui a lista inteira de efeitos.
/// </summary>
public sealed record AbilityRankDefinition
{
    public required int Rank { get; init; }
    public ResourceCost? Cost { get; init; }
    public int? CooldownTicks { get; init; }
    public int? CastTimeTicks { get; init; }
    public TargetingDefinition? Targeting { get; init; }
    public TelegraphDefinition? Telegraph { get; init; }
    public IReadOnlyList<EffectDefinition>? Effects { get; init; }
    public AbilityHitsDefinition? Hits { get; init; }
    public int? CooldownCharges { get; init; }
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

    /// <summary>Multiplica a duração de status com tag <c>control</c> recebidos por quem tem este status (0,5 = metade). T-017.</summary>
    public double ControlDurationMultiplier { get; init; } = 1;
}

/// <summary>Redução de dano recebido só quando a fonte está dentro de um arco à frente do alvo (Guarda Firme, Guarda Esquelético).</summary>
public sealed record FrontalModifier
{
    /// <summary>Abertura total do arco em graus, centrado no facing do alvo (180 = semicírculo frontal).</summary>
    public required double ArcDegrees { get; init; }

    /// <summary>Multiplicador de dano recebido quando a fonte está no arco (0.5 = -50%).</summary>
    public required double DamageTakenMultiplier { get; init; }

    /// <summary>Fração do dano corpo a corpo BLOQUEADO pelo arco devolvida a quem atacou (Guarda Firme NH5, T-061). 0 = nada.</summary>
    public double ReflectMeleeFraction { get; init; }
}

/// <summary>
/// Fortalece o próximo uso de uma das <see cref="Abilities"/> de quem tem o status (Recuo Rápido NH3, T-061):
/// a potência daquele uso (dano, cura, escudo, em todos os acertos) é multiplicada e o status é consumido.
/// </summary>
public sealed record EmpowerDefinition
{
    public required IReadOnlyList<string> Abilities { get; init; }
    public required double Multiplier { get; init; }
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

    /// <summary>Enquanto ativo, status novos com qualquer uma destas tags não pegam (Purificação NH5, T-061).</summary>
    public IReadOnlyList<string> ImmuneToTags { get; init; } = [];

    /// <summary>Fortalece o próximo uso de certas habilidades e é consumido (T-061).</summary>
    public EmpowerDefinition? Empower { get; init; }
}

public sealed record AreaDefinition
{
    public required string Id { get; init; }
    public required double Radius { get; init; }
    public required int DurationTicks { get; init; }
    /// <summary>Intervalo entre aplicações (modo periodic).</summary>
    public int IntervalTicks { get; init; }

    /// <summary>Tempo de armar (JSON: "armTime", segundos). Antes disso a área não dispara. 0 = armada na criação. T-017.</summary>
    public int ArmTimeTicks { get; init; }
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

    /// <summary>
    /// Nível padrão do monstro (T-203): XP ao matar e nível do atacante na defesa. A vida e os números das habilidades
    /// são os do nível 1 e crescem com o nível em que ele surge (T-206, <see cref="MonsterScalingRules"/>).
    /// </summary>
    public int Level { get; init; } = 1;
    public IReadOnlyList<string> Tags { get; init; } = [];
    /// <summary>Resistência por escola (chave = nome da escola em snake_case; 0.25 = -25% de dano).</summary>
    public IReadOnlyDictionary<string, double> Resistances { get; init; } = new Dictionary<string, double>();

    /// <summary>Armadura (contra dano físico) e resistência (contra as outras escolas) na fórmula de defesa da T-201.</summary>
    public double Armor { get; init; }
    public double Resistance { get; init; }
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

    /// <summary>NH por habilidade (T-060), para encontros de teste; ausente = NH1.</summary>
    public IReadOnlyDictionary<string, int> Ranks { get; init; } = new Dictionary<string, int>();
}

public sealed record EnemySpawnDefinition
{
    public required string Monster { get; init; }
    public required PositionDefinition Position { get; init; }

    /// <summary>Nível deste monstro (T-206). Null = <see cref="EncounterDefinition.MonsterLevel"/> ou o nível da definição.</summary>
    public int? Level { get; init; }
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

    /// <summary>Nível de todos os monstros do encontro (T-206; vida e potência pela <see cref="MonsterScalingRules"/>). Null = o de cada definição.</summary>
    public int? MonsterLevel { get; init; }
}
