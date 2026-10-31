using System.Text.Json.Nodes;
using Game.Core.Definitions;
using Game.Core.Spatial;

namespace Game.Data.Tests;

/// <summary>T-051: mapas do Tiled (<c>*.tmj</c>) → arena com colisão, pontos de surgimento, áreas e transições.</summary>
public class TiledMapLoaderTests
{
    private const string Content = """
        {
          "monsters": [ { "id": "monster.rat", "name": "Rat", "health": 10, "speed": 3, "radius": 0.3, "ai": { "id": "ai.passive" } } ],
          "arenas": [ { "id": "arena.b", "lines": ["....", "...."] } ],
          "zones": [
            { "id": "zone.a", "name": "A", "arena": "map.test", "spawn": { "x": 1.5, "y": 1.5 } },
            { "id": "zone.b", "name": "B", "arena": "arena.b", "spawn": { "x": 0.5, "y": 0.5 } }
          ]
        }
        """;

    /// <summary>Mapa 6×4 com borda bloqueada, no formato que o Tiled exporta.</summary>
    private static JsonObject Map(params JsonObject[] objectLayers)
    {
        var data = new JsonArray();
        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 6; x++)
            {
                data.Add(x == 0 || y == 0 || x == 5 || y == 3 ? 1 : 0);
            }
        }

        var layers = new JsonArray
        {
            TileLayer("ground", new JsonArray([.. Enumerable.Range(0, 24).Select(_ => (JsonNode?)7)])),
            TileLayer("collision", data),
        };
        foreach (var l in objectLayers)
        {
            layers.Add(l);
        }

        return new JsonObject
        {
            ["type"] = "map",
            ["orientation"] = "orthogonal",
            ["infinite"] = false,
            ["width"] = 6,
            ["height"] = 4,
            ["tilewidth"] = 32,
            ["tileheight"] = 32,
            ["layers"] = layers,
            ["tilesets"] = new JsonArray(new JsonObject { ["firstgid"] = 1, ["source"] = "tilesets/collision.tsx" }),
        };
    }

    private static JsonObject TileLayer(string name, JsonArray data) => new()
    {
        ["name"] = name,
        ["type"] = "tilelayer",
        ["width"] = 6,
        ["height"] = 4,
        ["x"] = 0,
        ["y"] = 0,
        ["data"] = data,
    };

    private static JsonObject Objects(string name, params JsonObject[] objects) => new()
    {
        ["name"] = name,
        ["type"] = "objectgroup",
        ["objects"] = new JsonArray([.. objects]),
    };

    private static JsonObject Obj(string name, string type, double x, double y, double w = 0, double h = 0, bool point = false, params (string Name, JsonNode Value)[] props)
    {
        var o = new JsonObject { ["id"] = 1, ["name"] = name, ["type"] = type, ["x"] = x, ["y"] = y, ["width"] = w, ["height"] = h, ["rotation"] = 0 };
        if (point)
        {
            o["point"] = true;
        }

        if (props.Length > 0)
        {
            o["properties"] = new JsonArray([.. props.Select(p => (JsonNode)new JsonObject { ["name"] = p.Name, ["type"] = "string", ["value"] = p.Value })]);
        }

        return o;
    }

    private static LoadResult Load(JsonObject map, string file = "maps/test.tmj") =>
        GameDataLoader.LoadSources([("content.json", Content), (file, map.ToJsonString())]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private static void AssertError(LoadResult r, string path, string fragment)
    {
        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path.Contains(path, StringComparison.Ordinal) && e.Message.Contains(fragment, StringComparison.Ordinal));
    }

    [Fact]
    public void Map_becomes_an_arena_with_collision_spawns_areas_and_transitions()
    {
        var r = Load(Map(
            Objects("spawns",
                Obj("start", "player_spawn", 48, 48, point: true),
                Obj("rat_1", "monster_spawn", 96, 32, 32, 32, props: [("monster", "monster.rat"), ("level", 3)])),
            Objects("areas", Obj("altar", "altar", 64, 32, 64, 32, props: [("seal", true)])),
            Objects("transitions", Obj("to_b", "transition", 128, 64, 32, 32, props: [("to", "zone.b")]))));

        Assert.True(r.Success, Errors(r));
        var map = r.Database!.Arenas["map.test"];
        Assert.Equal("maps/test.tmj", map.Source);
        Assert.Equal(["######", "#....#", "#....#", "######"], map.Lines);

        Assert.Equal(new MapSpawn { Kind = MapSpawnKind.Player, Name = "start", Position = new PositionDefinition(1.5, 1.5) }, map.Spawns[0]);
        Assert.Equal(MapSpawnKind.Monster, map.Spawns[1].Kind);
        Assert.Equal(new PositionDefinition(3.5, 1.5), map.Spawns[1].Position); // retângulo: o centro
        Assert.Equal("monster.rat", map.Spawns[1].Monster);
        Assert.Equal(3, map.Spawns[1].Level);

        Assert.Equal("altar", map.Areas[0].Kind);
        Assert.Equal(new RectDefinition(2, 1, 2, 1), map.Areas[0].Area);
        Assert.Equal("true", map.Areas[0].Properties["seal"]);

        Assert.Equal(new RectDefinition(4, 2, 1, 1), map.Transitions[0].Area);
        Assert.Equal("zone.b", map.Transitions[0].To);
        Assert.Null(map.Transitions[0].Arrival);

        // Zonas usam o mapa pelo id.
        Assert.Equal("map.test", r.Database.Zones["zone.a"].Arena);
    }

    [Fact]
    public void Any_painted_collision_tile_blocks_including_flipped_gids()
    {
        var map = Map();
        var data = map["layers"]![1]!["data"]!.AsArray();
        data[8] = 2147483649; // gid 1 com espelhamento horizontal
        data[9] = 42;
        var r = Load(map);

        Assert.True(r.Success, Errors(r));
        Assert.Equal("#.##.#", r.Database!.Arenas["map.test"].Lines[1]);
    }

    [Fact]
    public void Group_layers_are_flattened()
    {
        var map = Map();
        var layers = map["layers"]!.AsArray();
        var collision = layers[1]!;
        layers.RemoveAt(1);
        layers.Add(new JsonObject { ["name"] = "logic", ["type"] = "group", ["layers"] = new JsonArray(collision) });

        Assert.True(Load(map).Success);
    }

    [Fact]
    public void Missing_collision_layer_is_reported()
    {
        var map = Map();
        map["layers"]!.AsArray().RemoveAt(1);
        AssertError(Load(map), "layers", "Camada 'collision' faltando");
    }

    [Fact]
    public void Unknown_monster_is_reported_with_the_object_name()
    {
        var r = Load(Map(Objects("spawns", Obj("wolf_1", "monster_spawn", 48, 48, point: true, props: [("monster", "monster.wolf")]))));
        AssertError(r, "layers[spawns].objects[wolf_1]", "Monstro desconhecido: 'monster.wolf'");
        Assert.Equal("maps/test.tmj", r.Errors.Single().File);
    }

    [Fact]
    public void Monster_spawn_needs_monster_and_valid_level()
    {
        var r = Load(Map(Objects("spawns",
            Obj("a", "monster_spawn", 48, 48, point: true),
            Obj("b", "monster_spawn", 80, 48, point: true, props: [("monster", "monster.rat"), ("level", 0)]))));
        AssertError(r, "objects[a]", "Propriedade 'monster'");
        AssertError(r, "objects[b]", "'level'");
    }

    [Fact]
    public void Spawn_inside_a_wall_or_outside_the_map_is_reported()
    {
        var r = Load(Map(Objects("spawns",
            Obj("wall", "player_spawn", 8, 8, point: true),
            Obj("out", "player_spawn", 500, 8, point: true))));
        AssertError(r, "objects[wall]", "célula bloqueada");
        AssertError(r, "objects[out]", "fora do mapa");
    }

    [Fact]
    public void Wrong_object_class_unknown_layer_and_duplicate_names_are_reported()
    {
        var r = Load(Map(
            Objects("spawns", Obj("x", "chest", 48, 48, point: true), Obj("s", "player_spawn", 48, 48, point: true), Obj("s", "player_spawn", 80, 48, point: true)),
            Objects("spawn")));
        AssertError(r, "objects[x]", "Classe 'chest' inválida");
        AssertError(r, "layers[spawns]", "Nome 's' repetido");
        AssertError(r, "layers[spawn]", "Camada de objetos desconhecida 'spawn'");
    }

    [Fact]
    public void Areas_and_transitions_must_be_rectangles_inside_the_map()
    {
        var r = Load(Map(
            Objects("areas", Obj("web", "web", 48, 48, point: true), Obj("big", "seal", 0, 0, 1000, 32)),
            Objects("transitions", Obj("t", "transition", 32, 32, 32, 32))));
        AssertError(r, "objects[web]", "Use um retângulo");
        AssertError(r, "objects[big]", "fora do mapa");
        AssertError(r, "objects[t]", "Propriedade 'to'");
    }

    [Fact]
    public void Transition_to_unknown_zone_or_arrival_is_reported()
    {
        var r = Load(Map(
            Objects("spawns", Obj("start", "player_spawn", 48, 48, point: true)),
            Objects("transitions",
                Obj("t1", "transition", 32, 32, 32, 32, props: [("to", "zone.nowhere")]),
                Obj("t2", "transition", 64, 32, 32, 32, props: [("to", "zone.a"), ("arrival", "gate")]),
                Obj("t3", "transition", 96, 32, 32, 32, props: [("to", "zone.a"), ("arrival", "start")]))));
        AssertError(r, "objects[t1]", "Zona desconhecida: 'zone.nowhere'");
        AssertError(r, "objects[t2]", "Chegada 'gate' não é um player_spawn");
        Assert.DoesNotContain(r.Errors, e => e.Path.Contains("t3", StringComparison.Ordinal));
    }

    [Fact]
    public void Map_format_problems_are_reported_clearly()
    {
        var tiles = Map();
        tiles["tilewidth"] = 16;
        AssertError(Load(tiles), "tilewidth", "32×32 px");

        var base64 = Map();
        base64["layers"]![1]!["data"] = "AAAAAA==";
        AssertError(Load(base64), "layers[collision].data", "'CSV'");

        var infinite = Map();
        infinite["infinite"] = true;
        AssertError(Load(infinite), "infinite", "infinito");

        var shortData = Map();
        shortData["layers"]![1]!["data"]!.AsArray().RemoveAt(0);
        AssertError(Load(shortData), "layers[collision].data", "23 células; esperado 24");

        AssertError(Load(Map(), "maps/Vigil-Fields.tmj"), "$", "snake_case");
        AssertError(GameDataLoader.LoadSources([("content.json", Content), ("maps/test.tmj", "{ nope")]), "$", "JSON inválido");
    }

    [Fact]
    public void Map_id_clashing_with_another_arena_is_reported()
    {
        var r = GameDataLoader.LoadSources([
            ("content.json", Content.Replace("\"arena.b\", \"lines\"", "\"map.test\", \"lines\"", StringComparison.Ordinal)),
            ("maps/test.tmj", Map().ToJsonString()),
        ]);
        Assert.False(r.Success);
    }

    [Fact]
    public void Load_directory_reads_maps_and_they_enter_the_fingerprint()
    {
        var dir = Directory.CreateTempSubdirectory("t051-");
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "content.json"), Content);
            Directory.CreateDirectory(Path.Combine(dir.FullName, "maps"));
            var path = Path.Combine(dir.FullName, "maps", "test.tmj");
            File.WriteAllText(path, Map().ToJsonString());
            var first = GameDataLoader.LoadDirectory(dir.FullName);
            Assert.True(first.Success, Errors(first));
            Assert.Equal("maps/test.tmj", first.Database!.Arenas["map.test"].Source);

            var changed = Map();
            changed["layers"]![1]!["data"]![10] = 1;
            File.WriteAllText(path, changed.ToJsonString());
            Assert.NotEqual(first.Database.Fingerprint, GameDataLoader.LoadDirectory(dir.FullName).Database!.Fingerprint);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
