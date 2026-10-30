using Game.Core.Combat;
using Game.Core.Combat.Items;
using Godot;
using MmoRpgClient.Hud;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Item no chão (T-222): losango na cor da raridade, esmaecido quando o item é de outro. O nome aparece com o mouse em
/// cima e no item que a tecla F pegaria (o mais perto ao alcance, com "[F]" e borda branca). Raro e épico pulsam.
/// </summary>
public partial class GroundItemView : Node2D
{
    public const float RadiusPx = 9f;

    private readonly Label _name = new();
    private GroundItem? _item;
    private bool _mine;
    private bool _next;
    private bool _hovered;
    private double _time;

    public override void _Ready()
    {
        ZIndex = ZLayers.Areas;
        _name.HorizontalAlignment = HorizontalAlignment.Center;
        _name.Position = new Vector2(-80, -RadiusPx - 22);
        _name.Size = new Vector2(160, 16);
        _name.AddThemeFontSizeOverride("font_size", 10);
        _name.AddThemeConstantOverride("outline_size", 4);
        _name.AddThemeColorOverride("font_outline_color", Colors.Black);
        AddChild(_name);
    }

    /// <param name="next">É o item que a tecla F pegaria agora.</param>
    public void Apply(GroundItem item, Vector2 pixels, ContentCatalog content, bool mine, bool next, bool hovered)
    {
        Position = pixels;
        if (!ReferenceEquals(_item, item) || _mine != mine || _next != next || _hovered != hovered)
        {
            _item = item;
            _mine = mine;
            _next = next;
            _hovered = hovered;
            _name.Text = ItemText.Name(content, item.Item) + (next ? "  [F]" : "") + (mine ? "" : " (de outro)");
            _name.AddThemeColorOverride("font_color", ItemText.RarityColor(item.Item.Rarity));
            _name.Modulate = new Color(1, 1, 1, mine ? 1f : 0.6f);
            _name.Visible = next || hovered;
            ZIndex = next || hovered ? ZLayers.Units + 1 : ZLayers.Areas;
            QueueRedraw();
        }
    }

    public override void _Process(double delta)
    {
        if (_item?.Item.Rarity >= Game.Core.Definitions.ItemRarity.Rare)
        {
            _time += delta;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_item is null)
        {
            return;
        }

        var color = ItemText.RarityColor(_item.Item.Rarity);
        var alpha = _mine ? 1f : 0.4f;
        var r = RadiusPx * (_hovered ? 1.25f : 1f);
        if (_item.Item.Rarity >= Game.Core.Definitions.ItemRarity.Rare)
        {
            var pulse = 0.5f + (0.5f * Mathf.Sin((float)_time * 4f));
            DrawCircle(Vector2.Zero, r * (1.6f + (0.3f * pulse)), new Color(color, 0.18f * alpha));
        }

        var diamond = new[] { new Vector2(0, -r), new Vector2(r, 0), new Vector2(0, r), new Vector2(-r, 0) };
        DrawColoredPolygon(diamond, new Color(color, 0.85f * alpha));
        DrawPolyline([.. diamond, diamond[0]], new Color(_next ? Colors.White : Colors.Black, alpha), _next ? 2f : 1.5f);
    }
}
