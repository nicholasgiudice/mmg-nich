using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-201: perfil de atributos da classe, equipamento inicial (T-204), escala dos efeitos, defesa de monstros e regras de estatísticas.</summary>
public class StatsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private const string Hit = """{ "id": "hero.hit", "name": "Hit", "targeting": { "type": "self" } }""";

    private static string Profile(string attributes = """{ "str": 12, "agi": 6, "int": 4, "vit": 10 }""", string baseHealth = "140") => $$"""
        { "classes": [ { "id": "class.hero", "name": "Hero", "speed": 4, "radius": 0.4,
            "attributes": {{attributes}}, "baseHealth": {{baseHealth}}, "healthGrowth": 14,
            "startingEquipment": [ { "item": "weapon.blade", "level": 2 } ],
            "resource": { "type": "mana", "max": 100 }, "basicAttack": "hero.hit" } ],
          "abilities": [ {{Hit}} ],
          "items": [ { "id": "weapon.blade", "name": "Lâmina", "slot": "weapon", "stats": [ { "stat": "weapon_damage", "base": 4, "perLevel": 1 } ] } ] }
        """;

    [Fact]
    public void Class_profile_loads_without_fixed_health()
    {
        var r = Load(Profile());

        Assert.True(r.Success, Errors(r));
        var c = r.Database!.Classes["class.hero"];
        Assert.Equal(12, c.Attributes!.Str);
        Assert.Equal(0, c.Attributes.Dex);
        Assert.Equal(140, c.BaseHealth);
        Assert.Equal(14, c.HealthGrowth);
        Assert.Equal("weapon.blade", Assert.Single(c.StartingEquipment).Item);
        Assert.Equal(2, c.StartingEquipment[0].Level);
    }

    [Fact]
    public void Class_needs_health_or_a_valid_profile()
    {
        var noHealth = Load($$"""
            { "classes": [ { "id": "class.hero", "name": "Hero", "speed": 4, "radius": 0.4,
                "resource": { "type": "mana", "max": 100 }, "basicAttack": "hero.hit" } ],
              "abilities": [ {{Hit}} ] }
            """);
        Assert.False(noHealth.Success);
        Assert.Contains("health", Errors(noHealth));

        var badBase = Load(Profile(baseHealth: "0"));
        Assert.False(badBase.Success);
        Assert.Contains("baseHealth", Errors(badBase));

        var negative = Load(Profile(attributes: """{ "str": -1 }"""));
        Assert.False(negative.Success);
        Assert.Contains("attributes", Errors(negative));
    }

    [Fact]
    public void Scaling_loads_on_damage_heal_and_shield()
    {
        var r = Load("""
            { "abilities": [ { "id": "hero.chain", "name": "Chain", "targeting": { "type": "self" },
                "effects": [
                  { "type": "damage", "base": 10, "scaling": { "weapon": 1.0, "str": 2.5 } },
                  { "type": "heal", "base": 5, "scaling": { "int": 1.2 } },
                  { "type": "shield", "amount": 20, "duration": 5, "scaling": { "vit": 0.5, "dex": 0.1 } },
                  { "type": "damage", "base": 3 } ] } ] }
            """);

        Assert.True(r.Success, Errors(r));
        var fx = r.Database!.Abilities["hero.chain"].Effects;
        Assert.Equal(2.5, Assert.IsType<DamageEffect>(fx[0]).Scaling["str"]);
        Assert.Equal(1.2, Assert.IsType<HealEffect>(fx[1]).Scaling["int"]);
        Assert.Equal(0.5, Assert.IsType<ShieldEffect>(fx[2]).Scaling["vit"]);
        Assert.Empty(Assert.IsType<DamageEffect>(fx[3]).Scaling);
    }

    [Theory]
    [InlineData("\"agi\": 1.0", "scaling.agi")]
    [InlineData("\"str\": -1.0", "scaling.str")]
    public void Invalid_scaling_is_an_error(string entry, string path)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "hero.hit", "name": "Hit", "targeting": { "type": "self" },
                "effects": [ { "type": "damage", "base": 10, "scaling": { {{entry}} } } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(path, Errors(r));
    }

    [Fact]
    public void Monster_defense_loads_with_zero_default()
    {
        var r = Load("""
            { "monsters": [
                { "id": "monster.golem", "name": "Golem", "health": 100, "speed": 2, "radius": 0.5, "ai": { "id": "ai.passive" }, "armor": 30, "resistance": 10 },
                { "id": "monster.rat", "name": "Rat", "health": 10, "speed": 3, "radius": 0.3, "ai": { "id": "ai.passive" } } ] }
            """);

        Assert.True(r.Success, Errors(r));
        Assert.Equal(30, r.Database!.Monsters["monster.golem"].Armor);
        Assert.Equal(10, r.Database.Monsters["monster.golem"].Resistance);
        Assert.Equal(0, r.Database.Monsters["monster.rat"].Armor);
    }

    [Fact]
    public void Stat_rules_load_and_default_to_the_design_values()
    {
        var r = Load("""{ "rules": { "stats": { "critBase": 0.1, "defenseMax": 0.5 } } }""");
        Assert.True(r.Success, Errors(r));
        Assert.Equal(0.1, r.Database!.Rules.Stats.CritBase);
        Assert.Equal(0.5, r.Database.Rules.Stats.DefenseMax);
        Assert.Equal(1.5, r.Database.Rules.Stats.CritMultiplier);

        var bad = Load("""{ "rules": { "stats": { "critMax": 2, "critMultiplier": 0.5 } } }""");
        Assert.False(bad.Success);
        Assert.Contains(bad.Errors, e => e.Path.Contains("critMax"));
        Assert.Contains(bad.Errors, e => e.Path.Contains("critMultiplier"));
    }
}
