using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Data;

namespace Game.Sim.Tests;

/// <summary>
/// T-055: Cidade e Campos são mapas do Tiled. Do nascimento de cada zona, um personagem chega andando (células livres,
/// 4 vizinhos) a todos os pontos de surgimento, chegadas e portões; e os portões da zona batem com as transições do mapa.
/// Desde a T-331 a zona inteira (nascimento, portões, surgimentos) é montada a partir do mapa. T-343: Ruínas da Estrada Antiga. T-352: Cripta do Vigia. T-342: Floresta Baixa.
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

    public static TheoryData<string> Zones() => new() { "zone.city", "zone.vigil_fields", "zone.matriarch_lair", "zone.old_road_ruins", "zone.watcher_crypt", "zone.lower_forest" };

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

    /// <summary>T-352: o portão para a Cripta fica dentro da área <c>crypt_entrance</c> das Ruínas.</summary>
    [Fact]
    public void Crypt_gate_is_at_the_crypt_entrance_of_the_ruins()
    {
        var ruins = Db.Value.Zones["zone.old_road_ruins"];
        var entrance = Assert.Single(Db.Value.Arenas[ruins.Arena].Areas, a => a.Kind == "crypt_entrance").Area;
        var gate = Assert.Single(ruins.Gates, g => g.To == "zone.watcher_crypt").Area;
        Assert.True(gate.X >= entrance.X && gate.Y >= entrance.Y && gate.X + gate.Width <= entrance.X + entrance.Width
            && gate.Y + gate.Height <= entrance.Y + entrance.Height, "portão fora da entrada da Cripta");
    }

    /// <summary>
    /// T-352: a Cripta é uma instância cuja saída leva de volta às Ruínas, com as salas em sequência e, no fim, a Sala do
    /// Vigia: o Vigia nível 15 e três selos (objetivos), todos alcançáveis a pé da entrada.
    /// </summary>
    [Fact]
    public void Crypt_is_an_instance_with_the_rooms_in_sequence_and_the_watcher_with_three_seals()
    {
        var crypt = Db.Value.Zones["zone.watcher_crypt"];
        Assert.Equal(ZoneKind.Instance, crypt.Kind);
        Assert.Equal("zone.old_road_ruins", crypt.ExitGate()!.To);

        var rooms = crypt.MonsterSpawns.Select(s => s.Area).Distinct().ToList();
        Assert.Equal(["Salão dos Escudos", "Galeria das Cinzas", "Câmara dos Juramentos", "Rota dos Mortos", "Passagem Estreita", "Antessala", "Sala do Vigia"], rooms);

        var boss = crypt.MonsterSpawns.Where(s => s.Area == "Sala do Vigia").ToList();
        var watcher = Assert.Single(boss, s => s.Monster == "monster.faceless_watcher");
        Assert.Equal(15, watcher.Level);
        var seals = boss.Where(s => s.Objective).ToList();
        Assert.Equal(3, seals.Count);
        Assert.All(seals, s => Assert.Equal("monster.watcher_seal", s.Monster));
        Assert.All(crypt.MonsterSpawns.Where(s => s.Monster != "monster.faceless_watcher" && !s.Objective), s => Assert.InRange(s.Level ?? 0, 12, 14));
    }

    /// <summary>
    /// T-342: a Floresta Baixa sai pelo portão norte da Cidade e tem as quatro subáreas, com os monstros nos níveis 5–8.
    /// O Covil da Matriarca sai dos Campos e passa a ter o portão a partir da Floresta (a caverna do Ninho), ida e volta.
    /// </summary>
    [Fact]
    public void Forest_is_north_of_the_city_with_the_four_subareas_and_the_lair_gate()
    {
        var forest = Db.Value.Zones["zone.lower_forest"];
        Assert.Equal(ZoneKind.Field, forest.Kind);
        var city = Db.Value.Zones["zone.city"];
        var north = Assert.Single(city.Gates, g => g.To == "zone.lower_forest");
        Assert.Equal(0, north.Area.Y);
        Assert.Contains(forest.Gates, g => g.To == "zone.city");

        Assert.Equal(["Bosque das Teias", "Clareira da Névoa", "Ninho da Matriarca", "Trilha dos Caçadores"],
            forest.MonsterSpawns.Select(s => s.Area).Distinct().Order(StringComparer.Ordinal));
        Assert.All(forest.MonsterSpawns, s => Assert.InRange(s.Level ?? 0, 5, 8));
        Assert.Single(forest.MonsterSpawns, s => s.Monster == "monster.spider_big" && s.Area == "Bosque das Teias");
        Assert.Contains(forest.MonsterSpawns, s => s.Monster == "monster.corrupted_ent" && s.Area == "Clareira da Névoa");

        Assert.Equal("zone.matriarch_lair", Assert.Single(forest.Gates, g => g.To != "zone.city").To);
        Assert.Equal(["zone.lower_forest"], Db.Value.Zones["zone.matriarch_lair"].Gates.Select(g => g.To));
        Assert.DoesNotContain(Db.Value.Zones["zone.vigil_fields"].Gates, g => g.To == "zone.matriarch_lair");
    }

    /// <summary>T-342: os grupos de monstros da Floresta ficam a pelo menos 12 tiles uns dos outros.</summary>
    [Fact]
    public void Forest_groups_are_at_least_12_tiles_apart()
    {
        var spawns = Db.Value.Zones["zone.lower_forest"].MonsterSpawns;
        foreach (var a in spawns)
        {
            foreach (var b in spawns.Where(b => !ReferenceEquals(a, b)))
            {
                var d = Math.Sqrt(Math.Pow(a.Position.X - b.Position.X, 2) + Math.Pow(a.Position.Y - b.Position.Y, 2));
                Assert.True(d >= 12, $"{a.Monster} ({a.Position.X}, {a.Position.Y}) a {d:F1} de {b.Monster} ({b.Position.X}, {b.Position.Y})");
            }
        }
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
