using System.Numerics;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Spatial;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-352: a Cripta do Vigia (<c>data/maps/watcher_crypt.tmj</c>, instância de grupo, níveis 12–14 e o Vigia nível 15).
/// <list type="bullet">
/// <item>Salas: cada grupo (pontos da mesma subárea a até 4 tiles, como nos Campos) vira um encontro na arena aberta do
/// protótipo, contra um grupo de 4 (Guerreiro, Arqueiro, Mago, Clérigo) no perfil esperado do nível do grupo: vitória ≥ 80%.</item>
/// <item>Chefe: o encontro da T-351 (<see cref="WatcherDesignTests"/>) posto na Sala do Vigia real, com o Vigia e os selos nos
/// pontos do mapa e o grupo na porta oeste: a meta da T-351 (média dos níveis 14 e 15 entre 50% e 80%, 4–6 min).</item>
/// <item>Duração: soma das lutas, da caminhada da entrada ao chefe e de uma pausa entre grupos (estimativa; meta 25–40 min).</item>
/// </list>
/// Relatório: <c>tasks/reports/balance-v0.15.md</c>.
/// </summary>
public class CryptDesignTests(ITestOutputHelper output)
{
    public const string Zone = "zone.watcher_crypt";
    public const string BossRoom = "Sala do Vigia";
    private const string BossEncounter = "encounter.faceless_watcher.crypt";

    /// <summary>Pausa entre um grupo e o próximo (recuperar vida e Mana, juntar o grupo), em segundos.</summary>
    public const double PauseSeconds = 45;

    private static readonly string[] Party = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    private static readonly Dictionary<string, string> Strategy = new()
    {
        ["class.warrior"] = "rotation+dodge",
        ["class.archer"] = "kite+dodge",
        ["class.mage"] = "kite+dodge",
        ["class.cleric"] = "rotation+dodge",
    };

    private static readonly Lazy<IReadOnlyList<FieldGroup>> AllGroups = new(() => FieldGroups.Of(Zone));

    private static IReadOnlyList<FieldGroup> RoomGroups => AllGroups.Value.Where(g => g.Area != BossRoom).ToList();

    private static IReadOnlyList<FieldGroup> Typical => RoomGroups.GroupBy(g => g.Key).Select(g => g.First()).ToList();

    public static TheoryData<string> Groups()
    {
        var data = new TheoryData<string>();
        foreach (var g in Typical)
        {
            data.Add(g.Key);
        }

        return data;
    }

    private static string Fmt(BatchSummary s) => $"{s.Encounter}: win {s.WinRate:P0}, t {s.TimeToWinMean:F0}s, mortes {s.PlayerDeaths:F2}";

    [Theory]
    [MemberData(nameof(Groups))]
    public void A_party_of_four_of_the_group_level_clears_each_room_group_at_least_80_percent(string group)
    {
        var s = Room(Typical.Single(t => t.Key == group));
        output.WriteLine($"{group}: {Fmt(s)}");
        Assert.True(s.WinRate >= 0.80, $"{group}: {Fmt(s)}");
    }

    /// <summary>A meta da T-351 na Sala do Vigia real (30 × 24, selos nas bordas).</summary>
    [Fact]
    public void A_group_of_four_with_rares_beats_the_watcher_in_the_real_room_50_to_80_percent_in_4_to_6_minutes()
    {
        var batches = new[] { 14, 15 }.Select(Boss).ToList();
        var report = string.Join("; ", batches.Select(Fmt));
        output.WriteLine(report);

        Assert.InRange(batches.Average(s => s.WinRate), 0.50, 0.80);
        Assert.All(batches, s =>
        {
            Assert.True(s.WinRate is >= 0.40 and <= 0.90, report);
            Assert.True(s.TimeToWinMean is >= 240 and <= 360, report);
        });
    }

