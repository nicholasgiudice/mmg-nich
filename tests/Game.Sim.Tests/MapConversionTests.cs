using System.Text.Json;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Data;

namespace Game.Sim.Tests;

/// <summary>
/// T-051: as 5 arenas ASCII dos encontros viraram mapas do Tiled (<c>data/maps/*.tmj</c>). As originais ficam em
/// <c>MapFixtures/ascii-arenas.json</c>; a grade tem de ser a mesma célula a célula e o simulador, o mesmo resultado.
/// </summary>
public class MapConversionTests
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

    /// <summary>Arenas ASCII de antes da conversão, já com o id do mapa que as substituiu (<c>arena.fields</c> → <c>map.fields</c>).</summary>
    private static readonly Lazy<IReadOnlyList<ArenaDefinition>> Ascii = new(() =>
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MapFixtures", "ascii-arenas.json"));
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("arenas").EnumerateArray()
            .Select(a => new ArenaDefinition
            {
                Id = "map." + a.GetProperty("id").GetString()!["arena.".Length..],
                Lines = a.GetProperty("lines").EnumerateArray().Select(l => l.GetString()!).ToList(),
            })
            .ToList();
    });

    public static TheoryData<string> Encounters() => new()
    {
        "encounter.three_wolves",
        "encounter.wolf_and_bandit",
        "encounter.four_rats",
        "encounter.skeleton_guard",
        "encounter.forest_matriarch",
    };

    [Fact]
    public void Every_encounter_uses_a_tiled_map()
    {
        foreach (var e in Db.Value.Encounters.Values)
        {
            var arena = Db.Value.Arenas[e.Arena];
            Assert.True(arena.Source is not null, $"{e.Id} usa a arena ASCII '{e.Arena}'.");
        }
    }

    [Fact]
    public void Maps_have_the_same_grid_as_the_ascii_arenas()
    {
        Assert.Equal(5, Ascii.Value.Count);
        foreach (var ascii in Ascii.Value)
        {
            var map = Db.Value.Arenas[ascii.Id];
            Assert.Equal($"maps/{ascii.Id["map.".Length..]}.tmj", map.Source);
            Assert.Equal(ascii.Lines, map.Lines);

            var a = EntityFactory.BuildMap(ascii);
            var b = EntityFactory.BuildMap(map);
            Assert.Equal((a.Width, a.Height), (b.Width, b.Height));
            for (var y = 0; y < a.Height; y++)
            {
                for (var x = 0; x < a.Width; x++)
                {
                    Assert.True(a.IsBlocked(x, y) == b.IsBlocked(x, y), $"{ascii.Id} ({x}, {y})");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Encounters))]
    public void Simulator_gives_the_same_runs_on_the_map_and_on_the_ascii_arena(string encounter)
    {
        var fromMap = Catalog(Db.Value.Arenas.Values);
        var fromAscii = Catalog(Db.Value.Arenas.Values.Where(a => Ascii.Value.All(x => x.Id != a.Id)).Concat(Ascii.Value));
        var spec = new RunSpec { EncounterId = encounter, Strategy = "kite+dodge", MaxSeconds = 120 };

        for (ulong seed = 1; seed <= 4; seed++)
        {
            var a = EncounterRunner.Run(fromMap, spec, seed);
            var b = EncounterRunner.Run(fromAscii, spec, seed);
            Assert.Equal(b.StateHash, a.StateHash);
            Assert.Equal(b.Ticks, a.Ticks);
            Assert.Equal(b.Outcome, a.Outcome);
        }
    }

    private static ContentCatalog Catalog(IEnumerable<ArenaDefinition> arenas)
    {
        var db = Db.Value;
        return new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, arenas, db.Encounters.Values, db.Items.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
            Zones = db.Zones,
        };
    }
}
