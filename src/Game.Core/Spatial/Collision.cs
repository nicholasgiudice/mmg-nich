using System.Numerics;

namespace Game.Core.Spatial;

/// <summary>Colisão de círculo contra a grade. Funções puras e determinísticas.</summary>
public static class Collision
{
    private const int MaxPushPasses = 4;

    /// <summary>
    /// Move um círculo por <paramref name="delta"/> em sub-steps (nunca atravessa parede, mesmo em alta velocidade),
    /// deslizando ao longo de paredes. Retorna a posição final.
    /// </summary>
    public static Vector2 MoveCircle(GridMap map, Vector2 position, float radius, Vector2 delta)
    {
        var dist = delta.Length();
        if (dist <= 0f || !float.IsFinite(dist))
        {
            return position;
        }

        var steps = StepCount(dist, radius);
        var step = delta / steps;
        for (var i = 0; i < steps; i++)
        {
            position = PushOut(map, position + step, radius);
        }

        return position;
    }

    internal static int StepCount(float distance, float radius)
    {
        var maxStep = MathF.Max(0.05f, MathF.Min(0.25f, radius * 0.5f));
        return Math.Max(1, (int)MathF.Ceiling(distance / maxStep));
    }

    /// <summary>Empurra o círculo para fora de células bloqueadas (e de fora do mapa).</summary>
    public static Vector2 PushOut(GridMap map, Vector2 p, float radius)
    {
        for (var pass = 0; pass < MaxPushPasses; pass++)
        {
            var moved = false;
            var x0 = (int)MathF.Floor(p.X - radius);
            var x1 = (int)MathF.Floor(p.X + radius);
            var y0 = (int)MathF.Floor(p.Y - radius);
            var y1 = (int)MathF.Floor(p.Y + radius);
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    if (!map.IsBlocked(x, y))
                    {
                        continue;
                    }

                    var cp = new Vector2(Math.Clamp(p.X, x, x + 1), Math.Clamp(p.Y, y, y + 1));
                    var d = p - cp;
                    var d2 = d.LengthSquared();
                    if (d2 >= radius * radius)
                    {
                        continue;
                    }

                    if (d2 > 1e-12f)
                    {
                        var len = MathF.Sqrt(d2);
                        p += d / len * (radius - len);
                    }
                    else
                    {
                        p = ExitCell(map, p, x, y, radius);
                    }

                    moved = true;
                }
            }

            if (!moved)
            {
                break;
            }
        }

        return p;
    }

    // Centro dentro (ou sobre a borda de) uma célula bloqueada: sai pela face mais próxima cujo vizinho é livre.
    private static Vector2 ExitCell(GridMap map, Vector2 p, int x, int y, float r)
    {
        var costs = new[] { p.X - x, x + 1 - p.X, p.Y - y, y + 1 - p.Y };
        var targets = new[]
        {
            new Vector2(x - r, p.Y), new Vector2(x + 1 + r, p.Y), new Vector2(p.X, y - r), new Vector2(p.X, y + 1 + r),
        };
        var free = new[] { !map.IsBlocked(x - 1, y), !map.IsBlocked(x + 1, y), !map.IsBlocked(x, y - 1), !map.IsBlocked(x, y + 1) };
        var best = 0;
        for (var i = 1; i < 4; i++)
        {
            if ((free[i] && !free[best]) || (free[i] == free[best] && costs[i] < costs[best]))
            {
                best = i;
            }
        }

        return targets[best];
    }
}
