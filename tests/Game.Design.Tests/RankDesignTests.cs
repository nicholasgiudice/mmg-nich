using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-063: metas de NH de design/progression-v0.1.md §8 nos encontros do protótipo. Os perfis gastam os 15 pontos do
/// nível 15 com os atributos do nível 1 (o simulador ainda não tem monstros por nível): medem distribuições, não a curva.
/// Relatório: tasks/reports/balance-v0.6.md.
/// </summary>
public class RankDesignTests(ITestOutputHelper output)
{
    /// <summary>Encontro solo de cada classe com a estratégia correta (a mesma dos testes de NH1).</summary>
    public static TheoryData<string, string> SoloEncounters => new()
    {
        { "encounter.three_wolves", "rotation" },
        { "encounter.wolf_and_bandit", "kite" },
        { "encounter.four_rats", "rotation" },
        { "encounter.skeleton_guard", "rotation" },
    };

    private static string Fmt(BatchSummary s) => $"{s.Encounter} / {s.Strategy}: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s";

    [Theory]
    [MemberData(nameof(SoloEncounters))]
    public void Two_skills_at_rank_5_and_spread_points_both_win_solo_at_least_70_percent(string encounter, string strategy)
    {
        var focused = RunRanked(encounter, strategy, RankProfiles.Focused);
        var spread = RunRanked(encounter, strategy, RankProfiles.Spread);
        Assert.True(focused.WinRate >= 0.70, Fmt(focused));
        Assert.True(spread.WinRate >= 0.70, Fmt(spread));
    }

    [Fact]
    public void Higher_ranks_never_lose_more_than_rank_1_in_the_matriarch_fight()
    {
        var nh1 = Run("encounter.forest_matriarch", "kite+dodge");
        foreach (var profile in new[] { RankProfiles.Focused, RankProfiles.Spread })
        {
            var s = RunRanked("encounter.forest_matriarch", "kite+dodge", profile);
            Assert.True(s.WinRate >= nh1.WinRate, $"{Fmt(s)} vs {Fmt(nh1)}");
        }
    }

    /// <summary>
    /// "Nenhuma habilidade no NH5 sozinha resolve um encontro sem movimento" — mesma regra da Explosão no NH1: o spam
    /// dela vence no máximo metade das vezes ou demora mais que a rotação com os mesmos NH. O Clérigo fica de fora: no
    /// encontro solo a rotação dele já é só a Corrente (cura, escudo e purificação não têm uso), então os dois empatam.
    /// </summary>
    [Theory]
    [InlineData("encounter.three_wolves", "class.warrior", "warrior.heavy_strike")]
    [InlineData("encounter.three_wolves", "class.warrior", "warrior.whirlwind")]
    [InlineData("encounter.wolf_and_bandit", "class.archer", "archer.power_shot")]
    [InlineData("encounter.wolf_and_bandit", "class.archer", "archer.rain_of_arrows")]
    public void No_single_rank_5_skill_solves_an_encounter_alone(string encounter, string cls, string ability)
    {
        var pair = new[] { $"{cls}:{ability}=5" };
        var opt = new Dictionary<string, string> { [$"{cls}:explosion"] = ability };
        var rot = RunRanked(encounter, "rotation", RankProfiles.Nh1, pair, opt);
        var spam = RunRanked(encounter, "explosion_spam", RankProfiles.Nh1, pair, opt);
        Assert.True(spam.WinRate <= 0.50 || spam.TimeToWinMean > rot.TimeToWinMean, $"{Fmt(spam)} vs {Fmt(rot)}");
    }

    /// <summary>
    /// T-206 (reativado; balance-v0.6.md §4): medido no nível certo, com o Mago nível 9 no perfil esperado, a habilidade
    /// no NH5 e as outras no NH1 (o Campo leva o ponto que sobra), contra ratos do nível 9. Nos NH altos a regra é a
    /// opção 3 da v0.6 (decisão do designer, balance-v0.7 §4): o spam vence no máximo metade das vezes, ou a rotação
    /// não é mais que 1,5× mais lenta que ele.
    /// </summary>
    [Theory]
    [InlineData("mage.elemental_blast", "mage.lightning_field")]
    [InlineData("mage.lightning_field", "mage.elemental_blast")]
    public void Mage_rank_5_area_skill_does_not_solve_the_rats_alone(string ability, string other)
    {
        var ranks = new Dictionary<string, int>
        {
            [ability] = 5,
            [other] = ability == "mage.lightning_field" ? 1 : 2,
            ["mage.ice_prison"] = 1,
            ["mage.arcane_barrier"] = 1,
        };
        var profile = CharacterProfile.Expected(9) with { Ranks = new Dictionary<string, Dictionary<string, int>> { ["class.mage"] = ranks } };
        var opt = new Dictionary<string, string> { ["class.mage:explosion"] = ability };
        var rot = RunProfile("encounter.four_rats", "rotation", profile, 9, opt);
        var spam = RunProfile("encounter.four_rats", "explosion_spam", profile, 9, opt);
        output.WriteLine($"{Fmt(spam)} | {Fmt(rot)} | rotação/spam = {rot.TimeToWinMean / spam.TimeToWinMean:F2}");
        Assert.True(spam.WinRate <= 0.50 || rot.TimeToWinMean <= 1.5 * spam.TimeToWinMean, $"{Fmt(spam)} vs {Fmt(rot)}");
    }
}
