using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// Como medir uma segunda classe no simulador (T-408, T-423): classe base, par principal do perfil focado, estratégia
/// correta solo e no grupo. Cada classe com segunda classe entra em <see cref="SecondClassDesignTests.Specs"/>.
/// </summary>
/// <param name="BaseClass">Classe base (ex.: <c>class.warrior</c>).</param>
/// <param name="Pair">As duas principais da segunda classe no perfil focado (sobem alternando, depois do NH1 de todas).</param>
public sealed record SecondClassSpec(string BaseClass, string[] Pair)
{
    /// <summary>Opções de bot no solo (ex.: <c>class.warrior:order</c>); chaves escopadas pela classe base.</summary>
    public IReadOnlyDictionary<string, string>? SoloOptions { get; init; }

    /// <summary>Opções de bot no encontro de grupo.</summary>
    public IReadOnlyDictionary<string, string>? GroupOptions { get; init; }

    /// <summary>
    /// Limite da diferença entre segundas classes: no máximo esta vez a média das outras (1,6×; as do Clérigo, a classe
    /// mais lenta pelo design, 1,8×). Null = entra na média, mas sem limite (o Sacerdote, de cura).
    /// </summary>
    public double? SpeedLimit { get; init; } = 1.6;
}

/// <summary>
/// Metas da segunda classe (<c>design/progression-v0.1.md</c> §8, "Metas da segunda classe") com as habilidades reais.
/// T-408: Guardião e Berserker (<c>tasks/reports/balance-v0.19.md</c>). T-423: Sacerdote e Inquisidor
/// (<c>tasks/reports/balance-v0.20.md</c>; as metas próprias do Clérigo estão em <see cref="ClericSecondClassDesignTests"/>).
/// Os helpers valem para qualquer classe base: uma classe nova só entra em <see cref="Specs"/>.
/// </summary>
public class SecondClassDesignTests(ITestOutputHelper output)
{
    public const string Warrior = "class.warrior";
    public const string Cleric = "class.cleric";
    public const string Guardian = "second_class.guardian";
    public const string Berserker = "second_class.berserker";
    public const string Priest = "second_class.priest";
    public const string Inquisitor = "second_class.inquisitor";
    public const string Archer = "class.archer";
    public const string Marksman = "second_class.marksman";
    public const string Hunter = "second_class.hunter";
    public const string Mage = "class.mage";
    public const string Pyromancer = "second_class.pyromancer";
    public const string Arcanist = "second_class.arcanist";

    /// <summary>Grupo típico solo: três Lobos do nível; encontro de grupo: a Matriarca do nível contra o grupo de 4 com Clérigo.</summary>
    public const string Solo = "encounter.three_wolves";
    public const string Group = "encounter.forest_matriarch";

