using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-406: formato JSON e validação de <c>redirect</c>, <c>flags.knockbackImmune</c> e <c>damage.threatMultiplier</c>.</summary>
public class GuardianMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    [Fact]
    public void Redirect_knockback_immunity_and_threat_multiplier_load()
    {
        var r = Load("""
            {
              "statuses": [
                { "id": "status.intercepted", "name": "Interceptado", "defaultDuration": 4,
                  "redirect": { "fraction": 0.3, "maxDistance": 10 } },
                { "id": "status.oath", "name": "Juramento", "defaultDuration": 8,
                  "redirect": { "fraction": 0.5, "reduction": 0.4, "maxDistance": 6 } },
                { "id": "status.bastion", "name": "Bastião", "defaultDuration": 8, "flags": { "knockbackImmune": true } }
              ],
              "abilities": [ { "id": "guardian.shield_bash", "name": "Golpe de Escudo", "targeting": { "type": "melee_arc", "range": 1.9, "angle": 60 },
                "effects": [ { "type": "damage", "base": 10, "threatMultiplier": 3 }, { "type": "damage", "base": 1 } ] } ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        Assert.Equal(new RedirectDefinition { Fraction = 0.3, MaxDistance = 10 }, db.Statuses["status.intercepted"].Redirect);
        Assert.Equal(0.4, db.Statuses["status.oath"].Redirect!.Reduction);
        Assert.True(db.Statuses["status.bastion"].Flags.KnockbackImmune);
        Assert.Null(db.Statuses["status.bastion"].Redirect);
        var fx = db.Abilities["guardian.shield_bash"].Effects;
        Assert.Equal(3, Assert.IsType<DamageEffect>(fx[0]).ThreatMultiplier);
        Assert.Null(Assert.IsType<DamageEffect>(fx[1]).ThreatMultiplier);
    }

    [Theory]
    [InlineData("""{ "fraction": 0, "maxDistance": 10 }""", "redirect.fraction")]
    [InlineData("""{ "fraction": 1.2, "maxDistance": 10 }""", "redirect.fraction")]
    [InlineData("""{ "fraction": 0.3, "reduction": 1, "maxDistance": 10 }""", "redirect.reduction")]
    [InlineData("""{ "fraction": 0.3, "reduction": -0.1, "maxDistance": 10 }""", "redirect.reduction")]
    [InlineData("""{ "fraction": 0.3, "maxDistance": 0 }""", "redirect.maxDistance")]
    public void Invalid_redirect_is_reported(string redirect, string field)
    {
        var r = Load($$"""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4, "redirect": {{redirect}} } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Fact]
    public void Redirect_without_max_distance_and_negative_threat_multiplier_are_errors()
    {
        var missing = Load("""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4, "redirect": { "fraction": 0.3 } } ] }
            """);
        Assert.False(missing.Success);
        Assert.Contains("maxDistance", Errors(missing));

        var negative = Load("""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 2 },
              "effects": [ { "type": "damage", "base": 1, "threatMultiplier": -1 } ] } ] }
            """);
        Assert.False(negative.Success);
        Assert.Contains("threatMultiplier", Errors(negative));
    }
}
