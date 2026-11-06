using Game.Core.Definitions;
using Game.Core.Simulation;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Painel "Batalha" (T-312): uma linha por monstro da tela, com nome, nível, vida e a marca de quem mira o jogador.
/// Clique numa linha escolhe o alvo; o mouse sobre a linha destaca o monstro no mapa. Rola com mais de
/// <see cref="VisibleRows"/> linhas. Só exibe: a escolha vira intenção no <c>PlayerInput</c>.
/// </summary>
public partial class BattleListPanel : PanelContainer
{
    public const float PanelWidth = 220f;
    public const int VisibleRows = 10;
    private const float RowHeight = 30f;

    private static readonly Color EliteColor = new(1f, 0.82f, 0.35f);
    private static readonly Color BossColor = new(1f, 0.45f, 0.30f);
    private static readonly Color AimColor = new(1f, 0.35f, 0.30f);
    private static readonly Color SelectedRow = new(1f, 0.85f, 0.2f, 0.22f);
    private static readonly Color HoverRow = new(1f, 1f, 1f, 0.10f);

    private readonly Label _title = new();
    private readonly ScrollContainer _scroll = new();
    private readonly VBoxContainer _rows = new();
    private readonly List<Row> _pool = new();

    /// <summary>Clique numa linha: o monstro escolhido.</summary>
    public event Action<EntityId>? RowClicked;

    /// <summary>Monstro sob o mouse na lista (para destacar no mapa); null = nenhum.</summary>
    public EntityId? Hovered { get; private set; }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(PanelWidth, 0);
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = HudTheme.Panel,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 6,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        AddChild(box);

        _title.Text = "Batalha (B)";
        _title.AddThemeFontSizeOverride("font_size", 12);
        _title.MouseFilter = MouseFilterEnum.Ignore;
        box.AddChild(_title);

        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _scroll.MouseFilter = MouseFilterEnum.Pass;
        box.AddChild(_scroll);

        _rows.AddThemeConstantOverride("separation", 2);
        _rows.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _rows.MouseFilter = MouseFilterEnum.Pass;
        _scroll.AddChild(_rows);
    }

    /// <summary>Mostra as linhas (reaproveita os controles; barato a cada frame).</summary>
    public void Show(IReadOnlyList<BattleEntry> entries)
    {
        while (_pool.Count < entries.Count)
        {
            var row = new Row();
            row.Clicked += id => RowClicked?.Invoke(id);
            row.HoverChanged += (id, inside) =>
            {
                if (inside)
                {
                    Hovered = id;
                }
                else if (Hovered == id)
                {
                    Hovered = null;
                }
            };
            _rows.AddChild(row);
            _pool.Add(row);
        }

        for (var i = 0; i < _pool.Count; i++)
        {
            var visible = i < entries.Count;
            _pool[i].Visible = visible;
            if (visible)
            {
                _pool[i].Display(entries[i]);
            }
        }

        if (Hovered is { } h && !entries.Any(e => e.Id == h))
        {
            Hovered = null;
        }

        // Lista vazia: só o título. Com mais de VisibleRows, a altura fica fixa e a lista rola.
        _scroll.Visible = entries.Count > 0;
        var shown = Math.Min(entries.Count, VisibleRows);
        _scroll.CustomMinimumSize = new Vector2(0, shown * (RowHeight + 2));
        _title.Text = entries.Count > 0 ? $"Batalha (B) · {entries.Count}" : "Batalha (B)";
        ResetSize();
    }

    private sealed partial class Row : PanelContainer
    {
        private readonly Label _marker = new();
        private readonly Label _name = new();
        private readonly HudBar _health = new();
        private readonly StyleBoxFlat _style = new() { BgColor = Colors.Transparent, ContentMarginLeft = 2, ContentMarginRight = 2 };
        private EntityId _id;
        private bool _selected;
        private bool _hover;

        public event Action<EntityId>? Clicked;
        public event Action<EntityId, bool>? HoverChanged;

        public Row()
        {
            CustomMinimumSize = new Vector2(0, RowHeight);
            MouseFilter = MouseFilterEnum.Stop;
            MouseDefaultCursorShape = CursorShape.PointingHand;
            AddThemeStyleboxOverride("panel", _style);

            var line = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            line.AddThemeConstantOverride("separation", 4);
            AddChild(line);

            _marker.CustomMinimumSize = new Vector2(14, 0);
            _marker.HorizontalAlignment = HorizontalAlignment.Center;
            _marker.VerticalAlignment = VerticalAlignment.Center;
            _marker.MouseFilter = MouseFilterEnum.Ignore;
            _marker.AddThemeColorOverride("font_color", AimColor);
            _marker.AddThemeFontSizeOverride("font_size", 14);
            line.AddChild(_marker);

            var stack = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            stack.AddThemeConstantOverride("separation", 1);
            line.AddChild(stack);

            _name.MouseFilter = MouseFilterEnum.Ignore;
            _name.ClipText = true;
            _name.AddThemeFontSizeOverride("font_size", 11);
            stack.AddChild(_name);

            _health.CustomMinimumSize = new Vector2(0, 7);
            _health.MouseFilter = MouseFilterEnum.Ignore;
            _health.FontSize = 0;
            stack.AddChild(_health);

            MouseEntered += () => SetHover(true);
            MouseExited += () => SetHover(false);
        }

        public void Display(BattleEntry entry)
        {
            _id = entry.Id;
            _selected = entry.Selected;
            var star = entry.Rank switch { MonsterRank.Boss => "★ ", MonsterRank.Elite => "◆ ", _ => "" };
            var text = $"{star}{entry.Name} · nv {entry.Level}";
            if (_name.Text != text)
            {
                _name.Text = text;
            }

            _name.AddThemeColorOverride("font_color", entry.Rank switch
            {
                MonsterRank.Boss => BossColor,
                MonsterRank.Elite => EliteColor,
                _ => Colors.White,
            });
            _marker.Text = entry.TargetsPlayer ? "!" : "";
            TooltipText = entry.TargetsPlayer ? "Está atacando você" : "";
            _health.Set(entry.HealthFraction, entry.HealthFraction < 0.3f ? HudTheme.HealthLow : HudTheme.Health, "");
            Restyle();
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            {
                Clicked?.Invoke(_id);
                AcceptEvent();
            }
        }

        private void SetHover(bool inside)
        {
            _hover = inside;
            HoverChanged?.Invoke(_id, inside);
            Restyle();
        }

        private void Restyle()
        {
            var color = _selected ? SelectedRow : _hover ? HoverRow : Colors.Transparent;
            if (_style.BgColor != color)
            {
                _style.BgColor = color;
            }
        }
    }
}
