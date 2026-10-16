using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>Carrega a pasta data/ real do repositório (T-014).</summary>
public class PrototypeContentTests
{
    private static readonly Lazy<LoadResult> Loaded = new(() => GameDataLoader.LoadDirectory(RepoDir("data")));

    private static string RepoDir(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir!.FullName, relative);
    }

    private static GameDatabase Db()
    {
        Assert.True(Loaded.Value.Success, string.Join("\n", Loaded.Value.Errors));
        return Loaded.Value.Database!;
    }

    [Fact]
    public void Real_data_directory_loads_without_errors()
    {
        Assert.Empty(Loaded.Value.Errors);
        Assert.NotNull(Loaded.Value.Database);
    }

    [Fact]
    public void Loader_ignores_schemas_subfolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mmo-schemas-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "schemas"));
        try
        {
            File.WriteAllText(Path.Combine(dir, "schemas", "x.json"), "{ \"type\": \"object\", \"properties\": {} }");
            File.WriteAllText(Path.Combine(dir, "empty.json"), "{ }");
            var result = GameDataLoader.LoadDirectory(dir);
            Assert.True(result.Success, string.Join("\n", result.Errors));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    [InlineData("class.warrior", "warrior.sword_strike", new[] { "warrior.heavy_strike", "warrior.whirlwind", "warrior.firm_guard" })]
    [InlineData("class.archer", "archer.shot", new[] { "archer.hunters_mark", "archer.quick_retreat", "archer.containment_trap" })]
    [InlineData("class.mage", "mage.arcane_missile", new[] { "mage.elemental_blast", "mage.ice_prison", "mage.arcane_barrier" })]
    [InlineData("class.cleric", "cleric.holy_strike", new[] { "cleric.direct_heal", "cleric.sacred_shield", "cleric.holy_chain" })]
    public void Each_class_has_basic_attack_and_three_abilities(string classId, string basic, string[] abilities)
    {
        var db = Db();
        var cls = db.Classes[classId];
        Assert.Equal(basic, cls.BasicAttack);
        Assert.Equal(abilities, cls.Abilities);
        Assert.Contains(basic, db.Abilities.Keys);
        foreach (var a in abilities)
        {
            Assert.Contains(a, db.Abilities.Keys);
            // O prefixo do id da habilidade identifica a classe dona (warrior.*, archer.*...).
            Assert.StartsWith(classId["class.".Length..] + ".", a, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Prototype_monsters_exist_with_expected_ai_profiles()
    {
        var m = Db().Monsters;
        Assert.Equal("ai.swarm", m["monster.field_rat"].Ai.Id);
        Assert.Equal("ai.pack", m["monster.grey_wolf"].Ai.Id);
        Assert.Equal("ai.ranged_kiter", m["monster.road_bandit"].Ai.Id);
        Assert.Equal("ai.guardian", m["monster.skeleton_guard"].Ai.Id);
        Assert.Contains("undead", m["monster.skeleton_guard"].Tags);
        Assert.Equal("ai.boss", m["monster.forest_matriarch"].Ai.Id);
        Assert.Equal(3, m["monster.forest_matriarch"].Phases.Count);
        Assert.Contains("monster.spider_small", m.Keys);
        Assert.Contains("monster.spider_big", m.Keys);
    }

    [Fact]
    public void Five_scenarios_exist_with_expected_classes()
    {
        var e = Db().Encounters;
        Assert.Equal(5, e.Count);
        Assert.Equal(["class.warrior"], e["encounter.three_wolves"].Players.Select(p => p.Class));
        Assert.Equal(3, e["encounter.three_wolves"].Enemies.Count(x => x.Monster == "monster.grey_wolf"));
        Assert.Equal(["class.archer"], e["encounter.wolf_and_bandit"].Players.Select(p => p.Class));
        Assert.Equal(["class.mage"], e["encounter.four_rats"].Players.Select(p => p.Class));
        Assert.Equal(4, e["encounter.four_rats"].Enemies.Count);
        Assert.Equal(["class.cleric"], e["encounter.skeleton_guard"].Players.Select(p => p.Class));
        Assert.Equal(
            ["class.warrior", "class.archer", "class.mage", "class.cleric"],
            e["encounter.forest_matriarch"].Players.Select(p => p.Class));
    }

    [Fact]
    public void Every_encounter_references_existing_arena_classes_and_monsters()
    {
        var db = Db();
        foreach (var enc in db.Encounters.Values)
        {
            Assert.Contains(enc.Arena, db.Arenas.Keys);
            Assert.All(enc.Players, p => Assert.Contains(p.Class, db.Classes.Keys));
            Assert.All(enc.Enemies, x => Assert.Contains(x.Monster, db.Monsters.Keys));
        }
    }
}
