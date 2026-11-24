using Game.Core.Combat;
using Game.Core.Combat.Status;

namespace Game.Sim.Tests;

/// <summary>
/// T-502: na arena de teste <c>map.terrain_test</c> (metade oeste de água funda), o mesmo encontro, espelhado, com o Guerreiro
/// contra dois Salteadores (atiradores que recuam): lutar em terra seca rende mais que dentro da água, onde o Guerreiro
/// demora mais para alcançá-los (os dois lados ficam lentos, mas quem leva tiro enquanto corre é ele).
/// </summary>
public class TerrainSimTests
{
    private const string Water = "encounter.terrain_water";
    private const string Dry = "encounter.terrain_dry";

    private static readonly Lazy<ContentCatalog> Content = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        return ContentLoading.Load(Path.Combine(dir.FullName, "data"));
    });

    private static BatchSummary Run(string encounter, string strategy) =>
        Batch.Run(Content.Value, new RunSpec { EncounterId = encounter, Strategy = strategy, Runs = 30 });

    [Fact]
    public void Water_encounter_starts_everyone_in_the_water_and_the_dry_one_outside()
    {
        foreach (var (id, wet) in new[] { (Water, true), (Dry, false) })
        {
            var setup = EncounterSetup.Create(1, Content.Value, Content.Value.Encounters[id]);
            setup.World.Step();
            Assert.All(setup.Players.Concat(setup.Enemies), e => Assert.Equal(wet, StatusManager.Has(setup.World, e, "status.deep_water")));
        }
    }

    [Theory]
    [InlineData("basic_only")]
    [InlineData("rotation")]
    public void Melee_fights_better_on_dry_land(string strategy)
    {
        var water = Run(Water, strategy);
        var dry = Run(Dry, strategy);

        Assert.True(dry.WinRate >= water.WinRate, $"vitórias: seco {dry.WinRate:P0}, água {water.WinRate:P0}");
        Assert.True(dry.DamageTaken < water.DamageTaken * 0.9, $"dano recebido: seco {dry.DamageTaken:0}, água {water.DamageTaken:0}");
        Assert.True(dry.TimeToWinMean < water.TimeToWinMean * 0.85, $"tempo até vencer: seco {dry.TimeToWinMean:0.0} s, água {water.TimeToWinMean:0.0} s");
    }
}
