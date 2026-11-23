using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Meteoro sobre o aviso no chão (T-444): uma pedra em chamas cai de cima, na diagonal, e chega ao chão no fim do aviso
/// (<see cref="SetProgress"/> de 0 a 1). Quem cria põe a explosão quando o aviso some.
/// </summary>
public partial class MeteorView : Node2D
{
    /// <summary>De onde a pedra sai, em relação ao ponto do aviso (pixels).</summary>
    private static readonly Vector2 Start = new(-140f, -420f);

    private Color _color = Colors.OrangeRed;
    private float _progress;
    private double _time;

    public static MeteorView Create(Color color) => new() { _color = color, ZAsRelative = false, ZIndex = ZLayers.Effects };

    public void SetProgress(Vector2 groundPx, float progress)
    {
        Position = groundPx;
        _progress = Mathf.Clamp(progress, 0f, 1f);
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Só aparece na segunda metade do aviso e acelera até o chão.
        var k = Mathf.Clamp((_progress - 0.35f) / 0.65f, 0f, 1f);
        if (k <= 0f)
        {
            return;
        }

        var at = Start * (1f - k * k);
        var dir = (-Start).Normalized();
        var size = 12f + 6f * k;
        for (var i = 6; i >= 1; i--)
        {
            var f = i / 6f;
            DrawCircle(at - dir * size * 1.1f * i, size * (1f - f * 0.75f), new Color(_color, 0.6f * (1f - f)));
        }

        var flicker = 0.9f + 0.1f * Mathf.Sin((float)_time * 35f);
        DrawCircle(at, size * 1.5f * flicker, new Color(_color, 0.35f));
        DrawCircle(at, size, new Color(0.35f, 0.2f, 0.15f));
        DrawCircle(at + new Vector2(-size * 0.25f, -size * 0.25f), size * 0.55f, _color.Lightened(0.3f));
    }
}
