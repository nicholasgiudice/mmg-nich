using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Onda que sai do lançador ao usar uma habilidade em volta dele (T-412: Bastião, Juramento): um anel achatado que cresce
/// até o raio da habilidade e some. Só visual; fica onde o lançador estava.
/// </summary>
public partial class BurstView : Node2D
{
    private const double LifetimeSeconds = 0.6;

    private CastBurst _burst = new(Colors.White, 1f);
    private double _age;

    public static BurstView Create(Vector2 feetPx, CastBurst burst) => new()
    {
        Position = feetPx,
        _burst = burst,
        ZAsRelative = false,
        ZIndex = ZLayers.Areas,
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
        var ease = 1f - (1f - k) * (1f - k);
        var r = _burst.Radius * Units.PixelsPerUnit * ease;
        var alpha = 1f - k;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1, 0.5f));
        DrawCircle(Vector2.Zero, r, new Color(_burst.Color, 0.18f * alpha));
        DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, new Color(_burst.Color, 0.9f * alpha), 3f);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
