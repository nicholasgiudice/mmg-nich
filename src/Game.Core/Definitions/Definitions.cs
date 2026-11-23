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
    /// Itens com que o personagem novo começa no inventário (T-320; ex.: 5 poções de vida). Substitui os consumíveis como
    /// habilidade da T-017.
    /// </summary>
    public IReadOnlyList<StartingItemDefinition> StartingInventory { get; init; } = [];

    /// <summary>Regras de geração de recurso por evento (T-010).</summary>
    public IReadOnlyList<ResourceRuleDefinition> ResourceRules { get; init; } = [];

    /// <summary>
    /// Segundas classes que o personagem desta classe pode escolher no nível <c>progression.secondClassLevel</c>
    /// (T-403, <c>design/segunda-classe-v0.1.md</c> §4). Vazio = a classe não tem segunda classe.
    /// </summary>
    public IReadOnlyList<SecondClassDefinition> SecondClasses { get; init; } = [];
}

/// <summary>
/// Segunda classe (T-403): escolhida uma vez, de forma definitiva, entre as da classe base. As habilidades recebem pontos
/// como as da base, com os níveis por NH da segunda classe (<c>progression.secondClassRankLevels</c>), e entram na barra
/// depois das da base (passivas não entram). Id: <c>second_class.*</c>.
/// </summary>
public sealed record SecondClassDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
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

    /// <summary>
    /// Interrompida (efeito <c>interrupt</c>), a habilidade entra na recarga cheia em vez de ficar pronta (T-351: o Último
    /// Comando do Vigia; sem isso, o boss reconjura no tick seguinte e a interrupção não ganha tempo).
    /// </summary>
    public bool CooldownOnInterrupt { get; init; }
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

    /// <summary>
    /// Passiva (T-403, <c>design/segunda-classe-v0.1.md</c> §4): recebe pontos e NH, mas não entra na barra, não pode ser
    /// usada e não tem custo, recarga nem alvo (targeting <c>self</c>). Com NH ≥ 1, o personagem mantém o status
    /// permanente <see cref="PassiveStatus"/> do NH atual.
    /// </summary>
    public bool Passive { get; init; }

    /// <summary>Status permanente de uma passiva (T-403); um NH pode trocar por outro.</summary>
    public string? PassiveStatus { get; init; }

    /// <summary>
    /// Canalização com pulsos (T-442, Raio Arcano e Feixe do Vazio): os efeitos saem a cada intervalo enquanto quem lança
    /// canaliza, em vez de uma vez. Null = habilidade comum. Não combina com <c>castTime</c>, <c>hits</c> nem telegráfo.
    /// </summary>
    public ChannelDefinition? Channel { get; init; }

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

    /// <summary>Status permanente da passiva neste NH (T-403); null herda.</summary>
    public string? PassiveStatus { get; init; }

    /// <summary>Canalização neste NH (T-442); null herda. Declarada, substitui a canalização inteira.</summary>
    public ChannelDefinition? Channel { get; init; }
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

    /// <summary>Multiplica a cura recebida por quem tem este status (0,5 = metade). T-341: Maldição do Sacerdote Morto.</summary>
    public double HealingTakenMultiplier { get; init; } = 1;

    /// <summary>
    /// Multiplicador de dano recebido só de uma escola (chave = nome snake_case, ex.: <c>holy</c>; 1,4 = +40%). Vale junto com
    /// <see cref="DamageTakenMultiplier"/>. T-351: o Rosto Vazio do Vigia fica mais vulnerável a dano sagrado.
    /// </summary>
    public IReadOnlyDictionary<string, double> DamageTakenBySchool { get; init; } = new Dictionary<string, double>();
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

/// <summary>
/// Reflexo (T-341, Escudo Espectral): quem tem o status devolve <see cref="Fraction"/> do dano direto que recebe de outra
/// entidade a quem causou, na mesma escola e sem multiplicadores. Dano de status (veneno, reflexos) não é devolvido.
/// </summary>
public sealed record ReflectDefinition
{
    public required double Fraction { get; init; }
}

