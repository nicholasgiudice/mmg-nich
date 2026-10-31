using Godot;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Account;

/// <summary>Prévia animada do sprite de uma classe (andando para baixo), na criação de personagem.</summary>
public partial class SpritePreview : Control
{
    private const int Zoom = 3;

    private SpriteDefinition? _sprite;
    private double _time;

    public SpriteDefinition? Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            QueueRedraw();
        }
    }

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        CustomMinimumSize = new Godot.Vector2(64 * Zoom, 64 * Zoom);
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Godot.Vector2.Zero, Size), new Color(0.13f, 0.17f, 0.12f));
        if (_sprite?.For("walk") is not { } walk)
        {
            return;
        }

        var strip = walk.For(SpriteDirection.Down);
        var frame = strip.Frame((int)(_time * walk.Fps) % strip.Count);
        var feet = new Godot.Vector2(Size.X / 2, Size.Y - 8);
        DrawTextureRectRegion(_sprite.Texture, new Rect2(feet - strip.Pivot * Zoom, frame.Size * Zoom), frame);
    }
}
