using Game.Core.Definitions;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-505: a medida de XP por hora (<see cref="ZonePace"/>), em duas partes.
/// <list type="bullet">
/// <item>O modelo, com a luta trocada por um valor fixo (rápido): respeita o renascimento, divide a XP do grupo como a T-310 e
/// aceita qualquer zona pelo id.</item>
/// <item>As zonas de hoje (Campos, Floresta, Ruínas e Cripta), com as lutas do simulador: só a medida, sem meta, como referência
/// do que o designer já jogou e aprovou. Os números vão para <c>tasks/reports/balance-v0.23.md</c>; os testes escrevem tudo na
/// saída (<c>dotnet test --logger "console;verbosity=detailed"</c>).</item>
/// </list>
/// As metas das zonas novas estão em <see cref="ZoneGoalDesignTests"/>.
/// </summary>
public class ZonePaceDesignTests(ITestOutputHelper output)
{
    private const string Fields = FieldGroups.Zone;
    private const string Forest = ForestZoneDesignTests.Zone;
    private const string Ruins = RuinsZoneDesignTests.Zone;
    private const string Crypt = CryptDesignTests.Zone;

    // ---------- o modelo ----------

    /// <summary>Luta instantânea e sempre vencida: sobra só o descanso, a caminhada e o renascimento.</summary>
    private static readonly Func<FieldGroup, Fight> Instant = _ => new Fight(0, 1);

    [Fact]
    public void Field_pace_is_the_xp_per_group_over_the_cycle_of_walk_and_rest()
    {
        var pace = ZonePace.Field(Fields, 1, 2, 1, Instant);

        var level = Assert.Single(pace.Levels);
        Assert.Equal(1, level.Tier);
        Assert.Equal(0, level.FightSeconds);
        Assert.True(level.WalkSeconds > 0, "o circuito pelos grupos tem caminhada");

        // Os Campos têm grupos do nível 1 que renascem mais devagar do que o jogador limpa: o limite é o renascimento.
        var hunted = FieldGroups.Of(Fields).Where(g => g.Level == 1).ToList();
        var cycle = level.WalkSeconds + ZonePace.FieldRestSeconds;
        var perSecond = Math.Min(1 / cycle, hunted.Sum(g => 1 / g.RespawnSeconds));
        Assert.Equal(level.XpPerGroup * perSecond * 3600, level.XpPerHour, 6);
        Assert.Equal(Content.Rules.Progression.XpToNext(1) / (level.XpPerHour / 60), level.Minutes, 6);
    }

    [Fact]
    public void Slower_respawn_never_makes_the_pace_faster_than_what_respawns()
    {
        var pace = ZonePace.Field(Fields, 1, 5, 1, Instant);
        foreach (var l in pace.Levels)
        {
            var hunted = FieldGroups.Of(Fields).Where(g => g.Level == l.Tier).ToList();
            var cap = l.XpPerGroup * hunted.Sum(g => 1 / g.RespawnSeconds) * 3600;
            Assert.True(l.XpPerHour <= cap + 1e-6, $"nv{l.Level}: {l.XpPerHour:F0} XP/h > teto do renascimento {cap:F0}");
        }
    }

    [Fact]
    public void Party_members_get_the_t310_share_of_each_monsters_xp()
    {
        var rules = Content.Rules.Progression;
        foreach (var group in FieldGroups.Typical)
        {
            var solo = ZonePace.XpOf(group, 3, 1);
            var party = ZonePace.XpOf(group, 3, 4);

            // Cada um leva (1 + 10% × 3) / 4 = 32,5% do monstro; a soma do grupo é 1,3× a de quem caça sozinho.
            Assert.InRange(party, solo * 0.325 - group.Monsters, (solo * 0.325) + group.Monsters);
            Assert.Equal(group.Points.Sum(p => p.Count * rules.Share(rules.MonsterXp(p.Level ?? 1, Content.Monsters[p.Monster].Rank, 3), 4)), party);
        }
    }

