using System.Text.Json;
using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-331: a zona montada a partir do mapa do Tiled é igual à que vinha do <c>zones.json</c> antes da migração
/// (<c>Fixtures/zones-before-t331.json</c>): nascimento, portões (retângulo, destino, chegada) e pontos de surgimento (na
/// mesma ordem, que é o grupo de aggro de cada ponto), sem nenhuma posição mudada.
/// </summary>
public class ZoneMapEquivalenceTests
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

    private static readonly Lazy<Dictionary<string, JsonElement>> Before = new(() =>
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "zones-before-t331.json"));
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("zones").EnumerateArray()
            .ToDictionary(z => z.GetProperty("id").GetString()!, z => z.Clone(), StringComparer.Ordinal);
    });

    public static TheoryData<string> Zones() => new() { "zone.city", "zone.vigil_fields", "zone.matriarch_lair" };

    private static T[] Read<T>(JsonElement zone, string field) =>
        zone.TryGetProperty(field, out var v) ? JsonSerializer.Deserialize<T[]>(v.GetRawText(), GameJson.Options)! : [];

    [Fact]
    public void The_same_zones_exist()
    {
        // T-350: zonas novas depois da migração só podem ser instâncias (a de teste e as dungeons).
        var open = Db.Value.Zones.Values.Where(z => z.Kind != ZoneKind.Instance).Select(z => z.Id);
        Assert.Equal(Before.Value.Keys.Order(StringComparer.Ordinal), open.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public void Zone_built_from_the_map_equals_the_old_zones_json(string zoneId)
    {
        var old = Before.Value[zoneId];
        var zone = Db.Value.Zones[zoneId];

        var spawn = JsonSerializer.Deserialize<PositionDefinition>(old.GetProperty("spawn").GetRawText(), GameJson.Options);
        Assert.Equal(spawn, zone.Spawn);
        Assert.Equal(Read<GateDefinition>(old, "gates"), zone.Gates);
        Assert.Equal(Read<MonsterSpawnDefinition>(old, "monsterSpawns"), zone.MonsterSpawns);

        // O que não é posição continua no zones.json, igual.
        Assert.Equal(old.GetProperty("name").GetString(), zone.Name);
        Assert.Equal(old.GetProperty("arena").GetString(), zone.Arena);
        Assert.Equal(old.TryGetProperty("restPerSecond", out var rest) ? rest.GetDouble() : 0, zone.RestPerSecond);
        Assert.Equal(old.TryGetProperty("corpse", out var corpse) ? TickConversion.SecondsToTicks(corpse.GetDouble()) : 100, zone.CorpseTicks);
    }

    [Fact]
    public void City_npcs_come_from_the_map()
    {
        var npcs = Db.Value.Zones["zone.city"].Npcs;

        Assert.Equal(
            ["npc.innkeeper", "npc.merchant", "npc.priest", "npc.smith", "npc.storage", "npc.trainer"],
            npcs.Select(n => n.Npc).Order(StringComparer.Ordinal));
        var smith = Assert.Single(npcs, n => n.Npc == "npc.smith");
        Assert.Equal(new PositionDefinition(33, 9.5), smith.Position); // retângulo (1040, 288) 32×32 px: o centro
        Assert.Equal("Ferreiro", smith.Properties["name"]);
        Assert.Equal("shop.smith", smith.Shop); // T-321
        Assert.Equal("shop.temple", Assert.Single(npcs, n => n.Npc == "npc.priest").Shop);
        Assert.All(npcs.Where(n => n.Npc is not ("npc.smith" or "npc.priest")), n => Assert.Null(n.Shop));
    }
}
