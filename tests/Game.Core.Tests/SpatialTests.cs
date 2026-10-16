using System.Numerics;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Tests;

public class SpatialTests
{
    private static World Arena(GridMap map) => WorldBuilder.CreateDefault(1, map);

    private static Entity Unit(World w, Vector2 pos, float radius = 0.4f, float speed = 4f) =>
        w.Spawn(new Transform { Position = pos }, new Health(10), new Movement(speed), new Collider(radius));

    private static Entity Dummy(World w, Vector2 pos) => w.Spawn(new Transform { Position = pos }, new Health(10), new Collider(0.3f));

    // ---------- GridMap ----------

    [Fact]
    public void FromAscii_parses_and_out_of_bounds_is_blocked()
    {
        var m = GridMap.FromAscii("#.", "..");
        Assert.Equal(2, m.Width);
        Assert.Equal(2, m.Height);
        Assert.True(m.IsBlocked(0, 0));
        Assert.False(m.IsBlocked(1, 0));
        Assert.True(m.IsBlocked(-1, 0));
        Assert.True(m.IsBlocked(2, 1));
        Assert.Throws<ArgumentException>(() => GridMap.FromAscii("#.", "."));
        Assert.Throws<ArgumentException>(() => GridMap.FromAscii("#x"));
    }

    // ---------- Movimento ----------

    [Fact]
    public void Entity_stops_at_wall_and_slides_along_it()
    {
        var w = Arena(GridMap.FromAscii("#######", "#.....#", "#.....#", "#.....#", "#######"));
        var u = Unit(w, new Vector2(3.5f, 2.5f));
        for (var i = 0; i < 60; i++)
        {
            w.Enqueue(new MoveIntent(u.Id, new Vector2(1, 1)));
            w.Step();
        }

        var p = u.Get<Transform>().Position;
        Assert.True(p.Y <= 4f - 0.4f + 1e-3f, $"y={p.Y}");
        Assert.True(p.X <= 6f - 0.4f + 1e-3f, $"x={p.X}");
        Assert.True(p.X > 5.5f, "should have slid to the east wall");
    }

    [Fact]
    public void Slides_along_wall_keeping_tangential_speed()
    {
        var w = Arena(GridMap.FromAscii("#########", "#.......#", "#########"));
        var u = Unit(w, new Vector2(1.5f, 1.5f));
        for (var i = 0; i < 20; i++)
        {
            w.Enqueue(new MoveIntent(u.Id, new Vector2(1, 1)));
            w.Step();
        }

        var p = u.Get<Transform>().Position;
        Assert.True(p.Y >= 1.4f - 1e-3f && p.Y <= 1.6f + 1e-3f);
        Assert.True(p.X > 4f, $"x={p.X}");
    }

    [Fact]
    public void Does_not_tunnel_through_wall_at_very_high_speed()
    {
        var w = Arena(GridMap.FromAscii("..#..", "..#..", "..#.."));
        var u = Unit(w, new Vector2(0.5f, 1.5f), speed: 500f);
        for (var i = 0; i < 10; i++)
        {
            w.Enqueue(new MoveIntent(u.Id, new Vector2(1, 0)));
            w.Step();
        }

        Assert.True(u.Get<Transform>().Position.X <= 2f - 0.4f + 1e-3f);
    }

    [Fact]
    public void Corner_does_not_trap_or_penetrate()
    {
        var w = Arena(GridMap.FromAscii("......", "..#...", "......", "......"));
        var u = Unit(w, new Vector2(0.5f, 0.5f));
        for (var i = 0; i < 40; i++)
        {
            w.Enqueue(new MoveIntent(u.Id, new Vector2(1, 1)));
            w.Step();
            var p = u.Get<Transform>().Position;
            var cx = Math.Clamp(p.X, 2, 3);
            var cy = Math.Clamp(p.Y, 1, 2);
            Assert.True(Vector2.Distance(p, new Vector2(cx, cy)) >= 0.4f - 1e-3f);
        }

        Assert.True(u.Get<Transform>().Position.X > 3f, "should slide around the block corner");
    }

