using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-432: formato JSON e validação de <c>damage.movingBonus</c>, <c>apply_status.stacks</c>, do efeito <c>copy_status</c> e,
/// no status, de <c>hitEffects</c>, <c>byRank</c> e <c>jumpOnExpire</c>.
/// </summary>
public class HunterMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private const string Venoms = """
        { "id": "status.hunter_venom_1", "name": "Veneno do Caçador", "tags": ["poison", "hunter_venom"], "defaultDuration": 4,
          "stacking": "stack", "maxStacks": 3,
          "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "base": 1, "scaling": { "dex": 0.12 }, "school": "nature" } ] } ] },
        { "id": "status.hunter_venom_5", "name": "Veneno do Caçador", "tags": ["poison", "hunter_venom"], "defaultDuration": 6,
          "stacking": "stack", "maxStacks": 5,
          "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "base": 1.8, "scaling": { "dex": 0.216 }, "school": "nature" } ] } ] }
        """;

    [Fact]
    public void Hunter_fields_load()
    {
        var r = Load($$"""
            {
              "abilities": [
                { "id": "hunter.poisoned_arrow", "name": "Flecha Envenenada", "passive": true, "passiveStatus": "status.poisoned_arrow",
                  "targeting": { "type": "self" } },
                { "id": "hunter.scatter_arrow", "name": "Flecha Dispersa", "tags": ["ranged"],
                  "targeting": { "type": "unit", "range": 9 },
                  "effects": [
                    { "type": "damage", "base": 8, "scaling": { "weapon": 0.6, "dex": 1.2 } },
                    { "type": "copy_status", "tags": ["hunter_venom", "bleed"], "radius": 4, "count": 2 } ] },
                { "id": "hunter.plague", "name": "Praga", "targeting": { "type": "circle_at_point", "range": 9, "radius": 6 },
                  "effects": [
                    { "type": "apply_status", "status": "status.hunter_venom", "stacks": 3 },
                    { "type": "apply_status", "status": "status.plague_1", "target": "self" } ] }
              ],
              "statuses": [
                {{Venoms}},
                { "id": "status.hunter_venom", "name": "Veneno do Caçador", "defaultDuration": 4,
                  "byRank": { "ability": "hunter.poisoned_arrow",
                              "statuses": ["status.hunter_venom_1", "status.hunter_venom_1", "status.hunter_venom_5"] } },
                { "id": "status.poisoned_arrow", "name": "Flecha Envenenada", "defaultDuration": 1,
                  "hitEffects": [ { "type": "apply_status", "status": "status.hunter_venom" } ] },
                { "id": "status.plague_1", "name": "Praga", "defaultDuration": 8,
                  "jumpOnExpire": { "tags": ["hunter_venom"], "radius": 4 } },
                { "id": "status.open_wound_5", "name": "Ferida Aberta", "tags": ["bleed"], "defaultDuration": 8,
                  "periodic": [ { "interval": 1, "effects": [ { "type": "damage", "base": 10.8, "scaling": { "dex": 1.08 }, "movingBonus": 1 } ] } ] }
              ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        var copy = Assert.IsType<CopyStatusEffect>(db.Abilities["hunter.scatter_arrow"].Effects[1]);
        Assert.Equal(["hunter_venom", "bleed"], copy.Tags);
        Assert.Equal((4.0, 2, FactionFilter.Enemy), (copy.Radius, copy.Count, copy.Affects));
        var plague = db.Abilities["hunter.plague"].Effects.Cast<ApplyStatusEffect>().ToList();
        Assert.Equal((3, 1), (plague[0].Stacks, plague[1].Stacks));

        var alias = db.Statuses["status.hunter_venom"];
        Assert.Equal("hunter.poisoned_arrow", alias.ByRank!.Ability);
        Assert.Equal(3, alias.ByRank.Statuses.Count);
        var passive = db.Statuses["status.poisoned_arrow"];
        Assert.Equal("status.hunter_venom", Assert.IsType<ApplyStatusEffect>(Assert.Single(passive.HitEffects)).Status);
        var jump = db.Statuses["status.plague_1"].JumpOnExpire!;
        Assert.Equal(["hunter_venom"], jump.Tags);
        Assert.Equal(4.0, jump.Radius);
        var wound = Assert.IsType<DamageEffect>(Assert.Single(Assert.Single(db.Statuses["status.open_wound_5"].Periodic).Effects));
        Assert.Equal(1, wound.MovingBonus);
    }

    [Fact]
    public void Defaults_without_the_fields()
    {
        var r = Load("""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 5 },
                "effects": [ { "type": "damage", "base": 10 }, { "type": "apply_status", "status": "status.x" },
                             { "type": "copy_status", "tags": ["bleed"], "radius": 2 } ] } ],
              "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4 } ] }
            """);

        Assert.True(r.Success, Errors(r));
        var fx = r.Database!.Abilities["a.x"].Effects;
        Assert.Equal(0, Assert.IsType<DamageEffect>(fx[0]).MovingBonus);
        Assert.Equal(1, Assert.IsType<ApplyStatusEffect>(fx[1]).Stacks);
        var copy = Assert.IsType<CopyStatusEffect>(fx[2]);
        Assert.Equal((1, FactionFilter.Enemy), (copy.Count, copy.Affects));
        var x = r.Database.Statuses["status.x"];
        Assert.Empty(x.HitEffects);
        Assert.Null(x.ByRank);
        Assert.Null(x.JumpOnExpire);
    }

    [Theory]
    [InlineData("""{ "type": "damage", "base": 10, "movingBonus": -0.5 }""", "movingBonus")]
    [InlineData("""{ "type": "apply_status", "status": "status.x", "stacks": 0 }""", "stacks")]
    [InlineData("""{ "type": "copy_status", "tags": [], "radius": 4 }""", "tags")]
    [InlineData("""{ "type": "copy_status", "tags": [" "], "radius": 4 }""", "tags")]
    [InlineData("""{ "type": "copy_status", "tags": ["bleed"], "radius": 0 }""", "radius")]
    [InlineData("""{ "type": "copy_status", "tags": ["bleed"], "radius": 4, "count": -1 }""", "count")]
    public void Invalid_effect_fields_are_reported(string effect, string field)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 5 }, "effects": [ {{effect}} ] } ],
              "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 4 } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Theory]
    [InlineData("""{ "hitEffects": [ { "type": "apply_status", "status": "status.missing" } ] }""", "hitEffects[0].status")]
    [InlineData("""{ "byRank": { "ability": "a.missing", "statuses": ["status.y"] } }""", "byRank.ability")]
    [InlineData("""{ "byRank": { "ability": "a.x", "statuses": [] } }""", "byRank.statuses")]
    [InlineData("""{ "byRank": { "ability": "a.x", "statuses": ["status.missing"] } }""", "byRank.statuses[0]")]
    [InlineData("""{ "byRank": { "ability": "a.x", "statuses": ["status.x"] } }""", "byRank.statuses[0]")]
    [InlineData("""{ "byRank": { "ability": "a.x", "statuses": ["status.alias"] } }""", "byRank.statuses[0]")]
    [InlineData("""{ "jumpOnExpire": { "tags": [], "radius": 4 } }""", "jumpOnExpire.tags")]
    [InlineData("""{ "jumpOnExpire": { "tags": ["hunter_venom"], "radius": 0 } }""", "jumpOnExpire.radius")]
    public void Invalid_status_fields_are_reported(string fields, string field)
    {
        var body = fields.Trim()[1..^1];
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "passive": true, "targeting": { "type": "self" } } ],
              "statuses": [
                { "id": "status.x", "name": "X", "defaultDuration": 4, {{body}} },
                { "id": "status.y", "name": "Y", "defaultDuration": 4 },
                { "id": "status.alias", "name": "Alias", "defaultDuration": 4, "byRank": { "ability": "a.x", "statuses": ["status.y"] } } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }
}
