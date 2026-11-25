using Godot;
using MmoRpgClient.Input;

namespace MmoRpgClient.Hud;

/// <summary>Estado de um slot da barra de habilidades (calculado pelo HUD a partir do snapshot).</summary>
public readonly record struct AbilitySlotState(
    string Abbreviation,
    string Key,
    string Cost,
    float CooldownFraction,
    float CooldownSeconds,
    bool NoResource,
    bool OutOfRange,
    bool Casting,
    string? Charges,
    string Rank = "",
    bool NotLearned = false,
    bool Empty = false,
    bool Depleted = false);

/// <summary>
/// Um slot: ícone (T-080; sem ícone, a sigla), tecla, custo, NH (T-064), cargas, varredura de recarga e estado indisponível.
/// Os espaços 1–6 (T-081) aceitam o ícone arrastado da tela de habilidades ou de outro espaço (troca os dois); arrastar
/// para fora da barra ou clicar com o botão direito esvazia o espaço. M1 e Q são fixos.
/// </summary>
public partial class AbilitySlot : Control
{
    /// <summary>Chave do dado arrastado: id da habilidade.</summary>
    public const string DragAbility = "ability";

    /// <summary>Chave do dado arrastado: espaço de origem na barra (-1 = da tela de habilidades).</summary>
    public const string DragFrom = "from";

    private static readonly Color EmptyBackground = new(0.10f, 0.11f, 0.14f, 0.85f);
    private static readonly Color NotLearnedShade = new(0.05f, 0.05f, 0.06f, 0.70f);
    private static readonly Color DropHighlight = new(1f, 0.85f, 0.45f, 0.9f);

    /// <summary>Lado do espaço na barra, numa linha só (metade dos 52 px de antes, mais 10%).</summary>
    public const float SlotSize = 29f;

    /// <summary>Lado para o qual as fontes e margens do desenho foram pensadas.</summary>
    private const float ReferenceSize = 52f;

    /// <summary>Menor fonte usada nos textos do espaço.</summary>
    private const int MinFontSize = 8;

    private static readonly Color Background = new(0.18f, 0.20f, 0.26f);
    private static readonly Color CooldownShade = new(0f, 0f, 0f, 0.65f);
    private static readonly Color NoResourceTint = new(0.25f, 0.30f, 0.75f, 0.55f);
    private static readonly Color OutOfRangeTint = new(0.80f, 0.20f, 0.20f, 0.40f);
    private static readonly Color CastingBorder = new(1f, 0.85f, 0.3f);

    private AbilitySlotState _state;
    private Texture2D? _icon;
    private bool _dropHover;
    private bool _dragging;

    /// <summary>Barra que este espaço edita; null = espaço fixo (M1, Q).</summary>
    public HotbarLayout? Hotbar { get; set; }

    /// <summary>Índice na <see cref="Hotbar"/> (0–5).</summary>
    public int Index { get; set; } = -1;

    /// <summary>Habilidade mostrada agora (para arrastar); null = espaço vazio.</summary>
    public string? AbilityId { get; set; }

    /// <summary>Pode ir para a barra (aprendida, ataque básico ou consumível)? Decide quem monta o espaço.</summary>
    public bool Draggable { get; set; }

