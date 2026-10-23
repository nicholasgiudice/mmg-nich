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

    // ---- seção T-011 (threat): adicione abaixo ----
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
