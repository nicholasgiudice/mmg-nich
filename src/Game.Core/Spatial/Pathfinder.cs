using System.Numerics;

namespace Game.Core.Spatial;

/// <summary>A* em 8 direções sem cortar quinas, com suavização por linha de visão.</summary>
public static class Pathfinder
{
    private const float Diagonal = 1.41421356f;

    private static readonly (int Dx, int Dy, float Cost)[] Neighbors =
    [
        (1, 0, 1f), (-1, 0, 1f), (0, 1, 1f), (0, -1, 1f),
        (1, 1, Diagonal), (1, -1, Diagonal), (-1, 1, Diagonal), (-1, -1, Diagonal),
    ];

    /// <summary>
    /// Caminho de <paramref name="start"/> a <paramref name="goal"/> (posições de mundo). Retorna waypoints SEM o ponto
    /// inicial, terminando exatamente em <paramref name="goal"/>. Vazio se não há caminho (ou início/destino bloqueados).
    /// <paramref name="radius"/> só afeta a suavização (linha de visão "grossa").
    /// </summary>
    public static IReadOnlyList<Vector2> FindPath(GridMap map, Vector2 start, Vector2 goal, float radius = 0f, bool smooth = true)
    {
        var (sx, sy) = GridMap.CellOf(start);
        var (gx, gy) = GridMap.CellOf(goal);
        if (map.IsBlocked(sx, sy) || map.IsBlocked(gx, gy))
        {
            return Array.Empty<Vector2>();
        }

        if (sx == gx && sy == gy)
        {
            return new[] { goal };
        }

        var cells = map.Width * map.Height;
        var g = new float[cells];
        Array.Fill(g, float.PositiveInfinity);
        var parent = new int[cells];
        Array.Fill(parent, -1);
        var closed = new bool[cells];
        var open = new PriorityQueue<int, (float F, float H, int Index)>();

        float H(int x, int y)
        {
            var dx = Math.Abs(x - gx);
            var dy = Math.Abs(y - gy);
            return (Math.Max(dx, dy) - Math.Min(dx, dy)) + (Diagonal * Math.Min(dx, dy));
        }

        var si = (sy * map.Width) + sx;
        var gi = (gy * map.Width) + gx;
        g[si] = 0;
        open.Enqueue(si, (H(sx, sy), H(sx, sy), si));
        while (open.TryDequeue(out var cur, out _))
        {
            if (closed[cur])
            {
                continue;
            }

            closed[cur] = true;
            if (cur == gi)
            {
                break;
            }

            var cx = cur % map.Width;
            var cy = cur / map.Width;
            foreach (var (dx, dy, cost) in Neighbors)
            {
                int nx = cx + dx, ny = cy + dy;
                if (map.IsBlocked(nx, ny))
                {
                    continue;
                }

                if (dx != 0 && dy != 0 && (map.IsBlocked(cx + dx, cy) || map.IsBlocked(cx, cy + dy)))
                {
                    continue;
                }

                var ni = (ny * map.Width) + nx;
                var ng = g[cur] + cost;
                if (closed[ni] || ng >= g[ni])
                {
                    continue;
                }

                g[ni] = ng;
                parent[ni] = cur;
                var h = H(nx, ny);
                open.Enqueue(ni, (ng + h, h, ni));
            }
        }

        if (parent[gi] < 0)
        {
            return Array.Empty<Vector2>();
        }

        var cellsPath = new List<int>();
        for (var c = gi; c != si; c = parent[c])
        {
            cellsPath.Add(c);
        }

        var raw = new List<Vector2> { start };
        for (var i = cellsPath.Count - 1; i >= 0; i--)
        {
            raw.Add(GridMap.CellCenter(cellsPath[i] % map.Width, cellsPath[i] / map.Width));
        }

        raw[^1] = goal;

        if (!smooth)
        {
            raw.RemoveAt(0);
            return raw;
        }

        var result = new List<Vector2>();
        var anchor = 0;
        while (anchor < raw.Count - 1)
        {
            var far = anchor + 1;
            for (var j = raw.Count - 1; j > anchor + 1; j--)
            {
                if (map.HasLineOfSight(raw[anchor], raw[j], radius))
                {
                    far = j;
                    break;
                }
            }

            result.Add(raw[far]);
            anchor = far;
        }

        return result;
    }
}
