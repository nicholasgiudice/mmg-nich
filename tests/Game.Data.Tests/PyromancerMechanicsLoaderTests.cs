using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-441: formato JSON e validação do efeito <c>if_status</c>, de <c>exclusive</c> no status e de <c>explodeRadius</c> na mira
/// em linha.
/// </summary>
public class PyromancerMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    [Fact]
    public void Pyromancer_fields_load()
    {
        var r = Load("""
            {
              "abilities": [
                { "id": "pyromancer.combustion", "name": "Combustão", "targeting": { "type": "circle_self", "radius": 8 },
                  "effects": [
                    { "type": "if_status", "tags": ["burn"], "consume": true,
                      "effects": [ { "type": "damage", "school": "fire", "base": 20, "scaling": { "int": 1.5 } } ] } ],
                  "ranks": [
                    { "rank": 2, "effects": [
                      { "type": "if_status", "tags": ["burn"],
                        "effects": [ { "type": "damage", "school": "fire", "base": 36, "scaling": { "int": 2.7 } },
                                     { "type": "copy_status", "tags": ["burn"], "radius": 2, "count": 0 } ] } ] } ] },
                { "id": "pyromancer.fireball", "name": "Bola de Fogo", "tags": ["ranged"],
                  "targeting": { "type": "line", "length": 10, "width": 1, "explodeRadius": 2 },
                  "effects": [ { "type": "damage", "school": "fire", "base": 38, "scaling": { "int": 2.8 } },
                               { "type": "apply_status", "status": "status.burn_1" } ],
                  "ranks": [ { "rank": 2, "targeting": { "type": "line", "length": 10, "width": 1, "explodeRadius": 2.5 } } ] }
              ],
              "statuses": [
                { "id": "status.burn_1", "name": "Queimadura", "tags": ["debuff", "burn", "dot"], "defaultDuration": 4,
                  "exclusive": { "group": "pyromancer_burn", "tier": 1 },
                  "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "school": "fire", "base": 3, "scaling": { "int": 0.3 } } ] } ] }
              ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        var combustion = db.Abilities["pyromancer.combustion"];
        var nh1 = Assert.IsType<IfStatusEffect>(Assert.Single(combustion.Effects));
        Assert.Equal(["burn"], nh1.Tags);
        Assert.True(nh1.Consume);
        Assert.IsType<DamageEffect>(Assert.Single(nh1.Effects));
        var nh5 = Assert.IsType<IfStatusEffect>(Assert.Single(combustion.Ranks[0].Effects!));
        Assert.False(nh5.Consume);
        Assert.IsType<CopyStatusEffect>(nh5.Effects[1]);

        Assert.Equal(2, Assert.IsType<LineTargeting>(db.Abilities["pyromancer.fireball"].Targeting).ExplodeRadius);
        Assert.Equal(2.5, Assert.IsType<LineTargeting>(db.Abilities["pyromancer.fireball"].Ranks[0].Targeting).ExplodeRadius);

        var burn = db.Statuses["status.burn_1"].Exclusive!;
        Assert.Equal(("pyromancer_burn", 1), (burn.Group, burn.Tier));
    }

    [Fact]
    public void Defaults_without_the_fields()
    {
        var r = Load("""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "line", "length": 10, "width": 1 },
                               "effects": [ { "type": "if_status", "tags": ["burn"], "effects": [ { "type": "damage", "base": 1 } ] } ] } ],
              "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4 } ] }
            """);

        Assert.True(r.Success, Errors(r));
        Assert.Null(Assert.IsType<LineTargeting>(r.Database!.Abilities["a.x"].Targeting).ExplodeRadius);
        Assert.False(Assert.IsType<IfStatusEffect>(r.Database.Abilities["a.x"].Effects[0]).Consume);
        Assert.Null(r.Database.Statuses["status.x"].Exclusive);
    }

    [Theory]
    [InlineData("""{ "type": "if_status", "tags": [], "effects": [ { "type": "damage", "base": 1 } ] }""", "tags")]
    [InlineData("""{ "type": "if_status", "tags": [" "], "effects": [ { "type": "damage", "base": 1 } ] }""", "tags")]
    [InlineData("""{ "type": "if_status", "tags": ["burn"], "effects": [] }""", "effects")]
    [InlineData("""{ "type": "if_status", "tags": ["burn"], "effects": [ { "type": "apply_status", "status": "status.missing" } ] }""", "effects[0].status")]
    public void Invalid_effect_fields_are_reported(string effect, string field)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "circle_self", "radius": 8 }, "effects": [ {{effect}} ] } ],
              "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4 } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Explode_radius_must_be_positive(double radius)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "line", "length": 10, "width": 1, "explodeRadius": {{radius}} },
                               "effects": [ { "type": "damage", "base": 1 } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains("explodeRadius", Errors(r));
    }

    [Theory]
    [InlineData("""{ "exclusive": { "group": "", "tier": 1 } }""", "exclusive.group")]
    [InlineData("""{ "exclusive": { "group": "burn", "tier": 0 } }""", "exclusive.tier")]
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
