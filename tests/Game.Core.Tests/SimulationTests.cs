using System.Numerics;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

public class SimulationTests
{
    private sealed record Ping(int N) : GameEvent;

    private sealed class Lambda(Action<World> a) : ISimulationSystem
    {
        public void Update(World world) => a(world);
    }

    private static Entity SpawnUnit(World w, Faction f = Faction.Player, float speed = 4f) =>
        w.Spawn(new Transform(), new Health(100), new FactionTag(f), new Movement(speed), new Collider(0.4f), new ResourcePool("resource.vigor", 100, 100));

    private static ulong RunScenario(ulong seed, int ticks)
    {
        var w = WorldBuilder.CreateDefault(seed);
        var a = SpawnUnit(w);
        var b = SpawnUnit(w, Faction.Monster);
        for (var i = 0; i < ticks; i++)
        {
            var r = w.Random.NextInt(8);
            var dir = new Vector2(MathF.Cos(r * MathF.PI / 4), MathF.Sin(r * MathF.PI / 4));
            w.Enqueue(new MoveIntent(a.Id, dir));
            if (i % 7 == 0)
            {
                w.Enqueue(new StopIntent(b.Id));
            }

            if (i % 3 == 0)
            {
                w.Enqueue(new MoveIntent(b.Id, new Vector2(1, 1)));
            }

            w.Step();
        }

        return w.ComputeStateHash();
    }

    [Fact]
    public void Step_advances_exactly_one_tick()
    {
        var w = WorldBuilder.CreateDefault(1);
        Assert.Equal(20, World.TickRate);
        w.Step();
        w.Step();
        Assert.Equal(2, w.Tick);
    }

    [Fact]
    public void Same_seed_and_intents_give_same_hash_after_1000_ticks()
    {
        Assert.Equal(RunScenario(42, 1000), RunScenario(42, 1000));
    }

    [Fact]
    public void Different_seed_changes_hash()
    {
        Assert.NotEqual(RunScenario(1, 200), RunScenario(2, 200));
    }

    [Fact]
    public void Different_seeds_produce_different_rng_sequences_and_same_seed_repeats()
    {
        var a = new SimRandom(1);
        var b = new SimRandom(2);
        var c = new SimRandom(1);
        var sa = Enumerable.Range(0, 8).Select(_ => a.NextUInt()).ToArray();
        var sb = Enumerable.Range(0, 8).Select(_ => b.NextUInt()).ToArray();
        var sc = Enumerable.Range(0, 8).Select(_ => c.NextUInt()).ToArray();
        Assert.NotEqual(sa, sb);
        Assert.Equal(sa, sc);
    }

