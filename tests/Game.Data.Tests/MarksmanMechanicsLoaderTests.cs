using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-431: formato JSON e validação de <c>damage.behindFraction</c>, <c>damage.ignoreArmor</c>, <c>crit</c> e
/// <c>rangeBonus</c> no status.
/// </summary>
public class MarksmanMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private static string N(double v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Marksman_fields_load()
    {
        var r = Load("""
            {
              "abilities": [
                { "id": "marksman.piercing_arrow", "name": "Flecha Perfurante", "tags": ["ranged"],
                  "targeting": { "type": "line", "length": 10, "width": 1 },
                  "effects": [ { "type": "damage", "base": 14, "scaling": { "weapon": 1.4, "dex": 2.8 }, "behindFraction": 0.6 } ] },
                { "id": "marksman.sure_shot", "name": "Tiro Certeiro", "tags": ["ranged"],
                  "targeting": { "type": "unit", "range": 12 },
                  "effects": [ { "type": "damage", "base": 25, "scaling": { "weapon": 2, "dex": 4 }, "ignoreArmor": 0.3 } ] }
              ],
              "statuses": [
                { "id": "status.steady_aim_5", "name": "Mira Firme", "defaultDuration": 1,
                  "crit": { "bonus": 0.12, "minDistance": 6, "multiplier": 1.65 }, "rangeBonus": 1 },
                { "id": "status.hawk_eye_3", "name": "Olho de Falcão", "defaultDuration": 20,
                  "crit": { "guaranteed": true }, "rangeBonus": 2 }
              ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        var pierce = Assert.IsType<DamageEffect>(Assert.Single(db.Abilities["marksman.piercing_arrow"].Effects));
        Assert.Equal(0.6, pierce.BehindFraction);
        Assert.Equal(0, pierce.IgnoreArmor);
        var sure = Assert.IsType<DamageEffect>(Assert.Single(db.Abilities["marksman.sure_shot"].Effects));
        Assert.Equal(0.3, sure.IgnoreArmor);
        Assert.Null(sure.BehindFraction);

        var aim = db.Statuses["status.steady_aim_5"];
        Assert.Equal((0.12, 6.0, (double?)1.65, false), (aim.Crit!.Bonus, aim.Crit.MinDistance, aim.Crit.Multiplier, aim.Crit.Guaranteed));
        Assert.Equal(1, aim.RangeBonus);
        var hawk = db.Statuses["status.hawk_eye_3"];
        Assert.True(hawk.Crit!.Guaranteed);
        Assert.Equal((0.0, 0.0), (hawk.Crit.Bonus, hawk.Crit.MinDistance));
        Assert.Null(hawk.Crit.Multiplier);
        Assert.Equal(2, hawk.RangeBonus);
    }

    [Fact]
    public void Statuses_without_the_fields_have_no_crit_and_no_range_bonus()
    {
        var r = Load("""{ "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4 } ] }""");

        Assert.True(r.Success, Errors(r));
        Assert.Null(r.Database!.Statuses["status.x"].Crit);
        Assert.Equal(0, r.Database.Statuses["status.x"].RangeBonus);
    }

    [Theory]
    [InlineData("behindFraction", 0)]
    [InlineData("behindFraction", 1.5)]
    [InlineData("ignoreArmor", -0.1)]
    [InlineData("ignoreArmor", 1.1)]
    public void Invalid_damage_fields_are_reported(string field, double value)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 5 },
              "effects": [ { "type": "damage", "base": 10, "{{field}}": {{N(value)}} } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Theory]
    [InlineData("""{ "crit": { } }""", "crit")]
    [InlineData("""{ "crit": { "bonus": -0.1 } }""", "crit.bonus")]
    [InlineData("""{ "crit": { "bonus": 1.5 } }""", "crit.bonus")]
    [InlineData("""{ "crit": { "bonus": 0.1, "minDistance": -1 } }""", "crit.minDistance")]
    [InlineData("""{ "crit": { "multiplier": 0.9 } }""", "crit.multiplier")]
    [InlineData("""{ "rangeBonus": -1 }""", "rangeBonus")]
    public void Invalid_status_fields_are_reported(string fields, string field)
    {
        var body = fields.Trim()[1..^1];
        var r = Load($$"""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4, {{body}} } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }
}
