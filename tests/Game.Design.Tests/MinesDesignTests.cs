using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-533: as Minas Fundas (<c>data/maps/deep_mines.tmj</c>, instância de 4, salas nos níveis 24–26 e o Capataz Forjado nível 27),
/// no modelo da Cripta (<see cref="CryptDesignTests"/>).
/// <list type="bullet">
/// <item>Salas: cada grupo (pontos da mesma subárea a até 4 tiles) vira um encontro na arena aberta do protótipo contra o grupo
/// de 4 de referência da masmorra com segunda classe (<see cref="ZonePace.Party"/>: Guardião, Caçador, Piromante e Sacerdote),
/// no perfil esperado do nível do grupo: vitória ≥ 80%. São os mesmos lotes do ritmo de XP (<see cref="ZoneGoalDesignTests"/>).</item>
/// <item>Chefe: o encontro da T-512 (<see cref="OverseerDesignTests"/>) posto na Fornalha real, com o Capataz no ponto do mapa e
/// o grupo na entrada da arena: as metas da T-512 (com Guardião e Sacerdote ≥ 75% em 3–6 min; sem os dois ≤ 40%).</item>
/// <item>Duração: lutas, pausa por grupo e caminhada da entrada ao chefe, por cada rota do Veio Partido (meta 20–35 min).</item>
/// </list>
/// Relatório: <c>tasks/reports/balance-v0.28.md</c>.
/// </summary>
public class MinesDesignTests(ITestOutputHelper output)
{
    public const string Zone = "zone.deep_mines";
    public const string BossRoom = "Fornalha";
    public const string RichRoute = "Veio Rico";
    public const string NarrowRoute = "Veio Estreito";
    private const string BossEncounter = "encounter.forged_overseer.mines";

    private static readonly Lazy<IReadOnlyList<FieldGroup>> AllGroups = new(() => FieldGroups.Of(Zone));

    internal static IReadOnlyList<FieldGroup> RoomGroups => AllGroups.Value.Where(g => g.Area != BossRoom).ToList();

    public static TheoryData<string> Groups()
    {
        var data = new TheoryData<string>();
        foreach (var key in RoomGroups.Select(g => g.Key).Distinct())
        {
            data.Add(key);
        }

        return data;
    }

    private static string Fmt(Fight f) => $"vitória {f.WinRate:P0}, {f.Seconds:F0} s";

    [Theory]
    [MemberData(nameof(Groups))]
    public void A_party_of_four_of_the_group_level_clears_each_room_group_at_least_80_percent(string group)
    {
        var f = Room(RoomGroups.First(g => g.Key == group));
        output.WriteLine($"{group}: {Fmt(f)}");
        Assert.True(f.WinRate >= 0.80, $"{group}: {Fmt(f)}");
    }

    /// <summary>As metas da T-512 com o Capataz na Fornalha real (paredes, entrada no canto noroeste, 40 × 28 livres).</summary>
    [Fact]
    public void In_the_real_furnace_the_overseer_keeps_the_goals_of_t512()
    {
        var with = Boss(OverseerDesignTests.WithRoles);
        var without = Boss(OverseerDesignTests.WithoutRoles);
        output.WriteLine($"Guardião + Sacerdote: vitória {with.WinRate:P0}, {with.TimeToWinMean:F0} s, mortes {with.PlayerDeaths:F2}");
        output.WriteLine($"Berserker + Inquisidor: vitória {without.WinRate:P0}, {without.TimeToWinMean:F0} s, mortes {without.PlayerDeaths:F2}");
        output.WriteLine($"grupo de referência da masmorra (Guardião, Caçador, Piromante, Sacerdote): {Fmt(ReferenceBoss())}");

        Assert.True(with.WinRate >= 0.75, $"com os papéis: {with.WinRate:P0}");
        Assert.InRange(with.TimeToWinMean, 180, 360);
        Assert.True(without.WinRate <= 0.40, $"sem os papéis: {without.WinRate:P0}");
    }

    /// <summary>
    /// Duração de uma ida completa por cada rota do Veio Partido: lutas (tempo médio de vitória) + pausa por grupo
    /// (<see cref="CryptDesignTests.PauseSeconds"/>) + caminhada da entrada ao chefe passando por todos os grupos da rota, pela
    /// velocidade do grupo + o chefe. Meta da tarefa: 20 a 35 min.
    /// </summary>
    [Fact]
    public void A_full_run_takes_20_to_35_minutes_by_either_route()
    {
        var lines = new List<string>();
        var minutes = new List<double>();
        foreach (var (route, skip) in new[] { ("pelo Veio Rico", NarrowRoute), ("pelo Veio Estreito", RichRoute) })
        {
            var run = Run(4, MinLevel, skip);
            minutes.Add(run.RunSeconds / 60);
            lines.Add($"{route}: {run.Describe()}");
        }

        output.WriteLine(string.Join("\n", lines));
        Assert.All(minutes, m => Assert.InRange(m, 20, 35));
    }

    /// <summary>Nível do grupo que entra (o mínimo das salas).</summary>
    public const int MinLevel = 24;

    /// <summary>Uma ida pela masmorra pulando uma das rotas, com o chefe da Fornalha real (grupo de referência).</summary>
    internal static DungeonPace Run(int partySize, int level, string skipRoute) =>
        ZonePace.Dungeon(Zone, level, partySize, g => ZonePace.IsHuntable(g) && g.Area != skipRoute, (BossGroup, ReferenceBoss()), Room);

    // ---------- lotes ----------

    internal static FieldGroup BossGroup => AllGroups.Value.Single(g => g.Area == BossRoom);

    /// <summary>Luta de sala: o grupo de 4 de referência (<see cref="ZonePace.FightOf(FieldGroup, int, int?)"/>).</summary>
    internal static Fight Room(FieldGroup group) => ZonePace.FightOf(group, 4);

    /// <summary>O Capataz na Fornalha contra um grupo da T-512 (segundas classes dadas, raros, nível 27).</summary>
    internal static BatchSummary Boss(string[] seconds) => OverseerDesignTests.Group(seconds, BossContent.Value, BossEncounter);

    /// <summary>A luta do chefe para o ritmo: o grupo de referência da masmorra (<see cref="ZonePace.PartySecondClasses"/>) na Fornalha.</summary>
    internal static Fight ReferenceBoss() => ZonePace.FightOf([Boss(ZonePace.PartySecondClasses)], ZonePace.BossTimeLimit);

    /// <summary>Grupo na entrada da Fornalha (o pé da descida da Antessala, no canto noroeste): Guerreiro à frente, Clérigo atrás.</summary>
    private static readonly (double X, double Y)[] BossPlayers = [(109.5, 57.5), (104.5, 55.5), (104.5, 52.5), (101.5, 52.5)];

    private static readonly Lazy<ContentCatalog> BossContent = new(() =>
    {
        var c = Content;
        var boss = BossGroup.Points.Single();
        var real = c.Encounters[OverseerDesignTests.Encounter];
        var encounter = real with
        {
            Id = BossEncounter,
            Arena = c.Zones[Zone].Arena,
            Players = real.Players.Select((p, i) => p with { Position = new PositionDefinition(BossPlayers[i].X, BossPlayers[i].Y) }).ToList(),
            Enemies = [new EnemySpawnDefinition { Monster = boss.Monster, Position = boss.Position, Level = boss.Level }],
        };
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Append(encounter), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
            Shops = c.Shops,
        };
    });
}