/// <summary>
/// Como o status acaba antes do tempo (T-341, Escudo Espectral): quebra quando quem o tem recebe, desde a aplicação,
/// <see cref="DamageFraction"/> da vida máxima em dano (0 = não quebra por dano), e/ou quando é atingido por um efeito
/// <c>interrupt</c> (<see cref="Interrupt"/>), mesmo sem estar conjurando.
/// </summary>
public sealed record StatusBreakDefinition
{
    public double DamageFraction { get; init; }
    public bool Interrupt { get; init; }
}

/// <summary>
/// Desvio de dano (T-406: Interceptação, Juramento; depois o Elo da Vida): parte do dano que quem tem o status receberia
/// vai para o protetor (quem aplicou o status, ou a entidade que o efeito escolheu). A parte desviada sai do dano antes de
/// tudo, não passa pelos escudos nem pelos modificadores de quem tem o status, passa pela defesa e pelos modificadores do
/// protetor e é reduzida por <see cref="Reduction"/>. Dano desviado não é desviado de novo; protetor morto, longe demais
/// (<see cref="MaxDistance"/>) ou o próprio alvo não recebe nada.
/// </summary>
public sealed record RedirectDefinition
{
    /// <summary>Fração do dano desviada para o protetor, em (0, 1]. Vários desvios no mesmo alvo somam até 1.</summary>
    public required double Fraction { get; init; }

    /// <summary>Redução sobre a parte desviada, em [0, 1) (Juramento: o Guardião recebe o dano desviado 40% menor). 0 = nenhuma.</summary>
    public double Reduction { get; init; }

    /// <summary>Distância máxima (tiles) entre o alvo e o protetor para o desvio valer.</summary>
    public required double MaxDistance { get; init; }
}

/// <summary>
/// Vida pelo dano causado (T-407: Sede de Sangue; depois o Selo da Luz e o Julgamento do Inquisidor): quem tem o status cura
/// <see cref="Fraction"/> (× acúmulos) do dano final que causa em outra entidade. A cura passa pela cura recebida
/// (<c>healingTakenMultiplier</c>) e não é crítica. Dano refletido, dano desviado e dano em si mesmo não geram vida.
/// </summary>
public sealed record LifestealDefinition
{
    /// <summary>Fração do dano final que volta como vida, em (0, 1].</summary>
    public required double Fraction { get; init; }

    /// <summary>
    /// Com vida atual abaixo desta fração da vida máxima (estritamente), a fração é multiplicada por
    /// <see cref="BelowHealthMultiplier"/>. 0 = nunca. Em [0, 1].
    /// </summary>
    public double BelowHealth { get; init; }

    /// <summary>Multiplicador da fração com pouca vida (2 = dobra). Pelo menos 1.</summary>
    public double BelowHealthMultiplier { get; init; } = 2;

    /// <summary>Ao matar um inimigo (facção diferente) com o dano, cura também esta fração da vida máxima. 0 = nada. Em [0, 1].</summary>
    public double KillHeal { get; init; }

    /// <summary>
    /// Habilidade a que a cura é atribuída no evento <c>HealApplied</c> e no log (a que deu o status, ex.:
    /// <c>berserker.bloodthirst</c>). Null = o próprio status.
    /// </summary>
    public string? Ability { get; init; }
}

public sealed record StatusFlags
{
    public bool Stunned { get; init; }
    public bool Rooted { get; init; }
    public bool Silenced { get; init; }

    /// <summary>
    /// Não pode ser alvo (T-340, Fada da Névoa "na névoa"): outras entidades não o escolhem como alvo de unidade e ele não é
    /// atingido por arcos, linhas, círculos, telegráfos, áreas, correntes nem alvos extras. Status que já tem continuam valendo.
    /// </summary>
    public bool Untargetable { get; init; }

