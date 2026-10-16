using System.Numerics;

namespace Game.Core.Spatial;

/// <summary>
/// Mapa em grade. 1 célula = 1 unidade de mundo; a célula (x, y) cobre [x, x+1) x [y, y+1).
/// Fora dos limites conta como bloqueado. Imutável depois de criado.
/// </summary>
public sealed class GridMap
{
    private readonly bool[] _blocked;

    public GridMap(int width, int height, Func<int, int, bool>? isBlocked = null)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Map dimensions must be positive.");
        }

        Width = width;
        Height = height;
        _blocked = new bool[width * height];
        if (isBlocked is not null)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    _blocked[y * width + x] = isBlocked(x, y);
                }
            }
        }
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>'#' = bloqueado, '.' = livre. Linhas devem ter o mesmo comprimento; a linha 0 é y = 0.</summary>
    public static GridMap FromAscii(params string[] lines)
    {
        if (lines.Length == 0 || lines[0].Length == 0)
        {
            throw new ArgumentException("Map must have at least one cell.", nameof(lines));
        }

        var w = lines[0].Length;
        foreach (var l in lines)
        {
            if (l.Length != w)
            {
                throw new ArgumentException("All map lines must have the same length.", nameof(lines));
            }

            foreach (var ch in l)
            {
                if (ch != '#' && ch != '.')
                {
                    throw new ArgumentException($"Invalid map character '{ch}'.", nameof(lines));
                }
            }
        }

        return new GridMap(w, lines.Length, (x, y) => lines[y][x] == '#');
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public bool IsBlocked(int x, int y) => !InBounds(x, y) || _blocked[y * Width + x];

    public bool IsBlocked(Vector2 point) => IsBlocked((int)MathF.Floor(point.X), (int)MathF.Floor(point.Y));

    public static (int X, int Y) CellOf(Vector2 point) => ((int)MathF.Floor(point.X), (int)MathF.Floor(point.Y));

    public static Vector2 CellCenter(int x, int y) => new(x + 0.5f, y + 0.5f);

    /// <summary>
    /// Raycast em grade (DDA). Retorna true se o segmento atravessa célula bloqueada; <paramref name="hit"/> é o ponto de
    /// entrada na primeira célula bloqueada (ou <paramref name="from"/> se já começa dentro de uma).
    /// Ao passar exatamente por um vértice de célula, as duas células laterais são verificadas (conservador).
    /// </summary>
    public bool Raycast(Vector2 from, Vector2 to, out Vector2 hit)
    {
        var (cx, cy) = CellOf(from);
        if (IsBlocked(cx, cy))
        {
            hit = from;
            return true;
        }

        var d = to - from;
        var (ex, ey) = CellOf(to);
        var stepX = d.X > 0 ? 1 : -1;
        var stepY = d.Y > 0 ? 1 : -1;
        var tDeltaX = d.X == 0 ? float.PositiveInfinity : MathF.Abs(1f / d.X);
        var tDeltaY = d.Y == 0 ? float.PositiveInfinity : MathF.Abs(1f / d.Y);
        var tMaxX = d.X == 0 ? float.PositiveInfinity : ((d.X > 0 ? cx + 1 - from.X : from.X - cx) * tDeltaX);
        var tMaxY = d.Y == 0 ? float.PositiveInfinity : ((d.Y > 0 ? cy + 1 - from.Y : from.Y - cy) * tDeltaY);

        var guard = Math.Abs(ex - cx) + Math.Abs(ey - cy) + 2;
        while ((cx != ex || cy != ey) && guard-- > 0)
        {
            float t;
            if (tMaxX < tMaxY)
            {
                t = tMaxX;
                tMaxX += tDeltaX;
                cx += stepX;
                if (Blocked(cx, cy, t, out hit))
                {
                    return true;
                }
            }
            else if (tMaxY < tMaxX)
            {
                t = tMaxY;
                tMaxY += tDeltaY;
                cy += stepY;
                if (Blocked(cx, cy, t, out hit))
                {
                    return true;
                }
            }
            else
            {
                t = tMaxX;
                tMaxX += tDeltaX;
                tMaxY += tDeltaY;
                if (Blocked(cx + stepX, cy, t, out hit) || Blocked(cx, cy + stepY, t, out hit))
                {
                    return true;
                }

                cx += stepX;
                cy += stepY;
                if (Blocked(cx, cy, t, out hit))
                {
                    return true;
                }
            }
        }

        hit = to;
        return false;

        bool Blocked(int x, int y, float t, out Vector2 point)
        {
            point = from + d * MathF.Min(t, 1f);
            return IsBlocked(x, y);
        }
    }

    /// <summary>Linha de visão entre dois pontos: nenhuma célula bloqueada no caminho.</summary>
    public bool HasLineOfSight(Vector2 from, Vector2 to) => !Raycast(from, to, out _);

    /// <summary>Linha de visão para um corpo circular: o raio central e dois raios paralelos afastados por <paramref name="radius"/>.</summary>
    public bool HasLineOfSight(Vector2 from, Vector2 to, float radius)
    {
        if (!HasLineOfSight(from, to))
        {
            return false;
        }

        if (radius <= 0f)
        {
            return true;
        }

        var d = to - from;
        if (d.LengthSquared() < 1e-12f)
        {
            return true;
        }

        var n = Vector2.Normalize(new Vector2(-d.Y, d.X)) * radius;
        return HasLineOfSight(from + n, to + n) && HasLineOfSight(from - n, to - n);
    }
}