    public AbilitySlot()
    {
        CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        TooltipText = "";
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public void SetIcon(Texture2D? icon)
    {
        if (icon == _icon)
        {
            return;
        }

        _icon = icon;
        QueueRedraw();
    }

    public void SetState(AbilitySlotState state, string tooltip)
    {
        TooltipText = tooltip;
        if (state == _state)
        {
            return;
        }

        _state = state;
        QueueRedraw();
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Hotbar is null || AbilityId is null)
        {
            return default;
        }

        _dragging = true;
        SetDragPreview(DragPreview(_icon, _state.Abbreviation));
        return new Godot.Collections.Dictionary { [DragAbility] = AbilityId, [DragFrom] = Index };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var ok = Hotbar is not null && data.VariantType == Variant.Type.Dictionary
            && data.AsGodotDictionary().ContainsKey(DragAbility);
        SetDropHover(ok);
        return ok;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        SetDropHover(false);
        var d = data.AsGodotDictionary();
        var from = d.TryGetValue(DragFrom, out var f) ? f.AsInt32() : -1;
        if (from >= 0)
        {
            Hotbar?.Swap(from, Index);
        }
        else
        {
            Hotbar?.Set(Index, d[DragAbility].AsString());
        }
    }

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd)
        {
            return;
        }

        SetDropHover(false);
        // Arrastado daqui e solto onde nada aceita (fora da barra): esvazia o espaço.
        if (_dragging && Hotbar is not null && !GetViewport().GuiIsDragSuccessful())
        {
            Hotbar.Clear(Index);
        }

        _dragging = false;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (Hotbar is not null && AbilityId is not null && @event is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
        {
            Hotbar.Clear(Index);
            AcceptEvent();
        }
    }

    /// <summary>Ícone que acompanha o mouse ao arrastar (também usado pela tela de habilidades).</summary>
    public static Control DragPreview(Texture2D? icon, string fallback)
    {
        if (icon is null)
        {
            return new Label { Text = fallback };
        }

        return new TextureRect
        {
            Texture = icon,
            Size = new Vector2(40, 40),
            Position = new Vector2(-20, -20),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = TextureFilterEnum.Nearest,
            Modulate = new Color(1, 1, 1, 0.85f),
        };
    }

    private void SetDropHover(bool value)
    {
        if (_dropHover != value)
        {
            _dropHover = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        var font = ThemeDB.FallbackFont;
        var keyFont = FontFor(11);
        var top = TopBaseline(font, keyFont);
        if (_state.Empty)
        {
            DrawRect(rect, EmptyBackground);
            DrawString(font, new Vector2(2, top), _state.Key, HorizontalAlignment.Left, -1, keyFont, new Color(1, 1, 1, 0.6f));
            DrawRect(rect, _dropHover ? DropHighlight : new Color(1, 1, 1, 0.12f), filled: false, width: _dropHover ? 2f : 1f);
            return;
        }

        DrawRect(rect, Background);
        if (_icon is not null)
        {
            DrawTextureRect(_icon, rect.Grow(-Mathf.Max(1f, Mathf.Round(3 * DrawScale))), tile: false);
        }
        else
        {
            DrawString(font, Middle(font, FontFor(16)), _state.Abbreviation, HorizontalAlignment.Center, Size.X, FontFor(16), Colors.White);
        }

        if (_state.NotLearned)
        {
            DrawRect(rect, NotLearnedShade);
            if (Size.X >= 40)
            {
                DrawString(font, new Vector2(0, Size.Y / 2 + 4), "não", HorizontalAlignment.Center, Size.X, 10, new Color(1, 1, 1, 0.8f));
                DrawString(font, new Vector2(0, Size.Y / 2 + 15), "aprendida", HorizontalAlignment.Center, Size.X, 9, new Color(1, 1, 1, 0.8f));
            }
        }
        else if (_state.Depleted)
        {
            // T-323: atalho de consumível sem nenhuma unidade no inventário.
            DrawRect(rect, NotLearnedShade);
        }
        else if (_state.NoResource)
        {
            DrawRect(rect, NoResourceTint);
        }
        else if (_state.OutOfRange)
        {
            DrawRect(rect, OutOfRangeTint);
        }

        if (_state.CooldownFraction > 0)
        {
            DrawSweep(_state.CooldownFraction);
            var cdFont = FontFor(15);
            var at = Middle(font, cdFont);
            DrawStringOutline(font, at, $"{_state.CooldownSeconds:0.0}", HorizontalAlignment.Center, Size.X, cdFont, 3, Colors.Black);
            DrawString(font, at, $"{_state.CooldownSeconds:0.0}", HorizontalAlignment.Center, Size.X, cdFont, Colors.White);
        }

        DrawKeyText(font, new Vector2(2, top), _state.Key, HorizontalAlignment.Left, keyFont, new Color(1, 1, 1, 0.9f));
        var smallFont = FontFor(10);
        var bottom = Size.Y - 1 - font.GetDescent(smallFont);
        if (_state.Cost.Length > 0)
        {
            DrawKeyText(font, new Vector2(0, bottom), _state.Cost, HorizontalAlignment.Right, smallFont, new Color(0.8f, 0.9f, 1f));
        }

        if (_state.Rank.Length > 0)
        {
            DrawKeyText(font, new Vector2(2, bottom), _state.Rank, HorizontalAlignment.Left, smallFont, new Color(1f, 0.85f, 0.45f));
        }

        if (_state.Charges is { } charges)
        {
            DrawKeyText(font, new Vector2(0, top), charges, HorizontalAlignment.Right, keyFont, new Color(1f, 0.9f, 0.5f));
        }

        DrawRect(rect, _dropHover ? DropHighlight : _state.Casting ? CastingBorder : Colors.Black, filled: false,
            width: _dropHover || _state.Casting ? 2f : 1f);
    }

    /// <summary>Quanto o espaço está reduzido em relação ao desenho original de 52 px (fontes e margens acompanham).</summary>
    private float DrawScale => Size.X / ReferenceSize;

    /// <summary>Fonte do desenho original reduzida com o espaço, sem ficar ilegível.</summary>
    private int FontFor(int original) => Math.Max(MinFontSize, Mathf.RoundToInt(original * DrawScale));

    private static float TopBaseline(Font font, int fontSize) => font.GetAscent(fontSize);

    private Vector2 Middle(Font font, int fontSize) =>
        new(0, (Size.Y + font.GetAscent(fontSize) - font.GetDescent(fontSize)) / 2);

    /// <summary>Texto pequeno com contorno, para ler por cima do ícone num espaço pequeno.</summary>
    private void DrawKeyText(Font font, Vector2 at, string text, HorizontalAlignment align, int fontSize, Color color)
    {
        var width = align == HorizontalAlignment.Right ? Size.X - 2 : -1;
        DrawStringOutline(font, at, text, align, width, fontSize, 2, Colors.Black);
        DrawString(font, at, text, align, width, fontSize, color);
    }

    /// <summary>Setor escuro no sentido horário a partir do topo, proporcional à recarga restante.</summary>
    private void DrawSweep(float fraction)
    {
        var center = Size / 2;
        var points = new List<Vector2> { center };
        const int segments = 32;
        var start = -Mathf.Pi / 2;
        var sweep = Mathf.Tau * fraction;
        for (var i = 0; i <= segments; i++)
        {
            // Raio do centro até a borda do quadrado nesse ângulo.
            var dir = Vector2.FromAngle(start + sweep * i / segments);
            var t = Mathf.Min(
                Mathf.Abs(dir.X) > 1e-5f ? center.X / Mathf.Abs(dir.X) : float.MaxValue,
                Mathf.Abs(dir.Y) > 1e-5f ? center.Y / Mathf.Abs(dir.Y) : float.MaxValue);
            points.Add(center + dir * t);
        }

        if (fraction >= 0.999f)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), CooldownShade);
            return;
        }

        DrawColoredPolygon(points.ToArray(), CooldownShade);
    }
}
