using Game.Core.Combat;
using Game.Core.Simulation;
using Game.Protocol;
using Godot;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Painel do grupo (T-311), à esquerda, abaixo das barras do jogador: um quadro de pedra por membro (ícone da classe,
/// nome, nível, vida e Mana; coroa no líder; esmaecido em outra zona), o convite pelo nome e o botão de sair.
/// Só mostra o <see cref="PartyModel"/> e devolve intenções por eventos; quem envia ao servidor é o <c>Main</c>.
/// </summary>
public partial class PartyPanel : VBoxContainer
{
    public const float FrameWidth = 250f;

    private readonly HBoxContainer _header = new();
    private readonly Label _title = new();
    private readonly Button _toggleInvite = new();
    private readonly HBoxContainer _inviteRow = new();
    private readonly LineEdit _inviteName = new();
    private readonly Label _message = new();
    private readonly VBoxContainer _members = new();
    private readonly Button _leave = new();
    private readonly PopupMenu _memberMenu = new();
    private readonly List<MemberFrame> _frames = new();
    private ContentCatalog _content = ContentCatalog.Empty;
    private PartyModel? _model;
    private int _menuMember;
    private double _messageLeft;

    /// <summary>Convite pelo nome (caixa de texto do painel).</summary>
    public event Action<string>? InviteByName;

    public event Action? Leave;

    /// <summary>O líder expulsa este membro (id de membro).</summary>
    public event Action<int>? Kick;

    /// <summary>Clique no quadro de um membro na mesma zona: a entidade vira alvo (útil para cura).</summary>
    public event Action<EntityId>? Select;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 4);

        _header.AddThemeConstantOverride("separation", 6);
        _header.MouseFilter = MouseFilterEnum.Ignore;
        _title.Text = "Grupo";
        Outline(_title, 13);
        _header.AddChild(_title);
        _toggleInvite.Text = "+ Convidar";
        _toggleInvite.FocusMode = FocusModeEnum.None;
        _toggleInvite.AddThemeFontSizeOverride("font_size", 11);
        HudTheme.StoneButton(_toggleInvite);
        _toggleInvite.Pressed += () =>
        {
            _inviteRow.Visible = !_inviteRow.Visible;
            if (_inviteRow.Visible)
            {
                _inviteName.GrabFocus();
            }
        };
        _header.AddChild(_toggleInvite);
        AddChild(_header);

        _inviteRow.AddThemeConstantOverride("separation", 4);
        _inviteRow.Visible = false;
        _inviteName.PlaceholderText = "Nome do personagem";
        _inviteName.CustomMinimumSize = new Vector2(160, 0);
        _inviteName.AddThemeFontSizeOverride("font_size", 12);
        _inviteName.TextSubmitted += _ => SubmitInvite();
        _inviteRow.AddChild(_inviteName);
        var send = new Button { Text = "Convidar", FocusMode = FocusModeEnum.None };
        send.AddThemeFontSizeOverride("font_size", 11);
        HudTheme.StoneButton(send);
        send.Pressed += SubmitInvite;
        _inviteRow.AddChild(send);
        AddChild(_inviteRow);

        _message.Visible = false;
        _message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _message.CustomMinimumSize = new Vector2(FrameWidth, 0);
        _message.AddThemeColorOverride("font_color", HudTheme.Suggested);
        Outline(_message, 12);
        AddChild(_message);

        _members.AddThemeConstantOverride("separation", 4);
        _members.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_members);

        _leave.Text = "Sair do grupo";
        _leave.FocusMode = FocusModeEnum.None;
        _leave.AddThemeFontSizeOverride("font_size", 11);
        _leave.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        HudTheme.StoneButton(_leave);
        _leave.Pressed += () => Leave?.Invoke();
        AddChild(_leave);

        _memberMenu.AddItem("Expulsar do grupo", 0);
        _memberMenu.IdPressed += _ => Kick?.Invoke(_menuMember);
        AddChild(_memberMenu);
    }

    public void SetContent(ContentCatalog content) => _content = content;

    /// <summary>Texto curto no painel (recusas e avisos de convite); some sozinho.</summary>
    public void ShowMessage(string text, double seconds = 4)
    {
        _message.Text = text;
        _message.Visible = text.Length > 0;
        _messageLeft = seconds;
    }

    /// <summary>A caixa de nome está com o foco (as teclas não devem virar movimento).</summary>
    public bool IsTyping => _inviteName.HasFocus();

    public override void _Process(double delta)
    {
        if (_messageLeft > 0)
        {
            _messageLeft -= delta;
            _message.Visible = _messageLeft > 0;
        }
    }

    public void Display(PartyModel model)
    {
        _model = model;
        var others = model.Others;
        _title.Text = model.InParty ? $"Grupo ({others.Count + 1})" : "Grupo";
        _leave.Visible = model.InParty;
        // Em grupo, só o líder convida (o servidor recusaria os outros).
        _toggleInvite.Visible = !model.InParty || model.IsLeader;
        if (!_toggleInvite.Visible)
        {
            _inviteRow.Visible = false;
        }

        while (_frames.Count < others.Count)
        {
            var frame = new MemberFrame();
            frame.Clicked += OnFrameClicked;
            _frames.Add(frame);
            _members.AddChild(frame);
        }

        for (var i = 0; i < _frames.Count; i++)
        {
            _frames[i].Visible = i < others.Count;
            if (i < others.Count)
            {
                var m = others[i];
                _frames[i].Show(m, model.IsLeaderMember(m), model.SameZone(m), ZoneName(m.ZoneId), ClassIcon(m.ClassId),
                    ClassName(m.ClassId));
            }
        }
    }

    private void OnFrameClicked(PartyMemberInfo member, MouseButton button)
    {
        if (_model is null)
        {
            return;
        }

        if (button == MouseButton.Left && _model.SameZone(member) && member.Alive)
        {
            Select?.Invoke(member.Entity);
        }
        else if (button == MouseButton.Right && _model.IsLeader)
        {
            _menuMember = member.MemberId;
            _memberMenu.Position = (Vector2I)GetViewport().GetMousePosition();
            _memberMenu.Popup();
        }
    }

    private void SubmitInvite()
    {
        var name = _inviteName.Text.Trim();
        if (name.Length == 0)
        {
            return;
        }

        InviteByName?.Invoke(name);
        _inviteName.Text = "";
        _inviteName.ReleaseFocus();
        _inviteRow.Visible = false;
    }

    private string ZoneName(string zoneId) => _content.Zones.TryGetValue(zoneId, out var zone) ? zone.Name : zoneId;

    private string ClassName(string classId) => _content.Classes.TryGetValue(classId, out var c) ? c.Name : classId;

    /// <summary>Ícone da classe: o do ataque básico dela (os ícones de classe ainda não existem).</summary>
    private Texture2D? ClassIcon(string classId) =>
        _content.Classes.TryGetValue(classId, out var c) ? IconCatalog.Shared.For(c.BasicAttack) : null;

    internal static void Outline(Label label, int size)
    {
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeConstantOverride("outline_size", 4);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Quadro de um membro: moldura de pedra, ícone da classe, nome e nível, coroa, vida e Mana (ou a zona).</summary>
    private sealed partial class MemberFrame : PanelContainer
    {
        private readonly TextureRect _icon = new();
        private readonly Label _name = new();
        private readonly TextureRect _crown = new();
        private readonly HudBar _health = new();
        private readonly HudBar _mana = new();
        private readonly Label _zone = new();
        private PartyMemberInfo? _member;

        public event Action<PartyMemberInfo, MouseButton>? Clicked;

        public MemberFrame()
        {
            MouseFilter = MouseFilterEnum.Stop;
            MouseDefaultCursorShape = CursorShape.PointingHand;
            AddThemeStyleboxOverride("panel", HudTheme.MemberFrame());
            CustomMinimumSize = new Vector2(FrameWidth, 0);

            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 6);
            AddChild(row);

            _icon.CustomMinimumSize = new Vector2(34, 34);
            _icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _icon.TextureFilter = TextureFilterEnum.Nearest;
            _icon.MouseFilter = MouseFilterEnum.Ignore;
            _icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(_icon);

            var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddThemeConstantOverride("separation", 2);
            row.AddChild(column);

            var nameRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            nameRow.AddThemeConstantOverride("separation", 4);
            PartyPanel.Outline(_name, 12);
            _name.AddThemeColorOverride("font_color", HudTheme.StoneText);
            nameRow.AddChild(_name);
            _crown.Texture = HudTheme.Crown();
            _crown.CustomMinimumSize = new Vector2(16, 16);
            _crown.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _crown.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _crown.TextureFilter = TextureFilterEnum.Nearest;
            _crown.MouseFilter = MouseFilterEnum.Ignore;
            _crown.TooltipText = "Líder do grupo";
            nameRow.AddChild(_crown);
            column.AddChild(nameRow);

            _health.CustomMinimumSize = new Vector2(0, 10);
            _health.FontSize = 9;
            _health.BorderColor = HudTheme.BarBorderGold;
            _health.MouseFilter = MouseFilterEnum.Ignore;
            column.AddChild(_health);
            _mana.CustomMinimumSize = new Vector2(0, 6);
            _mana.FontSize = 0;
            _mana.BorderColor = HudTheme.BarBorderGold;
            _mana.MouseFilter = MouseFilterEnum.Ignore;
            column.AddChild(_mana);
            PartyPanel.Outline(_zone, 11);
            _zone.AddThemeColorOverride("font_color", new Color(0.8f, 0.78f, 0.7f));
            column.AddChild(_zone);
        }

        public void Show(PartyMemberInfo m, bool leader, bool sameZone, string zoneName, Texture2D? icon, string className)
        {
            _member = m;
            _icon.Texture = icon;
            var name = $"{m.Name}  ·  nv {m.Level}";
            if (!m.Alive)
            {
                name += "  (caído)";
            }

            if (_name.Text != name)
            {
                _name.Text = name;
            }

            _crown.Visible = leader;
            _health.Visible = sameZone;
            _mana.Visible = sameZone;
            _zone.Visible = !sameZone;
            if (sameZone)
            {
                var frac = m.MaxHealth > 0 ? m.Health / m.MaxHealth : 0f;
                _health.Set(frac, frac < 0.3f ? HudTheme.HealthLow : HudTheme.Health, m.Alive ? $"{m.Health:0} / {m.MaxHealth:0}" : "Caído");
                _mana.Set(m.MaxMana > 0 ? m.Mana / m.MaxMana : 0f, HudTheme.Resource("mana"), "");
            }
            else
            {
                _zone.Text = zoneName;
            }

            Modulate = new Color(1, 1, 1, sameZone && m.Alive ? 1f : 0.55f);
            TooltipText = sameZone ? $"{className} — clique para selecionar" : $"{className} — em {zoneName}";
        }

        public override void _GuiInput(InputEvent e)
        {
            if (e is InputEventMouseButton { Pressed: true } mb && _member is { } member
                && mb.ButtonIndex is MouseButton.Left or MouseButton.Right)
            {
                Clicked?.Invoke(member, mb.ButtonIndex);
                AcceptEvent();
            }
        }
    }
}
