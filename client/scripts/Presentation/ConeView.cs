using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Cone de fogo ao usar uma habilidade em arco (T-444, Rajada de Chamas): sai de quem usou na direção dele, cresce até o
/// alcance em pouco tempo e some. Línguas de fogo na borda. Só visual.
/// </summary>
public partial class ConeView : Node2D
{
    private const double LifetimeSeconds = 0.5;
    private const int Segments = 16;

    private Color _color = Colors.Orange;
    private float _rangePx;
    private float _halfAngle;
    private double _age;

    public static ConeView Create(Vector2 originPx, Vector2 direction, float rangeTiles, float angleDegrees, Color color) => new()
    {
        Position = originPx,
        Rotation = direction.Angle(),
        _color = color,
        _rangePx = rangeTiles * Units.PixelsPerUnit,
        _halfAngle = Mathf.DegToRad(angleDegrees) / 2f,
        ZAsRelative = false,
        ZIndex = ZLayers.Effects,
    };

    public override void _Process(double delta)
    {
        _age += delta;
        if (_age >= LifetimeSeconds)
        {
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var k = (float)(_age / LifetimeSeconds);
        var grow = Math.Min(k * 3f, 1f);
        var alpha = k < 0.4f ? 1f : 1f - (k - 0.4f) / 0.6f;
        Wedge(_rangePx * grow, new Color(_color, 0.45f * alpha));
        Wedge(_rangePx * grow * 0.6f, new Color(1f, 0.85f, 0.35f, 0.55f * alpha));
        for (var i = 0; i <= 6; i++)
        {
            var a = -_halfAngle + i * 2 * _halfAngle / 6;
            var r = _rangePx * grow;
            var flicker = 0.7f + 0.3f * Mathf.Sin((float)_age * 30f + i * 1.7f);
            var tip = Vector2.FromAngle(a) * (r + 10f * flicker);
            var side = Vector2.FromAngle(a + Mathf.Pi / 2) * 5f;
            var at = Vector2.FromAngle(a) * r * 0.92f;
            DrawColoredPolygon([at - side, at + side, tip], new Color(_color.Lightened(0.15f), 0.9f * alpha));
        }
    }

    private void Wedge(float r, Color color)
    {
        if (r < 1f)
        {
            return;
        }

        var points = new Vector2[Segments + 2];
        points[0] = Vector2.Zero;
        for (var i = 0; i <= Segments; i++)
        {
            points[i + 1] = Vector2.FromAngle(-_halfAngle + i * 2 * _halfAngle / Segments) * r;
        }

        DrawColoredPolygon(points, color);
    }
}
