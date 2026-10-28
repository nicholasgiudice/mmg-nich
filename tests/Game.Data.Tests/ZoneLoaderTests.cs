using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-214: seção <c>zones</c> (mundo aberto).</summary>
public class ZoneLoaderTests
{
    private const string Arenas = """
        "arenas": [
          { "id": "arena.a", "lines": ["######", "#....#", "#....#", "######"] },
          { "id": "arena.b", "lines": ["######", "#....#", "#....#", "######"] } ],
        "monsters": [ { "id": "monster.rat", "name": "Rat", "health": 10, "speed": 3, "radius": 0.3, "ai": { "id": "ai.passive" } } ]
        """;

    private static LoadResult Load(string zones) => GameDataLoader.LoadSources([("t.json", "{" + Arenas + ", \"zones\": [" + zones + "] }")]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private const string City = """
        { "id": "zone.a", "name": "A", "arena": "arena.a", "kind": "safe", "spawn": { "x": 1.5, "y": 1.5 }, "restPerSecond": 0.05,
          "gates": [ { "area": { "x": 4, "y": 1, "width": 1, "height": 2 }, "to": "zone.b", "arrival": { "x": 1.5, "y": 2.5 } } ] }
        """;

    [Fact]
    public void Zones_load_with_seconds_converted_and_defaults()
    {
        var r = Load(City + """
            , { "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 2.5, "y": 1.5 }, "corpse": 2,
                "gates": [ { "area": { "x": 1, "y": 1, "width": 1, "height": 1 }, "to": "zone.a", "arrival": { "x": 2.5, "y": 1.5 } } ],
                "monsterSpawns": [ { "monster": "monster.rat", "position": { "x": 3.5, "y": 2.5 }, "count": 2, "radius": 1, "level": 2, "respawn": 45 } ] }
            """);

        Assert.True(r.Success, Errors(r));
        var a = r.Database!.Zones["zone.a"];
        var b = r.Database.Zones["zone.b"];
        Assert.Equal(ZoneKind.Safe, a.Kind);
        Assert.Equal(100, a.CorpseTicks);
        Assert.Equal(ZoneKind.Field, b.Kind);
        Assert.Equal(40, b.CorpseTicks);
        Assert.Equal(900, b.MonsterSpawns[0].RespawnTicks);
        Assert.True(b.Gates[0].Area.Contains(1.5, 1.5));
        Assert.False(b.Gates[0].Area.Contains(2, 1.5));
    }

    [Theory]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.x", "spawn": { "x": 1.5, "y": 1.5 } }""", "arena")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 0.5, "y": 0.5 } }""", "spawn")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.b", "kind": "safe", "spawn": { "x": 1.5, "y": 1.5 }, "monsterSpawns": [ { "monster": "monster.rat", "position": { "x": 2.5, "y": 1.5 } } ] }""", "monsterSpawns")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 1.5, "y": 1.5 }, "restPerSecond": 0.1 }""", "restPerSecond")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 1.5, "y": 1.5 }, "gates": [ { "area": { "x": 4, "y": 1, "width": 1, "height": 1 }, "to": "zone.z", "arrival": { "x": 1.5, "y": 1.5 } } ] }""", "gates[0].to")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 1.5, "y": 1.5 }, "gates": [ { "area": { "x": 5, "y": 1, "width": 3, "height": 1 }, "to": "zone.a", "arrival": { "x": 2.5, "y": 1.5 } } ] }""", "gates[0].area")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 2.5, "y": 1.5 }, "gates": [ { "area": { "x": 1, "y": 1, "width": 1, "height": 1 }, "to": "zone.a", "arrival": { "x": 4.5, "y": 1.5 } } ] }""", "gates[0].arrival")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 1.5, "y": 1.5 }, "monsterSpawns": [ { "monster": "monster.rat", "position": { "x": 2.5, "y": 1.5 }, "count": 0, "respawn": 0 } ] }""", "monsterSpawns[0].count")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 1.5, "y": 1.5 }, "monsterSpawns": [ { "monster": "monster.wolf", "position": { "x": 2.5, "y": 1.5 } } ] }""", "monsterSpawns[0].monster")]
    public void Invalid_zones_are_errors(string zoneB, string field)
    {
        var r = Load(City + ", " + zoneB);

        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path.EndsWith("." + field, StringComparison.Ordinal));
    }

    [Fact]
    public void Real_data_has_the_city_and_the_fields_linked_by_gates()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        var db = GameDataLoader.LoadDirectory(Path.Combine(dir.FullName, "data")).GetDatabaseOrThrow();

        var city = db.Zones["zone.city"];
        var fields = db.Zones["zone.vigil_fields"];
        Assert.Equal(ZoneKind.Safe, city.Kind);
        Assert.Empty(city.MonsterSpawns);
        Assert.Contains(city.Gates, g => g.To == fields.Id);
        Assert.Contains(fields.Gates, g => g.To == city.Id);
        Assert.All(fields.MonsterSpawns, s => Assert.InRange(s.Level ?? db.Monsters[s.Monster].Level, 1, 4));
    }
}
