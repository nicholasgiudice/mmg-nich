using Game.Core.Definitions;
using Godot;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Desenha a arena. Com mapa do Tiled (T-052), as camadas de tiles com o tileset: chão, decoração e muros aqui, por
/// baixo de áreas, telegráfos e unidades; a camada <c>above</c> num filho por cima das unidades, transparente perto do
/// jogador. Sem mapa do Tiled (arenas ASCII) ou com <c>--debug-grid</c>, a grade de quadrados de antes.
/// A colisão continua vindo do Core (<see cref="GridMap"/>); a camada <c>collision</c> não é desenhada.
/// No mundo aberto (T-223) os portões da zona aparecem por cima do chão, com o nome da zona de destino.
/// </summary>
public partial class ArenaView : Node2D
{
    private static readonly Color FloorA = new(0.23f, 0.30f, 0.20f);
    private static readonly Color FloorB = new(0.25f, 0.32f, 0.22f);
    private static readonly Color Wall = new(0.32f, 0.29f, 0.27f);
    private static readonly Color WallEdge = new(0.18f, 0.16f, 0.15f);
    private static readonly Color GateFill = new(0.95f, 0.80f, 0.35f, 0.22f);
    private static readonly Color GateEdge = new(0.95f, 0.80f, 0.35f, 0.85f);

    private GridMap? _map;
    private ArenaDefinition? _arena;
    private TiledVisual? _visual;
    private bool _debugGrid;
    private readonly AboveLayerView _above = new() { Name = "Above", ZIndex = ZLayers.Above };
    private IReadOnlyList<(RectDefinition Area, string Label)> _gates = [];

    /// <summary>Força a grade de quadrados mesmo com mapa do Tiled (depuração). Trocar com a arena já mostrada redesenha (T-085).</summary>
    public bool DebugGrid
    {
        get => _debugGrid;
        set
        {
            if (_debugGrid == value)
            {
                return;
            }

            _debugGrid = value;
            if (_map is not null)
            {
                var gates = _gates;
                Show(_map, _arena);
                _gates = gates;
            }
        }
    }

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
        _arena = arena;
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
        _gates = [];
        QueueRedraw();
    }

    /// <summary>Portões da zona (T-223): retângulo em tiles e texto (zona de destino). Chamar depois de <see cref="Show"/>.</summary>
    public void ShowGates(IReadOnlyList<(RectDefinition Area, string Label)> gates)
    {
        _gates = gates;
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

            DrawGates();
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

        DrawGates();
    }

    private void DrawGates()
    {
        const float s = Units.PixelsPerUnit;
        const int fontSize = 14;
        var font = ThemeDB.FallbackFont;
        foreach (var (area, label) in _gates)
        {
            var rect = new Rect2((float)area.X * s, (float)area.Y * s, (float)area.Width * s, (float)area.Height * s);
            DrawRect(rect, GateFill);
            DrawRect(rect, GateEdge, filled: false, width: 2f);

            // Texto acima do portão, centrado e dentro do mapa (portões ficam na borda).
            var size = font.GetStringSize(label, HorizontalAlignment.Left, -1, fontSize);
            var maxX = (_map?.Width ?? 0) * s - size.X - 4;
            var x = Math.Clamp(rect.GetCenter().X - size.X / 2, 4, Math.Max(4, maxX));
            var at = new Godot.Vector2(x, rect.Position.Y - 6);
            DrawString(font, at + new Godot.Vector2(1, 1), label, HorizontalAlignment.Left, -1, fontSize, new Color(0, 0, 0, 0.8f));
            DrawString(font, at, label, HorizontalAlignment.Left, -1, fontSize, GateEdge with { A = 1 });
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
