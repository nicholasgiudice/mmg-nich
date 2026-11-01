using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Data;

namespace Game.Sim.Tests;

/// <summary>
/// T-055: Cidade e Campos são mapas do Tiled. Do nascimento de cada zona, um personagem chega andando (células livres,
/// 4 vizinhos) a todos os pontos de surgimento, chegadas e portões; e os portões do mapa batem com os de <c>zones.json</c>.
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

    public static TheoryData<string> Zones() => new() { "zone.city", "zone.vigil_fields" };

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
