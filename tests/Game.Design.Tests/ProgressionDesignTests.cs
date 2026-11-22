using Game.Core.Definitions;
using Game.Sim;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-206: metas de <c>design/progression-v0.1.md</c> §8 com perfis por nível e monstros no nível da área
/// (<c>rules.monsters</c>). Os encontros solo do protótipo rodam com monstros dos níveis 5 (Floresta) e 9 (Ruínas);
/// "esperado" = personagem no nível, atributos sugeridos e equipamento comum do nível; "fraco" = 3 níveis abaixo.
/// Relatório: <c>tasks/reports/balance-v0.7.md</c>.
/// </summary>
public class ProgressionDesignTests
{
    public static TheoryData<string, string, int> SoloAtLevel()
    {
        var data = new TheoryData<string, string, int>();
        foreach (var level in AreaLevels)
        {
            foreach (var (encounter, strategy) in Solo)
            {
                data.Add(encounter, strategy, level);
            }
        }

        return data;
    }

    private static readonly int[] AreaLevels = [5, 9];

    private static readonly (string Encounter, string Strategy)[] Solo =
    [
        ("encounter.three_wolves", "rotation"),
        ("encounter.wolf_and_bandit", "kite"),
        ("encounter.four_rats", "rotation"),
        ("encounter.skeleton_guard", "rotation"),
    ];

    private const string ClericSolo = "encounter.skeleton_guard";

    private static string Fmt(BatchSummary s) => $"{s.Encounter} / {s.Strategy}: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s";

    [Theory]
    [MemberData(nameof(SoloAtLevel))]
    public void Expected_profile_wins_solo_at_least_85_percent_within_40_seconds(string encounter, string strategy, int level)
    {
        var s = RunProfile(encounter, strategy, CharacterProfile.Expected(level), level);
        Assert.True(s.WinRate >= 0.85, Fmt(s));
        Assert.True(s.TimeToWinMean <= 40, Fmt(s));
    }

    /// <summary>
    /// Perfil fraco: vence no máximo metade ou, se vence, demora pelo menos 1,25× o perfil esperado. A segunda parte era só a
    /// exceção do Clérigo contra o Guarda (balance-v0.7 §3); desde o balanceamento do playtest de 2026-11-16, aprovado pelo
    /// designer, o fraco de outras classes também vence, e a meta passou a ser o tempo (balance-v0.18 §4).
    /// </summary>
    [Theory]
    [MemberData(nameof(SoloAtLevel))]
    public void Weak_profile_wins_at_most_half_or_takes_1_25_times_as_long(string encounter, string strategy, int level)
    {
        var weak = RunProfile(encounter, strategy, CharacterProfile.Weak(level), level);
        var expected = RunProfile(encounter, strategy, CharacterProfile.Expected(level), level);
        Assert.True(weak.WinRate <= 0.50 || weak.TimeToWinMean >= WeakSlowdown * expected.TimeToWinMean,
            $"{Fmt(weak)} vs {Fmt(expected)} ({weak.TimeToWinMean / expected.TimeToWinMean:F2}×)");
    }

    /// <summary>Quanto o perfil fraco que vence precisa demorar a mais que o esperado (balance-v0.18 §4).</summary>
    public const double WeakSlowdown = 1.25;

    [Theory]
    [InlineData("encounter.three_wolves", "rotation")]
    [InlineData("encounter.wolf_and_bandit", "kite")]
    [InlineData("encounter.four_rats", "rotation")]
    [InlineData("encounter.skeleton_guard", "rotation")]
    public void Suggested_attributes_win_faster_but_spread_still_wins_60_percent(string encounter, string strategy)
    {
        var suggested = RunProfile(encounter, strategy, CharacterProfile.Expected(9), 9);
        var spread = RunProfile(encounter, strategy, CharacterProfile.Expected(9) with { Attributes = AttributePlan.Spread }, 9);
        Assert.True(spread.WinRate >= 0.60, Fmt(spread)); // espalhado = metade no sugerido, metade nos outros (balance-v0.7 §4)
        Assert.True(suggested.TimeToWinMean < spread.TimeToWinMean, $"{Fmt(suggested)} vs {Fmt(spread)}");
    }

    [Theory]
    [InlineData("encounter.three_wolves", "rotation")]
    [InlineData("encounter.wolf_and_bandit", "kite")]
    [InlineData("encounter.four_rats", "rotation")]
    [InlineData("encounter.skeleton_guard", "rotation")]
    public void Focused_and_spread_skill_points_both_win_at_least_70_percent(string encounter, string strategy)
    {
        var focused = RunProfile(encounter, strategy, CharacterProfile.Expected(9), 9);
        var spread = RunProfile(encounter, strategy, CharacterProfile.Expected(9) with { Skills = RankProfiles.Spread }, 9);
        Assert.True(focused.WinRate >= 0.70, Fmt(focused));
        Assert.True(spread.WinRate >= 0.70, Fmt(spread));
    }

    /// <summary>
    /// Nenhuma classe mais de 1,6× mais lenta que a média das quatro (perfil esperado, solo). O Clérigo, classe de
    /// sustentação e a mais lenta pelo design, tem limite 1,8× (era 1,7×, decisão do designer na balance-v0.7 §4): o
    /// balanceamento do playtest de 2026-11-16 acelerou o Mago e baixou a média, sem mudar o Clérigo (balance-v0.18 §4).
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(9)]
    public void No_class_is_more_than_1_6_times_slower_than_the_average(int level)
    {
        var all = Solo.Select(x => RunProfile(x.Encounter, x.Strategy, CharacterProfile.Expected(level), level)).ToList();
        var mean = all.Average(s => s.TimeToWinMean);
        Assert.All(all, s => Assert.True(s.TimeToWinMean <= (s.Encounter == ClericSolo ? 1.8 : 1.6) * mean, $"{Fmt(s)}; média {mean:F1}s"));
    }

    /// <summary>
    /// Matriarca nível 8 (crescimento de chefe), grupo de 4 no nível 7 e no 8 com incomuns: vitória 60–97%, 2–4 min (era
    /// 60–90%; o balanceamento do playtest de 2026-11-16 subiu o nível 8 para 94%: balance-v0.18 §4).
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    public void Matriarch_level_8_group_with_uncommons_wins_60_to_97_percent_in_2_to_4_minutes(int level)
    {
        var s = RunProfile("encounter.forest_matriarch", "kite+dodge", CharacterProfile.Expected(level) with { Gear = ItemRarity.Uncommon }, 8);
        Assert.True(s.WinRate is >= 0.60 and <= 0.97, Fmt(s));
        Assert.True(s.TimeToWinMean is >= 120 and <= 240, Fmt(s));
    }
}
