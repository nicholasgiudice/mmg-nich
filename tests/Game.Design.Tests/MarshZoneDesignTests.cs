using Game.Bots;
using Game.Core.Combat;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-530: a zona Brejo dos Afogados (<c>data/maps/drowned_marsh.tmj</c>), como as Ruínas (T-343). Cada grupo típico (pontos da
/// mesma subárea a até 4 tiles) vira um encontro na arena aberta do protótipo, com os monstros nos níveis do mapa (sem a água
/// funda, que fica para o jogo e o playtest):
/// <list type="bullet">
/// <item>Estrada dos Juncos, Aldeia das Palafitas e Charco Fundo: o perfil esperado do nível do grupo vence sozinho ≥ 85%, nas
/// 4 classes;</item>
/// <item>Cabana da Bruxa ("grupo recomendado", bandos de 4 a 6 Afogados nv19 em volta da Bruxa): uma dupla do nível vence
/// ≥ 80%. A Bruxa é testada em <see cref="MarshDesignTests"/>.</item>
/// </list>
/// O ritmo (16→20 entre 75% e 130% do tempo da curva) é o de <see cref="ZoneGoalDesignTests"/>, só com os grupos solo
/// (<see cref="SoloHunt"/>).
/// </summary>
public class MarshZoneDesignTests(ITestOutputHelper output)
{
    public const string Zone = ZoneGoalDesignTests.Marsh;
    public const string Hut = "Cabana da Bruxa";

    private static readonly Dictionary<string, string> Strategy = new()
    {
        // Como na T-510: todos desviam dos telegráfos; Arqueiro e Mago atacam de longe.
        ["class.warrior"] = "rotation+dodge",
        ["class.archer"] = "kite+dodge",
        ["class.mage"] = "kite+dodge",
        ["class.cleric"] = "rotation+dodge",
    };

    private static readonly string[] AllClasses = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    private static readonly Lazy<IReadOnlyList<FieldGroup>> AllGroups = new(() => FieldGroups.Of(Zone));

    /// <summary>Um grupo por composição (sem o chefe).</summary>
    internal static IReadOnlyList<FieldGroup> Typical =>
        AllGroups.Value.Where(ZonePace.IsHuntable).GroupBy(g => g.Key).Select(g => g.First()).ToList();

    /// <summary>Grupos de caça solo do Brejo: tudo menos a Cabana da Bruxa (grupo recomendado) e a própria Bruxa.</summary>
    internal static bool SoloHunt(FieldGroup g) => ZonePace.IsHuntable(g) && g.Area != Hut;

    public static TheoryData<string, string> SoloGroupsAndClasses()
    {
        var data = new TheoryData<string, string>();
        if (!Content.Zones.ContainsKey(Zone))
        {
            return data;
        }

        foreach (var group in Typical.Where(SoloHunt))
        {
            foreach (var cls in AllClasses)
            {
                data.Add(group.Key, cls);
            }
        }

        return data;
    }

    public static TheoryData<string, string, string> HutGroupsAndDuos()
    {
        var data = new TheoryData<string, string, string>();
        if (!Content.Zones.ContainsKey(Zone))
        {
            return data;
        }

        foreach (var group in Typical.Where(g => g.Area == Hut))
        {
            foreach (var duo in RuinsDesignTests.Duos())
            {
                data.Add(group.Key, (string)duo[0], (string)duo[1]);
            }
        }

        return data;
    }

    private static string Fmt(BatchSummary s) => $"{s.Encounter} [{s.Strategy}]: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s";

    [Theory]
    [MemberData(nameof(SoloGroupsAndClasses))]
    public void Expected_profile_of_the_group_level_clears_each_solo_group_at_least_85_percent(string group, string cls)
    {
        var s = Run(Typical.Single(g => g.Key == group), cls);
        output.WriteLine($"{group} / {cls}: {Fmt(s)}");
        Assert.True(s.WinRate >= 0.85, $"{group} / {cls}: {Fmt(s)}");
    }

    /// <summary>Cabana da Bruxa: os bandos grandes de Afogados nv19 pedem dupla; uma dupla do nível vence ≥ 80%.</summary>
    [Theory]
    [MemberData(nameof(HutGroupsAndDuos))]
    public void A_duo_of_the_group_level_clears_each_hut_group_at_least_80_percent(string group, string a, string b)
    {
        var s = Run(Typical.Single(g => g.Key == group), a, b);
        output.WriteLine($"{group}: {Fmt(s)}");
        Assert.True(s.WinRate >= 0.80, $"{group}: {Fmt(s)}");
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    /// <summary>Lote do perfil esperado do nível do grupo (as classes, na ordem; a primeira dá a estratégia base).</summary>
    internal static BatchSummary Run(FieldGroup group, params string[] classes) =>
        Cache.GetOrAdd($"{group.Key}|{string.Join(",", classes)}", _ => new Lazy<BatchSummary>(() =>
        {
            var (content, encounter) = FieldGroups.Encounter(group, classes, classes.Length == 1 ? ZonePace.SoloTimeLimit : ZonePace.PartyTimeLimit);
            var profile = CharacterProfile.Expected(group.Level);
            return Batch.Run(content, new RunSpec
            {
                EncounterId = encounter,
                Strategy = Strategy[classes[0]],
                Roles = classes.Skip(1).Where(c => Strategy[c] != Strategy[classes[0]]).ToDictionary(c => c, c => Strategy[c]),
                Options = new BotOptions(Options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = classes.Length == 1 ? profile with { Focus = SoloFocus } : profile,
            });
        })).Value;
}