    [Fact]
    public void A_character_far_above_the_monsters_gets_the_xp_floor()
    {
        var rules = Content.Rules.Progression;
        var group = FieldGroups.Of(Ruins).First(g => g.Level == 11);
        var even = ZonePace.XpOf(group, 11, 1);
        var far = ZonePace.XpOf(group, 40, 1);
        Assert.InRange(far, (even * rules.XpMinLevelModifier) - group.Monsters, (even * rules.XpMinLevelModifier) + group.Monsters);
    }

    [Fact]
    public void The_measure_takes_any_zone_by_id_and_fails_clearly_on_a_zone_without_monsters()
    {
        foreach (var zone in new[] { Fields, Forest, Ruins })
        {
            var min = Content.Zones[zone].Levels!.Min;
            var pace = ZonePace.Field(zone, min, min + 1, 1, Instant);
            Assert.True(pace.XpPerHour > 0 && double.IsFinite(pace.XpPerHour), pace.Describe());
        }

        Assert.Throws<KeyNotFoundException>(() => ZonePace.Field("zone.does_not_exist", 1, 2, 1, Instant));
    }

    [Fact]
    public void A_dungeon_run_sums_the_rooms_and_the_boss()
    {
        var boss = BossGroup();
        var run = ZonePace.Dungeon(Crypt, 13, 4, include: Room, boss: (boss, new Fight(300, 1)), fight: _ => new Fight(60, 1));

        var rooms = FieldGroups.Of(Crypt).Where(Room).ToList();
        Assert.Equal(rooms.Count + 1, run.Groups);
        Assert.Equal((rooms.Count * 60) + 300, run.FightSeconds);
        Assert.Equal(rooms.Count * CryptDesignTests.PauseSeconds, run.RestSeconds);
        Assert.Equal(rooms.Sum(g => ZonePace.XpOf(g, 13, 4)) + ZonePace.XpOf(boss, 13, 4), run.XpPerPerson);
        Assert.Equal(run.XpPerPerson / run.RunSeconds * 3600, run.XpPerHour, 6);
    }

    private static bool Room(FieldGroup g) => ZonePace.IsHuntable(g) && g.Area != CryptDesignTests.BossRoom;

    private static FieldGroup BossGroup() =>
        FieldGroups.Of(Crypt).Single(g => g.Points.Any(p => p.Monster == "monster.faceless_watcher"));

    /// <summary>
    /// Do 20 em diante o perfil tem segunda classe (T-403): sozinho, a média das 8; em grupo, o grupo de referência de
    /// <see cref="ZonePace.PartySecondClasses"/>. Um grupo das Ruínas levado ao nível 21 exercita o caminho que a Serra e as Minas vão usar.
    /// </summary>
    [Fact]
    public void From_level_20_the_profiles_have_a_second_class_and_the_fights_run()
    {
        Assert.Equal(8, ZonePace.SoloProfiles(21).Count);
        Assert.All(ZonePace.SoloProfiles(21), p => Assert.NotNull(Assert.Single(p).Second));
        Assert.Equal(4, ZonePace.Party(21).Count);
        Assert.All(ZonePace.Party(21), m => Assert.NotNull(m.Second));
        Assert.Equal(4, ZonePace.SoloProfiles(19).Count);
        Assert.All(ZonePace.SoloProfiles(19), p => Assert.Null(Assert.Single(p).Second));

        var ruins = FieldGroups.Of(Ruins).Where(RuinsSoloGroups).First(g => g.Level == 11);
        var group = new FieldGroup(ruins.Area, ruins.Points.Select(p => p with { Level = 21 }).ToList());
        var profile = ZonePace.ProfileOf(21, ZonePace.Party(21));
        Assert.Equal(4, profile.SecondClass.Count);
        Assert.Equal(4, profile.SecondClassRanks.Count);

        foreach (var size in new[] { 1, 4 })
        {
            var fight = ZonePace.FightOf(group, size);
            output.WriteLine($"{group.Key} (grupo de {size}): {fight.Seconds:F0} s, vitória {fight.WinRate:P0}");
            Assert.True(fight is { Seconds: > 0 and <= 180, WinRate: > 0 });
        }
    }

    // ---------- as zonas de hoje (sem meta) ----------