    /// <summary>
    /// Medo (T-351): quem tem o status não age (como atordoado) e corre para longe do ponto de origem do medo (o centro do
    /// telegráfo/área que o aplicou, ou a posição de quem aplicou), na velocidade normal, ignorando as intenções de movimento.
    /// Use com a tag <c>control</c> para valerem as regras de controle (rank, retorno decrescente, imunidade).
    /// </summary>
    public bool Feared { get; init; }

    /// <summary>
    /// Imune a empurrão (T-406, Bastião NH5): <c>displacement</c> do tipo <c>knockback</c> vindo de inimigos (ou sem fonte)
    /// não move quem tem o status. Empurrão de aliado e avanço próprio continuam valendo.
    /// </summary>
    public bool KnockbackImmune { get; init; }

    /// <summary>
    /// Vida mínima de 1 (T-421, Santuário): um dano que mataria quem tem o status o deixa com 1 de vida. Vale para todo dano
    /// (direto, periódico, de área e a parte desviada a um protetor). Não muda cura nem escudo.
    /// </summary>
    public bool Undying { get; init; }
}

/// <summary>Valor que escala com as estatísticas de quem aplicou (T-421): <c>base + Σ coeficiente × estatística</c>.</summary>
public sealed record ScaledValueDefinition
{
    public required double Base { get; init; }

    /// <summary>Chaves de <see cref="ScalingKeys"/>. Fonte sem estatísticas = só a base.</summary>
    public IReadOnlyDictionary<string, double> Scaling { get; init; } = new Dictionary<string, double>();
}

/// <summary>
/// Estatísticas que um status soma a quem o tem (T-421: Bênção, Inspiração; depois o Sifão Arcano). O valor é calculado ao
/// aplicar, pelas estatísticas de quem aplicou, e fica fixo enquanto o status dura (renovar recalcula). Vezes os acúmulos.
/// Só vale em quem tem estatísticas de personagem (<c>CharacterStats</c>).
/// </summary>
public sealed record StatusStatsDefinition
{
    /// <summary>
    /// Pontos de atributo: chave <c>str</c>, <c>dex</c>, <c>agi</c>, <c>int</c>, <c>vit</c> ou <c>all</c> (os cinco). Arredondados
    /// para inteiro (metade para longe do zero). Tudo que deriva deles muda junto (vida e Mana máximas, dano, crítico).
    /// </summary>
    public IReadOnlyDictionary<string, ScaledValueDefinition> Attributes { get; init; } = new Dictionary<string, ScaledValueDefinition>();

    /// <summary>Mana por segundo a mais (soma na regeneração; não dá Mana na hora).</summary>
    public ScaledValueDefinition? ManaRegen { get; init; }

    /// <summary>Chaves aceitas em <see cref="Attributes"/>.</summary>
    public static readonly IReadOnlySet<string> AttributeKeys = new HashSet<string>(StringComparer.Ordinal) { "str", "dex", "agi", "int", "vit", "all" };
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

    // ---- T-431 (Atirador de Elite) ----

    /// <summary>Crítico a mais pela distância até o alvo, multiplicador próprio ou crítico garantido (T-431).</summary>
    public StatusCritDefinition? Crit { get; init; }

    /// <summary>
    /// Tiles a mais (× acúmulos) no alcance das habilidades com a tag <c>ranged</c> de quem tem o status (T-431: Mira Firme
    /// NH3, Olho de Falcão NH3): alcance de unidade, comprimento de linha e alcance de círculo no ponto. 0 = nenhum.
    /// </summary>
    public double RangeBonus { get; init; }

    // ---- fim T-431 ----

    /// <summary>Devolve parte do dano recebido a quem atacou (T-341).</summary>
    public ReflectDefinition? Reflect { get; init; }

    /// <summary>Quebra por dano recebido ou por interrupção (T-341).</summary>
    public StatusBreakDefinition? Breaks { get; init; }