    [Fact]
    public void Rng_ranges_are_respected()
    {
        var r = new SimRandom(7);
        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(r.NextInt(5), 0, 4);
            Assert.InRange(r.NextFloat(), 0f, 0.99999994f);
        }
    }

    [Fact]
    public void Entities_iterate_in_id_order_and_ids_are_not_reused()
    {
        var w = WorldBuilder.CreateDefault(1);
        var a = SpawnUnit(w);
        var b = SpawnUnit(w);
        var c = SpawnUnit(w);
        Assert.Equal([a.Id, b.Id, c.Id], w.Entities.Select(e => e.Id).ToArray());
        Assert.True(w.Remove(b.Id));
        var d = SpawnUnit(w);
        Assert.Equal([a.Id, c.Id, d.Id], w.Entities.Select(e => e.Id).ToArray());
        Assert.True(b.IsRemoved);
        Assert.Null(w.Find(b.Id));
        Assert.True(d.Id.Value > c.Id.Value);
    }

    [Fact]
    public void Move_intent_moves_entity_without_collision_and_stop_halts()
    {
        var w = WorldBuilder.CreateDefault(1);
        var a = SpawnUnit(w, speed: 4f);
        w.Enqueue(new MoveIntent(a.Id, new Vector2(1, 0)));
        for (var i = 0; i < 20; i++)
        {
            w.Step();
        }

        Assert.Equal(4f, a.Get<Transform>().Position.X, 3);
        Assert.Equal(new Vector2(1, 0), a.Get<Transform>().Facing);
        w.Enqueue(new StopIntent(a.Id));
        w.Step();
        var x = a.Get<Transform>().Position.X;
        w.Step();
        Assert.Equal(x, a.Get<Transform>().Position.X);
    }

    [Fact]
    public void Move_direction_is_clamped_to_unit_length()
    {
        var w = WorldBuilder.CreateDefault(1);
        var a = SpawnUnit(w);
        w.Enqueue(new MoveIntent(a.Id, new Vector2(10, 0)));
        w.Step();
        Assert.Equal(1f, a.Get<Movement>().Direction.Length(), 4);
    }

    [Fact]
    public void Intent_for_dead_or_missing_entity_is_discarded()
    {
        var w = WorldBuilder.CreateDefault(1);
        var a = SpawnUnit(w);
        w.Kill(a);
        w.Enqueue(new MoveIntent(a.Id, new Vector2(1, 0)));
        w.Enqueue(new UseAbilityIntent(a.Id, "warrior.heavy_strike"));
        w.Enqueue(new MoveIntent(new EntityId(999), new Vector2(1, 0)));
        w.Step();
        Assert.Empty(w.TickIntents);
        Assert.Equal(Vector2.Zero, a.Get<Movement>().Direction);
        Assert.Equal(Vector2.Zero, a.Get<Transform>().Position);
    }

    [Fact]
    public void Valid_ability_intent_is_exposed_to_systems_for_the_tick_only()
    {
        var w = WorldBuilder.CreateDefault(1);
        var a = SpawnUnit(w);
        var seen = new List<Intent>();
        var b = new WorldBuilder(1).AddSystem(100, new Lambda(x => seen.AddRange(x.TickIntents))).Build();
        var u = b.Spawn(new Health(10));
        b.Enqueue(new UseAbilityIntent(u.Id, "x", null, new Vector2(1, 2)));
        b.Step();
        b.Step();
        Assert.Single(seen);
        Assert.NotNull(a);
        Assert.Empty(b.TickIntents);
    }

    [Fact]
    public void Systems_run_in_explicit_order_regardless_of_registration_order()
    {
        var log = new List<string>();
        var w = new WorldBuilder(1)
            .AddSystem(300, new Lambda(_ => log.Add("c")))
            .AddSystem(100, new Lambda(_ => log.Add("a")))
            .AddSystem(200, new Lambda(_ => log.Add("b")))
            .Build();
        w.Step();
        Assert.Equal(["a", "b", "c"], log);
    }

    [Fact]
    public void Duplicate_system_order_is_rejected()
    {
        var b = new WorldBuilder(1).AddSystem(100, new Lambda(_ => { }));
        Assert.Throws<InvalidOperationException>(() => b.AddSystem(100, new Lambda(_ => { })));
    }

    [Fact]
    public void Later_system_reads_events_published_by_earlier_system_in_same_tick()
    {
        var seenLater = new List<int>();
        var seenEarlier = new List<int>();
        var w = new WorldBuilder(1)
            .AddSystem(100, new Lambda(x =>
            {
                seenEarlier.AddRange(x.Events.Of<Ping>().Select(p => p.N));
                x.Events.Publish(new Ping((int)x.Tick));
            }))
            .AddSystem(200, new Lambda(x => seenLater.AddRange(x.Events.Of<Ping>().Select(p => p.N))))
            .Build();
        w.Step();
        w.Step();
        Assert.Equal([1, 2], seenLater);
        Assert.Empty(seenEarlier); // sistema anterior nao ve o que o posterior publicaria / eventos de ticks passados
        var last = Assert.Single(w.LastTickEvents);
        Assert.Equal(2, last.Tick);
    }

    [Fact]
    public void Last_tick_events_contain_spawn_death_and_removal_with_ticks()
    {
        var w = WorldBuilder.CreateDefault(1);
        var a = SpawnUnit(w);
        w.Step();
        var spawned = Assert.IsType<EntitySpawned>(Assert.Single(w.LastTickEvents));
        Assert.Equal(a.Id, spawned.Entity);
        Assert.Equal(1, spawned.Tick);

        a.Get<Health>().Current = 0;
        w.Step();
        var died = Assert.IsType<EntityDied>(Assert.Single(w.LastTickEvents));
        Assert.Equal(2, died.Tick);
        Assert.False(a.IsAlive);

        w.Step();
        Assert.Empty(w.LastTickEvents); // morte publicada uma unica vez

        w.Remove(a.Id);
        w.Step();
        Assert.IsType<EntityRemoved>(Assert.Single(w.LastTickEvents));
    }

    [Fact]
    public void Snapshot_is_a_detached_copy()
    {
        var w = WorldBuilder.CreateDefault(1);
        var a = SpawnUnit(w);
        var snap = w.Snapshot();
        snap.Entities[0].Get<Transform>().Position = new Vector2(9, 9);
        a.Get<Transform>().Position = new Vector2(1, 1);
        Assert.Equal(new Vector2(9, 9), snap.Entities[0].Get<Transform>().Position);
        Assert.Equal(new Vector2(1, 1), w.Snapshot().Entities[0].Get<Transform>().Position);
        Assert.Equal(100, snap.Entities[0].Get<Health>().Max);
        Assert.Equal(Faction.Player, snap.Entities[0].Get<FactionTag>().Faction);
    }

    [Fact]
    public void Hash_changes_when_state_changes()
    {
        var w = WorldBuilder.CreateDefault(1);
        var a = SpawnUnit(w);
        var h1 = w.ComputeStateHash();
        a.Get<ResourcePool>().Current -= 1;
        Assert.NotEqual(h1, w.ComputeStateHash());
    }
}
