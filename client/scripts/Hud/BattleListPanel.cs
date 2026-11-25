using Game.Core.Definitions;
using Game.Core.Simulation;
using Godot;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Painel "Batalha" (T-312): uma linha por monstro da tela, com miniatura, nome, nível, vida e a marca de quem mira o
/// jogador. Clique numa linha escolhe o alvo; o mouse sobre a linha destaca o monstro no mapa. Mostra no máximo
/// <see cref="BattleList.ShownRows"/> linhas, sem rolar (T-382); o título dá o total. Só exibe: a escolha vira
/// intenção no <c>PlayerInput</c>.
/// </summary>
public partial class BattleListPanel : PanelContainer
{
    public const float PanelWidth = 180f;
    private const float RowHeight = 24f;

    /// <summary>Lado do quadrado da miniatura (pixels): a altura da linha, menos a margem.</summary>
    private const int ThumbSize = 22;

    /// <summary>Ícone de "está te atacando" (T-382, kit pedra e ouro); sem o arquivo, volta o "!" de texto.</summary>
    private const string AttackIconFile = "attack_marker.png";

    private static readonly Color EliteColor = new(1f, 0.82f, 0.35f);
    private static readonly Color BossColor = new(1f, 0.45f, 0.30f);
    private static readonly Color AimColor = new(1f, 0.35f, 0.30f);
    private static readonly Color SelectedRow = new(1f, 0.85f, 0.2f, 0.22f);
    private static readonly Color HoverRow = new(1f, 1f, 1f, 0.10f);

    private readonly Label _title = new();
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
            ContentMarginLeft = 5,
            ContentMarginRight = 5,
            ContentMarginTop = 3,
            ContentMarginBottom = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        AddChild(box);

        _title.Text = "Batalha" + Hud.KeyHint(Input.InputActions.BattleList);
        _title.AddThemeFontSizeOverride("font_size", 11);
        _title.MouseFilter = MouseFilterEnum.Ignore;
        box.AddChild(_title);

