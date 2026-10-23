using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-203: seção <c>rules.progression</c> e nível dos monstros.</summary>
public class ProgressionLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    [Fact]
    public void Progression_rules_load_and_default_to_the_design_values()
    {
        var r = Load("""{ "rules": { "progression": { "maxLevel": 20, "participationWindow": 10, "xpByRank": { "boss": 40 } } } }""");

        Assert.True(r.Success, Errors(r));
        var p = r.Database!.Rules.Progression;
        Assert.Equal(20, p.MaxLevel);
        Assert.Equal(200, p.ParticipationWindowTicks);
        Assert.Equal(40, p.XpByRank.Boss);
        Assert.Equal(5, p.XpByRank.Elite);
        Assert.Equal(100, p.CombatTimeoutTicks);
        Assert.Equal([1, 3, 5, 7, 9], p.SkillRankLevels);
        Assert.Equal([12, 13, 15], p.SignatureRankLevels);
        Assert.Equal(new ProgressionRules().XpToNext(4), Load("{}").Database!.Rules.Progression.XpToNext(4));
    }

    [Theory]
    [InlineData("""{ "maxLevel": 0 }""", "maxLevel")]
    [InlineData("""{ "xpCurveBase": 0 }""", "xpCurveBase")]
    [InlineData("""{ "xpPenaltyPerLevelBelow": 1.5 }""", "xpPenaltyPerLevelBelow")]
    [InlineData("""{ "xpZeroLevelsBelow": 0 }""", "xpZeroLevelsBelow")]
    [InlineData("""{ "participationWindow": 0 }""", "participationWindow")]
    [InlineData("""{ "skillRankLevels": [1, 3, 5, 7] }""", "skillRankLevels")]
    [InlineData("""{ "skillRankLevels": [1, 3, 2, 7, 9] }""", "skillRankLevels[2]")]
    [InlineData("""{ "signatureRankLevels": [12, 13, 16] }""", "signatureRankLevels[2]")]
    public void Invalid_progression_rules_are_errors(string section, string field)
    {
        var r = Load($$"""{ "rules": { "progression": {{section}} } }""");

        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path == "rules.progression." + field);
    }

    [Fact]
    public void Monster_level_loads_with_one_as_default_and_must_be_positive()
    {
        var r = Load("""
            { "monsters": [
                { "id": "monster.wolf", "name": "Wolf", "level": 3, "rank": "elite", "health": 100, "speed": 2, "radius": 0.5, "ai": { "id": "ai.passive" } },
                { "id": "monster.rat", "name": "Rat", "health": 10, "speed": 3, "radius": 0.3, "ai": { "id": "ai.passive" } } ] }
            """);

        Assert.True(r.Success, Errors(r));
        Assert.Equal(3, r.Database!.Monsters["monster.wolf"].Level);
        Assert.Equal(MonsterRank.Elite, r.Database.Monsters["monster.wolf"].Rank);
        Assert.Equal(1, r.Database.Monsters["monster.rat"].Level);

        var bad = Load("""{ "monsters": [ { "id": "monster.rat", "name": "Rat", "level": 0, "health": 10, "speed": 3, "radius": 0.3, "ai": { "id": "ai.passive" } } ] }""");
        Assert.False(bad.Success);
        Assert.Contains(bad.Errors, e => e.Path.EndsWith(".level", StringComparison.Ordinal));
    }

    [Fact]
    public void Real_monsters_have_the_design_levels()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "data", "monsters.json")))
        {
            dir = dir.Parent;
        }

        var db = GameDataLoader.LoadDirectory(Path.Combine(dir!.FullName, "data")).GetDatabaseOrThrow();

        Assert.Equal(1, db.Monsters["monster.field_rat"].Level);
        Assert.Equal(2, db.Monsters["monster.grey_wolf"].Level);
        Assert.Equal(3, db.Monsters["monster.road_bandit"].Level);
        Assert.Equal(9, db.Monsters["monster.skeleton_guard"].Level);
        Assert.Equal(8, db.Monsters["monster.forest_matriarch"].Level);
        Assert.Equal(MonsterRank.Boss, db.Monsters["monster.forest_matriarch"].Rank);
        Assert.Equal(15, db.Rules.Progression.MaxLevel);
    }
}
