using Game.Bots;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-408: metas da segunda classe do Guerreiro (<c>design/progression-v0.1.md</c> §8, "Metas da segunda classe") com as
/// habilidades reais do Guardião e do Berserker. Relatório: <c>tasks/reports/balance-v0.19.md</c>.
/// </summary>
public class SecondClassDesignTests(ITestOutputHelper output)
{
    public const string Warrior = "class.warrior";
    public const string Guardian = "second_class.guardian";
    public const string Berserker = "second_class.berserker";

    /// <summary>Grupo típico solo: três Lobos do nível; encontro de grupo: a Matriarca do nível contra o grupo de 4 com Clérigo.</summary>
    public const string Solo = "encounter.three_wolves";
    public const string Group = "encounter.forest_matriarch";

    /// <summary>As duas principais de cada segunda classe no perfil focado (sobem alternando, depois do NH1 de todas).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Pair = new Dictionary<string, string[]>
    {
        [Guardian] = ["guardian.bastion", "guardian.interception"],
        [Berserker] = ["berserker.execution", "berserker.fury"],
    };

    /// <summary>
    /// Estratégia correta de cada segunda classe no solo contra a matilha (a barra põe a base primeiro): o Berserker abre
    /// com a Fúria e guarda a Investida para alvo único (contra três Lobos, a Mana dela rende mais no Golpe Circular).
    /// O Guardião usa a rotação da barra.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SoloOptions =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            [Berserker] = new Dictionary<string, string>
            {
                [$"{Warrior}:order"] = "berserker.fury",
                [$"{Warrior}:skip"] = "berserker.charge",
            },
        };

    /// <summary>
    /// No grupo, o Guardião deixa o Golpe de Escudo de lado: a Provocação já segura a Matriarca, e a Mana rende mais no
    /// Bastião, na Interceptação e no Golpe Pesado (que interrompe).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> GuardianGroupOptions = new Dictionary<string, string>
    {
        [$"{Warrior}:skip"] = "guardian.shield_bash",
    };

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    private static ClassDefinition Cls => Content.Classes[Warrior];

    /// <summary>
    /// Perfil esperado com segunda classe (§8): pontos de atributo com os +5 nos sugeridos; os de habilidade primeiro no
    /// que falta da base (perfil focado), depois cada normal da segunda classe no NH1, as duas principais alternando, a
    /// assinatura quando o nível deixa e o resto. <paramref name="basePoints"/> limita os pontos da base.
    /// </summary>
    public static CharacterProfile WithSecondClass(int level, string second, int? basePoints = null)
    {
        var baseRanks = CharacterProfiles.SkillRanks(Content, Cls, Math.Min(basePoints ?? level, level), RankProfiles.Focused);
        var free = Content.Rules.Progression.SkillPointsAt(level) - baseRanks.Values.Sum();
        return CharacterProfile.Expected(level) with
        {
            Ranks = new Dictionary<string, Dictionary<string, int>> { [Warrior] = baseRanks },
            SecondClass = new Dictionary<string, string> { [Warrior] = second },
            SecondClassRanks = new Dictionary<string, Dictionary<string, int>> { [Warrior] = SecondClassRanks(level, second, free) },
        };
    }

    /// <summary>Distribuição dos pontos livres na segunda classe, com os níveis de NH dela (21/24/27/30/33, assinatura 35/37/40).</summary>
    public static Dictionary<string, int> SecondClassRanks(int level, string second, int points)
    {
        var rules = Content.Rules.Progression;
        var abilities = Cls.SecondClasses.Single(s => s.Id == second).Abilities;
        var normals = abilities.Where(id => !Content.Abilities[id].Signature).ToList();
        var signature = abilities.FirstOrDefault(id => Content.Abilities[id].Signature);
        var pair = Pair[second];
        var ranks = abilities.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);

        bool CanRaise(string id) =>
            ranks[id] < Content.Abilities[id].HighestRank
            && level >= rules.RequiredLevel(Content.Abilities[id].Signature, ranks[id] + 1, secondClass: true);

        string? Next()
        {
            if (normals.OrderBy(id => pair.Contains(id) ? 0 : 1).FirstOrDefault(id => ranks[id] == 0 && CanRaise(id)) is { } unlearned)
            {
                return unlearned;
            }

            return pair.OrderBy(id => ranks[id]).Concat(signature is null ? [] : [signature])
                .Concat(normals.Except(pair).OrderBy(id => ranks[id])).FirstOrDefault(CanRaise);
        }

        for (; points > 0 && Next() is { } id; points--)
        {
            ranks[id]++;
        }

        return ranks.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
    }

    /// <summary>Lote com o perfil (todos os jogadores do encontro), os monstros no nível e as opções de bot dadas.</summary>
    public static BatchSummary RunWith(
        string encounter, string strategy, CharacterProfile profile, int monsterLevel, IReadOnlyDictionary<string, string>? extra = null)
    {
        var ranks = string.Join(",", profile.Ranks.SelectMany(r => r.Value).Concat(profile.SecondClassRanks.SelectMany(r => r.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));
        var extraKey = extra is null ? "" : string.Join(",", extra.OrderBy(o => o.Key, StringComparer.Ordinal).Select(o => $"{o.Key}={o.Value}"));
        var key = $"{encounter}|{strategy}|{profile.Label}|{profile.SecondClass.GetValueOrDefault(Warrior)}|{ranks}|{monsterLevel}|{extraKey}";
        return Cache.GetOrAdd(key, _ => new Lazy<BatchSummary>(() =>
        {
            var options = new Dictionary<string, string>(Options);
            foreach (var (k, v) in extra ?? new Dictionary<string, string>())
            {
                options[k] = v;
            }

            return Batch.Run(Content, new RunSpec
            {
                EncounterId = encounter,
                Strategy = strategy,
                Options = new BotOptions(options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = profile.Focus.Count > 0 || encounter == Group ? profile : profile with { Focus = SoloFocus },
                MonsterLevel = monsterLevel,
            }, keepPerRun: true);
        })).Value;
    }

    public static BatchSummary BaseOnly(int level) => RunWith(Solo, "rotation", CharacterProfile.Expected(level), level);

    public static BatchSummary SoloWith(int level, string second, int? basePoints = null) =>
        RunWith(Solo, "rotation", WithSecondClass(level, second, basePoints), level, SoloOptions.GetValueOrDefault(second));

    public static BatchSummary GroupWith(int level, string? second) =>
        RunWith(Group, "kite+dodge", second is null ? CharacterProfile.Expected(level) : WithSecondClass(level, second), level,
            second == Guardian ? GuardianGroupOptions : null);

    /// <summary>Dano por segundo de luta nos membros do grupo que não são o Guerreiro (todas as execuções).</summary>
    public static double OthersDamagePerSecond(BatchSummary s) =>
        s.PerRun!.Sum(r => r.DamageTakenByClass.Where(kv => kv.Key != Warrior).Sum(kv => kv.Value)) / s.PerRun!.Sum(r => r.Seconds);

    private static string Fmt(string label, BatchSummary s) =>
        $"{label}: vitória {s.WinRate:P0}, {s.TimeToWinMean:F1} s, dano recebido {s.DamageTaken:F0}, usos "
        + string.Join(" ", s.AbilityUses.Where(u => !u.Key.StartsWith("consumable", StringComparison.Ordinal)).Select(u => $"{u.Key}={u.Value:F1}"));

    private void Log(string label, BatchSummary s) => output.WriteLine(Fmt(label, s));

    public static TheoryData<int> Levels => new() { 25, 30, 40 };

    // ---------- Berserker ----------

    [Theory]
    [MemberData(nameof(Levels))]
    public void Berserker_beats_three_wolves_in_at_most_85_percent_of_the_base_time(int level)
    {
        var baseOnly = BaseOnly(level);
        var berserker = SoloWith(level, Berserker);
        Log($"nv{level} só a base", baseOnly);
        Log($"nv{level} Berserker", berserker);

        Assert.True(berserker.WinRate >= 0.85, Fmt("Berserker", berserker));
        Assert.True(berserker.TimeToWinMean <= 0.85 * baseOnly.TimeToWinMean,
            $"{Fmt("Berserker", berserker)} | {Fmt("base", baseOnly)} | razão {berserker.TimeToWinMean / baseOnly.TimeToWinMean:P0}");
    }

    [Theory]
    [MemberData(nameof(Levels))]
    public void Berserker_takes_at_most_1_5_times_the_damage_of_the_base(int level)
    {
        var baseOnly = BaseOnly(level);
        var berserker = SoloWith(level, Berserker);

        Assert.True(berserker.WinRate >= 0.85, Fmt("Berserker", berserker));
        Assert.True(berserker.DamageTaken <= 1.5 * baseOnly.DamageTaken,
            $"{Fmt("Berserker", berserker)} | {Fmt("base", baseOnly)} | razão {berserker.DamageTaken / baseOnly.DamageTaken:F2}×");
    }

    // ---------- Guardião ----------

    [Theory]
    [MemberData(nameof(Levels))]
    public void Guardian_wins_solo_at_least_85_percent(int level)
    {
        var guardian = SoloWith(level, Guardian);
        Log($"nv{level} Guardião", guardian);
        Assert.True(guardian.WinRate >= 0.85, Fmt("Guardião", guardian));
    }

    /// <summary>
    /// Grupo de 4 com Clérigo contra a Matriarca do nível: com o Guardião no lugar do Guerreiro só com a base, a vitória
    /// não cai. Margem de 10 pontos: a diferença entre dois lotes de 100 execuções perto de 70% oscila uns 6,5 pontos (um
    /// desvio). Nos níveis 30 e 40 (a Matriarca leva ~1 min por lote na VPS).
    /// </summary>
    [Theory]
    [InlineData(30)]
    [InlineData(40)]
    public void Guardian_in_the_group_does_not_lower_the_win_rate(int level)
    {
        var baseOnly = GroupWith(level, null);
        var guardian = GroupWith(level, Guardian);
        Log($"nv{level} grupo só a base (outros {OthersDamagePerSecond(baseOnly):F1}/s)", baseOnly);
        Log($"nv{level} grupo com Guardião (outros {OthersDamagePerSecond(guardian):F1}/s)", guardian);

        Assert.True(guardian.WinRate >= baseOnly.WinRate - 0.10, $"{Fmt("Guardião", guardian)} | {Fmt("base", baseOnly)}");
    }

    /// <summary>
    /// Meta da T-402: os outros membros (Arqueiro, Mago e Clérigo) recebem pelo menos 15% menos dano com o Guardião.
    /// Medido por segundo de luta (o total por luta depende da duração e de quem morre cedo). <b>Não é atingida de forma
    /// estável</b> com os números do design: com equipamento comum a redução oscila de 8% a 23% conforme as sementes (as
    /// derrotas pesam; nas seeds 1–100 dá 17% no 30 e 12% no 40); com incomum, quase sem derrotas, fica entre 0% e 12%; no
    /// Cavaleiro Espectral, entre −5% e +5%. Três quartos do dano nos outros é veneno e poça de veneno em quem fica de
    /// longe, fora do raio do Bastião, e a conta do kit no NH5 dá no máximo uns 15% com uso perfeito. Rever a meta ou o
    /// Guardião é decisão do designer (<c>tasks/reports/balance-v0.19.md</c> §4); até lá o teste fica desligado.
    /// </summary>
    [Theory(Skip = "Meta pendente do designer (balance-v0.19.md §4): o Guardião reduz de 0% a 23% conforme o lote; a meta pede 15% estáveis.")]
    [InlineData(30)]
    [InlineData(40)]
    public void Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent(int level)
    {
        var baseOnly = GroupWith(level, null);
        var guardian = GroupWith(level, Guardian);
        var (before, after) = (OthersDamagePerSecond(baseOnly), OthersDamagePerSecond(guardian));

        Assert.True(after <= 0.85 * before, $"outros: {after:F1}/s com o Guardião × {before:F1}/s só a base ({after / before:P0})");
    }

    // ---------- as duas ----------

    /// <summary>Encontro solo de cada classe base (o mesmo dos testes de progressão).</summary>
    private static string SoloFor(string cls) => cls switch
    {
        "class.archer" => "encounter.wolf_and_bandit",
        "class.mage" => "encounter.four_rats",
        "class.cleric" => "encounter.skeleton_guard",
        _ => Solo,
    };

    /// <summary>Nenhuma segunda classe mais de 1,6× mais lenta que a média das outras (com as classes base do nível).</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void No_second_class_is_more_than_1_6_times_slower_than_the_others(int level)
    {
        var times = new Dictionary<string, double>
        {
            [Guardian] = SoloWith(level, Guardian).TimeToWinMean,
            [Berserker] = SoloWith(level, Berserker).TimeToWinMean,
        };
        foreach (var (cls, strategy) in LevelCurve.SoloStrategy.Where(kv => kv.Key != Warrior))
        {
            times[cls] = RunWith(SoloFor(cls), strategy, CharacterProfile.Expected(level), level).TimeToWinMean;
        }

        output.WriteLine(string.Join(", ", times.Select(t => $"{t.Key} {t.Value:F1} s")));
        foreach (var second in new[] { Guardian, Berserker })
        {
            var others = times.Where(t => t.Key != second).Average(t => t.Value);
            Assert.True(times[second] <= 1.6 * others, $"{second}: {times[second]:F1} s × média das outras {others:F1} s");
        }
    }

    /// <summary>
    /// Nível 21: com o NH1 de uma habilidade da segunda classe (o 21º ponto, em vez do 21º NH da base) e os +5 de atributo,
    /// não fica mais lento que só a base (margem de 2%).
    /// </summary>
    [Theory]
    [InlineData(Guardian)]
    [InlineData(Berserker)]
    public void At_level_21_the_rank_1_of_the_second_class_is_already_useful(string second)
    {
        var baseOnly = BaseOnly(21);
        var withSecond = SoloWith(21, second, basePoints: 20);
        Log("nv21 só a base", baseOnly);
        Log($"nv21 {second}", withSecond);

        Assert.True(withSecond.WinRate >= baseOnly.WinRate - 0.02, $"{Fmt(second, withSecond)} | {Fmt("base", baseOnly)}");
        Assert.True(withSecond.TimeToWinMean <= baseOnly.TimeToWinMean * 1.02, $"{Fmt(second, withSecond)} | {Fmt("base", baseOnly)}");
    }

    /// <summary>No nível 30, pontos do 21 em diante só na base (as 3 NH que faltam) ou na segunda classe: os dois vencem ≥ 70%.</summary>
    [Theory]
    [InlineData(Guardian)]
    [InlineData(Berserker)]
    public void Points_after_21_in_the_base_or_in_the_second_class_both_win(string second)
    {
        const int level = 30;
        var inBase = RunWith(Solo, "rotation", CharacterProfile.Expected(level) with
        {
            SecondClass = new Dictionary<string, string> { [Warrior] = second },
        }, level, SoloOptions.GetValueOrDefault(second));
        var inSecond = SoloWith(level, second, basePoints: 20);
        Log($"nv{level} {second} pontos na base", inBase);
        Log($"nv{level} {second} pontos na segunda classe", inSecond);

        Assert.True(inBase.WinRate >= 0.70, Fmt("na base", inBase));
        Assert.True(inSecond.WinRate >= 0.70, Fmt("na segunda classe", inSecond));
    }
}
