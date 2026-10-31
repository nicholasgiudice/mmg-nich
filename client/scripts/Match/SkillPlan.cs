using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Definitions;

namespace MmoRpgClient.Match;

/// <summary>
/// Níveis de habilidade (NH) escolhidos para uma classe (T-064). No protótipo é o "modo de teste": o menu deixa escolher
/// qualquer NH e só mostra a regra dos pontos. Na fase 2 a mesma estrutura vira a distribuição de pontos de habilidade
/// (pontos = nível do personagem, requisito de nível por NH): basta trocar <see cref="PointBudget"/> e ligar
/// <see cref="Enforce"/>. T-221: <see cref="FromState"/> monta o plano do personagem online (pontos e NH do servidor).
/// </summary>
public sealed class SkillPlan
{
    /// <summary>Pontos do nível 15 (1 por nível, §7 de design/progression-v0.1.md).</summary>
    public const int MaxLevelPoints = 15;

    /// <summary>Planos escolhidos nesta sessão, por classe (sobrevivem à troca de cena, como <see cref="MatchSettings"/>).</summary>
    private static readonly Dictionary<string, SkillPlan> Saved = new(StringComparer.Ordinal);

    private readonly Dictionary<string, int> _ranks = new(StringComparer.Ordinal);

    public SkillPlan(ClassDefinition cls, IReadOnlyDictionary<string, AbilityDefinition> abilities)
    {
        Class = cls;
        Abilities = cls.Abilities.Where(abilities.ContainsKey).Select(id => abilities[id]).ToList();
        ApplyProfile(Profile.Nh1);
    }

    public enum Profile
    {
        /// <summary>Normais no NH1, assinatura fora (o protótipo).</summary>
        Nh1,

        /// <summary>Duas habilidades no NH5, as outras duas no NH1, assinatura no NH3 (15 pontos).</summary>
        Focused,

        /// <summary>As quatro normais no NH3 e a assinatura no NH3 (15 pontos).</summary>
        Spread,
    }

    public ClassDefinition Class { get; }

    /// <summary>Habilidades da classe na ordem da barra (assinatura por último).</summary>
    public IReadOnlyList<AbilityDefinition> Abilities { get; }

    /// <summary>Pontos disponíveis (protótipo: os do nível 15). Na fase 2: o nível do personagem.</summary>
    public int PointBudget { get; init; } = MaxLevelPoints;

    /// <summary>
    /// Respeitar <see cref="PointBudget"/> ao mudar um NH. Desligado no modo de teste (o protótipo só avisa). Ligado (T-221),
    /// também vale o requisito de nível de <see cref="Rules"/> e as normais podem ficar no NH0 (fora da barra).
    /// </summary>
    public bool Enforce { get; init; }

    /// <summary>Nível do personagem (requisito de NH com <see cref="Enforce"/>).</summary>
    public int Level { get; init; } = MaxLevelPoints;

    /// <summary>Regras de pontos e requisitos de nível (data/rules.json, seção <c>progression</c>).</summary>
    public ProgressionRules Rules { get; init; } = new();

    /// <summary>NH de cada habilidade da classe (0 = fora da barra; só a assinatura pode ficar em 0 no protótipo).</summary>
    public IReadOnlyDictionary<string, int> Ranks => _ranks;

    public int PointsUsed => _ranks.Values.Sum();

    public bool OverBudget => PointsUsed > PointBudget;

    /// <summary>As duas habilidades do perfil focado (mesmas do simulador, tests/Game.Sim/RankProfiles.cs).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> FocusedPair = new Dictionary<string, string[]>
    {
        ["class.warrior"] = ["warrior.heavy_strike", "warrior.whirlwind"],
        ["class.archer"] = ["archer.power_shot", "archer.hunters_mark"],
        ["class.mage"] = ["mage.elemental_blast", "mage.lightning_field"],
        ["class.cleric"] = ["cleric.direct_heal", "cleric.sacred_shield"],
    };

    /// <summary>Menor NH escolhível: 1 nas normais (no protótipo toda normal está na barra), 0 na assinatura.</summary>
    public static int MinRank(AbilityDefinition a) => a.Signature ? 0 : 1;

    public static int MaxRank(AbilityDefinition a) => a.HighestRank;

    /// <summary>
    /// Plano do personagem com progressão (T-221): NH do estado (0 = não aprendida), pontos do nível e requisitos ligados.
    /// Só leitura do que o servidor mandou; mudar um NH de verdade é a intenção <c>RaiseSkillRankIntent</c>.
    /// </summary>
    public static SkillPlan FromState(ClassDefinition cls, ContentCatalog content, AbilityState state, int level)
    {
        var rules = content.Rules.Progression;
        var plan = new SkillPlan(cls, content.Abilities) { Enforce = true, Level = level, Rules = rules, PointBudget = rules.SkillPointsAt(level) };
        plan._ranks.Clear();
        foreach (var a in plan.Abilities)
        {
            plan._ranks[a.Id] = state.Known.Contains(a.Id) ? state.RankOf(a.Id) : 0;
        }

        return plan;
    }

