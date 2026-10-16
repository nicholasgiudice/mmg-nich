using Game.Core.Definitions;

namespace Game.Data.Tests;

public class TelegraphPhaseLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private const string Ability = """
        "abilities": [ { "id": "boss.hit", "name": "Hit", "targeting": { "type": "self" } } ]
        """;

    private static string Monster(string phases) => $$"""
        {
          {{Ability}},
          "monsters": [ { "id": "monster.boss", "name": "Boss", "health": 100, "speed": 1, "radius": 1,
            "abilities": [ "boss.hit" ], "ai": { "id": "ai.boss" }, "phases": {{phases}} } ]
        }
        """;

    [Fact]
    public void Valid_telegraph_and_phases_load()
    {
        var json = """
            {
              "abilities": [ { "id": "boss.charge", "name": "Charge", "targeting": { "type": "line", "length": 5, "width": 1 },
                "telegraph": { "shape": "line", "length": 5, "width": 1, "delay": 1.5, "charge": true, "affects": "enemy" },
                "effects": [ { "type": "damage", "base": 10 } ] } ],
              "monsters": [ { "id": "monster.boss", "name": "Boss", "health": 100, "speed": 1, "radius": 1,
                "abilities": [ "boss.charge" ], "ai": { "id": "ai.boss" },
                "phases": [
                  { "id": "one", "healthPercent": 100 },
                  { "id": "two", "healthPercent": 50, "noSummons": true, "abilities": [ "boss.charge" ],
                    "onEnter": [ { "type": "damage", "base": 1 } ] } ] } ]
            }
            """;
        var r = Load(json);
        Assert.True(r.Success, string.Join("\n", r.Errors));
        var charge = r.Database!.Abilities["boss.charge"].Telegraph!;
        Assert.Equal(TelegraphShape.Line, charge.Shape);
        Assert.Equal(30, charge.DelayTicks);
        Assert.True(charge.Charge);
        var phases = r.Database.Monsters["monster.boss"].Phases;
        Assert.Equal(2, phases.Count);
        Assert.True(phases[1].NoSummons);
        Assert.Null(phases[0].Abilities);
    }

    [Fact]
    public void Telegraph_errors_are_reported()
    {
        var json = """
            {
              "abilities": [
                { "id": "a.circle", "name": "A", "targeting": { "type": "self" },
                  "telegraph": { "shape": "circle", "delay": 1, "charge": true } },
                { "id": "a.cone", "name": "B", "targeting": { "type": "self" },
                  "telegraph": { "shape": "cone", "delay": 1, "length": 3, "angle": 400 } }
              ]
            }
            """;
        var r = Load(json);
        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path.EndsWith("telegraph.radius", StringComparison.Ordinal));
        Assert.Contains(r.Errors, e => e.Path.EndsWith("telegraph.charge", StringComparison.Ordinal));
        Assert.Contains(r.Errors, e => e.Path.EndsWith("telegraph.angle", StringComparison.Ordinal));
    }

    [Fact]
    public void Phase_errors_are_reported()
    {
        var r = Load(Monster("""
            [ { "id": "a", "healthPercent": 90 }, { "id": "b", "healthPercent": 95, "abilities": [ "boss.missing" ] },
              { "id": "a", "healthPercent": 0 } ]
            """));
        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path.EndsWith("phases[0].healthPercent", StringComparison.Ordinal)); // primeira != 100
        Assert.Contains(r.Errors, e => e.Path.EndsWith("phases[1].healthPercent", StringComparison.Ordinal)); // não decresce
        Assert.Contains(r.Errors, e => e.Path.EndsWith("phases[1].abilities[0]", StringComparison.Ordinal)); // referência
        Assert.Contains(r.Errors, e => e.Path.EndsWith("phases[2].id", StringComparison.Ordinal)); // duplicada
        Assert.Contains(r.Errors, e => e.Path.EndsWith("phases[2].healthPercent", StringComparison.Ordinal)); // 0 fora da faixa
    }

    [Fact]
    public void Summon_with_negative_max_alive_and_unknown_phase_effect_references_fail()
    {
        var r = Load(Monster("""
            [ { "id": "a", "healthPercent": 100,
                "onEnter": [ { "type": "summon", "monster": "monster.nope", "maxAlive": -1 } ] } ]
            """));
        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path.EndsWith("phases[0].onEnter[0].monster", StringComparison.Ordinal));
        Assert.Contains(r.Errors, e => e.Path.EndsWith("phases[0].onEnter[0].maxAlive", StringComparison.Ordinal));
    }
}
