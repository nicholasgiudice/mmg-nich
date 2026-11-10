using Game.Core.Combat.Shops;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// NPC da cidade (T-323; entidade da T-321, sem vida nem facção): o sprite parado, de frente, e o nome em cima. NPC com loja
/// tem o nome dourado e um saco de moedas ao lado do nome; com o mouse em cima, um anel no chão. Sem sprite, um círculo.
/// </summary>
public partial class NpcView : Node2D
{
    /// <summary>Raio (pixels) para escolher o NPC sob o cursor: o corpo inteiro, não só os pés.</summary>
    public const float PickRadiusPx = 22f;

    private static readonly Color ShopName = new(1f, 0.82f, 0.30f);
    private static readonly Color PlainName = new(0.85f, 0.9f, 1f);

    private readonly Label _name = new();
    private SpriteDefinition? _sprite;
    private bool _hovered;
    private bool _hasShop;

    /// <summary>Altura (pixels) do pé à cabeça: onde fica o nome e o meio do corpo para o clique.</summary>
    public float HeadPx => _sprite?.HeadPx ?? 16f;

    public override void _Ready()
    {
        ZIndex = ZLayers.Units;
        TextureFilter = TextureFilterEnum.Nearest;
        _name.HorizontalAlignment = HorizontalAlignment.Center;
        _name.Size = new Vector2(160, 16);
        _name.AddThemeFontSizeOverride("font_size", 11);
        _name.AddThemeConstantOverride("outline_size", 4);
        _name.AddThemeColorOverride("font_outline_color", Colors.Black);
        AddChild(_name);
    }

    public void Apply(NpcInfo info, Vector2 pixels, SpriteCatalog sprites, bool hovered)
    {
        Position = pixels;
        _sprite ??= sprites.For(info.DefinitionId);
        var hasShop = info.Shop is not null;
        var text = hasShop ? $"{info.Name} · loja" : info.Name;
        if (_name.Text != text || _hovered != hovered || _hasShop != hasShop)
        {
            _name.Text = text;
            _hasShop = hasShop;
            _hovered = hovered;
            _name.AddThemeColorOverride("font_color", hasShop ? ShopName : PlainName);
            _name.Position = new Vector2(-_name.Size.X / 2, -HeadPx - 18);
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_hovered && _hasShop)
        {
            DrawSetTransform(Vector2.Zero, 0, new Vector2(1f, 0.45f));
            DrawArc(Vector2.Zero, 18f, 0, Mathf.Tau, 32, new Color(ShopName, 0.9f), 2.5f);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }

        if (_sprite?.For("idle") is not { } idle)
        {
            DrawCircle(new Vector2(0, -10), 10, new Color(0.85f, 0.75f, 0.45f));
            return;
        }

        var strip = idle.For(SpriteDirection.Down);
        var frame = strip.Frame(0);
        var scale = _sprite.Scale;
        DrawTextureRectRegion(_sprite.Texture, new Rect2(-strip.Pivot * scale, frame.Size * scale), frame, _sprite.Tint);
    }
}
