namespace Game.Core.Definitions;

/// <summary>
/// Regras globais (data/rules.json, seção raiz "rules"). Cada tarefa tem a sua seção, opcional;
/// seções ausentes usam os padrões neutros do respectivo record. Para adicionar uma seção: uma propriedade aqui.
/// </summary>
public sealed record RulesDefinition
{
    // ---- seção T-009 ----
    public ControlRules? Control { get; init; }

    // ---- seção T-201 (estatísticas derivadas) ----
    public StatRules Stats { get; init; } = new();

    // ---- seção T-203 (níveis, XP e pontos) ----
    public ProgressionRules Progression { get; init; } = new();

    // ---- seção T-204 (itens, afixos e quedas; fica em data/items.json) ----
    public ItemRules Items { get; init; } = new();

    // ---- seção T-206 (escala dos monstros por nível) ----
    public MonsterScalingRules Monsters { get; init; } = new();

    // ---- seção T-011 (threat): adicione abaixo ----
}

/// <summary>Quanto vida e potência de um monstro crescem por nível acima do 1 (T-206). Frações: 0,4 = +40% por nível.</summary>
public sealed record MonsterGrowth
{
    public double HealthPerLevel { get; init; }
    public double PowerPerLevel { get; init; }
}

/// <summary>
/// Escala dos monstros por nível (T-206): os números de <c>monsters.json</c> valem para o nível 1; no nível L a vida e a
/// potência (dano, cura e escudo dos efeitos do monstro, inclusive áreas e status dele) são multiplicadas por
/// 1 + porNível × (L − 1). Elites e chefes podem ter crescimento próprio (<see cref="Elite"/>, <see cref="Boss"/>; null =
/// o geral). Padrão 0 = sem escala.
/// </summary>
public sealed record MonsterScalingRules
{
    public double HealthPerLevel { get; init; }
    public double PowerPerLevel { get; init; }
    public MonsterGrowth? Elite { get; init; }
    public MonsterGrowth? Boss { get; init; }

    public double HealthFactor(int level, MonsterRank rank = MonsterRank.Normal) =>
        1 + ((For(rank)?.HealthPerLevel ?? HealthPerLevel) * Math.Max(0, level - 1));

    public double PowerFactor(int level, MonsterRank rank = MonsterRank.Normal) =>
        1 + ((For(rank)?.PowerPerLevel ?? PowerPerLevel) * Math.Max(0, level - 1));

    private MonsterGrowth? For(MonsterRank rank) => rank switch
    {
        MonsterRank.Elite => Elite,
        MonsterRank.Boss => Boss,
        _ => null,
    };
}

/// <summary>Multiplicador da duração de controle (status com tag "control") por rank do alvo.</summary>
public sealed record ControlDurationByRank
{
    public double Normal { get; init; } = 1;
    public double Elite { get; init; } = 1;
    public double Boss { get; init; } = 1;

    public double For(MonsterRank rank) => rank switch
    {
        MonsterRank.Elite => Elite,
        MonsterRank.Boss => Boss,
        _ => Normal,
    };
}

/// <summary>Retorno decrescente: n-ésima aplicação de controle dentro da janela usa Steps[n] (a última se passar do fim).</summary>
public sealed record ControlDiminishing
{
    /// <summary>Janela contada a partir da primeira aplicação do grupo; depois dela o contador reinicia. No JSON: "window" (segundos).</summary>
    public int WindowTicks { get; init; }

    public IReadOnlyList<double> Steps { get; init; } = [1.0];
}

public sealed record ControlRules
{
    public static readonly ControlRules Neutral = new();

    public ControlDurationByRank DurationByRank { get; init; } = new();
    public ControlDiminishing Diminishing { get; init; } = new();
}
