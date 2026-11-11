using System.Text.Json.Nodes;
using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-214, T-331: seção <c>zones</c> (mundo aberto). O nascimento, os portões, os surgimentos e os NPCs vêm do mapa do Tiled
/// da zona; o <c>zones.json</c> só tem o resto.
/// </summary>
public class ZoneLoaderTests
{
    private const string Monsters = """
        "monsters": [ { "id": "monster.rat", "name": "Rat", "health": 10, "speed": 3, "radius": 0.3, "ai": { "id": "ai.passive" } } ]
        """;

    private const string ZoneA = """{ "id": "zone.a", "name": "A", "arena": "map.a", "kind": "safe", "restPerSecond": 0.05 }""";
    private const string ZoneB = """{ "id": "zone.b", "name": "B", "arena": "map.b", "corpse": 2, "levels": { "min": 2, "max": 4 } }""";

    /// <summary>Mapa 6×4 com borda bloqueada (livres: x 1–4, y 1–2), no formato que o Tiled exporta.</summary>
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
            new JsonObject { ["name"] = "collision", ["type"] = "tilelayer", ["width"] = 6, ["height"] = 4, ["x"] = 0, ["y"] = 0, ["data"] = data },
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
        };
    }

    private static JsonObject Layer(string name, params JsonObject[] objects) =>
        new() { ["name"] = name, ["type"] = "objectgroup", ["objects"] = new JsonArray([.. objects]) };

    /// <summary>Objeto; sem largura e altura vira ponto. Posição em pixels.</summary>
    private static JsonObject Obj(string name, string type, double x, double y, double w = 0, double h = 0, params (string Name, JsonNode Value)[] props)
    {
        var o = new JsonObject { ["id"] = 1, ["name"] = name, ["type"] = type, ["x"] = x, ["y"] = y, ["width"] = w, ["height"] = h, ["rotation"] = 0 };
        if (w == 0 && h == 0)
        {
            o["point"] = true;
        }

        if (props.Length > 0)
        {
            o["properties"] = new JsonArray([.. props.Select(p => (JsonNode)new JsonObject { ["name"] = p.Name, ["value"] = p.Value })]);
        }

        return o;
    }

    /// <summary>Cidade: nascimento em (1,5; 1,5), portão na coluna 4 para B, NPC em (2, 2).</summary>
    private static JsonObject CityMap() => Map(
        Layer("spawns", Obj("spawn", "player_spawn", 48, 48), Obj("from_b", "player_spawn", 80, 80)),
        Layer("areas", Obj("Ferreiro", "npc", 32, 32, 64, 64, ("npc", "npc.smith"), ("shop", "shop.smith"), ("name", "Ferreiro"))),
        Layer("transitions", Obj("east", "transition", 128, 32, 32, 64, ("to", "zone.b"))));

    /// <summary>Campo: nascimento em (2,5; 1,5), portão na coluna 1 para A (chegada 'from_b'), ratos dentro da subárea.</summary>
    private static JsonObject FieldMap(params JsonObject[] extraSpawns) => Map(
        Layer("spawns", [
            Obj("spawn", "player_spawn", 80, 48),
            Obj("rats", "monster_spawn", 112, 80, props: [("monster", "monster.rat"), ("level", 2), ("count", 3), ("radius", 0.5), ("respawn", 45)]),
            Obj("lone_rat", "monster_spawn", 144, 48, props: [("monster", "monster.rat")]),
            .. extraSpawns]),
        Layer("areas", Obj("Toca", "subarea", 96, 64, 64, 32)),
        Layer("transitions", Obj("west", "transition", 32, 32, 32, 64, ("to", "zone.a"), ("arrival", "from_b"))));

    /// <summary>T-321: a loja do NPC do mapa da cidade.</summary>
    private const string Shops = """ "shops": [ { "id": "shop.smith", "name": "Ferreiro" } ] """;

    private static LoadResult Load(string zones, JsonObject? a = null, JsonObject? b = null) => GameDataLoader.LoadSources([
        ("t.json", "{" + Monsters + ", " + Shops + ", \"zones\": [" + zones + "] }"),
        ("maps/a.tmj", (a ?? CityMap()).ToJsonString()),
        ("maps/b.tmj", (b ?? FieldMap()).ToJsonString()),
    ]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private static void AssertError(LoadResult r, string file, string path, string fragment)
    {
        Assert.False(r.Success);
        Assert.True(
            r.Errors.Any(e => e.File == file && e.Path.Contains(path, StringComparison.Ordinal) && e.Message.Contains(fragment, StringComparison.Ordinal)),
            $"esperado em {file}: {path} ~ '{fragment}'; erros:\n{Errors(r)}");
    }

    [Fact]
    public void Zones_take_spawn_gates_monsters_and_npcs_from_their_maps()
    {
        var r = Load(ZoneA + ", " + ZoneB);

        Assert.True(r.Success, Errors(r));
        var a = r.Database!.Zones["zone.a"];
        var b = r.Database.Zones["zone.b"];

        Assert.Equal(ZoneKind.Safe, a.Kind);
        Assert.Equal(100, a.CorpseTicks);
        Assert.Null(a.Levels);
        Assert.Equal(new PositionDefinition(1.5, 1.5), a.Spawn);
        var east = Assert.Single(a.Gates);
        Assert.Equal(new RectDefinition(4, 1, 1, 2), east.Area);
        Assert.Equal("zone.b", east.To);
        Assert.Equal(b.Spawn, east.Arrival); // sem 'arrival': o nascimento do destino
        var smith = Assert.Single(a.Npcs);
        Assert.Equal(("Ferreiro", "npc.smith", "shop.smith"), (smith.Name, smith.Npc, smith.Shop));
        Assert.Equal(new PositionDefinition(2, 2), smith.Position);
        Assert.Equal("Ferreiro", smith.Properties["name"]);
        Assert.False(smith.Properties.ContainsKey("npc"));

        Assert.Equal(ZoneKind.Field, b.Kind);
        Assert.Equal(40, b.CorpseTicks);
        Assert.Equal(new LevelRangeDefinition(2, 4), b.Levels);
        Assert.Equal(new PositionDefinition(2.5, 1.5), b.Spawn);
        Assert.Equal(new PositionDefinition(2.5, 2.5), Assert.Single(b.Gates).Arrival); // 'from_b' na cidade
        Assert.Equal(
            new MonsterSpawnDefinition { Area = "Toca", Monster = "monster.rat", Position = new(3.5, 2.5), Count = 3, Radius = 0.5, Level = 2, RespawnTicks = 900 },
            b.MonsterSpawns[0]);
        Assert.Equal(
            new MonsterSpawnDefinition { Area = null, Monster = "monster.rat", Position = new(4.5, 1.5), Count = 1, Radius = 0, Level = null, RespawnTicks = 600 },
            b.MonsterSpawns[1]);
    }

    [Theory]
    [InlineData("spawn", """{ "x": 1.5, "y": 1.5 }""")]
    [InlineData("gates", "[]")]
    [InlineData("monsterSpawns", "[]")]
    [InlineData("npcs", "[]")]
    public void Positions_in_zones_json_are_errors(string field, string value)
    {
        var r = Load(ZoneA + ", " + ZoneB.Replace("\"corpse\"", $"\"{field}\": {value}, \"corpse\"", StringComparison.Ordinal));

        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.File == "t.json" && e.Message.Contains(field, StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_monster_is_an_error_on_the_map_object()
    {
        var r = Load(ZoneA + ", " + ZoneB, b: FieldMap(Obj("wolf", "monster_spawn", 112, 48, props: [("monster", "monster.wolf")])));
        AssertError(r, "maps/b.tmj", "objects[wolf]", "Monstro desconhecido: 'monster.wolf'");
    }

    [Fact]
    public void Spawn_point_in_a_wall_is_an_error()
    {
        var r = Load(ZoneA + ", " + ZoneB, b: FieldMap(Obj("in_wall", "monster_spawn", 16, 16, props: [("monster", "monster.rat")])));
        AssertError(r, "maps/b.tmj", "objects[in_wall]", "célula bloqueada");
    }

    [Theory]
    [InlineData("count", 0, "'count'")]
    [InlineData("count", -2, "'count'")]
    [InlineData("radius", -1, "'radius'")]
    [InlineData("respawn", 0, "'respawn'")]
    public void Invalid_spawn_properties_are_errors(string property, int value, string fragment)
    {
        var r = Load(ZoneA + ", " + ZoneB, b: FieldMap(Obj("bad", "monster_spawn", 112, 48, props: [("monster", "monster.rat"), (property, value)])));
        AssertError(r, "maps/b.tmj", "objects[bad]", fragment);
    }

    [Fact]
    public void Transition_to_an_unknown_zone_is_an_error()
    {
        var map = CityMap();
        map["layers"]![3]!["objects"]![0]!["properties"]![0]!["value"] = "zone.nowhere";
        var r = Load(ZoneA + ", " + ZoneB, a: map);
        AssertError(r, "maps/a.tmj", "objects[east]", "Zona desconhecida: 'zone.nowhere'");
    }

    [Fact]
    public void Arrival_that_is_not_a_player_spawn_of_the_destination_is_an_error()
    {
        var map = FieldMap();
        map["layers"]![3]!["objects"]![0]!["properties"]![1]!["value"] = "Ferreiro";
        var r = Load(ZoneA + ", " + ZoneB, b: map);
        AssertError(r, "maps/b.tmj", "objects[west]", "Chegada 'Ferreiro' não é um player_spawn de 'maps/a.tmj'");
    }

    [Fact]
    public void Safe_zone_with_a_monster_spawn_is_an_error()
    {
        var map = CityMap();
        map["layers"]![1]!["objects"]!.AsArray().Add(Obj("rat", "monster_spawn", 80, 48, props: [("monster", "monster.rat")]));
        var r = Load(ZoneA + ", " + ZoneB, a: map);
        AssertError(r, "maps/a.tmj", "objects[rat]", "Zona segura 'zone.a' não tem monstros");
    }

    [Fact]
    public void Zone_without_a_spawn_is_an_error()
    {
        var map = FieldMap();
        map["layers"]![1]!["objects"]![0]!["name"] = "start";
        var r = Load(ZoneA + ", " + ZoneB, b: map);
        AssertError(r, "maps/b.tmj", "layers[spawns]", "Zona 'zone.b' sem nascimento: crie um player_spawn chamado 'spawn'");

        var ascii = GameDataLoader.LoadSources([
            ("t.json", """{ "arenas": [ { "id": "arena.x", "lines": ["...."] } ], "zones": [ { "id": "zone.x", "name": "X", "arena": "arena.x" } ] }"""),
        ]);
        AssertError(ascii, "t.json", "zones[0].arena", "Zona sem nascimento: 'arena.x' é uma arena ASCII");
    }

    [Fact]
    public void Spawn_or_arrival_inside_a_gate_is_an_error()
    {
        // O nascimento do campo dentro do próprio portão oeste.
        var b = FieldMap();
        b["layers"]![3]!["objects"]![0]!["width"] = 64;
        AssertError(Load(ZoneA + ", " + ZoneB, b: b), "maps/b.tmj", "objects[west]", "nascimento da zona");

        // A chegada na cidade ('from_b', em (2,5; 2,5)) dentro do portão leste da cidade: volta na hora.
        var a = CityMap();
        a["layers"]![3]!["objects"]![0]!["x"] = 64;
        AssertError(Load(ZoneA + ", " + ZoneB, a: a), "maps/b.tmj", "objects[west]", "voltaria na hora");
    }

    private const string InstanceB = """{ "id": "zone.b", "name": "B", "arena": "map.b", "kind": "instance", "maxPlayers": 2, "emptyTimeout": 60, "outsiderTimeout": 10 }""";

    [Fact]
    public void Instance_zone_loads_with_its_limits_and_exits_through_its_first_gate()
    {
        var r = Load(ZoneA + ", " + InstanceB);
        Assert.True(r.Success, Errors(r));
        var b = r.Database!.Zones["zone.b"];

        Assert.Equal(ZoneKind.Instance, b.Kind);
        Assert.Equal((2, 1200, 200), (b.MaxPlayers, b.EmptyTimeoutTicks, b.OutsiderTimeoutTicks));
        var exit = b.ExitGate()!;
        Assert.Equal("zone.a", exit.To);
        Assert.Equal(new PositionDefinition(2.5, 2.5), exit.Arrival); // 'from_b' da cidade
        Assert.Null(r.Database.Zones["zone.a"].ExitGate()); // zona aberta não tem saída

        var defaults = Load(ZoneA + ", " + """{ "id": "zone.b", "name": "B", "arena": "map.b", "kind": "instance" }""").Database!.Zones["zone.b"];
        Assert.Equal((4, 6000, 600), (defaults.MaxPlayers, defaults.EmptyTimeoutTicks, defaults.OutsiderTimeoutTicks));
    }

    [Fact]
    public void Instance_without_an_exit_or_exiting_into_another_instance_is_an_error()
    {
        var closed = FieldMap();
        closed["layers"]!.AsArray().RemoveAt(3);
        AssertError(Load(ZoneA + ", " + InstanceB, b: closed), "maps/b.tmj", "layers[transitions]", "Instância 'zone.b' sem saída");

        var cityInstance = """{ "id": "zone.a", "name": "A", "arena": "map.a", "kind": "instance" }""";
        var noNpc = CityMap();
        noNpc["layers"]![2]!["objects"]!.AsArray().Clear();
        AssertError(Load(cityInstance + ", " + InstanceB, a: noNpc), "maps/b.tmj", "objects[west]", "leva a outra instância ('zone.a')");
    }

    [Fact]
    public void Real_data_has_a_test_instance_that_exits_to_the_forest()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        var zones = GameDataLoader.LoadDirectory(Path.Combine(dir.FullName, "data")).GetDatabaseOrThrow().Zones;
        var test = zones["zone.test_instance"];

        Assert.Equal(ZoneKind.Instance, test.Kind);
        Assert.Equal((4, 300 * 20, 30 * 20), (test.MaxPlayers, test.EmptyTimeoutTicks, test.OutsiderTimeoutTicks));
        Assert.Equal("zone.lower_forest", test.ExitGate()!.To); // o mapa do Covil, que desde a T-342 sai na Floresta
        Assert.Single(test.MonsterSpawns, s => s.Monster == "monster.forest_matriarch");
    }

    [Fact]
    public void Npc_needs_a_valid_npc_id()
    {
        var map = CityMap();
        map["layers"]![2]!["objects"]!.AsArray().Add(Obj("Sem id", "npc", 32, 32, 32, 32, ("name", "?")));
        map["layers"]![2]!["objects"]!.AsArray().Add(Obj("Id ruim", "npc", 64, 32, 32, 32, ("npc", "Smith"), ("shop", "loja")));
        var r = Load(ZoneA + ", " + ZoneB, a: map);
        AssertError(r, "maps/a.tmj", "objects[Sem id]", "Propriedade 'npc'");
        AssertError(r, "maps/a.tmj", "objects[Id ruim]", "'npc' com id inválido");
        AssertError(r, "maps/a.tmj", "objects[Id ruim]", "'shop' com id inválido");
    }

    [Theory]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "map.x" }""", "arena")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "map.b", "restPerSecond": 0.1 }""", "restPerSecond")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "map.b", "levels": { "min": 5, "max": 4 } }""", "levels")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "map.b", "levels": { "min": 0, "max": 4 } }""", "levels")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "map.b", "kind": "instance", "maxPlayers": 0 }""", "maxPlayers")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "map.b", "kind": "instance", "emptyTimeout": 0 }""", "emptyTimeout")]
    [InlineData("""{ "id": "zone.b", "name": "B", "arena": "map.b", "kind": "instance", "outsiderTimeout": 0 }""", "outsiderTimeout")]
    public void Invalid_zone_fields_are_errors(string zoneB, string field)
    {
        var r = Load(ZoneA + ", " + zoneB);

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
        Assert.Equal(new LevelRangeDefinition(1, 4), fields.Levels);
        Assert.All(fields.MonsterSpawns, s => Assert.InRange(s.Level ?? db.Monsters[s.Monster].Level, fields.Levels!.Min, fields.Levels.Max));
    }

    /// <summary>
    /// T-215: as quatro subáreas do design existem, e pontos de grupos diferentes (mais de 4 tiles entre si) ficam a pelo
    /// menos 12 tiles — o aggro por proximidade é 8 — e longe da chegada pelo portão, para o jogador puxar um grupo por vez.
    /// </summary>
    [Fact]
    public void Fields_subareas_exist_and_groups_are_far_enough_apart_to_pull_one_at_a_time()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        var db = GameDataLoader.LoadDirectory(Path.Combine(dir.FullName, "data")).GetDatabaseOrThrow();
        var fields = db.Zones["zone.vigil_fields"];
        Assert.Equal(
            ["Acampamento dos Salteadores", "Clareira dos Caçadores", "Estrada Leste", "Fazenda Abandonada"],
            fields.MonsterSpawns.Select(s => s.Area ?? "").Distinct().Order(StringComparer.Ordinal).ToArray());

        static double Distance(PositionDefinition a, PositionDefinition b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
        var points = fields.MonsterSpawns;
        foreach (var a in points)
        {
            foreach (var b in points.Where(b => b != a && Distance(a.Position, b.Position) > 4))
            {
                Assert.True(Distance(a.Position, b.Position) >= 12, $"{a.Monster} em ({a.Position.X}, {a.Position.Y}) perto de {b.Monster} em ({b.Position.X}, {b.Position.Y})");
            }

            Assert.True(Distance(a.Position, fields.Spawn) >= 12, $"{a.Monster} perto da chegada"); // a chegada pelo portão é o nascimento
        }
    }

    /// <summary>
    /// T-343: Ruínas da Estrada Antiga. As quatro subáreas do design têm monstros, os níveis ficam na faixa 9–11 da zona, a
    /// zona liga com a Cidade nos dois sentidos e grupos diferentes ficam a pelo menos 12 tiles entre si e da chegada, como nos
    /// Campos (T-215).
    /// </summary>
    [Fact]
    public void Ruins_subareas_exist_and_groups_are_far_enough_apart_to_pull_one_at_a_time()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        var db = GameDataLoader.LoadDirectory(Path.Combine(dir.FullName, "data")).GetDatabaseOrThrow();
        var ruins = db.Zones["zone.old_road_ruins"];
        Assert.Equal(ZoneKind.Field, ruins.Kind);
        Assert.Equal(new LevelRangeDefinition(9, 11), ruins.Levels);
        Assert.Contains(ruins.Gates, g => g.To == "zone.city");
        Assert.Contains(db.Zones["zone.city"].Gates, g => g.To == ruins.Id);
        Assert.Equal(
            ["Estrada para a Cripta", "Portão Partido", "Pátio dos Mortos", "Torre Quebrada"],
            ruins.MonsterSpawns.Select(s => s.Area ?? "").Distinct().Order(StringComparer.Ordinal).ToArray());
        Assert.All(ruins.MonsterSpawns, s => Assert.InRange(s.Level ?? db.Monsters[s.Monster].Level, 9, 11));

        static double Distance(PositionDefinition a, PositionDefinition b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
        var points = ruins.MonsterSpawns;
        foreach (var a in points)
        {
            foreach (var b in points.Where(b => b != a && Distance(a.Position, b.Position) > 4))
            {
                Assert.True(Distance(a.Position, b.Position) >= 12, $"{a.Monster} em ({a.Position.X}, {a.Position.Y}) perto de {b.Monster} em ({b.Position.X}, {b.Position.Y})");
            }

            Assert.True(Distance(a.Position, ruins.Spawn) >= 12, $"{a.Monster} perto da chegada");
        }
    }

    // ---------- T-321: NPCs ----------

    [Fact]
    public void Npc_with_unknown_shop_is_an_error()
    {
        var city = Map(
            Layer("spawns", Obj("spawn", "player_spawn", 48, 48), Obj("from_b", "player_spawn", 80, 80)),
            Layer("areas", Obj("Feirante", "npc", 32, 32, 64, 64, ("npc", "npc.trader"), ("shop", "shop.nope"))),
            Layer("transitions", Obj("east", "transition", 128, 32, 32, 64, ("to", "zone.b"))));
        AssertError(Load(ZoneA + ", " + ZoneB, a: city), "maps/a.tmj", "objects[Feirante]", "Loja 'shop.nope' não existe");
    }

    [Fact]
    public void Npc_outside_a_safe_zone_or_inside_a_wall_is_an_error()
    {
        var field = Map(
            Layer("spawns", Obj("spawn", "player_spawn", 80, 48)),
            Layer("areas", Obj("Eremita", "npc", 64, 32, 32, 32, ("npc", "npc.hermit")), Obj("Preso", "npc", 0, 0, 32, 32, ("npc", "npc.stuck"))),
            Layer("transitions", Obj("west", "transition", 32, 32, 32, 64, ("to", "zone.a"), ("arrival", "from_b"))));
        var r = Load(ZoneA + ", " + ZoneB, b: field);
        AssertError(r, "maps/b.tmj", "objects[Eremita]", "fora de zona segura");
        AssertError(r, "maps/b.tmj", "objects[Preso]", "fora do chão");
    }
}
