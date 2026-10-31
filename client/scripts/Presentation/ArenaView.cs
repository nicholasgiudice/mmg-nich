using Game.Core.Definitions;
using Godot;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Desenha a arena. Com mapa do Tiled (T-052), as camadas de tiles com o tileset: chão, decoração e muros aqui, por
/// baixo de áreas, telegráfos e unidades; a camada <c>above</c> num filho por cima das unidades, transparente perto do
/// jogador. Sem mapa do Tiled (arenas ASCII) ou com <c>--debug-grid</c>, a grade de quadrados de antes.
/// A colisão continua vindo do Core (<see cref="GridMap"/>); a camada <c>collision</c> não é desenhada.
/// </summary>
public partial class ArenaView : Node2D
{
    private static readonly Color FloorA = new(0.23f, 0.30f, 0.20f);
    private static readonly Color FloorB = new(0.25f, 0.32f, 0.22f);
    private static readonly Color Wall = new(0.32f, 0.29f, 0.27f);
    private static readonly Color WallEdge = new(0.18f, 0.16f, 0.15f);

    private GridMap? _map;
    private TiledVisual? _visual;
    private readonly AboveLayerView _above = new() { Name = "Above", ZIndex = ZLayers.Above };

    /// <summary>Força a grade de quadrados mesmo com mapa do Tiled (depuração).</summary>
    public bool DebugGrid { get; set; }

    public GridMap? Map => _map;

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        AddChild(_above);
    }

    /// <summary>Troca a arena desenhada. <paramref name="arena"/> null (ou ASCII) = grade de quadrados.</summary>
    public void Show(GridMap map, ArenaDefinition? arena)
    {
        _map = map;
        _visual = null;
        if (!DebugGrid && arena?.Source is { } source)
        {
            try
            {
                _visual = TiledVisual.Load($"{Data.ContentLoader.DataDirectory}/{source}");
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Mapa {source} sem desenho do Tiled ({ex.Message}); usando a grade.");
            }
        }

        _above.Visual = _visual;
        QueueRedraw();
    }

    /// <summary>Posição (pixels) do jogador controlado: a camada <c>above</c> fica transparente em volta dela.</summary>
    public void SetFocus(Godot.Vector2? pixels) => _above.SetFocus(pixels);

    public override void _Draw()
    {
        if (_map is null)
        {
            return;
        }

        if (_visual is not null)
        {
            foreach (var layer in _visual.Layers.Where(l => l.Name is not (TiledVisual.Collision or TiledVisual.Above)))
            {
                _visual.DrawLayer(this, layer, _ => 1f);
            }

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

    /// <summary>Camada <c>above</c> (copas, telhados): por cima das unidades; some em volta do jogador.</summary>
    private sealed partial class AboveLayerView : Node2D
    {
        /// <summary>Raio (tiles, distância de Chebyshev) em volta do jogador onde a camada fica transparente.</summary>
        private const int FadeRadius = 2;
        private const float FadeAlpha = 0.35f;

        private TiledVisual? _visual;
        private Vector2I? _focusCell;

        public TiledVisual? Visual
        {
            set
            {
                _visual = value;
                QueueRedraw();
            }
        }

        public void SetFocus(Godot.Vector2? pixels)
        {
            Vector2I? cell = pixels is { } p
                ? new Vector2I((int)Math.Floor(p.X / Units.PixelsPerUnit), (int)Math.Floor(p.Y / Units.PixelsPerUnit))
                : null;
            if (cell != _focusCell)
            {
                _focusCell = cell;
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            if (_visual?.Layers.FirstOrDefault(l => l.Name == TiledVisual.Above) is not { } layer)
            {
                return;
            }

            var focus = _focusCell;
            _visual.DrawLayer(this, layer, cell =>
                focus is { } f && Math.Max(Math.Abs(cell.X - f.X), Math.Abs(cell.Y - f.Y)) <= FadeRadius ? FadeAlpha : 1f);
        }
    }
}
