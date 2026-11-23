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
        if (_burst.Blast)
        {
            DrawBlast(k);
            return;
        }

        var ease = 1f - (1f - k) * (1f - k);
        var r = _burst.Radius * Units.PixelsPerUnit * ease;
        var alpha = 1f - k;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1, 0.5f));
        DrawCircle(Vector2.Zero, r, new Color(_burst.Color, 0.18f * alpha));
        DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, new Color(_burst.Color, 0.9f * alpha), 3f);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>
    /// Explosão (T-444): disco no chão que cresce rápido até o raio e some, com o miolo claro, mais um clarão redondo no
    /// meio que encolhe. Lida de longe pelo tamanho e pela cor; sem detalhe.
    /// </summary>
    private void DrawBlast(float k)
    {
        var grow = 1f - (1f - Math.Min(k * 2.2f, 1f)) * (1f - Math.Min(k * 2.2f, 1f));
        var r = _burst.Radius * Units.PixelsPerUnit * grow;
        var alpha = 1f - k * k;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1, 0.5f));
        DrawCircle(Vector2.Zero, r, new Color(_burst.Color, 0.45f * alpha));
        DrawCircle(Vector2.Zero, r * 0.6f, new Color(_burst.Color.Lightened(0.35f), 0.55f * alpha));
        DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, new Color(_burst.Color.Darkened(0.15f), 0.95f * alpha), 3f);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        var flash = Units.PixelsPerUnit * 0.9f * (1f - k);
        DrawCircle(new Vector2(0, -12), flash, new Color(1f, 0.92f, 0.6f, 0.85f * alpha));
    }
}
