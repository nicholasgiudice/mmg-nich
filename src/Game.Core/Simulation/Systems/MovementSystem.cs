using System.Numerics;
using Game.Core.Simulation.Components;

namespace Game.Core.Simulation.Systems;

/// <summary>Movimento simples sem colisão. A T-007 substitui por versão com colisão.</summary>
public sealed class MovementSystem : ISimulationSystem
{
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
            if (move is null || tr is null || move.Direction == Vector2.Zero)
            {
                continue;
            }

            tr.Position += move.Direction * move.Speed * World.TickSeconds;
            tr.Facing = Vector2.Normalize(move.Direction);
        }
    }
}
