using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Data;

namespace Game.Sim.Tests;

/// <summary>
/// T-055: Cidade e Campos são mapas do Tiled. Do nascimento de cada zona, um personagem chega andando (células livres,
/// 4 vizinhos) a todos os pontos de surgimento, chegadas e portões; e os portões da zona batem com as transições do mapa.
/// Desde a T-331 a zona inteira (nascimento, portões, surgimentos) é montada a partir do mapa. T-343: Ruínas da Estrada Antiga.
/// </summary>
public class ZoneMapTests
{
    private static readonly Lazy<GameDatabase> Db = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        return GameDataLoader.LoadDirectory(Path.Combine(dir.FullName, "data")).GetDatabaseOrThrow();
    });

    public static TheoryData<string> Zones() => new() { "zone.city", "zone.vigil_fields", "zone.matriarch_lair", "zone.old_road_ruins" };

    [Theory]
    [MemberData(nameof(Zones))]
    public void Zone_uses_a_tiled_map(string zoneId)
    {
        var zone = Db.Value.Zones[zoneId];
        Assert.NotNull(Db.Value.Arenas[zone.Arena].Source);
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public void Every_spawn_and_gate_is_reachable_on_foot_from_the_zone_spawn(string zoneId)
    {
        var zone = Db.Value.Zones[zoneId];
        var reached = Reached(zone);

        foreach (var s in zone.MonsterSpawns)
        {
            Assert.True(reached.Contains(((int)s.Position.X, (int)s.Position.Y)), $"{zoneId}: {s.Monster} em ({s.Position.X}, {s.Position.Y}) inalcançável");
        }

        foreach (var g in zone.Gates)
        {
            var cells = Enumerable.Range((int)g.Area.X, (int)g.Area.Width)
                .SelectMany(x => Enumerable.Range((int)g.Area.Y, (int)g.Area.Height).Select(y => (x, y)));
            Assert.True(cells.Any(reached.Contains), $"{zoneId}: portão para {g.To} inalcançável");
        }
    }

    /// <summary>T-343: a entrada da Cripta (área <c>crypt_entrance</c> das Ruínas) é alcançável a pé a partir do nascimento.</summary>
    [Fact]
    public void Crypt_entrance_in_the_ruins_is_reachable_on_foot()
    {
        var zone = Db.Value.Zones["zone.old_road_ruins"];
        var entrance = Assert.Single(Db.Value.Arenas[zone.Arena].Areas, a => a.Kind == "crypt_entrance");
        var reached = Reached(zone);
        var cells = Enumerable.Range((int)entrance.Area.X, (int)entrance.Area.Width)
            .SelectMany(x => Enumerable.Range((int)entrance.Area.Y, (int)entrance.Area.Height).Select(y => (x, y)));
        Assert.Contains(cells, reached.Contains);
    }

    /// <summary>Células livres alcançáveis a pé (4 vizinhos) a partir do nascimento da zona.</summary>
    private static HashSet<(int, int)> Reached(ZoneDefinition zone)
    {
        var map = EntityFactory.BuildMap(Db.Value.Arenas[zone.Arena]);
        var reached = new HashSet<(int, int)>();
        var queue = new Queue<(int X, int Y)>();
        var start = ((int)zone.Spawn.X, (int)zone.Spawn.Y);
        Assert.False(map.IsBlocked(start.Item1, start.Item2));
        reached.Add(start);
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nx >= 0 && ny >= 0 && nx < map.Width && ny < map.Height && !map.IsBlocked(nx, ny) && reached.Add((nx, ny)))
                {
                    queue.Enqueue((nx, ny));
                }
            }
        }

        return reached;
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public void Map_transitions_match_the_zone_gates(string zoneId)
    {
        var zone = Db.Value.Zones[zoneId];
        var arena = Db.Value.Arenas[zone.Arena];
        Assert.Equal(zone.Gates.Count, arena.Transitions.Count);
        foreach (var t in arena.Transitions)
        {
            var gate = Assert.Single(zone.Gates, g => g.To == t.To);
            Assert.Equal((gate.Area.X, gate.Area.Y, gate.Area.Width, gate.Area.Height), (t.Area.X, t.Area.Y, t.Area.Width, t.Area.Height));
            var target = Db.Value.Arenas[Db.Value.Zones[t.To].Arena];
            var arrival = Assert.Single(target.Spawns, s => s.Name == t.Arrival);
            Assert.Equal((gate.Arrival.X, gate.Arrival.Y), (arrival.Position.X, arrival.Position.Y));
        }
    }
}
