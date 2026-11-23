using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Coluna de luz que cai sobre o alvo (T-424: Julgamento): desce do alto em pouco tempo, clareia o chão nos pés e some.
/// Só visual; fica onde o alvo estava.
/// </summary>
public partial class PillarView : Node2D
{
    private const double LifetimeSeconds = 0.55;
    private const float HeightPx = 150f;

    private LightPillar _pillar = new(Colors.White);
    private float _width = 20f;
    private double _age;

    public static PillarView Create(Vector2 feetPx, float headPx, LightPillar pillar) => new()
    {
        Position = feetPx,
        _pillar = pillar,
        _width = Math.Max(16f, headPx * 0.55f),
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
        var fall = Math.Min(1f, k / 0.25f); // desce no primeiro quarto
        var alpha = k < 0.25f ? 1f : 1f - (k - 0.25f) / 0.75f;
        var top = -HeightPx;
        var bottom = top + HeightPx * fall;
        var w = _width * (k < 0.25f ? 1f : 1f - 0.5f * (k - 0.25f) / 0.75f);
        var c = _pillar.Color;
        DrawRect(new Rect2(-w / 2 - 4, top, w + 8, bottom - top), new Color(c, 0.25f * alpha));
        DrawRect(new Rect2(-w / 2, top, w, bottom - top), new Color(c, 0.7f * alpha));
        DrawRect(new Rect2(-w / 6, top, w / 3, bottom - top), new Color(1f, 1f, 1f, 0.9f * alpha));
        if (fall >= 1f)
        {
            DrawSetTransform(Vector2.Zero, 0, new Vector2(1, 0.45f));
            DrawCircle(Vector2.Zero, w * 1.3f, new Color(c, 0.45f * alpha));
            DrawArc(Vector2.Zero, w * (1.3f + k), 0, Mathf.Tau, 32, new Color(c, 0.9f * alpha), 2.5f);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
    }
}