        _rows.AddThemeConstantOverride("separation", 2);
        _rows.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _rows.MouseFilter = MouseFilterEnum.Pass;
        box.AddChild(_rows);
    }

    /// <summary>Mostra as linhas (reaproveita os controles; barato a cada frame).</summary>
    /// <param name="entries">As linhas mostradas (as primeiras da lista).</param>
    /// <param name="total">Monstros na lista inteira, para o título.</param>
    public void Show(IReadOnlyList<BattleEntry> entries, int total)
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

        // Lista vazia: só o título.
        _rows.Visible = entries.Count > 0;
        var title = "Batalha" + Hud.KeyHint(Input.InputActions.BattleList);
        _title.Text = total > 0 ? $"{title} · {total}" : title;
        ResetSize();
    }

    private sealed partial class Row : PanelContainer
    {
        private static Texture2D? s_attackIcon;
        private static bool s_attackIconLoaded;

        private readonly MonsterThumbnail _thumb = new();
        private readonly Label _marker = new();
        private readonly TextureRect _markerIcon = new();
        private readonly Label _name = new();
        private readonly Label _level = new();
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

            _thumb.CustomMinimumSize = new Vector2(ThumbSize, ThumbSize);
            _thumb.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            _thumb.MouseFilter = MouseFilterEnum.Ignore;
            line.AddChild(_thumb);

            var stack = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            stack.AddThemeConstantOverride("separation", 1);
            line.AddChild(stack);

            // O nome encolhe (corta) e o nível fica sempre à direita, para caber no painel estreito.
            var nameRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            nameRow.AddThemeConstantOverride("separation", 3);
            stack.AddChild(nameRow);
            _name.MouseFilter = MouseFilterEnum.Ignore;
            _name.ClipText = true;
            _name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _name.AddThemeFontSizeOverride("font_size", 10);
            nameRow.AddChild(_name);
            _level.MouseFilter = MouseFilterEnum.Ignore;
            _level.AddThemeFontSizeOverride("font_size", 9);
            _level.AddThemeColorOverride("font_color", HudTheme.StoneText);
            nameRow.AddChild(_level);

            _health.CustomMinimumSize = new Vector2(0, 5);
            _health.MouseFilter = MouseFilterEnum.Ignore;
            _health.FontSize = 0;
            stack.AddChild(_health);

            if (!s_attackIconLoaded)
            {
                s_attackIconLoaded = true;
                s_attackIcon = HudTheme.UiTexture(AttackIconFile);
            }

            if (s_attackIcon is { } icon)
            {
                // Arte em 1×: o quadro do ícone fica do tamanho da textura.
                _markerIcon.Texture = icon;
                _markerIcon.CustomMinimumSize = icon.GetSize();
                _markerIcon.StretchMode = TextureRect.StretchModeEnum.KeepCentered;
                _markerIcon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                _markerIcon.TextureFilter = TextureFilterEnum.Nearest;
                _markerIcon.MouseFilter = MouseFilterEnum.Ignore;
                line.AddChild(_markerIcon);
            }
            else
            {
                _marker.CustomMinimumSize = new Vector2(14, 0);
                _marker.HorizontalAlignment = HorizontalAlignment.Center;
                _marker.VerticalAlignment = VerticalAlignment.Center;
                _marker.MouseFilter = MouseFilterEnum.Ignore;
                _marker.AddThemeColorOverride("font_color", AimColor);
                _marker.AddThemeFontSizeOverride("font_size", 14);
                line.AddChild(_marker);
            }

            MouseEntered += () => SetHover(true);
            MouseExited += () => SetHover(false);
        }

        public void Display(BattleEntry entry)
        {
            _id = entry.Id;
            _selected = entry.Selected;
            var star = entry.Rank switch { MonsterRank.Boss => "★ ", MonsterRank.Elite => "◆ ", _ => "" };
            var text = $"{star}{entry.Name}";
            if (_name.Text != text)
            {
                _name.Text = text;
            }

            var level = $"nv {entry.Level}";
            if (_level.Text != level)
            {
                _level.Text = level;
            }

            _name.AddThemeColorOverride("font_color", entry.Rank switch
            {
                MonsterRank.Boss => BossColor,
                MonsterRank.Elite => EliteColor,
                _ => Colors.White,
            });
            _thumb.Show(entry.DefinitionId, entry.Name);
            _marker.Text = entry.TargetsPlayer ? "!" : "";
            _markerIcon.Modulate = entry.TargetsPlayer ? Colors.White : Colors.Transparent;
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

    /// <summary>
    /// Miniatura do monstro (T-382): o primeiro quadro parado, virado para baixo, do sprite do <see cref="SpriteCatalog"/>,
    /// recortado no conteúdo e em escala inteira (<see cref="BattleThumbnail"/>). Sem sprite: a inicial do nome.
    /// </summary>
    private sealed partial class MonsterThumbnail : Control
    {
        private static readonly Color Back = new(0f, 0f, 0f, 0.35f);
        private static readonly Color Edge = new(1f, 1f, 1f, 0.12f);
        private static readonly Dictionary<string, Cut?> s_cuts = new(StringComparer.Ordinal);
        private static SpriteCatalog? s_sprites;

        private string? _definition;
        private string _initial = "";
        private Cut? _cut;

        public override void _Ready() => TextureFilter = TextureFilterEnum.Nearest;

        public void Show(string definitionId, string name)
        {
            var initial = name.Length > 0 ? name[..1].ToUpperInvariant() : "?";
            if (definitionId == _definition && initial == _initial)
            {
                return;
            }

            _definition = definitionId;
            _initial = initial;
            _cut = CutFor(definitionId);
            QueueRedraw();
        }

        public override void _Draw()
        {
            var area = new Rect2(Vector2.Zero, Size);
            DrawRect(area, Back);
            DrawRect(area, Edge, filled: false);
            if (_cut is { } cut)
            {
                var size = new Vector2(cut.Thumb.DrawWidth, cut.Thumb.DrawHeight);
                var source = new Rect2(cut.Thumb.X, cut.Thumb.Y, cut.Thumb.Width, cut.Thumb.Height);
                DrawTextureRectRegion(cut.Texture, new Rect2(((Size - size) / 2).Floor(), size), source, cut.Tint);
                return;
            }

            var font = GetThemeDefaultFont();
            const int fontSize = 13;
            var text = font.GetStringSize(_initial, HorizontalAlignment.Left, -1, fontSize);
            var baseline = new Vector2((Size.X - text.X) / 2, (Size.Y + font.GetAscent(fontSize) - font.GetDescent(fontSize)) / 2);
            DrawString(font, baseline.Floor(), _initial, HorizontalAlignment.Left, -1, fontSize, HudTheme.StoneText);
        }

        private static Cut? CutFor(string definitionId)
        {
            if (s_cuts.TryGetValue(definitionId, out var cached))
            {
                return cached;
            }

            s_sprites ??= SpriteCatalog.Load();
            Cut? cut = null;
            if (s_sprites.For(definitionId) is { } sprite && sprite.For("idle") is { } idle)
            {
                var frame = idle.For(SpriteDirection.Down).Frame(0);
                var image = sprite.Texture.GetImage();
                if (image is not null)
                {
                    if (image.IsCompressed())
                    {
                        image.Decompress();
                    }

                    var used = image.GetRegion((Rect2I)frame).GetUsedRect();
                    if (used.Size.X > 0 && used.Size.Y > 0)
                    {
                        var thumb = BattleThumbnail.Fit(
                            (int)frame.Position.X + used.Position.X, (int)frame.Position.Y + used.Position.Y,
                            used.Size.X, used.Size.Y, ThumbSize);
                        cut = new Cut(sprite.Texture, thumb, sprite.Tint);
                    }
                }
            }

            s_cuts[definitionId] = cut;
            return cut;
        }

        private sealed record Cut(Texture2D Texture, ThumbnailCut Thumb, Color Tint);
    }
}
