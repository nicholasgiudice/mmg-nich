using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Rest;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-214: descanso na Cidade.</summary>
public class RestTests
{
    [Fact]
    public void Resting_restores_a_fraction_of_max_health_and_mana_per_second_only_while_alive()
    {
        var world = WorldBuilder.CreateDefault(1, content: RealContent.Value);
        var hero = EntityFactory.SpawnPlayer(world, RealContent.Value.Classes["class.mage"], new Vector2(5, 5));
        var other = EntityFactory.SpawnPlayer(world, RealContent.Value.Classes["class.mage"], new Vector2(8, 5));
        hero.Add(new Resting(0.1));
        var health = hero.Get<Health>();
        var mana = hero.Get<ResourcePool>();
        health.Current = health.Max / 2;
        mana.Current = 0;
        other.Get<Health>().Current = 10;

        for (var i = 0; i < World.TickRate; i++)
        {
            world.Step();
        }

        Assert.Equal(health.Max * 0.6f, health.Current, 0.5f);
        Assert.True(mana.Current >= mana.Max * 0.1f - 0.5f);
        Assert.Equal(10, other.Get<Health>().Current); // sem descanso, sem regenerar vida

        for (var i = 0; i < 10 * World.TickRate; i++)
        {
            world.Step();
        }

        Assert.Equal(health.Max, health.Current);

        world.Kill(hero);
        health.Current = 0;
        world.Step();
        Assert.Equal(0, health.Current);
    }
}