    /// <summary>Menor NH neste plano: com <see cref="Enforce"/> tudo pode ficar no NH0; no modo de teste, <see cref="MinRank"/>.</summary>
    public int Floor(AbilityDefinition a) => Enforce ? 0 : MinRank(a);

    /// <summary>Nível do personagem exigido para o NH <paramref name="rank"/> da habilidade.</summary>
    public int RequiredLevel(AbilityDefinition a, int rank) => Rules.RequiredLevel(a.Signature, rank);

    /// <summary>Por que não dá para subir o NH agora (mesma ordem de checagem do Core); null = dá.</summary>
    public ProgressionRejectReason? RaiseBlocker(string abilityId)
    {
        if (Abilities.FirstOrDefault(a => a.Id == abilityId) is not { } ability)
        {
            return ProgressionRejectReason.NotLearnable;
        }

        var next = RankOf(abilityId) + 1;
        if (next > MaxRank(ability))
        {
            return ProgressionRejectReason.MaxRank;
        }

        if (Enforce && PointsUsed >= PointBudget)
        {
            return ProgressionRejectReason.NoSkillPoints;
        }

        return Enforce && Level < RequiredLevel(ability, next) ? ProgressionRejectReason.LevelTooLow : null;
    }

    /// <summary>Plano salvo da classe (ou um novo no NH1).</summary>
    public static SkillPlan For(ClassDefinition cls, ContentCatalog content)
    {
        if (!Saved.TryGetValue(cls.Id, out var plan))
        {
            plan = new SkillPlan(cls, content.Abilities);
            Saved[cls.Id] = plan;
        }

        return plan;
    }

    /// <summary>Esquece os planos salvos (testes).</summary>
    public static void ClearSaved() => Saved.Clear();

    public int RankOf(string abilityId) => _ranks.TryGetValue(abilityId, out var r) ? r : 0;

    /// <summary>Muda um NH (limitado ao intervalo da habilidade). Com <see cref="Enforce"/>, recusa se passar dos pontos.</summary>
    public bool Set(string abilityId, int rank)
    {
        var ability = Abilities.FirstOrDefault(a => a.Id == abilityId);
        if (ability is null)
        {
            return false;
        }

        var clamped = Math.Clamp(rank, Floor(ability), MaxRank(ability));
        if (Enforce && (PointsUsed - RankOf(abilityId) + clamped > PointBudget || (clamped > 0 && Level < RequiredLevel(ability, clamped))))
        {
            return false;
        }

        _ranks[abilityId] = clamped;
        return true;
    }

    public void ApplyProfile(Profile profile)
    {
        _ranks.Clear();
        foreach (var a in Abilities)
        {
            _ranks[a.Id] = profile switch
            {
                Profile.Nh1 => a.Signature ? 0 : 1,
                Profile.Spread => 3,
                _ when a.Signature => 3,
                _ => FocusedPair.TryGetValue(Class.Id, out var pair) && pair.Contains(a.Id) ? 5 : 1,
            };
        }
    }

    /// <summary>NH para o encontro (<c>PlayerSpawnDefinition.Ranks</c>): habilidades no NH0 ficam de fora (fora da barra).</summary>
    public IReadOnlyDictionary<string, int> ToSpawnRanks() =>
        _ranks.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

    /// <summary>
    /// Argumentos de depuração: <c>--ranks=nh1|focado|espalhado</c> e <c>--rank=id:n,id:n</c> (por cima do perfil).
    /// Retorna false se nenhum dos dois foi passado.
    /// </summary>
    public bool ApplyArgs(string? profile, string? pairs)
    {
        if (profile is null && pairs is null)
        {
            return false;
        }

        ApplyProfile(profile switch
        {
            "focado" => Profile.Focused,
            "espalhado" => Profile.Spread,
            _ => Profile.Nh1,
        });
        foreach (var pair in (pairs ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = pair.LastIndexOf(':');
            if (idx > 0 && int.TryParse(pair[(idx + 1)..], out var rank))
            {
                Set(pair[..idx], rank);
            }
        }

        return true;
    }

    /// <summary>NH em algarismos romanos (0 = "—").</summary>
    public static string Roman(int rank) => rank switch
    {
        <= 0 => "—",
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        _ => rank.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
}
