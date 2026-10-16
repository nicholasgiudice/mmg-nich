using System.Numerics;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Spatial;

/// <summary>
/// Consultas espaciais sobre entidades. Resultados em ordem de id (determinístico). Só entidades vivas com
/// <see cref="Transform"/>; o raio do <see cref="Collider"/> conta como tamanho do corpo. <c>filter</c> é opcional.
/// </summary>
public interface ISpatialQuery
{
    /// <summary>Entidades cujo corpo toca o círculo (centro, raio).</summary>
    IReadOnlyList<Entity> InRadius(Vector2 center, float radius, Func<Entity, bool>? filter = null);

    /// <summary>Cone: <paramref name="angleRadians"/> é a abertura TOTAL, centrada em <paramref name="direction"/>.</summary>
    IReadOnlyList<Entity> InCone(Vector2 origin, Vector2 direction, float angleRadians, float range, Func<Entity, bool>? filter = null);

    /// <summary>Retângulo orientado (linha): parte de <paramref name="origin"/> por <paramref name="length"/> ao longo de <paramref name="direction"/>, com <paramref name="width"/> total.</summary>
    IReadOnlyList<Entity> InOrientedRect(Vector2 origin, Vector2 direction, float length, float width, Func<Entity, bool>? filter = null);

    /// <summary>Entidade mais próxima do ponto (empate: menor id), ou null.</summary>
    Entity? Nearest(Vector2 point, float maxDistance = float.PositiveInfinity, Func<Entity, bool>? filter = null);
}

/// <summary>Implementação por força bruta (suficiente para &lt; 200 entidades).</summary>
public sealed class BruteForceSpatialQuery(World world) : ISpatialQuery
{
    public IReadOnlyList<Entity> InRadius(Vector2 center, float radius, Func<Entity, bool>? filter = null)
    {
        var r = new List<Entity>();
        foreach (var (e, pos, body) in Candidates(filter))
        {
            if (Vector2.Distance(pos, center) <= radius + body)
            {
                r.Add(e);
            }
        }

        return r;
    }

    public IReadOnlyList<Entity> InCone(Vector2 origin, Vector2 direction, float angleRadians, float range, Func<Entity, bool>? filter = null)
    {
        var r = new List<Entity>();
        if (direction.LengthSquared() < 1e-12f)
        {
            return r;
        }

        var dir = Vector2.Normalize(direction);
        var cosHalf = MathF.Cos(MathF.Min(angleRadians, 2 * MathF.PI) / 2f);
        foreach (var (e, pos, body) in Candidates(filter))
        {
            var d = pos - origin;
            var dist = d.Length();
            if (dist > range + body)
            {
                continue;
            }

            if (dist <= body || angleRadians >= (2 * MathF.PI) - 1e-4f || Vector2.Dot(d / dist, dir) >= cosHalf - 1e-5f)
            {
                r.Add(e);
            }
        }

        return r;
    }

    public IReadOnlyList<Entity> InOrientedRect(Vector2 origin, Vector2 direction, float length, float width, Func<Entity, bool>? filter = null)
    {
        var r = new List<Entity>();
        if (direction.LengthSquared() < 1e-12f)
        {
            return r;
        }

        var dir = Vector2.Normalize(direction);
        var side = new Vector2(-dir.Y, dir.X);
        var half = width / 2f;
        foreach (var (e, pos, body) in Candidates(filter))
        {
            var d = pos - origin;
            var lx = Vector2.Dot(d, dir);
            var ly = MathF.Abs(Vector2.Dot(d, side));
            var dx = lx - Math.Clamp(lx, 0f, length);
            var dy = ly - Math.Min(ly, half);
            if ((dx * dx) + (dy * dy) <= body * body)
            {
                r.Add(e);
            }
        }

        return r;
    }

    public Entity? Nearest(Vector2 point, float maxDistance = float.PositiveInfinity, Func<Entity, bool>? filter = null)
    {
        Entity? best = null;
        var bestD = float.PositiveInfinity;
        foreach (var (e, pos, _) in Candidates(filter))
        {
            var d = Vector2.Distance(pos, point);
            if (d <= maxDistance && d < bestD)
            {
                best = e;
                bestD = d;
            }
        }

        return best;
    }

    private IEnumerable<(Entity E, Vector2 Pos, float Body)> Candidates(Func<Entity, bool>? filter)
    {
        foreach (var e in world.Entities)
        {
            if (!e.IsAlive)
            {
                continue;
            }

            var tr = e.TryGet<Transform>();
            if (tr is null || (filter is not null && !filter(e)))
            {
                continue;
            }

            yield return (e, tr.Position, e.TryGet<Collider>()?.Radius ?? 0f);
        }
    }
}
