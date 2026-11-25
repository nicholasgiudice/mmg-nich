using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// Fato que só roda se as zonas existem em <c>data/zones.json</c>. Antes disso fica pulado com o motivo, e quando o mapa entra
/// o teste liga sozinho (T-530, T-532, T-533): quem cria a zona só precisa deixar a meta passar, ou rever a meta com o designer.
/// </summary>
public sealed class FactIfZoneAttribute : FactAttribute
{
    public FactIfZoneAttribute(params string[] zones)
    {
        var missing = zones.Where(z => !Content.Zones.ContainsKey(z)).ToList();
        if (missing.Count > 0)
        {
            Skip = $"pendente: a zona {string.Join(", ", missing)} ainda não existe (T-530/T-532/T-533)";
        }
    }
}

/// <summary>
/// T-505: metas de XP por hora das zonas novas da fase 5 (<c>design/mapas-16-40-v0.1.md</c> §1 e §8, decisão 3: a curva de XP
/// é mantida; ~5 h do 15 ao 20 e ~8 h do 20 ao 25). Medidas com <see cref="ZonePace"/>; as de hoje estão em <c>tasks/reports/balance-v0.23.md</c>.
/// <list type="bullet">
/// <item><b>Campo</b> (Brejo 16→20, Serra 21→25): com o perfil esperado solo (classe base até o 19; segunda classe do 20 em
/// diante), subir a faixa leva entre 75% e 130% do tempo da curva (<see cref="BrejoHours"/>, <see cref="SerraHours"/>).</item>
/// <item><b>Masmorra</b> (Minas Fundas 24–27): XP por hora por pessoa, num grupo de 4, pelo menos <see cref="DungeonOverField"/>
/// vezes a do campo da mesma faixa (a Serra, solo, nos níveis <see cref="MinesFrom"/> a <see cref="MinesTo"/> − 1).</item>
/// <item><b>Chefe de campo</b> (Bruxa do Brejo): só a medida, sem meta.</item>
/// </list>
/// Os testes ligam sozinhos quando a zona existe (<see cref="FactIfZoneAttribute"/>). Quem cria a zona pode filtrar os grupos
/// que não são caça solo (como as Ruínas com o Pátio e a Torre) em <see cref="SoloHunt"/>, e na masmorra pode trocar a luta do
/// chefe pela do encontro real posto na sala (como a Cripta, <see cref="CryptDesignTests"/>).
/// </summary>
public class ZoneGoalDesignTests(ITestOutputHelper output)
{
    public const string Marsh = "zone.drowned_marsh";
    public const string Ridge = "zone.broken_ridge";
    public const string Mines = "zone.deep_mines";

    /// <summary>Horas da curva para subir cada faixa (design/mapas-16-40-v0.1.md §1: 15→20 ~5 h, 20→25 ~8 h; a Serra começa no 21, ~1 h depois).</summary>
    public const double BrejoHours = 5;
    public const double SerraHours = 7;

    /// <summary>Janela do tempo da curva que o campo pode levar (T-505).</summary>
    public const double FieldMin = 0.75;
    public const double FieldMax = 1.30;

    /// <summary>A masmorra rende pelo menos isto vezes a XP por hora do campo da faixa, por pessoa.</summary>
    public const double DungeonOverField = 1.3;

    public const int MinesFrom = 24;
    public const int MinesTo = 26;

    /// <summary>
    /// Grupos de caça solo de uma zona de campo: tudo, menos chefe e objetivo. Troque aqui se a zona tiver áreas que pedem grupo.
    /// T-530: no Brejo, a Cabana da Bruxa (bandos de 4 a 6 Afogados, grupo recomendado) fica de fora.
    /// T-532: na Serra, os grupos grandes (5 ou mais, que pedem dano em área) e o Golem (elite) ficam de fora.
    /// </summary>
    private static bool SoloHunt(FieldGroup g) => ZonePace.IsHuntable(g) && MarshZoneDesignTests.SoloHunt(g) && RidgeZoneDesignTests.SoloHunt(g);

    [FactIfZone(Marsh)]
    public void Marsh_solo_from_level_16_to_20_takes_75_to_130_percent_of_the_curve_time()
    {
        var pace = ZonePace.Field(Marsh, 16, 20, 1, include: SoloHunt);
        output.WriteLine(pace.Describe());
        AssertWithinCurve(pace, BrejoHours);
    }

    [FactIfZone(Ridge)]
    public void Ridge_solo_with_a_second_class_from_level_21_to_25_takes_75_to_130_percent_of_the_curve_time()
    {
        var pace = ZonePace.Field(Ridge, 21, 25, 1, include: SoloHunt);
        output.WriteLine(pace.Describe());
        AssertWithinCurve(pace, SerraHours);
    }

    [FactIfZone(Marsh, Ridge, Mines)]
    public void Mines_in_a_party_of_four_give_at_least_1_3_times_the_xp_per_hour_of_the_field_per_person()
    {
        var boss = MinesBoss();
        var ratios = new List<string>();
        var dungeon = new List<double>();
        var field = new List<double>();
        for (var level = MinesFrom; level < MinesTo; level++)
        {
            var run = ZonePace.Dungeon(Mines, level, 4, g => ZonePace.IsHuntable(g), boss);
            var solo = ZonePace.Field(Ridge, level, level + 1, 1, include: SoloHunt);
            dungeon.Add(run.XpPerHour);
            field.Add(solo.XpPerHour);
            ratios.Add($"nv{level}: {run.Describe()} | campo {solo.XpPerHour:F0} XP/h");
        }

        output.WriteLine(string.Join("\n", ratios));
        var ratio = dungeon.Average() / field.Average();
        output.WriteLine($"masmorra ÷ campo: {ratio:F2}×");
        Assert.True(ratio >= DungeonOverField, $"masmorra ÷ campo = {ratio:F2}× (meta {DungeonOverField}×); {string.Join("; ", ratios)}");
    }

    /// <summary>Chefe de campo: só a medida, sem meta (a Bruxa do Brejo, nível 20; grupo de 4, que é como se luta com um chefe).</summary>
    [FactIfZone(Marsh)]
    public void Marsh_field_boss_is_measured_without_a_goal()
    {
        var m = ZonePace.FieldBoss(Marsh, 20, 4);
        output.WriteLine($"{m.Boss}: {m.XpPerPerson:F0} XP por pessoa a cada {m.CycleSeconds / 60:F1} min = {m.XpPerHour:F0} XP/h por pessoa, vitória {m.WinRate:P0}");
        Assert.True(m.XpPerHour > 0 && double.IsFinite(m.XpPerHour));
    }

    /// <summary>O chefe da masmorra (o monstro de categoria chefe da zona) numa luta de grupo na arena aberta, com o limite dos chefes.</summary>
    private static (FieldGroup, Fight) MinesBoss()
    {
        var boss = FieldGroups.Of(Mines).First(g => g.Points.Any(p => Content.Monsters[p.Monster].Rank == Game.Core.Definitions.MonsterRank.Boss && !p.Objective));
        return (boss, ZonePace.FightOf(boss, 4, ZonePace.BossTimeLimit));
    }

    private static void AssertWithinCurve(FieldPace pace, double curveHours)
    {
        var hours = pace.TotalMinutes / 60;
        Assert.True(hours >= curveHours * FieldMin && hours <= curveHours * FieldMax,
            $"{hours:F2} h para a faixa; a curva pede {curveHours} h ({curveHours * FieldMin:F2}–{curveHours * FieldMax:F2} h). {pace.Describe()}");
    }
}
