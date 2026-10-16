using Godot;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient.Presentation;

/// <summary>Desenha a grade da arena (chão e parede). Placeholder até a importação de mapas.</summary>
public partial class ArenaView : Node2D
{
    private static readonly Color FloorA = new(0.23f, 0.30f, 0.20f);
    private static readonly Color FloorB = new(0.25f, 0.32f, 0.22f);
    private static readonly Color Wall = new(0.32f, 0.29f, 0.27f);
    private static readonly Color WallEdge = new(0.18f, 0.16f, 0.15f);

    private GridMap? _map;

    public GridMap? Map
    {
        get => _map;
        set
        {
            _map = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_map is null)
        {
            return;
        }

        const float s = Units.PixelsPerUnit;
        for (var y = 0; y < _map.Height; y++)
        {
            for (var x = 0; x < _map.Width; x++)
            {
                var rect = new Rect2(x * s, y * s, s, s);
                if (_map.IsBlocked(x, y))
                {
                    DrawRect(rect, Wall);
                    DrawRect(rect, WallEdge, filled: false, width: 1f);
                }
                else
                {
                    DrawRect(rect, (x + y) % 2 == 0 ? FloorA : FloorB);
                }
            }
        }
    }
}
