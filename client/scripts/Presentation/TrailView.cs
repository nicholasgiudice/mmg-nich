using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>Rastro curto de um deslocamento forçado (dash, investida, empurrão) que some sozinho.</summary>
public partial class TrailView : Node2D
{
    private const double DefaultLifetime = 0.45;

    private Vector2 _from;
    private Vector2 _to;
    private float _width;
    private Color _color;
    private double _age;
    private double _lifetime = DefaultLifetime;

    /// <param name="lifetime">Segundos até sumir (padrão 0,45; saltos da Corrente duram mais para serem lidos).</param>
    public static TrailView Create(Vector2 fromPx, Vector2 toPx, float widthPx, Color color, double lifetime = DefaultLifetime) =>
        new() { _from = fromPx, _to = toPx, _width = widthPx, _color = color, _lifetime = lifetime, ZIndex = ZLayers.Trails };

    public override void _Process(double delta)
    {
        _age += delta;
        if (_age >= _lifetime)
        {
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var a = 1f - (float)(_age / _lifetime);
        DrawLine(_from, _to, new Color(_color, 0.35f * a), _width);
        DrawLine(_from, _to, new Color(Colors.White, 0.5f * a), Mathf.Max(1f, _width * 0.2f));
    }
}
