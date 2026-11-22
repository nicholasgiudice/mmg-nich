using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Linhas entre duas unidades enquanto um status as liga (T-412: a Interceptação, do protegido até o Guardião que recebe o
/// dano desviado). Refeitas a cada frame pela <see cref="WorldView"/>, com as posições já interpoladas.
/// </summary>
public partial class LinkView : Node2D
{
    private readonly List<(Vector2 From, Vector2 To, Color Color)> _links = new();
    private double _time;

    /// <summary>Altura (pixels) acima dos pés em que a linha passa: mais ou menos o peito.</summary>
    public const float ChestPx = 18f;

    public override void _Ready()
    {
        ZAsRelative = false;
        ZIndex = ZLayers.Effects;
    }

    public void Begin() => _links.Clear();

    public void Add(Vector2 fromFeet, Vector2 toFeet, Color color) =>
        _links.Add((fromFeet - new Vector2(0, ChestPx), toFeet - new Vector2(0, ChestPx), color));

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var (from, to, color) in _links)
        {
            var length = from.DistanceTo(to);
            if (length < 1f)
            {
                continue;
            }

            // Brilho largo por baixo, corrente de traços andando do protegido para o protetor por cima.
            var pulse = 0.5f + 0.5f * Mathf.Sin((float)_time * 5f);
            DrawLine(from, to, new Color(Colors.Black, 0.35f), 6f);
            DrawLine(from, to, new Color(color, 0.30f + 0.15f * pulse), 9f);
            const float dash = 9f;
            const float gap = 5f;
            var dir = (to - from) / length;
            var offset = (float)(_time * 30f % (dash + gap));
            for (var d = offset - (dash + gap); d < length; d += dash + gap)
            {
                var a = Math.Max(d, 0f);
                var b = Math.Min(d + dash, length);
                if (b > a)
                {
                    DrawLine(from + dir * a, from + dir * b, color.Lightened(0.25f), 3f);
                }
            }

            DrawCircle(from, 4f, color);
            DrawCircle(to, 4f, color);
        }
    }
}
