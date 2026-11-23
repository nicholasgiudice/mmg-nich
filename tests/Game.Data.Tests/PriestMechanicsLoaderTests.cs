using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-421: formato JSON e validação de <c>stats</c> (atributos e Mana por segundo de um status), <c>onExpire</c>,
/// <c>flags.undying</c> e <c>heal.maxHealthFraction</c>.
/// </summary>
public class PriestMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    [Fact]
    public void Stats_on_expire_undying_and_max_health_heal_load()
    {
        var r = Load("""
            {
              "statuses": [
                { "id": "status.blessing", "name": "Bênção", "defaultDuration": 15,
                  "stats": { "attributes": { "all": { "base": 2, "scaling": { "int": 0.08 } }, "vit": { "base": 1 } } } },
                { "id": "status.inspired", "name": "Inspiração", "defaultDuration": 12,
                  "stats": { "manaRegen": { "base": 1.5, "scaling": { "int": 0.04 } } } },
                { "id": "status.sanctuary", "name": "Santuário", "defaultDuration": 6, "flags": { "undying": true },
                  "onExpire": [ { "type": "heal", "base": 0, "maxHealthFraction": 0.2 } ] }
              ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        var blessing = db.Statuses["status.blessing"].Stats!;
        Assert.Equal(2, blessing.Attributes["all"].Base);
        Assert.Equal(0.08, blessing.Attributes["all"].Scaling["int"]);
        Assert.Equal(1, blessing.Attributes["vit"].Base);
        Assert.Null(blessing.ManaRegen);
        var inspired = db.Statuses["status.inspired"].Stats!;
        Assert.Empty(inspired.Attributes);
        Assert.Equal((1.5, 0.04), (inspired.ManaRegen!.Base, inspired.ManaRegen.Scaling["int"]));
        var sanctuary = db.Statuses["status.sanctuary"];
        Assert.True(sanctuary.Flags.Undying);
        var heal = Assert.IsType<HealEffect>(Assert.Single(sanctuary.OnExpire));
        Assert.Equal(0.2, heal.MaxHealthFraction);
        Assert.False(db.Statuses["status.blessing"].Flags.Undying);
        Assert.Empty(db.Statuses["status.blessing"].OnExpire);
    }

    [Theory]
    [InlineData("""{ }""", "stats")]
    [InlineData("""{ "attributes": { "luck": { "base": 2 } } }""", "stats.attributes.luck")]
    [InlineData("""{ "attributes": { "all": { "base": 2, "scaling": { "int": -1 } } } }""", "scaling.int")]
    [InlineData("""{ "attributes": { "all": { "base": 2, "scaling": { "agi": 1 } } } }""", "scaling.agi")]
    [InlineData("""{ "manaRegen": { "base": 2, "scaling": { "int": -0.1 } } }""", "stats.manaRegen")]
    public void Invalid_stats_are_reported(string stats, string field)
    {
        var r = Load($$"""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4, "stats": {{stats}} } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Fact]
    public void Invalid_on_expire_effect_is_reported()
    {
        var r = Load("""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4,
              "onExpire": [ { "type": "apply_status", "status": "status.missing" } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains("onExpire[0]", Errors(r));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Max_health_fraction_out_of_range_is_reported(double fraction)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "self" },
              "effects": [ { "type": "heal", "base": 0, "maxHealthFraction": {{fraction.ToString(System.Globalization.CultureInfo.InvariantCulture)}} } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains("maxHealthFraction", Errors(r));
    }
}
