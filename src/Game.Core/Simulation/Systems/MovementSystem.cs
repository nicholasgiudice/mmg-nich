using System.Numerics;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Simulation.Systems;

/// <summary>
/// Movimento com colisão: aplica <see cref="Movement"/> (velocidade x dt) com sub-steps, desliza em paredes
/// (<see cref="World.Map"/>; sem mapa = plano aberto) e depois separa entidades sobrepostas.
/// Uma direção imposta pelo combate (T-351: medo) substitui a intenção de movimento enquanto durar. Entidades com
/// <see cref="Movement"/> de velocidade 0 (T-351: os selos) são fixas: a separação empurra só a outra.
/// </summary>
public sealed class MovementSystem : ISimulationSystem
{
    /// <summary>Fração da sobreposição corrigida por tick (dividida entre as duas entidades).</summary>
    public const float SeparationStrength = 0.5f;

    public void Update(World world)
    {
        foreach (var e in world.Entities)
        {
            if (!e.IsAlive)
            {
                continue;
            }

            var move = e.TryGet<Movement>();
            var tr = e.TryGet<Transform>();
            if (move is null || tr is null)
            {
                continue;
            }

            var direction = world.Combat.ForcedDirection(world, e) ?? move.Direction;
            if (direction == Vector2.Zero)
            {
                continue;
            }

            var delta = direction * (move.Speed * (float)world.Combat.SpeedMultiplier(world, e)) * World.TickSeconds;
            if (world.Map is null)
            {
                tr.Position += delta;
            }
            else
            {
                tr.Position = Collision.MoveCircle(world.Map, tr.Position, e.TryGet<Collider>()?.Radius ?? 0f, delta);
            }

            if (!move.LockFacing)
            {
                tr.Facing = Vector2.Normalize(direction);
            }
        }

        Separate(world);
    }

    private static void Separate(World world)
    {
        var bodies = new List<(Entity E, Transform T, float R, bool Fixed)>();
        foreach (var e in world.Entities)
        {
            if (!e.IsAlive)
            {
                continue;
            }

            var t = e.TryGet<Transform>();
            var c = e.TryGet<Collider>();
            if (t is not null && c is not null && c.Radius > 0f)
            {
                bodies.Add((e, t, c.Radius, e.TryGet<Movement>() is { Speed: <= 0f }));
            }
        }

        for (var i = 0; i < bodies.Count; i++)
        {
            for (var j = i + 1; j < bodies.Count; j++)
            {
                var a = bodies[i];
                var b = bodies[j];
                var d = b.T.Position - a.T.Position;
                var dist = d.Length();
                var overlap = a.R + b.R - dist;
                if (overlap <= 1e-4f)
                {
                    continue;
                }

                // Posições idênticas: direção determinística fixa (a vai para -x, b para +x).
                var n = dist > 1e-6f ? d / dist : new Vector2(1f, 0f);
                if (a.Fixed && b.Fixed)
                {
                    continue;
                }

                // Uma fixa: a outra leva o empurrão inteiro (o mesmo total do par).
                var share = a.Fixed || b.Fixed ? 1f : 0.5f;
                var push = n * (overlap * SeparationStrength * share);
                if (!a.Fixed)
                {
                    a.T.Position = Nudge(world, a.T.Position, a.R, -push);
                }

                if (!b.Fixed)
                {
                    b.T.Position = Nudge(world, b.T.Position, b.R, push);
                }
            }
        }
    }

    private static Vector2 Nudge(World world, Vector2 pos, float radius, Vector2 push)
    {
        var p = pos + push;
        return world.Map is null ? p : Collision.PushOut(world.Map, p, radius);
    }
}
