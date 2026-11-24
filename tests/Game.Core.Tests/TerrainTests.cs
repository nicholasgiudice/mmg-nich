using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Tests;

/// <summary>T-502: terreno que deixa lento (área <c>slow_terrain</c> do mapa, estado <c>status.deep_water</c>).</summary>
public class TerrainTests
{
    private const string DeepWater = "status.deep_water";
    private const float Speed = 4f;
    private const float Step = Speed * World.TickSeconds; // 0,2 tile por tick

    private static TerrainArea Water(double x, double width, string name = "water", params string[] ignore) => new()
    {
        Name = name,
        Area = new RectDefinition(x, 0, width, 6),
        Status = DeepWater,
        IgnoreTags = ignore,
    };

    /// <summary>Mundo 30×6 aberto, com o conteúdo real (estados e regras de controle) e o terreno dado.</summary>
    private static World NewWorld(params TerrainArea[] terrain)
    {
        var real = RealContent.Value;
        var content = new ContentCatalog(statuses: real.Statuses.Values) { Rules = real.Rules };
        return WorldBuilder.CreateDefault(1, new GridMap(30, 6, null, terrain), content);
    }

    private static Entity Unit(World w, float x, MonsterRank rank = MonsterRank.Normal, params string[] tags)
    {
        var e = w.Spawn(
            new Transform { Position = new Vector2(x, 3) },
            new Health(1000),
            new FactionTag(Faction.Monster),
            new Movement(Speed),
            new UnitInfo { DefinitionId = "test", Rank = rank, Tags = tags });
        e.Get<Movement>().Direction = new Vector2(1, 0);
        return e;
    }

    private static float X(Entity e) => e.Get<Transform>().Position.X;

    private static bool InWater(World w, Entity e) => StatusManager.Has(w, e, DeepWater);

    [Fact]
    public void Deep_water_slows_by_40_percent_and_is_not_control()
    {
        var def = RealContent.Value.Statuses[DeepWater];
        Assert.Equal(0.6, def.Modifiers.SpeedMultiplier, 6);
        Assert.Empty(def.Periodic);
        Assert.DoesNotContain(StatusManager.ControlTag, def.Tags);
        Assert.DoesNotContain("slow", def.Tags); // imunidades a lentidão (Inabalável, Passo Livre) e limpezas de lentidão olham "slow"
    }

    [Fact]
    public void Speed_drops_on_the_tick_after_entering_and_returns_on_the_tick_after_leaving()
    {
        var w = NewWorld(Water(5, 5)); // água em x ∈ [5, 10)
        var u = Unit(w, 4.5f);

        // Entra: o tick em que o centro passa de x = 5 já termina com o estado (aplicado no mesmo tick).
        var before = X(u);
        while (X(u) < 5)
        {
            Assert.False(InWater(w, u));
            before = X(u);
            w.Step();
        }

        Assert.Equal(Step, X(u) - before, 4);
        Assert.True(InWater(w, u));
        Assert.Single(w.LastTickEvents.OfType<StatusApplied>(), e => e.Target == u.Id && e.StatusId == DeepWater);

        // Dentro: 60% da velocidade.
        before = X(u);
        w.Step();
        Assert.Equal(Step * 0.6f, X(u) - before, 4);

        // Sai: o tick em que o centro passa de x = 10 termina sem o estado (sem resto de duração).
        while (X(u) < 10)
        {
            Assert.True(InWater(w, u));
            w.Step();
        }

        Assert.False(InWater(w, u));
        Assert.Single(w.LastTickEvents.OfType<StatusRemoved>(), e => e.Entity == u.Id && e.StatusId == DeepWater);
        before = X(u);
        w.Step();
        Assert.Equal(Step, X(u) - before, 4);
    }

    [Fact]
    public void Staying_in_the_water_keeps_the_status_without_refresh_events()
    {
        var w = NewWorld(Water(0, 30));
        var u = Unit(w, 2);
        u.Get<Movement>().Direction = Vector2.Zero;
        w.Step();
        Assert.True(InWater(w, u));

        var duration = RealContent.Value.Statuses[DeepWater].DefaultDurationTicks;
        for (var i = 0; i < duration * 5; i++)
        {
            w.Step();
            Assert.True(InWater(w, u));
            Assert.DoesNotContain(w.LastTickEvents, e => e is StatusApplied or StatusRefreshed or StatusRemoved or StatusExpired);
        }
    }

    [Fact]
    public void Unit_with_an_ignored_tag_does_not_feel_the_water()
    {
        var w = NewWorld(Water(0, 30, "water", "amphibious", "boss"));
        var lizard = Unit(w, 2, tags: ["humanoid", "amphibious"]);
        var wolf = Unit(w, 20, tags: ["beast"]);
        var before = X(lizard);
        w.Step();
        w.Step();

        Assert.False(InWater(w, lizard));
        Assert.True(InWater(w, wolf));
        Assert.Equal(Step * 2, X(lizard) - before, 4);
    }