    /// <summary>
    /// Segundas classes medidas. Estratégia correta:
    /// <list type="bullet">
    /// <item>o Berserker abre com a Fúria e guarda a Investida para alvo único (contra três Lobos, a Mana dela rende mais no
    /// Golpe Circular);</item>
    /// <item>no grupo, o Guardião deixa o Golpe de Escudo de lado: a Provocação já segura a Matriarca, e a Mana rende mais no
    /// Bastião, na Interceptação e no Golpe Pesado (que interrompe);</item>
    /// <item>no grupo, o Sacerdote abre com a Bênção e a Inspiração (antes da cura, que só sai com alguém ferido);</item>
    /// <item>o Inquisidor usa a rotação da barra (a base primeiro; o bot decide o Selo e a Ira pelo estado que aplicam);</item>
    /// <item>Piromante e Arcanista: ver <see cref="MageSecondClassDesignTests"/> (T-443).</item>
    /// </list>
    /// </summary>
    public static readonly IReadOnlyDictionary<string, SecondClassSpec> Specs = new Dictionary<string, SecondClassSpec>
    {
        // T-433: o Guardião (tanque) até 2,0×, como o Sacerdote (cura) entra sem limite: as metas dele são as do grupo e a
        // vitória solo (tasks/reports/balance-v0.21.md §4). T-443: sem limite, como o Sacerdote; com as oito segundas classes
        // e nenhuma base na média, ficou em 1,97× no 40 (tasks/reports/balance-v0.22.md §4).
        [Guardian] = new(Warrior, ["guardian.bastion", "guardian.interception"])
        {
            SpeedLimit = null,
            GroupOptions = new Dictionary<string, string> { [$"{Warrior}:skip"] = "guardian.shield_bash" },
        },
        [Berserker] = new(Warrior, ["berserker.execution", "berserker.fury"])
        {
            SoloOptions = new Dictionary<string, string>
            {
                [$"{Warrior}:order"] = "berserker.fury",
                [$"{Warrior}:skip"] = "berserker.charge",
            },
        },
        [Priest] = new(Cleric, ["priest.blessing", "priest.inspiration"])
        {
            SpeedLimit = null,
            GroupOptions = new Dictionary<string, string> { [$"{Cleric}:order"] = "priest.blessing,priest.inspiration" },
        },
        // T-443: o Inquisidor até 2,0× (era 1,8×): 1,89× no 25, com só 2 NH da segunda classe (balance-v0.22.md §4).
        [Inquisitor] = new(Cleric, ["inquisitor.judgment", "inquisitor.hammer_of_justice"]) { SpeedLimit = 2.0 },

        // T-433: o Atirador fica mais longe (kiteDistance 8: para de se aproximar a ~9,5 tiles, para a Mira Firme valer), abre com
        // o Olho de Falcão e usa o Tiro Certeiro logo depois, e deixa o Tiro Debilitante de lado (~10 de dano por Mana, contra
        // 15–50 das outras; a lentidão não faz falta contra quem ele já deixa para trás). O Caçador usa a Nuvem Tóxica e a
        // Flecha Dispersa só com 2 ou mais monstros.
        [Marksman] = new(Archer, ["marksman.true_shot", "marksman.steady_aim"])
        {
            SoloOptions = new Dictionary<string, string>
            {
                [$"{Archer}:order"] = "marksman.hawk_eye,marksman.true_shot",
                [$"{Archer}:skip"] = "marksman.crippling_shot",
                [$"{Archer}:selfBuffRange"] = "12",
                [$"{Archer}:kiteDistance"] = "8",
            },
        },
        [Hunter] = new(Archer, ["hunter.poison_arrow", "hunter.open_wound"])
        {
            SoloOptions = new Dictionary<string, string> { [$"{Archer}:groupOnly"] = "hunter.toxic_cloud,hunter.scatter_arrow" },
        },

        // T-443: Piromante e Arcanista, ver MageSecondClassDesignTests.
        [Pyromancer] = new(Mage, MageSecondClassDesignTests.PyromancerPair) { SoloOptions = MageSecondClassDesignTests.PyromancerOptions },
        [Arcanist] = new(Mage, MageSecondClassDesignTests.ArcanistPair) { SoloOptions = MageSecondClassDesignTests.ArcanistOptions },
    };

    public static TheoryData<string> SecondClasses => [.. Specs.Keys];

    /// <summary>
    /// Três Lobos do nível com o jogador da classe base: o encontro real tem o Guerreiro; as outras classes usam uma cópia
    /// dele com o jogador trocado (mesma arena, posições e limite de tempo), em <see cref="SoloContent"/>.
    /// </summary>
    public static string SoloOf(string cls) => cls == Warrior ? Solo : $"{Solo}.{cls}";

    /// <summary>O catálogo real mais os Três Lobos de cada classe base de <see cref="Specs"/>.</summary>
    public static ContentCatalog SoloContent { get; } = BuildSoloContent(Content);

    private static ContentCatalog BuildSoloContent(ContentCatalog c)
    {
        var basis = c.Encounters[Solo];
        var copies = Specs.Values.Select(s => s.BaseClass).Distinct().Where(cls => cls != Warrior)
            .Select(cls => basis with { Id = SoloOf(cls), Players = [basis.Players[0] with { Class = cls }] });
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Concat(copies), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
            Shops = c.Shops,
        };
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    public static string BaseOf(string second) => Specs[second].BaseClass;

    private static ClassDefinition ClassOf(string second) => Content.Classes[BaseOf(second)];