    /// <summary>
    /// Duração esperada de uma ida completa, pela rota segura ou pela de risco: lutas (tempo médio de vitória) + pausa por
    /// grupo + caminhada da entrada ao chefe passando por todos os grupos da rota + o chefe no nível 14.
    /// </summary>
    [Fact]
    public void A_full_run_takes_25_to_40_minutes()
    {
        var crypt = Content.Zones[Zone];
        var map = EntityFactory.BuildMap(Content.Arenas[crypt.Arena]);
        var speed = Party.Average(c => Content.Classes[c].Speed);
        var watcher = AllGroups.Value.SelectMany(g => g.Points).Single(p => p.Monster == "monster.faceless_watcher");
        var lines = new List<string>();
        var minutes = new List<double>();
        foreach (var (route, skip) in new[] { ("segura", "Rota dos Mortos"), ("de risco", "Passagem Estreita") })
        {
            var groups = RoomGroups.Where(g => g.Area != skip).ToList();
            var walk = 0.0;
            var from = Pos(crypt.Spawn);
            foreach (var to in groups.Select(g => Pos(g.Points[0].Position)).Append(Pos(watcher.Position)))
            {
                var path = Pathfinder.FindPath(map, from, to);
                Assert.NotEmpty(path);
                walk += path.Prepend(from).Zip(path, Vector2.Distance).Sum();
                from = to;
            }

            var fights = groups.Sum(g => Room(g).TimeToWinMean) + Boss(14).TimeToWinMean;
            var total = (fights + (groups.Count * PauseSeconds) + (walk / speed)) / 60;
            minutes.Add(total);
            lines.Add($"rota {route}: {groups.Count} grupos, lutas {fights / 60:F1} min, pausas {groups.Count * PauseSeconds / 60:F1} min, "
                + $"caminhada {walk / speed / 60:F1} min ({walk:F0} tiles) = {total:F1} min");
        }

        output.WriteLine(string.Join("\n", lines));
        Assert.All(minutes, m => Assert.InRange(m, 25, 40));
    }

    private static Vector2 Pos(PositionDefinition p) => new((float)p.X, (float)p.Y);

    // ---------- lotes ----------

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    private static BatchSummary Room(FieldGroup group) =>
        Cache.GetOrAdd(group.Key, _ => new Lazy<BatchSummary>(() =>
        {
            var (content, encounter) = FieldGroups.Encounter(group, Party, 180);
            return Batch.Run(content, new RunSpec
            {
                EncounterId = encounter,
                Strategy = "rotation+dodge",
                Roles = Party.Where(c => Strategy[c] != "rotation+dodge").ToDictionary(c => c, c => Strategy[c]),
                Options = new BotOptions(Options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = CharacterProfile.Expected(group.Level),
            });
        })).Value;

    /// <summary>O Vigia da T-351 na Sala do Vigia do mapa, com a estratégia correta e raros (como <see cref="WatcherDesignTests"/>).</summary>
    private static BatchSummary Boss(int level) =>
        Cache.GetOrAdd($"boss|{level}", _ => new Lazy<BatchSummary>(() =>
        {
            var options = new Dictionary<string, string>(Options)
            {
                ["class.warrior:interrupt"] = "save",
                ["class.cleric:followInjured"] = "1",
            };
            return Batch.Run(BossContent.Value, new RunSpec
            {
                EncounterId = BossEncounter,
                Strategy = "rotation+dodge",
                Roles = Party.Where(c => Strategy[c] != "rotation+dodge").ToDictionary(c => c, c => Strategy[c]),
                Options = new BotOptions(options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = CharacterProfile.Expected(level) with { Gear = ItemRarity.Rare },
            });
        })).Value;

    /// <summary>Grupo na porta oeste da Sala do Vigia (Guerreiro à frente; Clérigo atrás), como no encontro da T-351.</summary>
    private static readonly (double X, double Y)[] BossPlayers = [(106.5, 51.5), (101.5, 49.5), (101.5, 53.5), (99.5, 51.5)];

    private static readonly Lazy<ContentCatalog> BossContent = new(() =>
    {
        var c = Content;
        var room = AllGroups.Value.Where(g => g.Area == BossRoom).SelectMany(g => g.Points).ToList();
        var watcher = room.Single(p => p.Monster == "monster.faceless_watcher");
        var real = c.Encounters[WatcherDesignTests.Encounter];
        var encounter = real with
        {
            Id = BossEncounter,
            Arena = c.Zones[Zone].Arena,
            Players = Party.Select((cls, i) => new PlayerSpawnDefinition { Class = cls, Position = new PositionDefinition(BossPlayers[i].X, BossPlayers[i].Y) }).ToList(),
            Enemies = [new EnemySpawnDefinition { Monster = watcher.Monster, Position = watcher.Position, Level = watcher.Level }],
            Objectives = room.Where(p => p.Objective).Select(p => real.Objectives[0] with { Monster = p.Monster, Position = p.Position, Level = p.Level }).ToList(),
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
