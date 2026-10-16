using System.Globalization;
using Game.Core.CombatLog;
using Game.Core.Simulation;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>Painel recolhível com o log de combate (T-013) e filtro por categoria.</summary>
public partial class CombatLogPanel : PanelContainer
{
    public const float PanelWidth = 400f;
    private const float LogHeight = 190f;

    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly (LogCategory Category, string Label)[] Filters =
    [
        (LogCategory.Dano, "Dano"),
        (LogCategory.Cura, "Cura"),
        (LogCategory.Status, "Status"),
        (LogCategory.Habilidade, "Hab."),
        (LogCategory.Recurso, "Rec."),
        (LogCategory.Fase, "Fase"),
        (LogCategory.Morte, "Morte"),
    ];

    private readonly HashSet<LogCategory> _enabled = Filters.Select(f => f.Category).ToHashSet();
    private readonly RichTextLabel _text = new();
    private readonly HBoxContainer _filters = new();
    private readonly Button _toggle = new();
    private IReadOnlyList<CombatLogEntry> _source = Array.Empty<CombatLogEntry>();
    private CombatLogEntry? _lastShown;
    private bool _dirty = true;

    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = HudTheme.Panel,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        });

        var box = new VBoxContainer();
        AddChild(box);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 2);
        box.AddChild(header);

        _toggle.Text = "Log ▾";
        _toggle.Flat = true;
        _toggle.FocusMode = FocusModeEnum.None;
        _toggle.AddThemeFontSizeOverride("font_size", 12);
        _toggle.Pressed += ToggleCollapsed;
        header.AddChild(_toggle);

        header.AddChild(_filters);
        foreach (var (category, label) in Filters)
        {
            var check = new CheckBox
            {
                Text = label,
                ButtonPressed = true,
                FocusMode = FocusModeEnum.None,
            };
            check.AddThemeFontSizeOverride("font_size", 10);
            check.AddThemeColorOverride("font_color", HudTheme.Category(category));
            check.AddThemeColorOverride("font_pressed_color", HudTheme.Category(category));
            check.Toggled += on =>
            {
                if (on)
                {
                    _enabled.Add(category);
                }
                else
                {
                    _enabled.Remove(category);
                }

                _dirty = true;
                Refresh(_source);
            };
            _filters.AddChild(check);
        }

        _text.BbcodeEnabled = true;
        _text.ScrollFollowing = true;
        _text.SelectionEnabled = true;
        _text.CustomMinimumSize = new Vector2(PanelWidth, LogHeight);
        _text.AddThemeFontSizeOverride("normal_font_size", 11);
        box.AddChild(_text);
    }

    /// <summary>Acrescenta as linhas novas (ou redesenha tudo se o filtro mudou ou o log foi podado).</summary>
    public void Refresh(IReadOnlyList<CombatLogEntry> entries)
    {
        _source = entries;
        var start = 0;
        if (!_dirty && _lastShown is not null)
        {
            var index = LastIndexOf(entries, _lastShown);
            if (index < 0)
            {
                _dirty = true;
            }
            else
            {
                start = index + 1;
            }
        }

        if (_dirty)
        {
            _text.Clear();
            start = 0;
            _dirty = false;
        }

        for (var i = start; i < entries.Count; i++)
        {
            var e = entries[i];
            if (_enabled.Contains(e.Category))
            {
                var time = (e.Tick / (double)World.TickRate).ToString("0.0", Pt);
                _text.AppendText($"[color=#888888]{time}s[/color] [color=#{HudTheme.Category(e.Category).ToHtml(false)}]{Hud.Escape(e.Text)}[/color]\n");
            }
        }

        if (entries.Count > 0)
        {
            _lastShown = entries[^1];
        }
    }

    private static int LastIndexOf(IReadOnlyList<CombatLogEntry> entries, CombatLogEntry entry)
    {
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(entries[i], entry))
            {
                return i;
            }
        }

        return -1;
    }

    private void ToggleCollapsed()
    {
        _text.Visible = !_text.Visible;
        _filters.Visible = _text.Visible;
        _toggle.Text = _text.Visible ? "Log ▾" : "Log ▸";
        ResetSize();
    }
}