    /// <summary>
    /// Perfil esperado com segunda classe (§8): pontos de atributo com os +5 nos sugeridos; os de habilidade primeiro no
    /// que falta da base (perfil focado), depois cada normal da segunda classe no NH1, as duas principais alternando, a
    /// assinatura quando o nível deixa e o resto. <paramref name="basePoints"/> limita os pontos da base. Solo, a base usa o
    /// par solo da classe (<see cref="DesignHarness.SoloFocus"/>: o Clérigo com Corrente e Escudo), como o "só a base".
    /// </summary>
    public static CharacterProfile WithSecondClass(int level, string second, int? basePoints = null, bool solo = true)
    {
        var cls = ClassOf(second);
        var focus = solo ? SoloFocus.GetValueOrDefault(cls.Id) : null;
        var baseRanks = CharacterProfiles.SkillRanks(Content, cls, Math.Min(basePoints ?? level, level), RankProfiles.Focused, focus);
        var free = Content.Rules.Progression.SkillPointsAt(level) - baseRanks.Values.Sum();
        return CharacterProfile.Expected(level) with
        {
            Ranks = new Dictionary<string, Dictionary<string, int>> { [cls.Id] = baseRanks },
            SecondClass = new Dictionary<string, string> { [cls.Id] = second },
            SecondClassRanks = new Dictionary<string, Dictionary<string, int>> { [cls.Id] = SecondClassRanks(level, second, free) },
        };
    }