    [Fact]
    public void Facing_follows_movement_unless_locked()
    {
        var w = Arena(GridMap.FromAscii("......", "......"));
        var u = Unit(w, new Vector2(1.5f, 1f));
        w.Enqueue(new MoveIntent(u.Id, new Vector2(1, 0)));
        w.Step();
        Assert.Equal(new Vector2(1, 0), u.Get<Transform>().Facing);
        u.Get<Movement>().LockFacing = true;
        w.Enqueue(new MoveIntent(u.Id, new Vector2(0, 1)));
        w.Step();
        Assert.Equal(new Vector2(1, 0), u.Get<Transform>().Facing);
    }

    [Fact]
    public void Overlapping_entities_are_separated()
    {
        var w = Arena(GridMap.FromAscii("..........", "..........", ".........."));
        var a = Unit(w, new Vector2(4.5f, 1.5f));
        var b = Unit(w, new Vector2(4.6f, 1.5f));
        for (var i = 0; i < 30; i++)
        {
            w.Step();
        }

        var d = Vector2.Distance(a.Get<Transform>().Position, b.Get<Transform>().Position);
        Assert.True(d >= 0.8f - 0.01f, $"d={d}");
    }

    [Fact]
    public void Identical_positions_separate_deterministically()
    {
        var w = Arena(GridMap.FromAscii("..........", "..........", ".........."));
        var a = Unit(w, new Vector2(4.5f, 1.5f));
        var b = Unit(w, new Vector2(4.5f, 1.5f));
        for (var i = 0; i < 30; i++)
        {
            w.Step();
        }

        Assert.True(a.Get<Transform>().Position.X < b.Get<Transform>().Position.X);
        Assert.True(Vector2.Distance(a.Get<Transform>().Position, b.Get<Transform>().Position) >= 0.79f);
    }

    [Fact]
    public void Separation_does_not_push_into_walls()
    {
        var w = Arena(GridMap.FromAscii("#####", "#...#", "#####"));
        var a = Unit(w, new Vector2(1.5f, 1.5f));
        var b = Unit(w, new Vector2(1.5f, 1.5f));
        for (var i = 0; i < 30; i++)
        {
            w.Step();
        }

        foreach (var e in new[] { a, b })
        {
            var p = e.Get<Transform>().Position;
            Assert.True(p.X >= 1.4f - 1e-3f && p.Y >= 1.4f - 1e-3f && p.Y <= 1.6f + 1e-3f);
        }
    }

    // ---------- Deslocamento forçado ----------

    [Fact]
    public void Forced_displacement_moves_full_distance_in_open_space()
    {
        var w = Arena(GridMap.FromAscii("..........", ".........."));
        var u = Unit(w, new Vector2(1f, 1f));
        var moved = ForcedMovement.Displace(w, u, new Vector2(2, 0), 5f);
        Assert.Equal(5f, moved, 2);
        Assert.Equal(6f, u.Get<Transform>().Position.X, 2);
    }

    [Fact]
    public void Forced_displacement_stops_at_wall()
    {
        var w = Arena(GridMap.FromAscii("....#...", "....#..."));
        var u = Unit(w, new Vector2(1f, 1f));
        var moved = ForcedMovement.Displace(w, u, new Vector2(1, 0), 10f);
        var x = u.Get<Transform>().Position.X;
        Assert.True(x <= 4f - 0.4f + 1e-3f && x > 3.5f, $"x={x}");
        Assert.True(moved < 3f);
    }

    [Fact]
    public void Forced_displacement_against_adjacent_wall_does_nothing()
    {
        var w = Arena(GridMap.FromAscii("..#", "..#"));
        var u = Unit(w, new Vector2(1.6f, 1f));
        Assert.Equal(0f, ForcedMovement.Displace(w, u, new Vector2(1, 0), 3f), 3);
    }

    // ---------- Consultas espaciais ----------