    /// <summary>Desvia parte do dano recebido para um protetor (T-406).</summary>
    public RedirectDefinition? Redirect { get; init; }

    /// <summary>Vida pelo dano causado (T-407).</summary>
    public LifestealDefinition? Lifesteal { get; init; }

    /// <summary>Atributos e regeneração de Mana somados a quem tem o status, pela INT (etc.) de quem aplicou (T-421).</summary>
    public StatusStatsDefinition? Stats { get; init; }

    /// <summary>
    /// Efeitos que rodam quando o status acaba pelo tempo (T-421, Santuário NH3), com a fonte do status e quem o tinha como
    /// alvo. Não rodam quando o status é tirado, quando quem o tem morre nem em status permanente.
    /// </summary>
    public IReadOnlyList<EffectDefinition> OnExpire { get; init; } = [];

    // ---- T-422 (Inquisidor) ----

    /// <summary>
    /// Vida pelo dano para quem bate em quem tem o status (T-422, Selo da Luz): todo aliado de quem aplicou (mesma facção,
    /// inclusive ele) que causa dano direto em quem tem o status cura uma fração do dano final, pelas mesmas regras de
    /// <see cref="Lifesteal"/> (cura recebida de quem bateu, sem dano indireto). A cura sai em nome de quem bateu, atribuída a
    /// <c>ability</c> (ou ao status).
    /// </summary>
    public LifestealDefinition? AttackerLifesteal { get; init; }

    /// <summary>
    /// Troca o ataque básico (T-422, Ira Divina): enquanto o status está ativo, a intenção de ataque básico de quem o tem usa
    /// esta habilidade (alvo, efeitos e recarga dela), com o id, a recarga e os eventos do ataque básico. Com vários status,
    /// vale o aplicado por último. Null = não troca.
    /// </summary>
    public string? BasicAttack { get; init; }
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

    /// <summary>
    /// Presa ao dono (T-407, Tempestade de Aço): nasce na posição de quem a criou e, a cada tick, acompanha a posição atual
    /// dele (inclusive para acertar). Acaba com a duração ou quando o dono morre ou some (<c>AreaExpireReason.OwnerDied</c>).
    /// </summary>
    public bool FollowOwner { get; init; }
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

    /// <summary>
    /// Efeitos aplicados ao entrar em cada objetivo vivo do encontro (<see cref="EncounterDefinition.Objectives"/>), com o boss
    /// como fonte (T-351: os selos começam a ruir no Último Comando).
    /// </summary>
    public IReadOnlyList<EffectDefinition> OnEnterObjectives { get; init; } = [];
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

    /// <summary>Arquivo do Tiled de onde a arena veio (T-051, ex.: <c>maps/fields.tmj</c>); null = arena ASCII.</summary>
    public string? Source { get; init; }

    /// <summary>Pontos de surgimento do mapa do Tiled (camada <c>spawns</c>).</summary>
    public IReadOnlyList<Spatial.MapSpawn> Spawns { get; init; } = [];

    /// <summary>Áreas de evento do mapa do Tiled (camada <c>areas</c>).</summary>
    public IReadOnlyList<Spatial.MapArea> Areas { get; init; } = [];

    /// <summary>Transições de zona do mapa do Tiled (camada <c>transitions</c>).</summary>
    public IReadOnlyList<Spatial.MapTransition> Transitions { get; init; } = [];
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

    /// <summary>
    /// Objetivos a proteger (T-351: os selos do Vigia). Nascem como unidades aliadas dos jogadores (facção dos jogadores), sem
    /// IA e sem se mover, a partir de uma definição de monstro; podem receber cura e escudo, e os monstros podem atacá-los.
    /// Quando todos são destruídos, o encontro falha (<c>EncounterFailed</c>). Vazio = sem objetivos.
    /// </summary>
    public IReadOnlyList<EnemySpawnDefinition> Objectives { get; init; } = [];
}
