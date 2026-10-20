using Game.Core.Combat;
using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-060: formato JSON e validação dos níveis de habilidade (ranks), acertos e cargas por recarga.</summary>
public class AbilityRankLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private static string Ability(string ranks, string extra = "") => $$"""
        { "abilities": [ { "id": "hero.shot", "name": "Shot", "cooldown": 7, {{extra}}
            "targeting": { "type": "unit", "range": 9 },
            "effects": [ { "type": "damage", "base": 40 } ],
            "ranks": [ {{ranks}} ] } ] }
        """;

    [Fact]
    public void Ranks_hits_and_charges_load_with_seconds_converted_to_ticks()
    {
        var r = Load(Ability("""
            { "rank": 2, "effects": [ { "type": "damage", "base": 48 } ] },
            { "rank": 3, "effects": [ { "type": "damage", "base": 34 } ], "hits": { "count": 2, "interval": 0.15 } },
            { "rank": 4, "cooldown": 6, "cooldownCharges": 2 }
            """));

        Assert.True(r.Success, Errors(r));
        var shot = r.Database!.Abilities["hero.shot"];
        Assert.Equal(4, shot.HighestRank);
        Assert.Equal(3, shot.Ranks[1].Hits!.IntervalTicks);

        var r4 = AbilityRanks.Resolve(shot, 4);
        Assert.Equal(34, Assert.IsType<DamageEffect>(r4.Effects[0]).Base); // herdado do NH3
        Assert.Equal(2, r4.HitCount);
        Assert.Equal(120, r4.CooldownTicks);
        Assert.Equal(2, r4.CooldownCharges);
        Assert.Equal(140, AbilityRanks.Resolve(shot, 3).CooldownTicks); // NH3 ainda com a recarga base
    }

    [Fact]
    public void Ranks_must_start_at_two_and_follow_in_order()
    {
        var r = Load(Ability("""{ "rank": 3, "effects": [ { "type": "damage", "base": 48 } ] }"""));
        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path.Contains("ranks[0]") && e.Message.Contains("Esperado 2"));
    }

    [Fact]
    public void Signature_allows_at_most_three_ranks_and_normal_skills_five()
    {
        var four = """{ "rank": 2 }, { "rank": 3 }, { "rank": 4 }""";
        var signature = Load(Ability(four, "\"signature\": true,"));
        Assert.False(signature.Success);
        Assert.Contains(signature.Errors, e => e.Message.Contains("máximo é 3"));

        Assert.True(Load(Ability(four)).Success);
        var six = Load(Ability("""{ "rank": 2 }, { "rank": 3 }, { "rank": 4 }, { "rank": 5 }, { "rank": 6 }"""));
        Assert.Contains(six.Errors, e => e.Message.Contains("máximo é 5"));
    }

    [Fact]
    public void Invalid_hits_charges_and_rank_effects_are_reported()
    {
        var r = Load(Ability("""
            { "rank": 2, "hits": { "count": 0 }, "cooldownCharges": 0, "effects": [ { "type": "damage", "base": -1 } ] }
            """));

        Assert.False(r.Success);
        var text = Errors(r);
        Assert.Contains("hits.count", text);
        Assert.Contains("cooldownCharges", text);
        Assert.Contains("ranks[0].effects[0]", text);
    }

    [Fact]
    public void Encounter_ranks_must_reference_existing_abilities_within_their_ranks()
    {
        var r = Load("""
            {
              "classes": [ { "id": "class.hero", "name": "Hero", "health": 100, "speed": 4, "radius": 0.4,
                "resource": { "type": "mana", "max": 100 }, "basicAttack": "hero.shot" } ],
              "abilities": [ { "id": "hero.shot", "name": "Shot", "targeting": { "type": "self" }, "ranks": [ { "rank": 2 } ] } ],
              "monsters": [ { "id": "monster.x", "name": "X", "health": 10, "speed": 1, "radius": 0.4, "ai": { "id": "ai.passive" } } ],
              "arenas": [ { "id": "arena.a", "lines": [ "....", "....", "...." ] } ],
              "encounters": [ { "id": "encounter.e", "name": "E", "arena": "arena.a",
                "players": [ { "class": "class.hero", "position": { "x": 0.5, "y": 0.5 }, "ranks": { "hero.shot": 3, "hero.nope": 1 } } ],
                "enemies": [ { "monster": "monster.x", "position": { "x": 2.5, "y": 1.5 } } ] } ]
            }
            """);

        Assert.False(r.Success);
        var text = Errors(r);
        Assert.Contains("NH 3 fora de 1..2", text);
        Assert.Contains("hero.nope", text);
    }
}
