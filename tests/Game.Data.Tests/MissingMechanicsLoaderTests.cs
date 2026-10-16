using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-017: formato JSON e validação de target, bonusVsTags, tags, charges, consumables, controlDurationMultiplier e armTime.</summary>
public class MissingMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    [Fact]
    public void New_fields_load_with_expected_values_and_defaults()
    {
        var r = Load("""
            {
              "classes": [ { "id": "class.hero", "name": "Hero", "health": 100, "speed": 4, "radius": 0.4,
                "resource": { "type": "mana", "max": 100 }, "basicAttack": "hero.hit",
                "abilities": [ "hero.chain" ], "consumables": [ "item.potion" ] } ],
              "abilities": [
                { "id": "hero.hit", "name": "Hit", "targeting": { "type": "self" } },
                { "id": "hero.chain", "name": "Chain", "targeting": { "type": "unit", "range": 5 },
                  "effects": [
                    { "type": "damage", "school": "holy", "base": 10, "bonusVsTags": { "undead": 0.5 } },
                    { "type": "heal", "base": 5, "target": "self" },
                    { "type": "remove_status", "tags": [ "poison", "curse" ] } ] },
                { "id": "item.potion", "name": "Potion", "cooldown": 20, "charges": 3, "targeting": { "type": "self" },
                  "effects": [ { "type": "heal", "base": 60 } ] } ],
              "statuses": [ { "id": "status.steady", "name": "Steady", "defaultDuration": 5,
                "modifiers": { "controlDurationMultiplier": 0.5 } },
                { "id": "status.plain", "name": "Plain", "defaultDuration": 5 } ],
              "areas": [ { "id": "area.trap", "radius": 1, "duration": 30, "armTime": 0.5, "mode": "on_enter_once" } ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        Assert.Equal(["item.potion"], db.Classes["class.hero"].Consumables);
        var fx = db.Abilities["hero.chain"].Effects;
        var dmg = Assert.IsType<DamageEffect>(fx[0]);
        Assert.Equal(0.5, dmg.BonusVsTags["undead"]);
        Assert.Equal(EffectTarget.Targets, dmg.Target);
        Assert.Equal(EffectTarget.Self, fx[1].Target);
        Assert.Equal(["poison", "curse"], Assert.IsType<RemoveStatusEffect>(fx[2]).Tags);
        Assert.Equal(3, db.Abilities["item.potion"].Charges);
        Assert.Null(db.Abilities["hero.chain"].Charges);
        Assert.Equal(400, db.Abilities["item.potion"].CooldownTicks);
        Assert.Equal(0.5, db.Statuses["status.steady"].Modifiers.ControlDurationMultiplier);
        Assert.Equal(1, db.Statuses["status.plain"].Modifiers.ControlDurationMultiplier);
        Assert.Equal(10, db.Areas["area.trap"].ArmTimeTicks);
    }

    [Fact]
    public void Unknown_consumable_reference_is_an_error()
    {
        var r = Load("""
            { "classes": [ { "id": "class.hero", "name": "Hero", "health": 100, "speed": 4, "radius": 0.4,
                "resource": { "type": "mana", "max": 100 }, "basicAttack": "hero.hit", "consumables": [ "item.nope" ] } ],
              "abilities": [ { "id": "hero.hit", "name": "Hit", "targeting": { "type": "self" } } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains("item.nope", Errors(r));
    }

    [Theory]
    [InlineData("\"charges\": 0")]
    [InlineData("\"charges\": -1")]
    public void Charges_must_be_positive(string field)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "item.potion", "name": "P", "targeting": { "type": "self" }, {{field}} } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains("charges", Errors(r));
    }

    [Fact]
    public void Invalid_target_value_and_empty_remove_status_are_errors()
    {
        var badTarget = Load("""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "self" },
                "effects": [ { "type": "heal", "base": 1, "target": "everyone" } ] } ] }
            """);
        Assert.False(badTarget.Success);

        var noTag = Load("""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "self" },
                "effects": [ { "type": "remove_status" } ] } ] }
            """);
        Assert.False(noTag.Success);
        Assert.Contains("tag", Errors(noTag));
    }

    [Fact]
    public void Bonus_vs_tags_must_be_greater_than_minus_one()
    {
        var r = Load("""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "self" },
                "effects": [ { "type": "damage", "base": 1, "bonusVsTags": { "undead": -1 } } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains("bonusVsTags", Errors(r));
    }

    [Fact]
    public void Negative_control_multiplier_and_arm_time_not_below_duration_are_errors()
    {
        var status = Load("""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 5, "modifiers": { "controlDurationMultiplier": -0.1 } } ] }
            """);
        Assert.False(status.Success);
        Assert.Contains("controlDurationMultiplier", Errors(status));

        var area = Load("""
            { "areas": [ { "id": "area.x", "radius": 1, "duration": 1, "armTime": 1, "mode": "on_enter_once" } ] }
            """);
        Assert.False(area.Success);
        Assert.Contains("armTime", Errors(area));
    }
}