    [Fact]
    public void Fields_solo_from_level_1_to_5_is_measured()
    {
        // Os lotes dos Campos (solo, as 4 classes) são os do modelo de ritmo aprovado na T-215/T-302 (FieldsDesignTests).
        var pace = ZonePace.Field(Fields, 1, 5, 1, fight: g => ZonePace.FightOf(
            ZonePace.BaseClasses.Select(c => FieldsDesignTests.RunGroup(FieldGroups.Typical.Single(t => t.Key == g.Key), c)), ZonePace.SoloTimeLimit));
        output.WriteLine(pace.Describe());
        AssertMeasured(pace);
    }

    [Fact]
    public void Forest_solo_from_level_5_to_8_is_measured()
    {
        var pace = ZonePace.Field(Forest, 5, 8, 1, fight: ForestSolo);
        output.WriteLine(pace.Describe());
        AssertMeasured(pace);
    }

    [Fact]
    public void Ruins_solo_from_level_9_to_12_is_measured()
    {
        // O Pátio dos Mortos e a Torre Quebrada não são caça solo (ver RuinsZoneDesignTests).
        var pace = ZonePace.Field(Ruins, 9, 12, 1, RuinsSolo, RuinsSoloGroups);
        output.WriteLine(pace.Describe());
        AssertMeasured(pace);
    }

    /// <summary>A Cripta, grupo de 4 pelas duas rotas (a segura sem a Rota dos Mortos; a de risco sem a Passagem Estreita), no nível 12 ao 14.</summary>
    [Fact]
    public void Crypt_in_a_party_of_four_from_level_12_to_14_is_measured_against_the_ruins_solo()
    {
        var boss = (BossGroup(), new Fight(CryptDesignTests.Boss(14).TimeToWinMean, CryptDesignTests.Boss(14).WinRate));
        var lines = new List<string>();
        var perHour = new List<double>();
        foreach (var (route, skip) in new[] { ("segura", "Rota dos Mortos"), ("de risco", "Passagem Estreita") })
        {
            foreach (var level in new[] { 12, 13, 14 })
            {
                var run = ZonePace.Dungeon(Crypt, level, 4, g => Room(g) && g.Area != skip, boss, g => ToFight(CryptDesignTests.Room(g)));
                lines.Add($"rota {route}, " + run.Describe());
                perHour.Add(run.XpPerHour);
                Assert.True(run.XpPerHour > 0 && double.IsFinite(run.XpPerHour), run.Describe());
            }
        }

        var ruins = ZonePace.Field(Ruins, 12, 15, 1, RuinsSolo, RuinsSoloGroups);
        lines.Add(ruins.Describe());
        lines.Add($"Cripta (média das rotas e níveis) ÷ Ruínas solo: {perHour.Average() / ruins.XpPerHour:F2}×");
        output.WriteLine(string.Join("\n", lines));
    }

    // Os lotes dos testes de zona (mesmo perfil, estratégia e limite): a medida não roda nada a mais.
    private static Fight ForestSolo(FieldGroup g) => ZonePace.FightOf(
        ZonePace.BaseClasses.Select(c => ForestZoneDesignTests.Run(ForestZoneDesignTests.Typical.Single(t => t.Key == g.Key), c)), ZonePace.SoloTimeLimit);

    private static Fight RuinsSolo(FieldGroup g) => ZonePace.FightOf(
        ZonePace.BaseClasses.Select(c => RuinsZoneDesignTests.Run(RuinsZoneDesignTests.Typical.Single(t => t.Key == g.Key), c)), ZonePace.SoloTimeLimit);

    /// <summary>O Pátio dos Mortos e a Torre Quebrada não são caça solo (ver <see cref="RuinsZoneDesignTests"/>).</summary>
    private static bool RuinsSoloGroups(FieldGroup g) =>
        ZonePace.IsHuntable(g) && g.Area is not (RuinsZoneDesignTests.Courtyard or RuinsZoneDesignTests.Tower);

    private static Fight ToFight(Game.Sim.BatchSummary s) => new(s.WinRate > 0 ? s.TimeToWinMean : 180, s.WinRate);

    private static void AssertMeasured(FieldPace pace)
    {
        Assert.All(pace.Levels, l => Assert.True(l.XpPerHour > 0 && double.IsFinite(l.XpPerHour) && l.Minutes is > 0 and < 1000, pace.Describe()));
    }
}
