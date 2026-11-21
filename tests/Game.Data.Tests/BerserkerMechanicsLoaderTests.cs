using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-407: formato JSON e validação de <c>damage.lowHealthBonus</c>, <c>damage.resetCooldownOnKill</c>, <c>lifesteal</c> e
/// <c>area.followOwner</c>.
/// </summary>
public class BerserkerMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    [Fact]
    public void Low_health_bonus_reset_lifesteal_and_follow_owner_load()
    {
        var r = Load("""
            {
              "statuses": [
                { "id": "status.bloodthirst", "name": "Sede de Sangue", "defaultDuration": 10,
                  "lifesteal": { "fraction": 0.1, "belowHealth": 0.3, "killHeal": 0.05, "ability": "berserker.bloodthirst" } },
                { "id": "status.seal", "name": "Selo", "defaultDuration": 10, "lifesteal": { "fraction": 0.2, "belowHealthMultiplier": 3 } }
              ],
              "areas": [ { "id": "area.steel_storm", "radius": 3, "duration": 4, "interval": 0.5, "followOwner": true,
                "effects": [ { "type": "damage", "base": 6 } ] } ],
              "abilities": [
                { "id": "berserker.bloodthirst", "name": "Sede de Sangue", "targeting": { "type": "self" } },
                { "id": "berserker.execute", "name": "Execução", "targeting": { "type": "melee_arc", "range": 1.9, "angle": 90 },
                  "effects": [ { "type": "damage", "base": 20, "lowHealthBonus": { "belowHealth": 0.35, "bonus": 0.75 }, "resetCooldownOnKill": true },
                               { "type": "damage", "base": 1 } ] }
              ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        Assert.Equal(
            new LifestealDefinition { Fraction = 0.1, BelowHealth = 0.3, KillHeal = 0.05, Ability = "berserker.bloodthirst" },
            db.Statuses["status.bloodthirst"].Lifesteal);
        Assert.Equal(2, db.Statuses["status.bloodthirst"].Lifesteal!.BelowHealthMultiplier);
        var seal = db.Statuses["status.seal"].Lifesteal!;
        Assert.Equal((0.2, 0.0, 3.0, 0.0, (string?)null), (seal.Fraction, seal.BelowHealth, seal.BelowHealthMultiplier, seal.KillHeal, seal.Ability));
        Assert.True(db.Areas["area.steel_storm"].FollowOwner);
        var fx = db.Abilities["berserker.execute"].Effects;
        var exec = Assert.IsType<DamageEffect>(fx[0]);
        Assert.Equal(new LowHealthBonusDefinition { BelowHealth = 0.35, Bonus = 0.75 }, exec.LowHealthBonus);
        Assert.True(exec.ResetCooldownOnKill);
        var plain = Assert.IsType<DamageEffect>(fx[1]);
        Assert.Null(plain.LowHealthBonus);
        Assert.False(plain.ResetCooldownOnKill);
    }

    [Theory]
    [InlineData("""{ "fraction": 0 }""", "lifesteal.fraction")]
    [InlineData("""{ "fraction": 1.5 }""", "lifesteal.fraction")]
    [InlineData("""{ "fraction": 0.1, "belowHealth": 1.2 }""", "lifesteal.belowHealth")]
    [InlineData("""{ "fraction": 0.1, "belowHealthMultiplier": 0.5 }""", "lifesteal.belowHealthMultiplier")]
    [InlineData("""{ "fraction": 0.1, "killHeal": -0.1 }""", "lifesteal.killHeal")]
    [InlineData("""{ "fraction": 0.1, "ability": "berserker.missing" }""", "lifesteal.ability")]
    public void Invalid_lifesteal_is_reported(string lifesteal, string field)
    {
        var r = Load($$"""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4, "lifesteal": {{lifesteal}} } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Theory]
    [InlineData("""{ "belowHealth": 0, "bonus": 0.5 }""", "lowHealthBonus.belowHealth")]
    [InlineData("""{ "belowHealth": 1.1, "bonus": 0.5 }""", "lowHealthBonus.belowHealth")]
    [InlineData("""{ "belowHealth": 0.3, "bonus": 0 }""", "lowHealthBonus.bonus")]
    public void Invalid_low_health_bonus_is_reported(string bonus, string field)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 2 },
              "effects": [ { "type": "damage", "base": 1, "lowHealthBonus": {{bonus}} } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }
}
