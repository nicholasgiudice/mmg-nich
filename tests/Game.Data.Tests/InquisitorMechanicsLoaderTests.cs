using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-422: formato JSON e validação de <c>damage.selfHeal</c>, <c>damage.allyHeal</c>, <c>attackerLifesteal</c> e
/// <c>basicAttack</c> no status.
/// </summary>
public class InquisitorMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    [Fact]
    public void Self_heal_ally_heal_attacker_lifesteal_and_basic_attack_load()
    {
        var r = Load("""
            {
              "statuses": [
                { "id": "status.seal_of_light_1", "name": "Selo da Luz", "defaultDuration": 8,
                  "attackerLifesteal": { "fraction": 0.1, "ability": "inquisitor.seal_of_light" } },
                { "id": "status.divine_wrath_1", "name": "Ira Divina", "defaultDuration": 10, "basicAttack": "inquisitor.divine_wrath_strike" },
                { "id": "status.plain", "name": "Comum", "defaultDuration": 1 }
              ],
              "abilities": [
                { "id": "inquisitor.seal_of_light", "name": "Selo da Luz", "targeting": { "type": "unit", "range": 6 },
                  "effects": [ { "type": "apply_status", "status": "status.seal_of_light_1" } ] },
                { "id": "inquisitor.divine_wrath", "name": "Ira Divina", "targeting": { "type": "self" } },
                { "id": "inquisitor.divine_wrath_strike", "name": "Golpe Sagrado", "cooldown": 1, "targeting": { "type": "unit", "range": 6 },
                  "effects": [ { "type": "damage", "base": 10, "school": "holy",
                                 "allyHeal": { "fraction": 0.1, "radius": 4, "ability": "inquisitor.divine_wrath" } },
                               { "type": "extra_targets", "radius": 2, "count": 0, "effects": [
                                 { "type": "damage", "base": 10, "allyHeal": { "fraction": 0.15, "radius": 4, "includeSelf": false } } ] } ] },
                { "id": "inquisitor.judgment", "name": "Julgamento", "targeting": { "type": "unit", "range": 5 },
                  "effects": [ { "type": "damage", "base": 36, "selfHeal": 0.2 }, { "type": "damage", "base": 1 } ] }
              ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        Assert.Equal(new LifestealDefinition { Fraction = 0.1, Ability = "inquisitor.seal_of_light" }, db.Statuses["status.seal_of_light_1"].AttackerLifesteal);
        Assert.Null(db.Statuses["status.seal_of_light_1"].Lifesteal);
        Assert.Equal("inquisitor.divine_wrath_strike", db.Statuses["status.divine_wrath_1"].BasicAttack);
        Assert.Null(db.Statuses["status.plain"].BasicAttack);
        Assert.Null(db.Statuses["status.plain"].AttackerLifesteal);

        var strike = db.Abilities["inquisitor.divine_wrath_strike"].Effects;
        Assert.Equal(
            new DamageAllyHealDefinition { Fraction = 0.1, Radius = 4, Ability = "inquisitor.divine_wrath" },
            Assert.IsType<DamageEffect>(strike[0]).AllyHeal);
        Assert.True(Assert.IsType<DamageEffect>(strike[0]).AllyHeal!.IncludeSelf);
        var nested = Assert.IsType<DamageEffect>(Assert.IsType<ExtraTargetsEffect>(strike[1]).Effects[0]).AllyHeal!;
        Assert.Equal((0.15, 4.0, false, (string?)null), (nested.Fraction, nested.Radius, nested.IncludeSelf, nested.Ability));

        var judgment = db.Abilities["inquisitor.judgment"].Effects;
        Assert.Equal(0.2, Assert.IsType<DamageEffect>(judgment[0]).SelfHeal);
        Assert.Equal(0, Assert.IsType<DamageEffect>(judgment[1]).SelfHeal);
        Assert.Null(Assert.IsType<DamageEffect>(judgment[1]).AllyHeal);
    }

    [Theory]
    [InlineData("\"selfHeal\": -0.1", "selfHeal")]
    [InlineData("\"selfHeal\": 1.5", "selfHeal")]
    [InlineData("\"allyHeal\": { \"fraction\": 0, \"radius\": 4 }", "allyHeal.fraction")]
    [InlineData("\"allyHeal\": { \"fraction\": 1.2, \"radius\": 4 }", "allyHeal.fraction")]
    [InlineData("\"allyHeal\": { \"fraction\": 0.1, \"radius\": 0 }", "allyHeal.radius")]
    [InlineData("\"allyHeal\": { \"fraction\": 0.1, \"radius\": 4, \"ability\": \"a.missing\" }", "allyHeal.ability")]
    public void Invalid_damage_heals_are_reported(string field, string error)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 2 },
              "effects": [ { "type": "damage", "base": 1, {{field}} } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(error, Errors(r));
    }

    [Theory]
    [InlineData("""{ "fraction": 0 }""", "attackerLifesteal.fraction")]
    [InlineData("""{ "fraction": 1.5 }""", "attackerLifesteal.fraction")]
    [InlineData("""{ "fraction": 0.1, "belowHealth": 1.2 }""", "attackerLifesteal.belowHealth")]
    [InlineData("""{ "fraction": 0.1, "belowHealthMultiplier": 0.5 }""", "attackerLifesteal.belowHealthMultiplier")]
    [InlineData("""{ "fraction": 0.1, "killHeal": -0.1 }""", "attackerLifesteal.killHeal")]
    [InlineData("""{ "fraction": 0.1, "ability": "inquisitor.missing" }""", "attackerLifesteal.ability")]
    public void Invalid_attacker_lifesteal_is_reported(string lifesteal, string field)
    {
        var r = Load($$"""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4, "attackerLifesteal": {{lifesteal}} } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Fact]
    public void Basic_attack_swap_to_a_missing_ability_is_reported()
    {
        var r = Load("""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4, "basicAttack": "inquisitor.missing" } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains("basicAttack", Errors(r));
        Assert.Contains("inquisitor.missing", Errors(r));
    }

    [Theory]
    [InlineData("""{ "id": "a.swap", "name": "S", "passive": true, "passiveStatus": "status.p", "targeting": { "type": "self" } }""", "é passiva e não pode virar o ataque básico")]
    [InlineData("""{ "id": "a.swap", "name": "S", "targeting": { "type": "self" }, "telegraph": { "shape": "circle", "delay": 1, "radius": 2 } }""", "tem telegráfo, que a troca")]
    public void Basic_attack_swap_to_a_passive_or_telegraphed_ability_is_reported(string ability, string error)
    {
        var r = Load($$"""
            {
              "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4, "basicAttack": "a.swap" },
                            { "id": "status.p", "name": "P", "defaultDuration": 4 } ],
              "abilities": [ {{ability}} ]
            }
            """);

        Assert.False(r.Success);
        Assert.Contains(error, Errors(r));
    }
}
