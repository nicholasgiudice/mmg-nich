using System.Text.Json;
using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-331: a zona montada a partir do mapa do Tiled tem o que vinha do <c>zones.json</c> antes da migração
/// (<c>Fixtures/zones-before-t331.json</c>). As posições (nascimento, portões e pontos de surgimento) eram comparadas uma a
/// uma até a T-330, que redesenhou a Cidade e os Campos maiores; agora só o que não é posição continua igual, e os NPCs da
/// Cidade são os mesmos.
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

    /// <summary>T-342: portões que mudaram de destino (null: o portão saiu).</summary>
    private static readonly Dictionary<(string Zone, string To), string?> Moved = new()
    {
        [("zone.vigil_fields", "zone.matriarch_lair")] = null,
        [("zone.matriarch_lair", "zone.vigil_fields")] = "zone.lower_forest",
    };

    public static TheoryData<string> Zones() => new() { "zone.city", "zone.vigil_fields", "zone.matriarch_lair" };

    private static T[] Read<T>(JsonElement zone, string field) =>
        zone.TryGetProperty(field, out var v) ? JsonSerializer.Deserialize<T[]>(v.GetRawText(), GameJson.Options)! : [];

    [Fact]
    public void The_zones_from_before_still_exist()
    {
        // Zonas novas (T-342, T-343) e instâncias (T-350) podem entrar; as de antes continuam.
        Assert.Subset(Db.Value.Zones.Keys.ToHashSet(StringComparer.Ordinal), Before.Value.Keys.ToHashSet(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public void Zone_built_from_the_map_keeps_what_is_not_a_position(string zoneId)
    {
        var old = Before.Value[zoneId];
        var zone = Db.Value.Zones[zoneId];

        // Os mesmos portões (destinos; portões para zonas novas podem entrar) e os mesmos monstros, em qualquer quantidade de pontos.
        // T-342: o Covil saiu dos Campos e passou para a Floresta.
        var before = Read<GateDefinition>(old, "gates").Select(g => Moved.GetValueOrDefault((zoneId, g.To), g.To)).OfType<string>();
        Assert.Subset(zone.Gates.Select(g => g.To).ToHashSet(StringComparer.Ordinal), before.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(Read<MonsterSpawnDefinition>(old, "monsterSpawns").Select(s => s.Monster).Distinct().Order(StringComparer.Ordinal),
            zone.MonsterSpawns.Select(s => s.Monster).Distinct().Order(StringComparer.Ordinal));

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
        Assert.Equal("Ferreiro", smith.Properties["name"]);
        Assert.Equal("shop.smith", smith.Shop); // T-321
        Assert.Equal("shop.temple", Assert.Single(npcs, n => n.Npc == "npc.priest").Shop);
        Assert.All(npcs.Where(n => n.Npc is not ("npc.smith" or "npc.priest")), n => Assert.Null(n.Shop));
    }
}
