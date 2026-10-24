namespace Game.Data.Tests;

/// <summary>T-206: <c>rules.monsters</c> e nível dos monstros no encontro.</summary>
public class MonsterScalingLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private const string Data = """
        "arenas": [ { "id": "arena.box", "lines": ["....", "...."] } ],
        "classes": [ { "id": "class.hero", "name": "H", "health": 10, "speed": 4, "radius": 0.4, "resource": { "type": "mana", "max": 1 }, "basicAttack": "hero.hit" } ],
        "abilities": [ { "id": "hero.hit", "name": "Hit", "targeting": { "type": "self" } } ],
        "monsters": [ { "id": "monster.rat", "name": "Rat", "health": 10, "speed": 3, "radius": 0.3, "ai": { "id": "ai.none" } } ]
        """;

    private static string Encounter(string extra, string enemyExtra = "") => $$"""
        { {{Data}},
          "encounters": [ { "id": "encounter.t", "name": "T", "arena": "arena.box", {{extra}}
            "players": [ { "class": "class.hero", "position": { "x": 0.5, "y": 0.5 } } ],
            "enemies": [ { "monster": "monster.rat", "position": { "x": 2.5, "y": 0.5 } {{enemyExtra}} } ] } ] }
        """;

    [Fact]
    public void Monster_scaling_and_levels_load()
    {
        var rules = Load("""{ "rules": { "monsters": { "healthPerLevel": 0.4, "powerPerLevel": 0.2, "boss": { "healthPerLevel": 0.3, "powerPerLevel": 0.4 } } } }""");
        Assert.True(rules.Success, Errors(rules));
        var m = rules.Database!.Rules.Monsters;
        Assert.Equal(2.6, m.HealthFactor(5), 6);
        Assert.Equal(1.8, m.PowerFactor(5), 6);
        Assert.Equal(2.6, m.PowerFactor(5, Game.Core.Definitions.MonsterRank.Boss), 6);
        Assert.Equal(1.8, m.PowerFactor(5, Game.Core.Definitions.MonsterRank.Elite), 6); // sem crescimento próprio = o geral
        Assert.Equal(1, Load("{}").Database!.Rules.Monsters.HealthFactor(9));

        var enc = Load(Encounter("\"monsterLevel\": 7,", ", \"level\": 3"));
        Assert.True(enc.Success, Errors(enc));
        var e = enc.Database!.Encounters["encounter.t"];
        Assert.Equal((7, 3), (e.MonsterLevel, e.Enemies[0].Level));
    }

    [Theory]
    [InlineData("""{ "rules": { "monsters": { "healthPerLevel": -0.1 } } }""", "rules.monsters.healthPerLevel")]
    [InlineData("""{ "rules": { "monsters": { "boss": { "powerPerLevel": -1 } } } }""", "rules.monsters.boss.powerPerLevel")]
    public void Negative_growth_is_an_error(string json, string field)
    {
        var r = Load(json);
        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Fact]
    public void Monster_level_below_1_is_an_error()
    {
        Assert.Contains("monsterLevel", Errors(Load(Encounter("\"monsterLevel\": 0,"))));
        Assert.Contains("enemies[0].level", Errors(Load(Encounter("", ", \"level\": 0"))));
    }
}
