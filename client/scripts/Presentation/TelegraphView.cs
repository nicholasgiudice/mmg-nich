using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Aviso no chão de um ataque que vai resolver (círculo, cone ou linha). O contorno mostra a área inteira
/// desde o início; o preenchimento cresce do centro/origem até a borda no tick de resolução.
/// O contorno é desenhado num filho por cima de unidades e efeitos (T-054); o preenchimento fica no chão.
/// </summary>
public partial class TelegraphView : Node2D
{
    private static readonly Color Hostile = new(1.00f, 0.35f, 0.20f);
    private static readonly Color Friendly = new(0.35f, 0.75f, 1.00f);

    private TelegraphState? _state;
    private Vector2 _direction = Vector2.Down;
    private float _progress;
    private bool _hostile = true;
    private readonly EdgeView _edge = new() { ZAsRelative = false, ZIndex = ZLayers.TelegraphEdges };

    public override void _Ready() => AddChild(_edge);

    /// <summary>Atualiza forma e progresso (0..1). Hostil = pode atingir o jogador local.</summary>
    public void Apply(TelegraphState state, Vector2 originPx, Vector2 direction, float progress, bool hostile)
    {
        ZIndex = ZLayers.Telegraphs;
        Position = originPx;
        _state = state;
        _direction = direction.LengthSquared() > 1e-6f ? direction.Normalized() : Vector2.Down;
        _progress = Mathf.Clamp(progress, 0f, 1f);
        _hostile = hostile;
        QueueRedraw();
        _edge.QueueRedraw();
    }

    public override void _Draw()
    {
        if (_state is null)
        {
            return;
        }

        var color = _hostile ? Hostile : Friendly;
        var area = new Color(color, 0.10f);
        var fill = new Color(color, 0.22f + 0.18f * _progress);
        const float s = Units.PixelsPerUnit;

        switch (_state.Shape)
        {
            case TelegraphShape.Circle:
                var r = _state.Radius * s;
                DrawCircle(Vector2.Zero, r, area);
                DrawCircle(Vector2.Zero, r * _progress, fill);
                break;

            case TelegraphShape.Cone:
                var len = _state.Length * s;
                var half = Mathf.DegToRad(_state.AngleDegrees) / 2f;
                DrawColoredPolygon(Sector(len, half), area);
                if (_progress > 0.01f)
                {
                    DrawColoredPolygon(Sector(len * _progress, half), fill);
                }

                break;

            default: // linha (investida): retângulo a partir da origem
                var length = _state.Length * s;
                var width = Mathf.Max(_state.Width, 0.2f) * s;
                DrawColoredPolygon(Rect(length, width), area);
                if (_progress > 0.01f)
                {
                    DrawColoredPolygon(Rect(length * _progress, width), fill);
                }

                break;
        }
    }

    /// <summary>Contorno (e a seta da investida), desenhado pelo filho <see cref="EdgeView"/>.</summary>
    private void DrawEdge(CanvasItem canvas)
    {
        if (_state is null)
        {
            return;
        }

        var outline = new Color(_hostile ? Hostile : Friendly, 0.85f);
        const float s = Units.PixelsPerUnit;
        switch (_state.Shape)
        {
            case TelegraphShape.Circle:
                canvas.DrawArc(Vector2.Zero, _state.Radius * s, 0, Mathf.Tau, 48, outline, 2f);
                break;

            case TelegraphShape.Cone:
                var edge = Sector(_state.Length * s, Mathf.DegToRad(_state.AngleDegrees) / 2f);
                canvas.DrawPolyline([.. edge, edge[0]], outline, 2f);
                break;

            default:
                var length = _state.Length * s;
                var width = Mathf.Max(_state.Width, 0.2f) * s;
                var rect = Rect(length, width);
                canvas.DrawPolyline([.. rect, rect[0]], outline, 2f);
                if (_state.Charge)
                {
                    // Seta na ponta: a fonte vai se deslocar até ali.
                    var tip = _direction * length;
                    var side = _direction.Orthogonal() * width * 0.35f;
                    canvas.DrawLine(tip, tip - _direction * width * 0.5f + side, outline, 2f);
                    canvas.DrawLine(tip, tip - _direction * width * 0.5f - side, outline, 2f);
                }

                break;
        }
    }

    private sealed partial class EdgeView : Node2D
    {
        public override void _Draw() => (GetParent() as TelegraphView)?.DrawEdge(this);
    }

    private Vector2[] Sector(float radius, float halfAngle)
    {
        const int segments = 20;
        var points = new Vector2[segments + 2];
        points[0] = Vector2.Zero;
        var baseAngle = _direction.Angle();
        for (var i = 0; i <= segments; i++)
        {
            var a = baseAngle - halfAngle + 2 * halfAngle * i / segments;
            points[i + 1] = Vector2.FromAngle(a) * radius;
        }

        return points;
    }

    private Vector2[] Rect(float length, float width)
    {
        var side = _direction.Orthogonal() * width / 2f;
        var tip = _direction * length;
        return [side, tip + side, tip - side, -side];
    }
}
