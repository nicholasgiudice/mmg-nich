using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-510: monstros do Brejo dos Afogados (Afogado nv16, Homem-lagarto nv17, Serpente do Charco nv18) e a Bruxa do Brejo (chefe
/// de campo nv20; <c>design/combat-encounters-v0.1.md</c> § Brejo dos Afogados). Os grupos típicos de <c>data/encounters.json</c>
/// rodam solo nas 4 classes base (o jogador do encontro é trocado pela classe), com o perfil do nível do grupo (T-206): o esperado
/// vence ≥ 85% e o fraco (3 níveis abaixo) no máximo 50%, ou demora ≥ 1,25× (a regra da Floresta, balance-v0.18 §4). A Bruxa:
/// grupo de 3 nos níveis 19 e 20 vence ≥ 80%; solo no nível 20, no máximo 30%. Relatório: <c>tasks/reports/balance-v0.24.md</c>.
/// </summary>
public class MarshDesignTests(ITestOutputHelper output)
{
    public const string Drowned = "encounter.drowned_pack";
    public const string Lizardmen = "encounter.marsh_lizardmen";
    public const string Serpents = "encounter.marsh_serpents";
    public const string Mixed = "encounter.marsh_mixed";
    public const string Witch = "encounter.marsh_witch";

    private static readonly string[] Groups = [Drowned, Lizardmen, Serpents, Mixed];

    private static readonly (string Class, string Strategy)[] Classes =
    [
        // Como na Floresta: todos desviam dos telegráfos (+dodge); Arqueiro e Mago atacam de longe e fogem.
        ("class.warrior", "rotation+dodge"),
        ("class.archer", "kite+dodge"),
        ("class.mage", "kite+dodge"),
        ("class.cleric", "rotation+dodge"),
    ];

    /// <summary>Nível do grupo típico = maior nível dos monstros do encontro.</summary>
    public static int GroupLevel(string encounter) => Content.Encounters[encounter].Enemies.Max(e => e.Level ?? 1);

    public static TheoryData<string, string> GroupsAndClasses()
    {
        var data = new TheoryData<string, string>();
        foreach (var encounter in Groups)
        {
            foreach (var (cls, _) in Classes)
            {
                data.Add(encounter, cls);
            }
        }

        return data;
    }

    public static TheoryData<string> AllClasses()
    {
        var data = new TheoryData<string>();
        foreach (var (cls, _) in Classes)
        {
            data.Add(cls);
        }

        return data;
    }

    private static string Fmt(BatchSummary s) =>
        $"{s.Encounter} / {s.Strategy}: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s, dano recebido {s.DamageTaken:F0}, mortes {s.PlayerDeaths:F2}";

    [Theory]
    [MemberData(nameof(GroupsAndClasses))]
    public void Expected_profile_wins_solo_at_least_85_percent(string encounter, string cls)
    {
        var s = Solo(encounter, cls, CharacterProfile.Expected(GroupLevel(encounter)));
        output.WriteLine(Fmt(s));
        Assert.True(s.WinRate >= 0.85, Fmt(s));
    }

    /// <summary>
    /// Exceção do Clérigo no Brejo (balance-v0.24 §3): ele alcança as Serpentes com a Corrente Sagrada e se cura do resto, então o
    /// fraco não perde; a diferença de 3 níveis aparece só no tempo, e no nível 15–18 ela é menor (~1,1–1,2×) que no 5–9.
    /// </summary>
    public const double ClericWeakSlowdown = 1.10;

    [Theory]
    [MemberData(nameof(GroupsAndClasses))]
    public void Weak_profile_wins_solo_at_most_half_or_takes_1_25_times_as_long(string encounter, string cls)
    {
        var weak = Solo(encounter, cls, CharacterProfile.Weak(GroupLevel(encounter)));
        var expected = Solo(encounter, cls, CharacterProfile.Expected(GroupLevel(encounter)));
        var slowdown = cls == "class.cleric" ? ClericWeakSlowdown : ProgressionDesignTests.WeakSlowdown;
        var report = $"{Fmt(weak)} vs {Fmt(expected)} ({weak.TimeToWinMean / expected.TimeToWinMean:F2}×, meta {slowdown:F2}×)";
        output.WriteLine(report);
        Assert.True(weak.WinRate <= 0.50 || weak.TimeToWinMean >= slowdown * expected.TimeToWinMean, report);
    }

    /// <summary>Grupo de 3 (Guerreiro + Arqueiro + Clérigo) nos níveis 19 e 20, perfil esperado: vence ≥ 80% nos dois.</summary>
    [Theory]
    [InlineData(19)]
    [InlineData(20)]
    public void A_group_of_three_beats_the_witch_at_least_80_percent(int level)
    {
        var s = WitchGroup(CharacterProfile.Expected(level));
        output.WriteLine(Fmt(s));
        Assert.True(s.WinRate >= 0.80, Fmt(s));
    }

    /// <summary>Solo no nível 20, perfil esperado: a Bruxa vence pelo menos 70% das vezes, em cada classe (o jogador, no máximo 30%).</summary>
    [Theory]
    [MemberData(nameof(AllClasses))]
    public void Solo_against_the_witch_wins_at_most_30_percent(string cls)
    {
        var s = Solo(Witch, cls, CharacterProfile.Expected(20));
        output.WriteLine(Fmt(s));
        Assert.True(s.WinRate <= 0.30, Fmt(s));
    }

    // ---- lotes ----

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    /// <summary>Catálogo real mais as variantes solo (<c>{encontro}.solo_{classe}</c>) de cada encontro do Brejo.</summary>
    internal static ContentCatalog Variants { get; } = BuildVariants(Content);

    internal static string SoloId(string encounter, string cls) => $"{encounter}.solo_{cls[(cls.IndexOf('.') + 1)..]}";

    internal static BatchSummary Solo(string encounter, string cls, CharacterProfile profile) =>
        Cache.GetOrAdd($"{encounter}|{cls}|{profile.Label}", _ => new Lazy<BatchSummary>(() => Batch.Run(Variants, new RunSpec
        {
            EncounterId = SoloId(encounter, cls),
            Strategy = Classes.Single(c => c.Class == cls).Strategy,
            Options = new BotOptions(Options),
            Runs = Runs,
            HazardSourceFactory = () => new TelegraphHazardSource(),
            Profile = profile with { Focus = SoloFocus },
        }))).Value;

    internal static BatchSummary WitchGroup(CharacterProfile profile) =>
        Cache.GetOrAdd($"{Witch}|group|{profile.Label}", _ => new Lazy<BatchSummary>(() => Batch.Run(Variants, new RunSpec
        {
            EncounterId = Witch,
            Strategy = "rotation+dodge",
            Roles = new Dictionary<string, string> { ["class.archer"] = "kite+dodge" },
            Options = new BotOptions(new Dictionary<string, string>(Options) { ["class.cleric:followInjured"] = "1" }),
            Runs = Runs,
            HazardSourceFactory = () => new TelegraphHazardSource(),
            Profile = profile,
        }))).Value;

    private static ContentCatalog BuildVariants(ContentCatalog c)
    {
        var extra = new List<EncounterDefinition>();
        foreach (var id in Groups.Append(Witch))
        {
            var real = c.Encounters[id];
            foreach (var (cls, _) in Classes)
            {
                extra.Add(real with
                {
                    Id = SoloId(id, cls),
                    Players = [real.Players[0] with { Class = cls }],
                });
            }
        }

        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Concat(extra), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
        };
    }
}