    [Fact]
    public void Overlapping_areas_do_not_stack()
    {
        var w = NewWorld(Water(5, 5, "a"), Water(8, 5, "b")); // [5, 10) e [8, 13): sobrepostas em [8, 10)
        var u = Unit(w, 4.9f);
        var applied = 0;
        var removed = 0;
        while (X(u) < 13)
        {
            w.Step();
            applied += w.LastTickEvents.OfType<StatusApplied>().Count();
            removed += w.LastTickEvents.OfType<StatusRemoved>().Count();
            if (X(u) is >= 8 and < 10)
            {
                var s = Assert.Single(StatusManager.Active(w, u));
                Assert.Equal(1, s.Stacks);
                Assert.Equal(0.6, w.Combat.SpeedMultiplier(w, u), 6);
            }
        }

        Assert.Equal(1, applied); // passar de uma área para a outra não tira nem põe de novo
        Assert.Equal(1, removed);
        Assert.False(InWater(w, u));
    }

    [Fact]
    public void Entering_many_times_has_no_diminishing_returns_nor_rank_reduction()
    {
        var w = NewWorld(Water(5, 1), Water(7, 1, "w2"), Water(9, 1, "w3"), Water(11, 1, "w4"), Water(13, 1, "w5"));
        var boss = Unit(w, 4.5f, MonsterRank.Boss);
        var entries = 0;
        var resisted = 0;
        while (X(boss) < 14.5f)
        {
            w.Step();
            resisted += w.LastTickEvents.OfType<StatusResisted>().Count();
            if (w.LastTickEvents.OfType<StatusApplied>().Any())
            {
                entries++;
                Assert.Equal(0.6, w.Combat.SpeedMultiplier(w, boss), 6);
            }
        }

        Assert.Equal(5, entries);
        Assert.Equal(0, resisted);
    }

    [Fact]
    public void Purification_and_slow_immunity_do_not_touch_the_water()
    {
        var w = NewWorld(Water(0, 30));
        var u = Unit(w, 2);
        StatusManager.Apply(w, u, u, "status.steadfast"); // imune a stun, root, freeze e slow
        w.Step();
        Assert.True(InWater(w, u));

        Assert.Equal(0, StatusManager.RemoveByTags(w, u, ["poison", "curse"], 0, u.Id)); // Purificação
        Assert.Equal(0, StatusManager.RemoveByTags(w, u, ["stun", "root", "freeze", "slow"], 0, u.Id)); // Inabalável
        Assert.True(InWater(w, u));
    }

    [Fact]
    public void Removed_by_other_means_inside_the_water_comes_back_next_tick()
    {
        var w = NewWorld(Water(0, 30));
        var u = Unit(w, 2);
        u.Get<Movement>().Direction = Vector2.Zero;
        w.Step();
        StatusManager.RemoveAll(w, u, u.Id); // ex.: levantar um caído (T-061)
        Assert.False(InWater(w, u));
        w.Step();
        Assert.True(InWater(w, u));
    }

    [Fact]
    public void Dead_unit_loses_the_status_and_units_without_movement_are_ignored()
    {
        var w = NewWorld(Water(0, 30));
        var u = Unit(w, 2);
        var still = w.Spawn(new Transform { Position = new Vector2(3, 3) }, new Health(10), new UnitInfo());
        w.Step();
        Assert.True(InWater(w, u));
        Assert.False(InWater(w, still));

        w.Kill(u);
        w.Step();
        Assert.False(InWater(w, u));
    }

    [Fact]
    public void Status_is_in_the_replicated_snapshot()
    {
        var w = NewWorld(Water(0, 30));
        var u = Unit(w, 2);
        w.Step();

        var snap = w.Snapshot(SnapshotScope.Replicated).Entities.Single(e => e.Id == u.Id);
        Assert.Contains(snap.Get<StatusEffects>().Active, s => s.StatusId == DeepWater && s.ExpiresAtTick > w.Tick);
    }

    [Fact]
    public void World_without_terrain_does_nothing()
    {
        var w = WorldBuilder.CreateDefault(1, new GridMap(30, 6), new ContentCatalog(statuses: RealContent.Value.Statuses.Values));
        var u = Unit(w, 2);
        w.Step();
        Assert.False(InWater(w, u));
    }

    [Fact]
    public void Arena_slow_terrain_areas_become_the_map_terrain()
    {
        var arena = new ArenaDefinition
        {
            Id = "map.test",
            Lines = ["......", "......"],
            Areas =
            [
                new MapArea
                {
                    Name = "pond",
                    Kind = TerrainArea.SlowTerrainClass,
                    Area = new RectDefinition(1, 0, 2, 2),
                    Properties = new Dictionary<string, string> { ["status"] = DeepWater, ["ignoreTags"] = " amphibious , ,beast" },
                },
                new MapArea { Name = "altar", Kind = "altar", Area = new RectDefinition(4, 0, 1, 1) },
            ],
        };

        var t = Assert.Single(EntityFactory.BuildMap(arena).Terrain);
        Assert.Equal("pond", t.Name);
        Assert.Equal(DeepWater, t.Status);
        Assert.Equal(["amphibious", "beast"], t.IgnoreTags);
        Assert.Empty(EntityFactory.BuildMap(arena with { Areas = [] }).Terrain);
        Assert.True(EntityFactory.BuildMap(arena).IsBlocked(-1, 0)); // o bloqueio continua o mesmo
        Assert.False(EntityFactory.BuildMap(arena).IsBlocked(1, 1));
    }
}
