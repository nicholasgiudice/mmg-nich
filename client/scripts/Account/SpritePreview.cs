using Godot;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Account;

/// <summary>
/// Prévia do sprite de uma classe olhando para baixo: na criação de personagem e nos cartões da seleção (T-074).
/// Com <see cref="Backdrop"/>, o fundo é essa textura em 1× (o nicho de pedra) e o tamanho é o dela; sem ele, um quadrado
/// verde-escuro. Com <see cref="Animate"/> desligado, fica parado no primeiro quadro.
/// </summary>
public partial class SpritePreview : Control
{
    private SpriteDefinition? _sprite;
    private double _time;

    /// <summary>Escala inteira do sprite.</summary>
    public int Zoom { get; init; } = 3;

    /// <summary>Fundo em 1× (nicho de pedra, T-074); null = quadrado liso de 64 × <see cref="Zoom"/>.</summary>
    public Texture2D? Backdrop { get; init; }

    /// <summary>Distância dos pés à borda de baixo, em pixels da tela.</summary>
    public float FeetInset { get; init; } = 8;

    public bool Animate { get; set; } = true;

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
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = Backdrop?.GetSize() ?? new Godot.Vector2(64 * Zoom, 64 * Zoom);
    }

    public override void _Process(double delta)
    {
        if (Animate)
        {
            _time += delta;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (Backdrop is { } backdrop)
        {
            DrawTexture(backdrop, ((Size - backdrop.GetSize()) / 2).Floor());
        }
        else
        {
            DrawRect(new Rect2(Godot.Vector2.Zero, Size), new Color(0.13f, 0.17f, 0.12f));
        }

        if (_sprite?.For("walk") is not { } walk)
        {
            return;
        }

        var strip = walk.For(SpriteDirection.Down);
        var index = Animate ? (int)(_time * walk.Fps) % strip.Count : 0;
        var frame = strip.Frame(index);
        var feet = new Godot.Vector2(Mathf.Floor(Size.X / 2), Size.Y - FeetInset);
        DrawTextureRectRegion(_sprite.Texture, new Rect2(feet - strip.Pivot * Zoom, frame.Size * Zoom), frame);
    }
}
