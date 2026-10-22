using System.Numerics;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Server.Interest;

/// <summary>
/// Interest management por grade (T-106). Uma vez por tick, as entidades replicáveis vão para células de
/// <c>cellSize</c> tiles; cada jogador vê as células a até <c>radiusCells</c> da sua (quadrado de
/// (2r+1)² células) e mais as que sempre importam: o próprio grupo (facção dos jogadores) e os chefes.
/// </summary>
public sealed class InterestGrid(float cellSize, int radiusCells)
{
    private readonly Dictionary<(int X, int Y), List<EntityId>> _cells = new();
    private readonly List<EntityId> _always = new();
    private readonly Dictionary<EntityId, Vector2> _positions = new();

    public float CellSize { get; } = cellSize;
    public int RadiusCells { get; } = radiusCells;

    /// <summary>Reparte as entidades do snapshot (chamar uma vez por tick, antes de <see cref="Visible"/>).</summary>
    public void Rebuild(WorldSnapshot snapshot)
    {
        foreach (var list in _cells.Values)
        {
            list.Clear();
        }

        _always.Clear();
        _positions.Clear();
        foreach (var e in snapshot.Entities)
        {
            if (e.TryGet<Transform>() is not { } t)
            {
                _always.Add(e.Id); // sem posição: não dá para filtrar
                continue;
            }

            _positions[e.Id] = t.Position;
            if (AlwaysRelevant(e))
            {
                _always.Add(e.Id);
                continue;
            }

            var key = Cell(t.Position);
            if (!_cells.TryGetValue(key, out var list))
            {
                _cells[key] = list = new List<EntityId>();
            }

            list.Add(e.Id);
        }
    }

    /// <summary>Entidades visíveis de <paramref name="center"/> (posição do jogador; null = só as que sempre importam).</summary>
    public HashSet<EntityId> Visible(Vector2? center)
    {
        var visible = new HashSet<EntityId>(_always);
        if (center is not { } c)
        {
            return visible;
        }

        var (cx, cy) = Cell(c);
        for (var x = cx - RadiusCells; x <= cx + RadiusCells; x++)
        {
            for (var y = cy - RadiusCells; y <= cy + RadiusCells; y++)
            {
                if (_cells.TryGetValue((x, y), out var list))
                {
                    visible.UnionWith(list);
                }
            }
        }

        return visible;
    }

    /// <summary>Posição da entidade no último <see cref="Rebuild"/>.</summary>
    public Vector2? PositionOf(EntityId id) => _positions.TryGetValue(id, out var p) ? p : null;

    private (int, int) Cell(Vector2 p) => ((int)MathF.Floor(p.X / CellSize), (int)MathF.Floor(p.Y / CellSize));

    private static bool AlwaysRelevant(EntitySnapshot e) =>
        e.TryGet<FactionTag>()?.Faction == Faction.Player || e.TryGet<UnitInfo>()?.Rank == MonsterRank.Boss;
}
