using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Prancha de conferência dos ícones (T-080, cena <c>res://scenes/tests/IconSheet.tscn</c>): todos os ícones de
/// <c>icons.json</c> ampliados 2×, com o id embaixo. Só para revisão; não entra no jogo.
/// </summary>
public partial class IconSheet : Control
{
    private const int Zoom = 2;

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        AddChild(new ColorRect { Color = new Color(0.13f, 0.14f, 0.17f), AnchorRight = 1, AnchorBottom = 1 });

        var icons = IconCatalog.Load();
        if (icons.Index is not { } index)
        {
            AddChild(new Label { Text = "icons.json não encontrado", Position = new Vector2(24, 24) });
            return;
        }

        var scroll = new ScrollContainer { AnchorRight = 1, AnchorBottom = 1 };
        AddChild(scroll);
        var grid = new GridContainer { Columns = 8 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(grid);

        foreach (var id in index.Icons.Keys)
        {
            var cell = new VBoxContainer { CustomMinimumSize = new Vector2(140, 0) };
            cell.AddChild(new TextureRect
            {
                Texture = icons.For(id),
                CustomMinimumSize = new Vector2(index.Size * Zoom, index.Size * Zoom),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            });
            var label = new Label { Text = id, HorizontalAlignment = HorizontalAlignment.Center };
            label.AddThemeFontSizeOverride("font_size", 11);
            cell.AddChild(label);
            grid.AddChild(cell);
        }
    }
}