    /// <summary>Distribuição dos pontos livres na segunda classe, com os níveis de NH dela (21/24/27/30/33, assinatura 35/37/40).</summary>
    public static Dictionary<string, int> SecondClassRanks(int level, string second, int points)
    {
        var rules = Content.Rules.Progression;
        var abilities = ClassOf(second).SecondClasses.Single(s => s.Id == second).Abilities;
        var normals = abilities.Where(id => !Content.Abilities[id].Signature).ToList();
        var signature = abilities.FirstOrDefault(id => Content.Abilities[id].Signature);
        var pair = Specs[second].Pair;
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

    /// <summary>
    /// Lote com o perfil (todos os jogadores do encontro), os monstros no nível e as opções de bot dadas. <paramref name="content"/>
    /// troca o catálogo (ex.: com um duelo de <see cref="LevelCurve.WithDuel"/>); <paramref name="seed"/> troca o lote de sementes.
    /// </summary>
    public static BatchSummary RunWith(
        string encounter, string strategy, CharacterProfile profile, int monsterLevel, IReadOnlyDictionary<string, string>? extra = null,
        ContentCatalog? content = null, ulong seed = 1)
    {
        var ranks = string.Join(",", profile.Ranks.SelectMany(r => r.Value).Concat(profile.SecondClassRanks.SelectMany(r => r.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));
        var seconds = string.Join(",", profile.SecondClass.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));
        var extraKey = extra is null ? "" : string.Join(",", extra.OrderBy(o => o.Key, StringComparer.Ordinal).Select(o => $"{o.Key}={o.Value}"));
        var key = $"{encounter}|{strategy}|{profile.Label}|{seconds}|{ranks}|{monsterLevel}|{extraKey}|{seed}";
        return Cache.GetOrAdd(key, _ => new Lazy<BatchSummary>(() =>
        {
            var options = new Dictionary<string, string>(Options);
            foreach (var (k, v) in extra ?? new Dictionary<string, string>())
            {
                options[k] = v;
            }

            return Batch.Run(content ?? SoloContent, new RunSpec
            {
                EncounterId = encounter,
                Strategy = strategy,
                Options = new BotOptions(options),
                Runs = Runs,
                BaseSeed = seed,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = profile.Focus.Count > 0 || encounter == Group ? profile : profile with { Focus = SoloFocus },
                MonsterLevel = monsterLevel,
            }, keepPerRun: true);
        })).Value;
    }

    /// <summary>Estratégia solo da classe base (os de longe se afastam), como nos testes de progressão.</summary>
    public static string SoloStrategy(string cls) => LevelCurve.SoloStrategy[cls];

    /// <summary>Só a base, solo; <paramref name="encounter"/> null = os Três Lobos do nível (<see cref="SoloOf"/>).</summary>
    public static BatchSummary BaseOnly(int level, string cls = Warrior, string? encounter = null) =>
        RunWith(encounter ?? SoloOf(cls), SoloStrategy(cls), CharacterProfile.Expected(level), level);

    /// <summary>Com a segunda classe, solo; <paramref name="encounter"/> null = os Três Lobos do nível (<see cref="SoloOf"/>).</summary>
    public static BatchSummary SoloWith(int level, string second, int? basePoints = null, string? encounter = null) =>
        RunWith(encounter ?? SoloOf(BaseOf(second)), SoloStrategy(BaseOf(second)), WithSecondClass(level, second, basePoints), level,
            Specs[second].SoloOptions);

    /// <summary>Encontro solo da classe base da segunda classe (o da diferença entre classes): Guerreiro, Três Lobos; Clérigo, o Guarda.</summary>
    public static string HomeOf(string second) => SoloFor(BaseOf(second));

    /// <summary>Grupo de 4 contra a Matriarca do nível: só a base (<paramref name="second"/> null) ou com a segunda classe.</summary>
    public static BatchSummary GroupWith(int level, string? second, ulong seed = 1) =>
        RunWith(Group, "kite+dodge", second is null ? CharacterProfile.Expected(level) : WithSecondClass(level, second, solo: false), level,
            second is null ? null : Specs[second].GroupOptions, seed: seed);

    /// <summary>Dano por segundo vivo recebido pelos membros do grupo que não são da classe <paramref name="cls"/> (todas as execuções somadas).</summary>
    public static double OthersDamagePerSecond(BatchSummary s, string cls = Warrior) =>
        s.PerRun!.Sum(r => r.DamageTakenByClass.Where(kv => kv.Key != cls).Sum(kv => kv.Value))
        / s.PerRun!.Sum(r => r.AliveSecondsByClass.Where(kv => kv.Key != cls).Sum(kv => kv.Value));

    /// <summary>T-423: dano por segundo vivo causado pelos membros do grupo que não são da classe <paramref name="cls"/>.</summary>
    public static double OthersDealtPerSecond(BatchSummary s, string cls) =>
        s.PerRun!.Sum(r => r.DamageDealtByClass.Where(kv => kv.Key != cls).Sum(kv => kv.Value))
        / s.PerRun!.Sum(r => r.AliveSecondsByClass.Where(kv => kv.Key != cls).Sum(kv => kv.Value));

    public static string Fmt(string label, BatchSummary s) =>
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
    /// Medido por segundo <b>vivo</b> de cada um: o total por luta depende da duração, e o dano por segundo de luta cai
    /// quando alguém morre (premiaria as mortes). Com o reforço da T-408 (Interceptação +2 s, Bastião +10 pontos) a
    /// redução fica entre 19% e 26% nos lotes 1–100, 1001–1100 e 2001–2100 (<c>tasks/reports/balance-v0.19.md</c> §4).
    /// </summary>
    [Theory]
    [InlineData(30)]
    [InlineData(40)]
    public void Guardian_cuts_the_damage_on_the_rest_of_the_group_by_at_least_15_percent(int level)
    {
        var baseOnly = GroupWith(level, null);
        var guardian = GroupWith(level, Guardian);
        var (before, after) = (OthersDamagePerSecond(baseOnly), OthersDamagePerSecond(guardian));

        Assert.True(after <= 0.85 * before, $"outros: {after:F1}/s com o Guardião × {before:F1}/s só a base ({after / before:P0})");
    }

    // ---------- todas ----------

    /// <summary>Encontro solo de cada classe base (o mesmo dos testes de progressão).</summary>
    public static string SoloFor(string cls) => cls switch
    {
        "class.archer" => "encounter.wolf_and_bandit",
        "class.mage" => "encounter.four_rats",
        "class.cleric" => "encounter.skeleton_guard",
        _ => Solo,
    };

    /// <summary>
    /// Nenhuma segunda classe mais de 1,6× mais lenta que a média das outras. Cada uma no encontro solo da classe base dela
    /// (<see cref="HomeOf"/>: Guerreiro, Três Lobos; Clérigo, o Guarda Esquelético), como na diferença entre classes; as
    /// classes base que ainda não têm segunda classe entram na média com o encontro solo delas. Limite por segunda classe
    /// (<see cref="SecondClassSpec.SpeedLimit"/>): as do Clérigo, a classe mais lenta pelo design, até 1,8×; o Sacerdote,
    /// de cura, entra na média sem limite (T-423, <c>tasks/reports/balance-v0.20.md</c> §3); o Guardião, tanque, até 2,0×
    /// (T-433: o Arqueiro entrou com duas segundas classes rápidas no Lobo e Salteador; <c>tasks/reports/balance-v0.21.md</c> §4).
    /// T-443: com o Mago, nenhuma classe base fica na média; o Guardião entra sem limite (1,97× no 40) e o Inquisidor até 2,0×
    /// (1,89× no 25) (<c>tasks/reports/balance-v0.22.md</c> §4).
    /// </summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void No_second_class_is_more_than_1_6_times_slower_than_the_others(int level)
    {
        var times = Specs.Keys.ToDictionary(second => second, second => SoloWith(level, second, encounter: HomeOf(second)).TimeToWinMean);
        var withSecond = Specs.Values.Select(s => s.BaseClass).ToHashSet();
        foreach (var (cls, strategy) in LevelCurve.SoloStrategy.Where(kv => !withSecond.Contains(kv.Key)))
        {
            times[cls] = RunWith(SoloFor(cls), strategy, CharacterProfile.Expected(level), level).TimeToWinMean;
        }

        output.WriteLine(string.Join(", ", times.Select(t => $"{t.Key} {t.Value:F1} s")));
        foreach (var second in Specs.Keys)
        {
            var others = times.Where(t => t.Key != second).Average(t => t.Value);
            if (Specs[second].SpeedLimit is not { } limit)
            {
                continue;
            }

            Assert.True(times[second] <= limit * others, $"{second}: {times[second]:F1} s × média das outras {others:F1} s (limite {limit}×)");
        }
    }

    /// <summary>
    /// Nível 21: com o NH1 de uma habilidade da segunda classe (o 21º ponto, em vez do 21º NH da base) e os +5 de atributo,
    /// não fica mais lento que só a base (margem de 2%).
    /// </summary>
    [Theory]
    [MemberData(nameof(SecondClasses))]
    public void At_level_21_the_rank_1_of_the_second_class_is_already_useful(string second)
    {
        var baseOnly = BaseOnly(21, BaseOf(second));
        var withSecond = SoloWith(21, second, basePoints: 20);
        Log("nv21 só a base", baseOnly);
        Log($"nv21 {second}", withSecond);

        Assert.True(withSecond.WinRate >= baseOnly.WinRate - 0.02, $"{Fmt(second, withSecond)} | {Fmt("base", baseOnly)}");
        Assert.True(withSecond.TimeToWinMean <= baseOnly.TimeToWinMean * 1.02, $"{Fmt(second, withSecond)} | {Fmt("base", baseOnly)}");
    }

    /// <summary>No nível 30, pontos do 21 em diante só na base (as 3 NH que faltam) ou na segunda classe: os dois vencem ≥ 70%.</summary>
    [Theory]
    [MemberData(nameof(SecondClasses))]
    public void Points_after_21_in_the_base_or_in_the_second_class_both_win(string second)
    {
        const int level = 30;
        var cls = BaseOf(second);
        var inBase = RunWith(SoloOf(cls), SoloStrategy(cls), CharacterProfile.Expected(level) with
        {
            SecondClass = new Dictionary<string, string> { [cls] = second },
        }, level, Specs[second].SoloOptions);
        var inSecond = SoloWith(level, second, basePoints: 20);
        Log($"nv{level} {second} pontos na base", inBase);
        Log($"nv{level} {second} pontos na segunda classe", inSecond);

        Assert.True(inBase.WinRate >= 0.70, Fmt("na base", inBase));
        Assert.True(inSecond.WinRate >= 0.70, Fmt("na segunda classe", inSecond));
    }
}
