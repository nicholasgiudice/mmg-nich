using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;
using static Game.Design.Tests.SecondClassDesignTests;

namespace Game.Design.Tests;

/// <summary>
/// T-532: a zona Serra Partida (<c>data/maps/broken_ridge.tmj</c>), a primeira área de segunda classe. Cada grupo típico do
/// mapa (pontos da mesma subárea a até 4 tiles) vira um encontro na arena aberta do protótipo, com os monstros nos níveis do
/// mapa, contra o perfil esperado com segunda classe do nível do grupo, nas 8 segundas classes (os mesmos lotes do ritmo,
/// <see cref="ZonePace.SoloBatches"/>):
/// <list type="bullet">
/// <item>grupos de caça solo (até 4 monstros: pares de Mineiros e de Lobos nos desfiladeiros, sentinelas do Acampamento,
/// Lobos do Ninho): as 8 vencem sozinhas ≥ 85%;</item>
/// <item>grupos grandes (5 a 10: filas de Mineiros, acampamentos, bandos de morcegos), que pedem dano em área: as 6 segundas
/// classes que não são do Clérigo vencem sozinhas ≥ 85%. O Sacerdote e o Inquisidor só são medidos (como o Acampamento da
/// T-511, <see cref="RidgeDesignTests.BigGroupNeedsAGroup"/>: o Clérigo pede grupo contra grupo grande).</item>
/// </list>
/// O Golem é testado em <see cref="RidgeDesignTests"/> (o mesmo monstro e nível). O ritmo (21→25 entre 75% e 130% do tempo da
/// curva) é o de <see cref="ZoneGoalDesignTests"/>, só com os grupos de caça solo (<see cref="SoloHunt"/>).
/// </summary>
public class RidgeZoneDesignTests(ITestOutputHelper output)
{
    public const string Zone = ZoneGoalDesignTests.Ridge;

    /// <summary>A partir de quantos monstros o grupo é "grande" (pede dano em área; <c>design/mapas-16-40-v0.1.md</c> §4).</summary>
    public const int BigGroup = 5;

    private static readonly Lazy<IReadOnlyList<FieldGroup>> AllGroups = new(() => FieldGroups.Of(Zone));

    /// <summary>Um grupo por composição (sem o Golem).</summary>
    internal static IReadOnlyList<FieldGroup> Typical =>
        AllGroups.Value.Where(g => ZonePace.IsHuntable(g) && !IsElite(g)).GroupBy(g => g.Key).Select(g => g.First()).ToList();

    private static bool IsElite(FieldGroup g) => g.Points.Any(p => Content.Monsters[p.Monster].Rank == MonsterRank.Elite);

    /// <summary>Caça solo: sem elite e com menos de <see cref="BigGroup"/> monstros (vale para qualquer zona; no Brejo não tira nada).</summary>
    internal static bool SoloHunt(FieldGroup g) => ZonePace.IsHuntable(g) && !IsElite(g) && g.Monsters < BigGroup;

    public static TheoryData<string, string> SoloGroupsAndSecondClasses() => Cases(big: false);

    public static TheoryData<string, string> BigGroupsAndSecondClasses() => Cases(big: true);

    private static TheoryData<string, string> Cases(bool big)
    {
        var data = new TheoryData<string, string>();
        if (!Content.Zones.ContainsKey(Zone))
        {
            return data;
        }

        foreach (var group in Typical.Where(g => SoloHunt(g) != big))
        {
            foreach (var second in Specs.Keys.Where(s => !big || !RidgeDesignTests.BigGroupNeedsAGroup.Contains(s)))
            {
                data.Add(group.Key, second);
            }
        }

        return data;
    }

    private static string Fmt(string label, BatchSummary s) =>
        $"{label}: vitória {s.WinRate:P0}, {s.TimeToWinMean:F1} s, dano recebido {s.DamageTaken:F0}";

    private static BatchSummary Of(string group, string second) =>
        ZonePace.SoloBatches(Typical.Single(g => g.Key == group)).Single(b => b.Member.Second == second).Batch;

    [Theory]
    [MemberData(nameof(SoloGroupsAndSecondClasses))]
    public void Second_class_of_the_group_level_clears_each_solo_group_at_least_85_percent(string group, string second)
    {
        var s = Of(group, second);
        output.WriteLine(Fmt($"{group} / {second}", s));
        Assert.True(s.WinRate >= 0.85, Fmt($"{group} / {second}", s));
    }

    [Theory]
    [MemberData(nameof(BigGroupsAndSecondClasses))]
    public void Second_class_without_the_cleric_clears_each_big_group_at_least_85_percent(string group, string second)
    {
        var s = Of(group, second);
        output.WriteLine(Fmt($"{group} / {second}", s));
        Assert.True(s.WinRate >= 0.85, Fmt($"{group} / {second}", s));
    }

    /// <summary>
    /// Só a medida, sem meta: XP por hora de quem caça só os grupos grandes, por segunda classe (luta + <see cref="ZonePace.FieldRestSeconds"/>
    /// por grupo, sem caminhada; o Clérigo, que perde, conta o limite de tempo). Mostra quanto o dano em área rende a mais que o
    /// ritmo solo de <see cref="ZoneGoalDesignTests"/>.
    /// </summary>
    [Fact]
    public void Big_groups_are_measured_in_xp_per_hour_for_each_second_class()
    {
        if (!Content.Zones.ContainsKey(Zone))
        {
            return;
        }

        var big = Typical.Where(g => !SoloHunt(g)).ToList();
        foreach (var second in Specs.Keys)
        {
            var parts = big.Select(g =>
            {
                var s = ZonePace.SoloBatches(g).Single(b => b.Member.Second == second).Batch;
                var seconds = (s.WinRate > 0 ? s.TimeToWinMean : ZonePace.SoloTimeLimit) + ZonePace.FieldRestSeconds;
                return (Group: g, Xp: ZonePace.XpOf(g, g.Level, 1) * s.WinRate, Seconds: seconds, s.WinRate);
            }).ToList();
            var xpPerHour = parts.Sum(p => p.Xp) / parts.Sum(p => p.Seconds) * 3600;
            output.WriteLine($"{second}: {xpPerHour:F0} XP/h nos grupos grandes; "
                + string.Join("; ", parts.Select(p => $"{p.Group.Monsters} nv{p.Group.Level} {p.WinRate:P0} em {p.Seconds - ZonePace.FieldRestSeconds:F0} s")));
            Assert.True(double.IsFinite(xpPerHour));
        }
    }
}
