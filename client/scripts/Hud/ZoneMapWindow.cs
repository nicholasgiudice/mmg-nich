using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Mapa grande da zona (T-392): a zona inteira no centro da tela, numa janela de pedra e ouro, com as mesmas marcas do
/// mini mapa. Tecla M ou Esc fecha.
/// </summary>
public partial class ZoneMapWindow : PanelContainer
{
    private readonly MiniMapCanvas _canvas = new() { Whole = true };
    private readonly Label _title = new() { HorizontalAlignment = HorizontalAlignment.Center };

    public MiniMapState State
    {
        get => _canvas.State;
        set => _canvas.State = value;
    }

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", HudTheme.WindowFrame() ?? new StyleBoxFlat
        {
            BgColor = HudTheme.Panel,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 10,
        });

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        AddChild(box);

        _title.AddThemeFontSizeOverride("font_size", 16);
        _title.AddThemeColorOverride("font_color", HudTheme.Suggested);
        _title.AddThemeColorOverride("font_outline_color", Colors.Black);
        _title.AddThemeConstantOverride("outline_size", 4);
        if (HudTheme.WindowTitlePlate() is { } plate)
        {
            var holder = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            holder.AddThemeStyleboxOverride("panel", plate);
            holder.AddChild(_title);
            box.AddChild(holder);
        }
        else
        {
            box.AddChild(_title);
        }

        _canvas.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        box.AddChild(_canvas);

        var hint = new Label { Text = "M ou Esc fecha", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.7f) };
        hint.AddThemeFontSizeOverride("font_size", 11);
        box.AddChild(hint);
    }

    /// <summary>Abre com o nome da zona, cabendo em <paramref name="screen"/> (pixels da tela).</summary>
    public void Open(string zoneName, Vector2 screen)
    {
        _title.Text = $"Mapa: {zoneName}";
        _canvas.Available = new System.Numerics.Vector2(screen.X * 0.8f - 48, screen.Y * 0.8f - 90);
        var frame = _canvas.Frame();
        _canvas.CustomMinimumSize = new Vector2(frame.PixelSize.X, frame.PixelSize.Y);
        Visible = true;
        MoveToFront(); // por cima do painel do chefe e das outras telas
        ResetSize();
    }

    public void Close() => Visible = false;
}
