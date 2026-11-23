using Game.Bots;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;
using static Game.Design.Tests.SecondClassDesignTests;

namespace Game.Design.Tests;

/// <summary>
/// T-423: metas da segunda classe do Clérigo (<c>design/progression-v0.1.md</c> §8, "Metas da segunda classe") com as
/// habilidades reais do Sacerdote e do Inquisidor. Relatório: <c>tasks/reports/balance-v0.20.md</c>. As metas gerais
/// (nível 21, distribuição de pontos, diferença entre segundas classes) estão em <see cref="SecondClassDesignTests"/>.
/// </summary>
public class ClericSecondClassDesignTests(ITestOutputHelper output)
{
    private const string Wolf = "monster.grey_wolf";

    private void Log(string label, BatchSummary s) => output.WriteLine(Fmt(label, s));

    public static TheoryData<int> Levels => SecondClassDesignTests.Levels;

    // ---------- Inquisidor ----------

    /// <summary>Inquisidor × Clérigo só com a base, Três Lobos do nível: vence ≥ 85% em no máximo 85% do tempo (a meta do Berserker).</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Inquisitor_beats_three_wolves_in_at_most_85_percent_of_the_base_time(int level)
    {
        var baseOnly = BaseOnly(level, Cleric);
        var inquisitor = SoloWith(level, Inquisitor);
        Log($"nv{level} Clérigo só a base", baseOnly);
        Log($"nv{level} Inquisidor", inquisitor);

        Assert.True(inquisitor.WinRate >= 0.85, Fmt("Inquisidor", inquisitor));
        Assert.True(inquisitor.TimeToWinMean <= 0.85 * baseOnly.TimeToWinMean,
            $"{Fmt("Inquisidor", inquisitor)} | {Fmt("base", baseOnly)} | razão {inquisitor.TimeToWinMean / baseOnly.TimeToWinMean:P0}");
    }

    /// <summary>
    /// Inquisidor no 40 contra um Lobo do nível (o duelo da curva de níveis da T-402, sem as assinaturas): no máximo 1,8× a
    /// média das quatro classes, com o Inquisidor no lugar do Clérigo. Só com a base, o Clérigo ficava em 1,92×
    /// (<c>tasks/reports/balance-v0.18.md</c> §4).
    /// </summary>
    [Fact]
    public void Inquisitor_against_the_wolf_at_level_40_is_within_1_8_times_the_class_average()
    {
        const int level = 40;
        var times = LevelCurve.Classes.Where(c => c != Cleric)
            .ToDictionary(c => c, c => LevelCurve.Kill(Content, c, level, Wolf, Runs).Batch.TimeToWinMean);
        var duel = LevelCurve.WithDuel(Content, Cleric, Wolf, out var encounter);
        var skipSignatures = new Dictionary<string, string> { [$"{Cleric}:skip"] = "cleric.miracle,inquisitor.divine_wrath" };
        var inquisitor = RunWith(encounter, SoloStrategy(Cleric), WithSecondClass(level, Inquisitor), level, skipSignatures, duel);
        times[Inquisitor] = inquisitor.TimeToWinMean;
        var mean = times.Values.Average();
        Log($"nv{level} Inquisidor contra o Lobo (média {mean:F1} s; {string.Join(", ", times.Select(t => $"{t.Key} {t.Value:F1} s"))})", inquisitor);

        Assert.True(inquisitor.WinRate >= 0.85, Fmt("Inquisidor", inquisitor));
        Assert.True(inquisitor.TimeToWinMean <= 1.8 * mean, $"Inquisidor {inquisitor.TimeToWinMean:F1} s × média {mean:F1} s ({inquisitor.TimeToWinMean / mean:F2}×)");
    }

    // ---------- Sacerdote ----------

    /// <summary>Sacerdote solo: vence ≥ 85% os Três Lobos do nível; pode ser mais lento que o Clérigo só com a base.</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Priest_wins_solo_at_least_85_percent(int level)
    {
        var priest = SoloWith(level, Priest);
        Log($"nv{level} Clérigo só a base", BaseOnly(level, Cleric));
        Log($"nv{level} Sacerdote", priest);
        Assert.True(priest.WinRate >= 0.85, Fmt("Sacerdote", priest));
    }

    /// <summary>
    /// Grupo de 4 contra a Matriarca do nível: com o Sacerdote no lugar do Clérigo só com a base, a vitória não cai (margem
    /// de 10 pontos, como a do Guardião).
    /// </summary>
    [Theory]
    [InlineData(30)]
    [InlineData(40)]
    public void Priest_in_the_group_does_not_lower_the_win_rate(int level)
    {
        var baseOnly = GroupWith(level, null);
        var priest = GroupWith(level, Priest);
        Log($"nv{level} grupo só a base (outros causam {OthersDealtPerSecond(baseOnly, Cleric):F1}/s)", baseOnly);
        Log($"nv{level} grupo com Sacerdote (outros causam {OthersDealtPerSecond(priest, Cleric):F1}/s)", priest);

        Assert.True(priest.WinRate >= baseOnly.WinRate - 0.10, $"{Fmt("Sacerdote", priest)} | {Fmt("base", baseOnly)}");
    }

    /// <summary>
    /// Com o Sacerdote, os outros membros (Guerreiro, Arqueiro e Mago) causam pelo menos 10% mais dano por segundo vivo
    /// (Bênção e Inspiração), como a medida do Guardião.
    /// </summary>
    [Theory]
    [InlineData(30)]
    [InlineData(40)]
    public void Priest_raises_the_damage_of_the_rest_of_the_group_by_at_least_10_percent(int level)
    {
        var (before, after) = (OthersDealtPerSecond(GroupWith(level, null), Cleric), OthersDealtPerSecond(GroupWith(level, Priest), Cleric));

        Assert.True(after >= 1.10 * before, $"outros: {after:F1}/s com o Sacerdote × {before:F1}/s só a base ({after / before - 1:P0})");
    }
}
