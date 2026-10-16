using System.Numerics;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Spatial;

/// <summary>Deslocamento forçado (dash, knockback, puxão): move até uma distância, parando em parede. Não altera o facing.</summary>
public static class ForcedMovement
{
    /// <summary>Retorna a distância efetivamente percorrida (0 se bloqueado ou entrada inválida).</summary>
    public static float Displace(World world, Entity entity, Vector2 direction, float distance)
    {
        var tr = entity.TryGet<Transform>();
        if (tr is null || distance <= 0f || !float.IsFinite(distance) || direction.LengthSquared() < 1e-12f)
        {
            return 0f;
        }

        var dir = Vector2.Normalize(direction);
        var from = tr.Position;
        if (world.Map is null)
        {
            tr.Position = from + (dir * distance);
            return distance;
        }

        // Sem deslizar: para na parede (dash/knockback não contornam quinas).
        var radius = entity.TryGet<Collider>()?.Radius ?? 0f;
        var steps = Collision.StepCount(distance, radius);
        var step = dir * (distance / steps);
        var pos = from;
        for (var i = 0; i < steps; i++)
        {
            var cand = pos + step;
            if (Vector2.DistanceSquared(Collision.PushOut(world.Map, cand, radius), cand) > 1e-8f)
            {
                break;
            }

            pos = cand;
        }

        tr.Position = pos;
        return Vector2.Distance(from, pos);
    }
}
