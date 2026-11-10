using Game.Bots;
using Game.Core.Combat;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-343: a zona Ruínas da Estrada Antiga (<c>data/maps/old_road_ruins.tmj</c>), como os Campos (T-215) e a Floresta (T-342).
/// Cada grupo típico (pontos da mesma subárea a até 4 tiles) vira um encontro na arena aberta do protótipo, com os monstros
/// nos níveis do mapa:
/// <list type="bullet">
/// <item>Portão Partido e Estrada para a Cripta: o perfil esperado do nível do grupo vence sozinho ≥ 85%, nas 4 classes;</item>
/// <item>Pátio dos Mortos ("grupo recomendado"): a meta da T-341, um par vence ≥ 80% e o solo fica entre 40% e 70%
/// (com o limite de 240 s do encontro da T-341);</item>
/// <item>Torre Quebrada: só o Cavaleiro Espectral nv11, com renascimento longo (a luta é a de <see cref="RuinsDesignTests"/>).</item>
/// </list>
/// O modelo de ritmo dos Campos mede o tempo do nível 9 ao 12 caçando sozinho (sem meta; a T-380 avalia).
/// Relatório: <c>tasks/reports/balance-v0.14.md</c>.
/// </summary>
public class RuinsZoneDesignTests(ITestOutputHelper output)
{
    public const string Zone = "zone.old_road_ruins";
    public const string Courtyard = "Pátio dos Mortos";
    public const string Tower = "Torre Quebrada";

    private static readonly Dictionary<string, string> Strategy = new()
    {
        // Como na T-341: todos desviam dos telegráfos; Arqueiro e Mago atacam de longe.
        ["class.warrior"] = "rotation+dodge",
        ["class.archer"] = "kite+dodge",
        ["class.mage"] = "kite+dodge",
        ["class.cleric"] = "rotation+dodge",
    };

    private static readonly string[] AllClasses = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    private static readonly Lazy<IReadOnlyList<FieldGroup>> AllGroups = new(() => FieldGroups.Of(Zone));

    /// <summary>Um grupo por composição.</summary>
    private static IReadOnlyList<FieldGroup> Typical => AllGroups.Value.GroupBy(g => g.Key).Select(g => g.First()).ToList();

    /// <summary>Grupos feitos para caçar sozinho (fora do Pátio e da Torre).</summary>
    private static IEnumerable<FieldGroup> SoloGroups(IEnumerable<FieldGroup> groups) => groups.Where(g => g.Area is not Courtyard and not Tower);

    public static TheoryData<string, string> SoloGroupsAndClasses()
    {
        var data = new TheoryData<string, string>();
        foreach (var group in SoloGroups(Typical))
        {
            foreach (var cls in AllClasses)
            {
                data.Add(group.Key, cls);
            }
        }

        return data;
    }

    public static TheoryData<string, string, string> CourtyardGroupsAndDuos()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var group in Typical.Where(g => g.Area == Courtyard))
        {
            foreach (var duo in RuinsDesignTests.Duos())
            {
                data.Add(group.Key, (string)duo[0], (string)duo[1]);
            }
        }

        return data;
    }

    public static TheoryData<string> CourtyardGroups()
    {
        var data = new TheoryData<string>();
        foreach (var group in Typical.Where(g => g.Area == Courtyard))
        {
            data.Add(group.Key);
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

    /// <summary>Pátio dos Mortos, meta da T-341: um par no nível do grupo (perfil esperado) vence ≥ 80%.</summary>
    [Theory]
    [MemberData(nameof(CourtyardGroupsAndDuos))]
    public void A_duo_of_the_group_level_clears_each_courtyard_group_at_least_80_percent(string group, string a, string b)
    {
        var s = Run(Typical.Single(g => g.Key == group), a, b);
        output.WriteLine($"{group}: {Fmt(s)}");
        Assert.True(s.WinRate >= 0.80, $"{group}: {Fmt(s)}");
    }

    /// <summary>Pátio dos Mortos, meta da T-341: sozinho é possível, mas arriscado (média das 4 classes entre 40% e 70%).</summary>
    [Theory]
    [MemberData(nameof(CourtyardGroups))]
    public void Solo_against_each_courtyard_group_is_possible_but_risky(string group)
    {
        var solos = AllClasses.Select(c => Run(Typical.Single(g => g.Key == group), c)).ToList();
        var report = string.Join("; ", solos.Select(Fmt));
        output.WriteLine($"{group}: {report}");
        Assert.InRange(solos.Average(s => s.WinRate), 0.40, 0.70);
        Assert.Contains(solos, s => s.WinRate >= 0.5);
    }

    /// <summary>
    /// A Torre Quebrada tem um único Cavaleiro Espectral nv11 (elite opcional), que renasce em 5 min. A luta (grupo de 3,
    /// 70–90%) é testada em <see cref="RuinsDesignTests"/>.
    /// </summary>
    [Fact]
    public void The_broken_tower_holds_one_spectral_knight_level_11_with_a_long_respawn()
    {
        var tower = Assert.Single(AllGroups.Value, g => g.Area == Tower);
        var knight = Assert.Single(tower.Points);
        Assert.Equal("monster.spectral_knight", knight.Monster);
        Assert.Equal(11, knight.Level);
        Assert.Equal(1, knight.Count);
        Assert.Equal(300, tower.RespawnSeconds);
        Assert.DoesNotContain(AllGroups.Value, g => g.Area != Tower && g.Points.Any(p => p.Monster == "monster.spectral_knight"));
    }

    /// <summary>
    /// Ritmo solo nas Ruínas (modelo dos Campos, <see cref="FieldGroups.Pace"/>): no nível L o jogador caça os grupos solo do
    /// nível min(L, 11) com o tempo médio de vitória das 4 classes mais 60 s fora da luta, limitado pelo renascimento. Sem meta:
    /// o tempo do nível 9 ao 12 vai para o relatório.
    /// </summary>
    [Fact]
    public void Solo_pace_from_level_9_to_12_is_measured()
    {
        var pace = Pace();
        var report = string.Join("; ", pace.Select(p => $"nv{p.Level}: {p.Minutes:F1} min ({p.XpPerMinute:F1} XP/min)"));
        output.WriteLine($"nível 9 → 12 em {pace.Sum(p => p.Minutes):F1} min; {report}");
        Assert.All(pace, p => Assert.True(p.Minutes is > 0 and < 1000, report));
    }

    private static List<(int Level, double Minutes, double XpPerMinute)> Pace()
    {
        var rules = Content.Rules.Progression;
        var solo = SoloGroups(AllGroups.Value).ToList();
        var result = new List<(int, double, double)>();
        for (var level = 9; level <= 11; level++)
        {
            var tier = solo.Where(g => g.Level <= level).Max(g => g.Level);
            var hunted = solo.Where(g => g.Level == tier).ToList();
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

    /// <summary>Lote do perfil esperado do nível do grupo (as classes, na ordem; a primeira dá a estratégia base).</summary>
    private static BatchSummary Run(FieldGroup group, params string[] classes) =>
        Cache.GetOrAdd($"{group.Key}|{string.Join(",", classes)}", _ => new Lazy<BatchSummary>(() =>
        {
            // O Pátio usa o limite do encontro da T-341 (240 s): sozinho, o Clérigo vence devagar.
            var (content, encounter) = FieldGroups.Encounter(group, classes, group.Area == Courtyard ? 240 : 120);
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
