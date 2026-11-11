using Game.Bots;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-342: a zona Floresta Baixa (<c>data/maps/lower_forest.tmj</c>), como os Campos (T-215) e as Ruínas (T-343). Cada grupo
/// típico (pontos da mesma subárea a até 4 tiles) vira um encontro na arena aberta do protótipo, com os monstros nos níveis do
/// mapa, e o perfil esperado do nível do grupo vence sozinho ≥ 85%, nas 4 classes. O modelo de ritmo dos Campos mede o tempo
/// do nível 5 ao 8 caçando sozinho (sem meta; a T-380 avalia). Relatório: <c>tasks/reports/balance-v0.16.md</c>.
/// </summary>
public class ForestZoneDesignTests(ITestOutputHelper output)
{
    public const string Zone = "zone.lower_forest";

    private static readonly Dictionary<string, string> Strategy = new()
    {
        // Como na T-340: todos desviam dos telegráfos; Arqueiro e Mago atacam de longe.
        ["class.warrior"] = "rotation+dodge",
        ["class.archer"] = "kite+dodge",
        ["class.mage"] = "kite+dodge",
        ["class.cleric"] = "rotation+dodge",
    };

    private static readonly string[] AllClasses = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    private static readonly Lazy<IReadOnlyList<FieldGroup>> AllGroups = new(() => FieldGroups.Of(Zone));

    /// <summary>Um grupo por composição.</summary>
    private static IReadOnlyList<FieldGroup> Typical => AllGroups.Value.GroupBy(g => g.Key).Select(g => g.First()).ToList();

    public static TheoryData<string, string> GroupsAndClasses()
    {
        var data = new TheoryData<string, string>();
        foreach (var group in Typical)
        {
            foreach (var cls in AllClasses)
            {
                data.Add(group.Key, cls);
            }
        }

        return data;
    }

    private static string Fmt(BatchSummary s) => $"{s.Encounter} [{s.Strategy}]: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s";

    [Theory]
    [MemberData(nameof(GroupsAndClasses))]
    public void Expected_profile_of_the_group_level_clears_each_group_at_least_85_percent(string group, string cls)
    {
        var s = Run(Typical.Single(g => g.Key == group), cls);
        output.WriteLine($"{group} / {cls}: {Fmt(s)}");
        Assert.True(s.WinRate >= 0.85, $"{group} / {cls}: {Fmt(s)}");
    }

    /// <summary>
    /// Ritmo solo na Floresta (modelo dos Campos, <see cref="FieldGroups.Pace"/>): no nível L o jogador caça os grupos do nível
    /// min(L, 7) com o tempo médio de vitória das 4 classes mais 60 s fora da luta, limitado pelo renascimento. Sem meta: o
    /// tempo do nível 5 ao 8 vai para o relatório.
    /// </summary>
    [Fact]
    public void Solo_pace_from_level_5_to_8_is_measured()
    {
        var pace = Pace();
        var report = string.Join("; ", pace.Select(p => $"nv{p.Level}: {p.Minutes:F1} min ({p.XpPerMinute:F1} XP/min)"));
        output.WriteLine($"nível 5 → 8 em {pace.Sum(p => p.Minutes):F1} min; {report}");
        Assert.All(pace, p => Assert.True(p.Minutes is > 0 and < 1000, report));
    }

    private static List<(int Level, double Minutes, double XpPerMinute)> Pace()
    {
        var rules = Content.Rules.Progression;
        var groups = AllGroups.Value;
        var result = new List<(int, double, double)>();
        for (var level = 5; level <= 7; level++)
        {
            var tier = groups.Where(g => g.Level <= level).Max(g => g.Level);
            var hunted = groups.Where(g => g.Level == tier).ToList();
            var xpPerGroup = hunted.Average(g => g.Points.Sum(p =>
                p.Count * Math.Round(rules.MonsterXp(p.Level ?? 1, Content.Monsters[p.Monster].Rank, level))));
            var cycle = hunted.Average(g => AllClasses.Average(c => Run(Typical.Single(t => t.Key == g.Key), c).TimeToWinMean))
                + FieldsDesignTests.OverheadSeconds;
            var groupsPerSecond = Math.Min(1 / cycle, hunted.Sum(g => 1 / g.RespawnSeconds));
            var xpPerMinute = xpPerGroup * groupsPerSecond * 60;
            result.Add((level, rules.XpToNext(level) / xpPerMinute, xpPerMinute));
        }

        return result;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    /// <summary>Lote do perfil esperado do nível do grupo, sozinho.</summary>
    private static BatchSummary Run(FieldGroup group, string cls) =>
        Cache.GetOrAdd($"{group.Key}|{cls}", _ => new Lazy<BatchSummary>(() =>
        {
            var (content, encounter) = FieldGroups.Encounter(group, [cls]);
            return Batch.Run(content, new RunSpec
            {
                EncounterId = encounter,
                Strategy = Strategy[cls],
                Options = new BotOptions(Options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = CharacterProfile.Expected(group.Level) with { Focus = SoloFocus },
            });
        })).Value;
}
