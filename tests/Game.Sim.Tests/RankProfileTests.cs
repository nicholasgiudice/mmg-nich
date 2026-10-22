using Game.Core.Combat;
using Game.Core.Definitions;

namespace Game.Sim.Tests;

/// <summary>T-063: NH no simulador (perfis, <c>--rank</c> e aplicação no encontro).</summary>
public class RankProfileTests
{
    private static readonly AbilityDefinition Shot = new()
    {
        Id = "hero.shot",
        Name = "Tiro",
        Targeting = new SelfTargeting(),
        Ranks = [new AbilityRankDefinition { Rank = 2 }, new AbilityRankDefinition { Rank = 3 }, new AbilityRankDefinition { Rank = 4 }, new AbilityRankDefinition { Rank = 5 }],
    };

    private static readonly AbilityDefinition Ult = Shot with { Id = "hero.ult", Signature = true, Ranks = Shot.Ranks.Take(2).ToList() };

    private static readonly ClassDefinition Hero = new()
    {
        Id = "class.archer",
        Name = "Herói",
        Health = 100,
        Speed = 4,
        Radius = 0.4,
        Resource = new ResourceDefinition { Type = "mana", Max = 100 },
        BasicAttack = "hero.basic",
        Abilities = ["archer.power_shot", "archer.hunters_mark", "hero.shot", Ult.Id],
    };

    private static ContentCatalog Content() => new(
        classes: [Hero],
        abilities: [Shot, Ult, Shot with { Id = "archer.power_shot" }, Shot with { Id = "archer.hunters_mark" }]);

    [Fact]
    public void Focused_profile_puts_the_class_pair_at_5_others_at_1_and_the_signature_at_3()
    {
        var ranks = RankProfiles.For(RankProfiles.Focused, Content())["class.archer"];
        Assert.Equal(5, ranks["archer.power_shot"]);
        Assert.Equal(5, ranks["archer.hunters_mark"]);
        Assert.Equal(1, ranks["hero.shot"]);
        Assert.Equal(3, ranks[Ult.Id]);
        Assert.Equal(14, ranks.Values.Sum()); // 5+5+1+3 (a classe real tem 4 normais: 15 pontos)
    }

    [Fact]
    public void Spread_profile_is_3_everywhere_and_nh1_is_empty()
    {
        Assert.All(RankProfiles.For(RankProfiles.Spread, Content())["class.archer"].Values, r => Assert.Equal(3, r));
        Assert.Empty(RankProfiles.For(RankProfiles.Nh1, Content()));
        Assert.Throws<ArgumentException>(() => RankProfiles.For("nope", Content()));
    }

    [Fact]
    public void Rank_pairs_override_the_profile_and_reach_the_encounter_players()
    {
        var ranks = RankProfiles.For(RankProfiles.Spread, Content());
        RankProfiles.AddPair(ranks, "class.archer:hero.shot=5");
        RankProfiles.AddPair(ranks, "class.mage:mage.x=2");
        Assert.Throws<ArgumentException>(() => RankProfiles.AddPair(ranks, "class.archer=5"));

        var spec = new RunSpec { EncounterId = "e", Ranks = ranks, RankLabel = "espalhado" };
        var encounter = new EncounterDefinition
        {
            Id = "e",
            Name = "E",
            Arena = "a",
            Players =
            [
                new PlayerSpawnDefinition { Class = "class.archer", Position = new PositionDefinition(0, 0), Ranks = new Dictionary<string, int> { ["hero.ult"] = 1 } },
                new PlayerSpawnDefinition { Class = "class.cleric", Position = new PositionDefinition(0, 0) },
            ],
            Enemies = [],
        };

        var withRanks = spec.WithRanks(encounter);
        Assert.Equal(5, withRanks.Players[0].Ranks["hero.shot"]);
        Assert.Equal(3, withRanks.Players[0].Ranks["hero.ult"]); // o perfil vence o NH do encontro
        Assert.Empty(withRanks.Players[1].Ranks);
        Assert.EndsWith("NH:espalhado", spec.Label);
    }

    [Fact]
    public void Cli_rejects_unknown_profile_and_malformed_rank()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        var err = new StringWriter();
        Assert.Equal(2, Cli.Run(["--data", dir, "--runs", "1", "--ranks", "nope"], new StringWriter(), err));
        Assert.Contains("nope", err.ToString());
        Assert.Equal(2, Cli.Run(["--data", dir, "--runs", "1", "--rank", "class.x=1"], new StringWriter(), new StringWriter()));
    }
}
