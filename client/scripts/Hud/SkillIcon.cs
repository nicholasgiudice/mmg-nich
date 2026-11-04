using Godot;

namespace MmoRpgClient.Hud;

/// <summary>Ícone da tela de habilidades (T-081): arrastável para a barra quando a habilidade pode ir para lá.</summary>
public partial class SkillIcon : Control
{
    public Texture2D? Icon { get; set; }

    /// <summary>Id arrastado; null = não arrasta (não aprendida).</summary>
    public string? AbilityId { get; set; }

    public string Fallback { get; set; } = "";

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Nearest;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = AbilityId is null ? CursorShape.Arrow : CursorShape.Drag;
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, new Color(0.18f, 0.20f, 0.26f));
        if (Icon is not null)
        {
            DrawTextureRect(Icon, rect.Grow(-3), tile: false);
        }
        else
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(0, Size.Y / 2 + 5), Fallback.Length > 2 ? Fallback[..2] : Fallback,
                HorizontalAlignment.Center, Size.X, 14, Colors.White);
        }

        DrawRect(rect, Colors.Black, filled: false);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (AbilityId is null)
        {
            return default;
        }

        SetDragPreview(AbilitySlot.DragPreview(Icon, Fallback));
        return new Godot.Collections.Dictionary { [AbilitySlot.DragAbility] = AbilityId, [AbilitySlot.DragFrom] = -1 };
    }
}