    [Fact]
    public void InRadius_returns_entities_in_id_order_and_skips_dead()
    {
        var w = Arena(GridMap.FromAscii(".........."));
        var a = Dummy(w, new Vector2(1, 0.5f));
        var far = Dummy(w, new Vector2(9, 0.5f));
        var dead = Dummy(w, new Vector2(1.2f, 0.5f));
        var b = Dummy(w, new Vector2(2, 0.5f));
        w.Kill(dead);
        var r = w.Spatial.InRadius(new Vector2(1.5f, 0.5f), 1f);
        Assert.Equal(new[] { a.Id, b.Id }, r.Select(e => e.Id).ToArray());
        Assert.DoesNotContain(far, r);
        Assert.Single(w.Spatial.InRadius(new Vector2(1.5f, 0.5f), 1f, e => e.Id == b.Id));
    }

    [Fact]
    public void InCone_respects_angle_and_range()
    {
        var w = Arena(GridMap.FromAscii(".........."));
        var front = Dummy(w, new Vector2(2, 4));
        var edge = Dummy(w, new Vector2(2, 3));
        var behind = Dummy(w, new Vector2(-3, 0));
        var tooFar = Dummy(w, new Vector2(9, 0));
        var on = Dummy(w, new Vector2(3, 0));
        var r = w.Spatial.InCone(Vector2.Zero, new Vector2(1, 0), MathF.PI / 2f, 5f).Select(e => e.Id).ToList();
        Assert.Contains(on.Id, r);
        Assert.DoesNotContain(behind.Id, r);
        Assert.DoesNotContain(tooFar.Id, r);
        Assert.DoesNotContain(front.Id, r);
        Assert.DoesNotContain(edge.Id, r);
        var wide = w.Spatial.InCone(Vector2.Zero, new Vector2(1, 0), MathF.PI * 1.2f, 5f).Select(e => e.Id).ToList();
        Assert.Contains(edge.Id, wide);
        Assert.Contains(front.Id, wide);
        Assert.DoesNotContain(behind.Id, wide);
    }

    [Fact]
    public void InOrientedRect_handles_rotation()
    {
        var w = Arena(GridMap.FromAscii(".........."));
        var inside = Dummy(w, new Vector2(3, 3));
        var sideOut = Dummy(w, new Vector2(3, 5));
        var beyond = Dummy(w, new Vector2(8, 8));
        var behind = Dummy(w, new Vector2(-2, -2));
        var r = w.Spatial.InOrientedRect(Vector2.Zero, new Vector2(1, 1), 5f, 2f).Select(e => e.Id).ToList();
        Assert.Contains(inside.Id, r);
        Assert.DoesNotContain(sideOut.Id, r);
        Assert.DoesNotContain(beyond.Id, r);
        Assert.DoesNotContain(behind.Id, r);
        Assert.Contains(inside.Id, w.Spatial.InOrientedRect(Vector2.Zero, new Vector2(1, 1), 5f, 0f).Select(e => e.Id));
    }

    [Fact]
    public void Nearest_picks_closest_with_filter_and_range()
    {
        var w = Arena(GridMap.FromAscii(".........."));
        var a = Dummy(w, new Vector2(5, 0));
        var b = Dummy(w, new Vector2(2, 0));
        Assert.Same(b, w.Spatial.Nearest(Vector2.Zero));
        Assert.Same(a, w.Spatial.Nearest(Vector2.Zero, filter: e => e != b));
        Assert.Null(w.Spatial.Nearest(Vector2.Zero, 1f));
    }

    // ---------- Linha de visão ----------

