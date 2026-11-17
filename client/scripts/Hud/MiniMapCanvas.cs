using Game.Core.Definitions;
using Godot;
using NVector2 = System.Numerics.Vector2;

namespace MmoRpgClient.Hud;

/// <summary>O que o mini mapa e o mapa grande mostram (T-392): o terreno da zona, os portões, o jogador e o grupo.</summary>
public sealed class MiniMapState
{
    public ImageTexture? Terrain { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public IReadOnlyList<RectDefinition> Gates { get; set; } = [];

    /// <summary>Posição do jogador em tiles; null antes de ele aparecer no snapshot.</summary>
    public NVector2? Player { get; set; }
    public IReadOnlyList<MapMark> Members { get; set; } = [];
}

/// <summary>
/// Desenho do mapa (T-392). No modo mini, o recorte em volta do jogador (<see cref="MiniMapView.Mini"/>); no modo
/// inteiro, a zona toda (<see cref="MiniMapView.Whole"/>). Membro fora do recorte vira uma seta presa na borda. O mouse
/// sobre uma marca mostra o nome. Só exibe.
/// </summary>
public partial class MiniMapCanvas : Control
{
    private static readonly Color PlayerColor = new(1f, 0.93f, 0.55f);
    private static readonly Color MemberColor = new(0.40f, 0.82f, 1f);
    private static readonly Color DeadColor = new(0.62f, 0.62f, 0.62f);
    private static readonly Color Outline = new(0.04f, 0.03f, 0.02f);
    private static readonly Color GateFill = new(0.95f, 0.80f, 0.35f, 0.45f);
    private static readonly Color GateEdge = new(1f, 0.85f, 0.40f);
    private static readonly Color Back = new(0.05f, 0.05f, 0.06f, 0.92f);

    /// <summary>Raio de clique e de dica sobre uma marca, em pixels.</summary>
    private const float HitRadius = 7f;

    private readonly List<(Vector2 At, string Name)> _drawnMarks = new();

    /// <summary>O que desenhar (o mesmo estado para o mini mapa e o mapa grande).</summary>
    public MiniMapState State { get; set; } = new();

    /// <summary>True = a zona inteira (mapa grande); false = o recorte em volta do jogador (mini mapa).</summary>
    public bool Whole { get; set; }

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Pass;
    }

    /// <summary>Tamanho que o modo inteiro pode ocupar (pixels); o canvas usa a maior escala inteira que caiba.</summary>
    public NVector2 Available { get; set; } = new(800, 600);

    /// <summary>Recorte atual, conforme o modo.</summary>
    public MapFrame Frame()
    {
        if (Whole)
        {
            return MiniMapView.Whole(State.Width, State.Height, Available);
        }

        var center = State.Player ?? new NVector2(State.Width / 2f, State.Height / 2f);
        return MiniMapView.Mini(State.Width, State.Height, center);
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        _drawnMarks.Clear();
        DrawRect(new Rect2(Vector2.Zero, Size), Back);
        if (State.Terrain is null || State.Width <= 0)
        {
            return;
        }

        var frame = Frame();
        var offset = ToGodot(MiniMapView.CenterOffset(frame, ToNumerics(Size)));
        Vector2 At(NVector2 tiles) => offset + ToGodot(frame.ToPixels(tiles));

        DrawTextureRectRegion(State.Terrain, new Rect2(offset, ToGodot(frame.PixelSize)), new Rect2(ToGodot(frame.Origin), ToGodot(frame.Size)));

        foreach (var gate in State.Gates)
        {
            var from = At(new NVector2((float)gate.X, (float)gate.Y));
            var to = At(new NVector2((float)(gate.X + gate.Width), (float)(gate.Y + gate.Height)));
            var rect = new Rect2(from, (to - from).Max(new Vector2(2, 2)));
            DrawRect(rect, GateFill);
            DrawRect(rect, GateEdge, filled: false, width: 1f);
        }

        var radius = Whole ? 4.5f : 3.5f;
        foreach (var member in State.Members)
        {
            var pixel = ToNumerics(At(member.Position) - offset);
            var (pinned, inside) = MiniMapView.PinToEdge(pixel, frame.PixelSize, radius + 2);
            var point = offset + ToGodot(pinned);
            var color = member.Alive ? MemberColor : DeadColor;
            if (inside)
            {
                DrawMark(point, radius, color, dead: !member.Alive);
            }
            else
            {
                DrawArrow(point, ToGodot(pixel - pinned).Normalized(), radius + 1.5f, color);
            }

            _drawnMarks.Add((point, member.Alive ? member.Name : $"{member.Name} (morto)"));
        }

        if (State.Player is { } player)
        {
            var point = At(player);
            DrawMark(point, radius + 0.5f, PlayerColor, dead: false);
            _drawnMarks.Add((point, "Você"));
        }
    }

    public override string _GetTooltip(Vector2 atPosition)
    {
        foreach (var (at, name) in _drawnMarks)
        {
            if (at.DistanceTo(atPosition) <= HitRadius)
            {
                return name;
            }
        }

        return "";
    }

    private void DrawMark(Vector2 at, float radius, Color color, bool dead)
    {
        DrawCircle(at, radius + 1.5f, Outline);
        DrawCircle(at, radius, color);
        if (dead)
        {
            var d = radius * 0.7f;
            DrawLine(at + new Vector2(-d, -d), at + new Vector2(d, d), Outline, 1.5f);
            DrawLine(at + new Vector2(-d, d), at + new Vector2(d, -d), Outline, 1.5f);
        }
    }

    private void DrawArrow(Vector2 at, Vector2 direction, float size, Color color)
    {
        if (direction == Vector2.Zero)
        {
            direction = Vector2.Up;
        }

        var side = new Vector2(-direction.Y, direction.X);
        var tip = at + direction * size;
        var back = at - direction * size * 0.6f;
        Vector2[] outline = [tip + direction * 1.5f, back + side * (size + 1.5f) - direction, back - side * (size + 1.5f) - direction];
        Vector2[] fill = [tip, back + side * size, back - side * size];
        DrawColoredPolygon(outline, Outline);
        DrawColoredPolygon(fill, color);
    }

    private static Vector2 ToGodot(NVector2 v) => new(v.X, v.Y);

    private static NVector2 ToNumerics(Vector2 v) => new(v.X, v.Y);
}
