using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-061: formato JSON e validação dos efeitos novos dos níveis de habilidade.</summary>
public class RankEffectLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private const string Statuses = """
        "statuses": [
          { "id": "status.mark", "name": "Marca", "defaultDuration": 10 },
          { "id": "status.purified", "name": "Purificado", "defaultDuration": 2, "immuneToTags": [ "poison", "curse" ] },
          { "id": "status.focus", "name": "Foco", "defaultDuration": 2, "empower": { "abilities": [ "hero.shot" ], "multiplier": 1.3 } },
          { "id": "status.guard", "name": "Guarda", "defaultDuration": 4,
            "frontal": { "arcDegrees": 180, "damageTakenMultiplier": 0.35, "reflectMeleeFraction": 0.2 } }
        ],
        "areas": [ { "id": "area.fire", "radius": 2, "duration": 4, "interval": 1, "effects": [ { "type": "damage", "base": 7 } ] } ]
        """;

    [Fact]
    public void New_effects_and_status_fields_load()
    {
        var r = Load($$"""
            { {{Statuses}},
              "abilities": [ { "id": "hero.shot", "name": "Tiro", "targeting": { "type": "unit", "range": 9 },
                "hits": { "count": 2, "interval": 0.5 },
                "effects": [
                  { "type": "damage", "base": 20 },
                  { "type": "spawn_area", "area": "area.fire", "onHit": 1 },
                  { "type": "chain", "jumps": 2, "radius": 4, "multiplier": 0.7, "effects": [ { "type": "damage", "base": 20 } ] },
                  { "type": "extra_targets", "radius": 6, "select": "most_injured", "count": 0, "affects": "ally", "multiplier": 0.5,
                    "effects": [ { "type": "apply_status", "status": "status.mark" } ] },
                  { "type": "shield", "amount": 30, "duration": 6, "refund": { "resource": "mana", "fraction": 0.3 } },
                  { "type": "remove_status", "tags": [ "control" ], "count": 0 },
                  { "type": "revive", "radius": 5, "healthFraction": 0.3 }
                ] } ] }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        var fx = db.Abilities["hero.shot"].Effects;
        Assert.Equal(1, Assert.IsType<SpawnAreaEffect>(fx[1]).OnHit);
        var chain = Assert.IsType<ChainEffect>(fx[2]);
        Assert.Equal((2, 4.0, 0.7), (chain.Jumps, chain.Radius, chain.Multiplier));
        var extra = Assert.IsType<ExtraTargetsEffect>(fx[3]);
        Assert.Equal((TargetSelect.MostInjured, 0, FactionFilter.Ally), (extra.Select, extra.Count, extra.Affects));
        Assert.Equal(0.3, Assert.IsType<ShieldEffect>(fx[4]).Refund!.Fraction);
        Assert.Equal(0, Assert.IsType<RemoveStatusEffect>(fx[5]).Count);
        Assert.Equal(0.3, Assert.IsType<ReviveEffect>(fx[6]).HealthFraction);
        Assert.Equal(["poison", "curse"], db.Statuses["status.purified"].ImmuneToTags);
        Assert.Equal(1.3, db.Statuses["status.focus"].Empower!.Multiplier);
        Assert.Equal(0.2, db.Statuses["status.guard"].Frontal!.ReflectMeleeFraction);
    }

    [Fact]
    public void Invalid_new_effects_are_reported()
    {
        var r = Load($$"""
            { {{Statuses.Replace("hero.shot", "hero.nope")}},
              "abilities": [ { "id": "hero.shot", "name": "Tiro", "targeting": { "type": "unit", "range": 9 },
                "effects": [
                  { "type": "damage", "base": 1, "onHit": 0 },
                  { "type": "chain", "jumps": 0, "radius": 4, "effects": [ { "type": "apply_status", "status": "status.nope" } ] },
                  { "type": "extra_targets", "radius": 0, "effects": [] },
                  { "type": "shield", "amount": 30, "duration": 6, "refund": { "resource": "mana", "fraction": 2 } },
                  { "type": "revive", "radius": 5, "healthFraction": 0 }
                ] } ] }
            """);

        Assert.False(r.Success);
        var text = Errors(r);
        Assert.Contains("onHit", text);
        Assert.Contains("jumps", text);
        Assert.Contains("effects[1].effects[0]", text); // efeitos aninhados também são validados
        Assert.Contains("refund.fraction", text);
        Assert.Contains("healthFraction", text);
        Assert.Contains("empower.abilities[0]", text);
    }
}
