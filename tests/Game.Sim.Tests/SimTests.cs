using System.Numerics;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Sim.Tests;

public class SimTests
{
    private const string Wolves = "encounter.fixture_knight_wolves";
    private const string Solo = "encounter.fixture_ranger_brute";
    private const string Mage = "encounter.fixture_mage_brutes";
    private const string Group = "encounter.fixture_group";

    private static readonly string FixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    private static readonly Lazy<ContentCatalog> Content = new(() => ContentLoading.Load(FixtureDir));

    private static RunSpec Spec(string encounter, string strategy = "rotation", int runs = 20) =>
        new() { EncounterId = encounter, Strategy = strategy, Runs = runs };

    [Fact]
    public void Same_seed_gives_same_result()
    {
        var spec = Spec(Wolves);
        var a = EncounterRunner.Run(Content.Value, spec, 42);
        var b = EncounterRunner.Run(Content.Value, spec, 42);
        Assert.Equal(a.StateHash, b.StateHash);
        Assert.Equal(a.Ticks, b.Ticks);
        Assert.Equal(a.Outcome, b.Outcome);
        Assert.Equal(a.DamageTaken, b.DamageTaken);
        Assert.Equal(a.AbilityUses, b.AbilityUses);
    }

    [Fact]
    public void Different_seeds_vary_starting_positions()
    {
        var spec = Spec(Wolves);
        var hashes = Enumerable.Range(1, 8).Select(s => EncounterRunner.Run(Content.Value, spec, (ulong)s).StateHash).Distinct();
        Assert.True(hashes.Count() > 1);
    }

    [Fact]
    public void Parallel_and_sequential_batches_are_identical()
    {
        var spec = Spec(Group, runs: 30);
        var seq = Batch.Run(Content.Value, spec, parallelism: 1, keepPerRun: true);
        var par = Batch.Run(Content.Value, spec, parallelism: 4, keepPerRun: true);
        Assert.Equal(seq.PerRun!.Select(r => r.StateHash), par.PerRun!.Select(r => r.StateHash));
        Assert.Equal(seq.WinRate, par.WinRate);
        Assert.Equal(seq.DamageTaken, par.DamageTaken);
    }

