using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>Texto curto que sobe e some acima de uma unidade (ex.: motivo de rejeição de habilidade).</summary>
public partial class FloatingText : Node2D
{
    private const double Lifetime = 1.0;
    private const float RisePixels = 18f;

    private readonly Label _label = new();
    private double _age;

    public static FloatingText Create(string text, Color color)
    {
        var node = new FloatingText { ZIndex = 10 };
        node._label.Text = text;
        node._label.Modulate = color;
        return node;
    }

    public override void _Ready()
    {
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.AddThemeFontSizeOverride("font_size", 12);
        _label.AddThemeConstantOverride("outline_size", 4);
        _label.AddThemeColorOverride("font_outline_color", Colors.Black);
        _label.Size = new Vector2(160, 18);
        _label.Position = new Vector2(-80, -9);
        AddChild(_label);
    }

    public override void _Process(double delta)
    {
        _age += delta;
        Position += new Vector2(0, -(float)(RisePixels * delta / Lifetime));
        var a = 1f - (float)(_age / Lifetime);
        Modulate = new Color(1, 1, 1, Mathf.Clamp(a * 2f, 0f, 1f));
        if (_age >= Lifetime)
        {
            QueueFree();
        }
    }
}
