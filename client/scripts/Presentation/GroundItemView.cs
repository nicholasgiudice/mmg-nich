using Game.Core.Combat;
using Game.Core.Combat.Items;
using Godot;
using MmoRpgClient.Hud;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Item no chão (T-222; ícone na T-084): o ícone do item (T-080) no tamanho da folha, sem borrão, com um contorno e um
/// brilho na cor da raridade e uma sombra por baixo, para ler sobre grama e terra. Esmaecido quando o item é de outro.
/// O nome aparece com o mouse em cima e no item que a tecla F pegaria (o mais perto ao alcance, com "[F]" e borda
/// branca). Raro e épico pulsam. Ao aparecer, o item salta e assenta (só visual: a posição é a do servidor).
/// Sem ícone, o losango de antes.
/// </summary>
public partial class GroundItemView : Node2D
{
    public const float RadiusPx = 9f;

    /// <summary>Duração do salto ao cair.</summary>
    private const double DropSeconds = 0.5;

    /// <summary>Altura do primeiro salto, em pixels.</summary>
    private const float DropHeightPx = 14f;

    private static readonly Vector2[] OutlineOffsets = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];

    /// <summary>Silhueta branca de cada ícone (para o contorno na cor da raridade), feita uma vez por item.</summary>
    private static readonly Dictionary<string, Texture2D> s_silhouettes = new(StringComparer.Ordinal);

    private readonly Label _name = new();
    private GroundItem? _item;
    private bool _mine;
    private bool _next;
    private bool _hovered;
    private double _time;
    private double _age;

    public override void _Ready()
    {
        ZIndex = ZLayers.Areas;
        TextureFilter = TextureFilterEnum.Nearest;
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
        var dropping = _age < DropSeconds;
        _age += delta;
        if (dropping || _item?.Item.Rarity >= Game.Core.Definitions.ItemRarity.Rare)
        {
            _time += delta;
            QueueRedraw();
        }
    }

    /// <summary>Altura do item acima do chão durante a queda: um salto alto e um pequeno, depois parado.</summary>
    private float Lift()
    {
        var t = (float)(_age / DropSeconds);
        if (t >= 1f)
        {
            return 0f;
        }

        return t < 0.7f
            ? DropHeightPx * Mathf.Sin(Mathf.Pi * t / 0.7f)
            : DropHeightPx * 0.25f * Mathf.Sin(Mathf.Pi * (t - 0.7f) / 0.3f);
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

        if (IconCatalog.Shared.For(_item.Item.BaseId) is { } icon)
        {
            DrawIcon(icon, Silhouette(_item.Item.BaseId, icon), color, alpha);
            return;
        }

        var diamond = new[] { new Vector2(0, -r), new Vector2(r, 0), new Vector2(0, r), new Vector2(-r, 0) };
        DrawColoredPolygon(diamond, new Color(color, 0.85f * alpha));
        DrawPolyline([.. diamond, diamond[0]], new Color(_next ? Colors.White : Colors.Black, alpha), _next ? 2f : 1.5f);
    }

    /// <summary>
    /// Ícone no tamanho da folha (sem escala, sem borrão), sobre sombra e brilho; o contorno é o próprio ícone tingido na
    /// cor da raridade, desenhado 1 px para cada lado por baixo. O próximo da tecla F ganha um anel branco.
    /// </summary>
    private void DrawIcon(Texture2D icon, Texture2D silhouette, Color color, float alpha)
    {
        var size = icon.GetSize();
        var lift = Lift();
        var shadowScale = 1f - (lift / DropHeightPx * 0.4f);
        DrawSetTransform(new Vector2(0, size.Y * 0.38f), 0, new Vector2(1f, 0.35f) * shadowScale);
        DrawCircle(Vector2.Zero, size.X * 0.42f, new Color(0, 0, 0, 0.45f * alpha));
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);

        var origin = new Vector2(-size.X / 2, (-size.Y / 2) - lift).Round();
        DrawCircle(origin + (size / 2), size.X * 0.5f, new Color(color, (_hovered ? 0.4f : 0.25f) * alpha));
        var outline = new Color(_next ? Colors.White : color, alpha);
        foreach (var offset in OutlineOffsets)
        {
            DrawTextureRect(silhouette, new Rect2(origin + offset, size), tile: false, outline);
        }

        DrawTextureRect(icon, new Rect2(origin, size), tile: false, new Color(1, 1, 1, alpha));
        if (_next)
        {
            DrawArc(origin + (size / 2), size.X * 0.62f, 0, Mathf.Tau, 32, Colors.White, 1.5f);
        }
    }

    /// <summary>O ícone com todo pixel visível branco (a transparência fica), para tingir com a cor da raridade.</summary>
    private static Texture2D Silhouette(string id, Texture2D icon)
    {
        if (s_silhouettes.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var image = icon.GetImage();
        if (image is null)
        {
            return s_silhouettes[id] = icon;
        }

        image.Decompress();
        image.Convert(Image.Format.Rgba8);
        for (var y = 0; y < image.GetHeight(); y++)
        {
            for (var x = 0; x < image.GetWidth(); x++)
            {
                var a = image.GetPixel(x, y).A;
                image.SetPixel(x, y, new Color(1, 1, 1, a > 0.1f ? 1f : 0f));
            }
        }

        return s_silhouettes[id] = ImageTexture.CreateFromImage(image);
    }
}
