using System;
using Godot;
using MmoRpgClient.Hud;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Account;

/// <summary>
/// Cartão da seleção de personagem (T-074): moldura de pedra (<c>skill_cell.png</c>), o nicho de pedra com o sprite da
/// classe e, embaixo, nome, classe, nível e zona. Clique seleciona, clique duplo joga. O selecionado ganha borda dourada
/// e anda; os outros ficam parados. O cartão de criar usa a mesma moldura, com o + dourado no nicho vazio.
/// </summary>
public partial class CharacterCard : MarginContainer
{
    public const int Width = 150;

    private static readonly Color NameColor = Colors.White;
    private static readonly Color DetailColor = new(0.85f, 0.82f, 0.74f);

    private readonly Panel _highlight = new() { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
    private readonly SpritePreview? _preview;
    private Label? _name;
    private bool _hover;

    /// <summary>Clique simples.</summary>
    public event Action? Chosen;

    /// <summary>Clique duplo.</summary>
    public event Action? Activated;

    private CharacterCard(SpritePreview? preview)
    {
        _preview = preview;
        CustomMinimumSize = new Godot.Vector2(Width, 0);
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        var frame = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        if (HudTheme.SkillCell() is { } cell)
        {
            frame.AddThemeStyleboxOverride("panel", cell);
        }

        AddChild(frame);
        AddChild(_highlight);
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 2);
        frame.AddChild(column);
        Column = column;
        MouseEntered += () => SetHover(true);
        MouseExited += () => SetHover(false);
    }

    private VBoxContainer Column { get; }

    public bool Selected { get; private set; }

    /// <summary>Cartão de um personagem.</summary>
    public static CharacterCard ForCharacter(SpriteDefinition? sprite, string name, string className, int level, string zone)
    {
        var preview = new SpritePreview { Zoom = 2, Backdrop = HudTheme.DollNiche(), FeetInset = 14, Animate = false, Sprite = sprite };
        var card = new CharacterCard(preview);
        card.Column.AddChild(Centered(preview));
        card.AddLine(name, 19, NameColor, out var nameLabel);
        card.AddLine(className, 15, HudTheme.Gold, out _);
        card.AddLine($"Nível {level}", 14, DetailColor, out _);
        card.AddLine(zone, 13, DetailColor, out _);
        card._name = nameLabel;
        return card;
    }

    /// <summary>Cartão de criar personagem: nicho vazio com o + dourado.</summary>
    public static CharacterCard ForCreate()
    {
        var card = new CharacterCard(null);
        var niche = new Control { MouseFilter = MouseFilterEnum.Ignore };
        var backdrop = HudTheme.DollNiche();
        niche.CustomMinimumSize = backdrop?.GetSize() ?? new Godot.Vector2(108, 140);
        if (backdrop is not null)
        {
            niche.AddChild(new TextureRect { Texture = backdrop, Modulate = new Color(1, 1, 1, 0.55f), MouseFilter = MouseFilterEnum.Ignore });
        }

        if (HudTheme.UiTexture("skill_plus.png") is { } plus)
        {
            var size = plus.GetSize() * 2;
            niche.AddChild(new TextureRect
            {
                Texture = plus,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Size = size,
                Position = ((niche.CustomMinimumSize - size) / 2).Floor(),
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }

        card.Column.AddChild(Centered(niche));
        card.AddLine("Criar", 19, NameColor, out _);
        card.AddLine("personagem", 15, HudTheme.Gold, out _);
        return card;
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
        if (_preview is not null)
        {
            _preview.Animate = selected;
            _preview.QueueRedraw();
        }

        _name?.AddThemeColorOverride("font_color", selected ? HudTheme.Gold : NameColor);
        UpdateHighlight();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click)
        {
            AcceptEvent();
            if (click.DoubleClick)
            {
                Activated?.Invoke();
            }
            else
            {
                Chosen?.Invoke();
            }
        }
    }

    private void SetHover(bool hover)
    {
        _hover = hover;
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        _highlight.Visible = Selected || _hover;
        _highlight.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            DrawCenter = false,
            BorderColor = Selected ? HudTheme.Gold : new Color(1, 1, 1, 0.35f),
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            BorderWidthTop = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });
    }

    private void AddLine(string text, int size, Color color, out Label label)
    {
        label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            CustomMinimumSize = new Godot.Vector2(Width - 24, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 3);
        Column.AddChild(label);
    }

    private static CenterContainer Centered(Control child)
    {
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.AddChild(child);
        return center;
    }
}