    [Fact]
    public void Thousand_solo_runs_finish_within_a_minute()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var summary = Batch.Run(Content.Value, Spec(Solo, runs: 1000));
        sw.Stop();
        Assert.Equal(1000, summary.Runs);
        Assert.True(sw.Elapsed < TimeSpan.FromMinutes(1), $"levou {sw.Elapsed}");
    }

    [Fact]
    public void Summary_has_win_rate_and_percentiles()
    {
        var s = Batch.Run(Content.Value, Spec(Wolves, runs: 20));
        Assert.Equal(20, s.Wins + s.Losses + s.Timeouts);
        Assert.Equal(1.0, s.WinRate);
        Assert.True(s.TimeToWinP50 > 0 && s.TimeToWinP90 >= s.TimeToWinP50);
        Assert.Equal(3.0, s.EnemyDeaths);
        Assert.True(s.DamageTaken > 0 && s.DamageDealt > 0);
    }

    [Fact]
    public void Percentile_uses_nearest_rank()
    {
        double[] v = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        Assert.Equal(5, Batch.Percentile(v, 0.5));
        Assert.Equal(9, Batch.Percentile(v, 0.9));
        Assert.Equal(0, Batch.Percentile([], 0.5));
    }

    [Fact]
    public void Taunt_and_basic_uses_only_taunt_and_basic()
    {
        var s = Batch.Run(Content.Value, Spec(Wolves, "taunt_and_basic"));
        Assert.Equal(["fixture_knight.slash", "fixture_knight.taunt"], s.AbilityUses.Keys.Order());
    }

    [Fact]
    public void Taunt_selector_can_be_overridden_by_option()
    {
        var spec = Spec(Wolves, "taunt_and_basic") with
        {
            Options = new BotOptions(new Dictionary<string, string> { ["class.fixture_knight:taunt"] = "fixture_knight.guard" }),
        };
        var s = Batch.Run(Content.Value, spec);
        Assert.Contains("fixture_knight.guard", s.AbilityUses.Keys);
        Assert.DoesNotContain("fixture_knight.taunt", s.AbilityUses.Keys);
    }

    [Fact]
    public void Explosion_spam_uses_the_area_ability_and_basic()
    {
        var s = Batch.Run(Content.Value, Spec(Mage, "explosion_spam"));
        Assert.Contains("fixture_mage.explosion", s.AbilityUses.Keys);
        Assert.All(s.AbilityUses.Keys, k => Assert.StartsWith("fixture_mage.", k));
    }

    [Fact]
    public void Heal_only_heals_and_never_attacks()
    {
        var spec = Spec(Group) with
        {
            Strategy = "rotation",
            Roles = new Dictionary<string, string> { ["class.fixture_healer"] = "heal_only" },
        };
        var s = Batch.Run(Content.Value, spec);
        Assert.True(s.Healing > 0);
        Assert.DoesNotContain("fixture_healer.smite", s.AbilityUses.Keys);
        Assert.Contains("fixture_healer.mend", s.AbilityUses.Keys);
    }

    [Fact]
    public void Idle_does_not_move_and_kite_beats_rotation_for_the_mage()
    {
        var idle = Batch.Run(Content.Value, Spec(Solo, "idle"));
        Assert.Equal(0, idle.DistanceMoved);

        var rotation = Batch.Run(Content.Value, Spec(Mage, "rotation"));
        var kite = Batch.Run(Content.Value, Spec(Mage, "kite"));
        Assert.True(kite.WinRate > rotation.WinRate);
        Assert.True(kite.DistanceMoved > rotation.DistanceMoved);
    }

    [Fact]
    public void Unknown_strategy_throws_with_available_names()
    {
        var ex = Assert.Throws<ArgumentException>(() => BotStrategies.Create("nope"));
        Assert.Contains("rotation", ex.Message);
    }

    private sealed class FixedHazard(Vector2 center, float radius) : IHazardSource
    {
        public void Collect(World world, List<Hazard> into) => into.Add(new CircleHazard(center, radius, world.Tick + 20));
    }

    [Fact]
    public void Dodge_leaves_a_hazard_but_plain_strategy_stays()
    {
        var content = Content.Value;
        var setup = EncounterSetup.Create(1, content, content.Encounters[Solo]);
        var player = setup.Players[0];
        var center = player.Get<Transform>().Position;
        var hazards = new FixedHazard(center, 2.5f);

        var director = new BotDirector(setup.World, hazards);
        director.Add(player, BotStrategies.Create("idle+dodge"));
        for (var i = 0; i < 40; i++)
        {
            director.Tick();
            setup.World.Step();
        }

        Assert.True(Vector2.Distance(player.Get<Transform>().Position, center) > 2.5f);

        var plain = EncounterSetup.Create(1, content, content.Encounters[Solo]);
        var plainDirector = new BotDirector(plain.World, hazards);
        plainDirector.Add(plain.Players[0], BotStrategies.Create("idle"));
        for (var i = 0; i < 40; i++)
        {
            plainDirector.Tick();
            plain.World.Step();
        }

        Assert.Equal(center, plain.Players[0].Get<Transform>().Position);
    }

    [Fact]
    public void Hazard_shapes_contain_points()
    {
        var rect = new RectHazard(Vector2.Zero, new Vector2(1, 0), 6, 2, 0);
        Assert.True(rect.Contains(new Vector2(3, 0.5f), 0));
        Assert.False(rect.Contains(new Vector2(3, 2f), 0));
        var cone = new ConeHazard(Vector2.Zero, new Vector2(1, 0), MathF.PI / 2, 5, 0);
        Assert.True(cone.Contains(new Vector2(3, 1), 0));
        Assert.False(cone.Contains(new Vector2(-1, 0), 0));
        Assert.False(cone.Contains(new Vector2(6, 0), 0));
    }

    [Fact]
    public void Cli_writes_json_and_rejects_bad_input()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sim-{Guid.NewGuid():N}.json");
        try
        {
            var o = new StringWriter();
            var e = new StringWriter();
            var code = Cli.Run(["--data", FixtureDir, "--encounter", Solo, "--strategy", "rotation,kite", "--runs", "5", "--out", path], o, e);
            Assert.Equal(0, code);
            Assert.Contains("win rate", o.ToString());
            using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(2, json.RootElement.GetProperty("results").GetArrayLength());
        }
        finally
        {
            File.Delete(path);
        }

        Assert.Equal(2, Cli.Run(["--data", FixtureDir, "--strategy", "nope", "--runs", "1"], new StringWriter(), new StringWriter()));
        Assert.Equal(2, Cli.Run(["--data", FixtureDir, "--encounter", "encounter.x"], new StringWriter(), new StringWriter()));
        Assert.Equal(2, Cli.Run(["--data", "/does/not/exist"], new StringWriter(), new StringWriter()));
    }
}
