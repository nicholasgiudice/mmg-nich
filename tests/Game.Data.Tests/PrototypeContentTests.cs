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
    [InlineData("class.warrior", "warrior.sword_strike", new[] { "warrior.heavy_strike", "warrior.whirlwind", "warrior.firm_guard", "warrior.taunt" })]
    [InlineData("class.archer", "archer.shot", new[] { "archer.power_shot", "archer.hunters_mark", "archer.quick_retreat" })]
    [InlineData("class.mage", "mage.arcane_missile", new[] { "mage.elemental_blast", "mage.ice_prison", "mage.arcane_barrier", "mage.lightning_field" })]
    [InlineData("class.cleric", "cleric.holy_strike", new[] { "cleric.direct_heal", "cleric.sacred_shield", "cleric.holy_chain", "cleric.purify" })]
    public void Each_class_has_basic_attack_and_its_abilities(string classId, string basic, string[] abilities)
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
    public void Every_class_has_the_minor_healing_potion_as_a_limited_consumable()
    {
        var db = Db();
        var potion = db.Abilities["item.minor_healing_potion"];
        Assert.InRange(potion.Charges ?? 0, 2, 3);
        Assert.InRange(potion.CooldownTicks, 15 * 20, 25 * 20);
        Assert.IsType<SelfTargeting>(potion.Targeting);
        foreach (var cls in db.Classes.Values)
        {
            Assert.Contains("item.minor_healing_potion", cls.Consumables);
            Assert.DoesNotContain("item.minor_healing_potion", cls.Abilities);
        }
    }

    [Fact]
    public void Warrior_taunt_and_cleric_purify_are_wired_as_designed()
    {
        var db = Db();
        var taunt = db.Abilities["warrior.taunt"];
        Assert.Contains("warrior.taunt", db.Classes["class.warrior"].Abilities);
        var threat = Assert.IsType<ThreatEffect>(Assert.Single(taunt.Effects));
        Assert.True(threat.TauntDurationTicks > 0);
        Assert.InRange(taunt.CooldownTicks, 8 * 20, 12 * 20);

        var purify = db.Abilities["cleric.purify"];
        Assert.Contains("cleric.purify", db.Classes["class.cleric"].Abilities);
        var remove = Assert.IsType<RemoveStatusEffect>(Assert.Single(purify.Effects));
        Assert.Equal(["poison", "curse"], remove.Tags);
        Assert.Equal(1, remove.Count);
    }

    [Fact]
    public void Mana_comes_only_from_regeneration_over_time()
    {
        // T-032: nenhuma classe ganha Mana ao atacar, curar, proteger ou purificar.
        foreach (var cls in Db().Classes.Values)
        {
            Assert.Equal("mana", cls.Resource.Type);
            Assert.Empty(cls.ResourceRules);
            Assert.True(cls.Resource.RegenPerSecond > 0, cls.Id);
        }
    }

    [Fact]
    public void Archer_has_a_damage_skill_and_mage_basic_attack_is_free()
    {
        var db = Db();
        var damaging = db.Classes["class.archer"].Abilities
            .Where(a => db.Abilities[a].Effects.OfType<DamageEffect>().Any())
            .ToList();
        Assert.Contains("archer.power_shot", damaging);
        Assert.Null(db.Abilities[db.Classes["class.mage"].BasicAttack].Cost);
    }

    [Fact]
    public void Holy_chain_heals_the_caster_and_hits_undead_harder()
    {
        var chain = Db().Abilities["cleric.holy_chain"];
        var dmg = Assert.Single(chain.Effects.OfType<DamageEffect>());
        Assert.Equal(EffectTarget.Targets, dmg.Target);
        Assert.True(dmg.BonusVsTags["undead"] > 0);
        Assert.True(dmg.BonusVsTags["corrupted"] > 0);
        var heal = Assert.Single(chain.Effects.OfType<HealEffect>());
        Assert.Equal(EffectTarget.Self, heal.Target);
        Assert.Empty(Db().Monsters["monster.skeleton_guard"].Resistances);
    }

    [Fact]
    public void Frenzy_reduces_control_duration_and_trap_needs_arming()
    {
        var db = Db();
        Assert.Equal(0.5, db.Statuses["status.frenzy"].Modifiers.ControlDurationMultiplier);
        Assert.Equal(10, db.Areas["area.containment_trap"].ArmTimeTicks); // 0,5 s
        Assert.Equal(0, db.Areas["area.venom_pool"].ArmTimeTicks);
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