    [Fact]
    public void LineOfSight_blocked_by_walls_and_clear_otherwise()
    {
        var m = GridMap.FromAscii(".....", "..#..", ".....");
        Assert.False(m.HasLineOfSight(new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f)));
        Assert.True(m.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(4.5f, 0.5f)));
        Assert.True(m.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 2.5f)));
        Assert.False(m.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(4.5f, 2.5f)));
        Assert.True(m.HasLineOfSight(new Vector2(3.5f, 1.5f), new Vector2(4.5f, 1.5f)));
        Assert.True(m.Raycast(new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f), out var hit));
        Assert.Equal(2f, hit.X, 3);
    }

    [Fact]
    public void LineOfSight_diagonal_corner_grazing_and_reverse_direction()
    {
        var m = GridMap.FromAscii("..#", "...", "...");
        Assert.False(m.HasLineOfSight(new Vector2(0.5f, 2.5f), new Vector2(2.5f, 0.5f)));
        Assert.False(m.HasLineOfSight(new Vector2(2.5f, 0.5f), new Vector2(0.5f, 2.5f)));
        Assert.True(m.HasLineOfSight(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 2.5f)));
    }

    [Fact]
    public void LineOfSight_with_radius_rejects_tight_gaps()
    {
        var m = GridMap.FromAscii("..#..", ".....", "..#..");
        Assert.True(m.HasLineOfSight(new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f), 0.4f));
        Assert.False(m.HasLineOfSight(new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f), 0.6f));
    }

    // ---------- Pathfinding ----------

    [Fact]
    public void AStar_finds_path_in_maze_and_never_cuts_corners()
    {
        var m = GridMap.FromAscii(
            ".....",
            ".###.",
            ".#...",
            ".#.#.",
            "...#.");
        var path = Pathfinder.FindPath(m, new Vector2(0.5f, 0.5f), new Vector2(4.5f, 4.5f), smooth: false);
        Assert.NotEmpty(path);
        Assert.Equal(new Vector2(4.5f, 4.5f), path[^1]);
        var prev = new Vector2(0.5f, 0.5f);
        foreach (var p in path)
        {
            Assert.False(m.IsBlocked(p));
            Assert.True(m.HasLineOfSight(prev, p));
            prev = p;
        }
    }

    [Fact]
    public void AStar_does_not_squeeze_diagonally_between_two_walls()
    {
        var m = GridMap.FromAscii(".#", "#.");
        Assert.Empty(Pathfinder.FindPath(m, new Vector2(0.5f, 0.5f), new Vector2(1.5f, 1.5f)));
    }

    [Fact]
    public void AStar_returns_empty_when_no_path_or_blocked_endpoints()
    {
        var m = GridMap.FromAscii("..#..", "..#..", "..#..");
        Assert.Empty(Pathfinder.FindPath(m, new Vector2(0.5f, 0.5f), new Vector2(4.5f, 0.5f)));
        Assert.Empty(Pathfinder.FindPath(m, new Vector2(0.5f, 0.5f), new Vector2(2.5f, 0.5f)));
        Assert.Single(Pathfinder.FindPath(m, new Vector2(0.5f, 0.5f), new Vector2(0.9f, 0.9f)));
    }

    [Fact]
    public void Path_smoothing_removes_redundant_waypoints()
    {
        var m = GridMap.FromAscii("........", "........", "........", "........");
        var raw = Pathfinder.FindPath(m, new Vector2(0.5f, 0.5f), new Vector2(7.5f, 3.5f), smooth: false);
        var smooth = Pathfinder.FindPath(m, new Vector2(0.5f, 0.5f), new Vector2(7.5f, 3.5f));
        Assert.True(raw.Count > 3);
        Assert.Single(smooth);
        Assert.Equal(new Vector2(7.5f, 3.5f), smooth[0]);
    }

    [Fact]
    public void Path_smoothing_keeps_corner_waypoints_around_obstacles()
    {
        var m = GridMap.FromAscii(".....", ".###.", ".....");
        var path = Pathfinder.FindPath(m, new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f), 0.3f);
        Assert.True(path.Count >= 2);
        var prev = new Vector2(0.5f, 1.5f);
        foreach (var p in path)
        {
            Assert.True(m.HasLineOfSight(prev, p));
            prev = p;
        }
    }

    // ---------- Determinismo ----------

    [Fact]
    public void Movement_with_map_is_deterministic()
    {
        static ulong Run()
        {
            var w = Arena(GridMap.FromAscii("#######", "#.....#", "#..#..#", "#.....#", "#######"));
            var a = Unit(w, new Vector2(1.5f, 1.5f));
            var b = Unit(w, new Vector2(1.6f, 1.5f));
            for (var i = 0; i < 300; i++)
            {
                var r = w.Random.NextInt(8);
                w.Enqueue(new MoveIntent(a.Id, new Vector2(MathF.Cos(r), MathF.Sin(r))));
                w.Enqueue(new MoveIntent(b.Id, new Vector2(MathF.Sin(r), MathF.Cos(r))));
                w.Step();
            }

            return w.ComputeStateHash();
        }

        Assert.Equal(Run(), Run());
    }
}
