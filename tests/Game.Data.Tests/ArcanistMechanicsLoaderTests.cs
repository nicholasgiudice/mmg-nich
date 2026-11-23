using Game.Core.Combat;
using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-442: formato JSON e validação de <c>channel</c> (canalização com pulsos) e do efeito <c>current_target</c>.</summary>
public class ArcanistMechanicsLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    [Fact]
    public void Channel_with_ranks_and_current_target_load()
    {
        var r = Load("""
            {
              "statuses": [
                { "id": "status.void_exposed", "name": "Exposto ao Vazio", "defaultDuration": 6,
                  "modifiers": { "damageTakenMultiplier": 1.15 } },
                { "id": "status.arcane_orbs_3", "name": "Orbes Arcanos", "defaultDuration": 10,
                  "periodic": [ { "interval": 1.5, "effects": [
                    { "type": "current_target", "range": 9, "count": 3,
                      "effects": [ { "type": "damage", "school": "arcane", "base": 2.8, "scaling": { "int": 0.28 } } ] } ] } ] }
              ],
              "abilities": [
                { "id": "arcanist.arcane_ray", "name": "Raio Arcano", "cooldown": 8,
                  "targeting": { "type": "unit", "range": 9 },
                  "effects": [ { "type": "damage", "school": "arcane", "base": 6, "scaling": { "int": 0.5 } } ],
                  "channel": { "pulses": 6, "interval": 0.5, "pulseGrowth": 0.15 },
                  "ranks": [
                    { "rank": 2 },
                    { "rank": 3, "channel": { "pulses": 8, "interval": 0.5, "pulseGrowth": 0.15 } },
                    { "rank": 4 },
                    { "rank": 5, "channel": { "pulses": 8, "interval": 0.5, "pulseGrowth": 0.15, "moveSpeedMultiplier": 0.5 } }
                  ] },
                { "id": "arcanist.void_beam", "name": "Feixe do Vazio", "signature": true,
                  "targeting": { "type": "unit", "range": 9 },
                  "effects": [ { "type": "damage", "school": "arcane", "base": 20, "scaling": { "int": 2 } } ],
                  "channel": { "pulses": 8, "interval": 0.5, "moveSpeedMultiplier": 0.5,
                               "onComplete": [ { "type": "apply_status", "status": "status.void_exposed" } ] } }
              ]
            }
            """);

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        var ray = db.Abilities["arcanist.arcane_ray"];
        var c1 = ray.Channel!;
        Assert.Equal((6, 10, 0.15), (c1.Pulses, c1.IntervalTicks, c1.PulseGrowth));
        Assert.Null(c1.MoveSpeedMultiplier);
        Assert.Empty(c1.OnComplete);
        Assert.Equal(6, AbilityRanks.Resolve(ray, 2).Channel!.Pulses);
        Assert.Equal(8, AbilityRanks.Resolve(ray, 4).Channel!.Pulses);
        Assert.Null(AbilityRanks.Resolve(ray, 4).Channel!.MoveSpeedMultiplier);
        Assert.Equal(0.5, AbilityRanks.Resolve(ray, 5).Channel!.MoveSpeedMultiplier);

        var beam = db.Abilities["arcanist.void_beam"].Channel!;
        Assert.Equal("status.void_exposed", Assert.IsType<ApplyStatusEffect>(Assert.Single(beam.OnComplete)).Status);

        var orbs = Assert.IsType<CurrentTargetEffect>(Assert.Single(db.Statuses["status.arcane_orbs_3"].Periodic[0].Effects));
        Assert.Equal((9.0, 3, FactionFilter.Enemy), (orbs.Range, orbs.Count, orbs.Affects));
        Assert.IsType<DamageEffect>(Assert.Single(orbs.Effects));
    }

    [Theory]
    [InlineData("""{ "pulses": 0, "interval": 0.5 }""", "channel.pulses")]
    [InlineData("""{ "pulses": 41, "interval": 0.5 }""", "channel.pulses")]
    [InlineData("""{ "pulses": 6, "interval": 0 }""", "channel.interval")]
    [InlineData("""{ "pulses": 6, "interval": 0.5, "pulseGrowth": -0.1 }""", "channel.pulseGrowth")]
    [InlineData("""{ "pulses": 6, "interval": 0.5, "moveSpeedMultiplier": 0 }""", "channel.moveSpeedMultiplier")]
    [InlineData("""{ "pulses": 6, "interval": 0.5, "moveSpeedMultiplier": 1.5 }""", "channel.moveSpeedMultiplier")]
    [InlineData("""{ "pulses": 6, "interval": 0.5, "onComplete": [ { "type": "apply_status", "status": "status.missing" } ] }""", "channel.onComplete[0]")]
    public void Invalid_channel_is_reported(string channel, string field)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 9 },
              "effects": [ { "type": "damage", "base": 1 } ], "channel": {{channel}} } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }

    [Theory]
    [InlineData(""" "castTime": 1, """, "castTime")]
    [InlineData(""" "hits": { "count": 2, "interval": 0.5 }, """, "hits")]
    [InlineData(""" "telegraph": { "shape": "circle", "delay": 1, "radius": 2 }, """, "telegraph")]
    public void Channel_does_not_mix_with_cast_time_hits_or_telegraph(string extra, string word)
    {
        var r = Load($$"""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 9 }, {{extra}}
              "effects": [ { "type": "damage", "base": 1 } ], "channel": { "pulses": 6, "interval": 0.5 } } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(word, Errors(r));
        Assert.Contains("channel", Errors(r));
    }

    [Fact]
    public void Cast_time_added_in_a_rank_of_a_channel_is_reported()
    {
        var r = Load("""
            { "abilities": [ { "id": "a.x", "name": "X", "targeting": { "type": "unit", "range": 9 },
              "effects": [ { "type": "damage", "base": 1 } ], "channel": { "pulses": 6, "interval": 0.5 },
              "ranks": [ { "rank": 2, "castTime": 1 } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains("channel (NH2)", Errors(r));
    }

    [Theory]
    [InlineData(""" "range": 0, "effects": [ { "type": "damage", "base": 1 } ] """, "range")]
    [InlineData(""" "range": 9, "count": 0, "effects": [ { "type": "damage", "base": 1 } ] """, "count")]
    [InlineData(""" "range": 9, "effects": [ ] """, "effects")]
    [InlineData(""" "range": 9, "effects": [ { "type": "apply_status", "status": "status.missing" } ] """, "effects[0]")]
    public void Invalid_current_target_is_reported(string body, string field)
    {
        var r = Load($$"""
            { "statuses": [ { "id": "status.x", "name": "X", "defaultDuration": 10,
              "periodic": [ { "interval": 1.5, "effects": [ { "type": "current_target", {{body}} } ] } ] } ] }
            """);

        Assert.False(r.Success);
        Assert.Contains(field, Errors(r));
    }
}
