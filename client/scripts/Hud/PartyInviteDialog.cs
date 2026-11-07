using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Janela "&lt;nome&gt; convidou você para o grupo" (T-311): painel de pedra, coroa, Aceitar e Recusar e o tempo
/// restante. Some quando o convite expira ou é respondido.
/// </summary>
public partial class PartyInviteDialog : PanelContainer
{
    private readonly Label _text = new();
    private readonly Label _timer = new();
    private int _inviteId;

    /// <summary>Resposta ao convite: (id do convite, aceitou).</summary>
    public event Action<int, bool>? Responded;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", HudTheme.StonePanel());
        CustomMinimumSize = new Vector2(400, 0);

        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 8);
        AddChild(box);

        if (HudTheme.Crown() is { } crown)
        {
            box.AddChild(new TextureRect
            {
                Texture = crown,
                TextureFilter = TextureFilterEnum.Nearest,
                StretchMode = TextureRect.StretchModeEnum.KeepCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }

        var title = new Label { Text = "Convite para o grupo", HorizontalAlignment = HorizontalAlignment.Center };
        PartyPanel.Outline(title, 18);
        title.AddThemeColorOverride("font_color", HudTheme.StoneText);
        box.AddChild(title);

        _text.HorizontalAlignment = HorizontalAlignment.Center;
        _text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        PartyPanel.Outline(_text, 14);
        _text.AddThemeColorOverride("font_color", HudTheme.StoneText);
        box.AddChild(_text);

        _timer.HorizontalAlignment = HorizontalAlignment.Center;
        PartyPanel.Outline(_timer, 12);
        _timer.AddThemeColorOverride("font_color", new Color(0.8f, 0.76f, 0.62f));
        box.AddChild(_timer);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        buttons.AddThemeConstantOverride("separation", 16);
        box.AddChild(buttons);
        buttons.AddChild(MakeButton("Aceitar", accept: true));
        buttons.AddChild(MakeButton("Recusar", accept: false));
    }

    /// <summary>Mostra o convite ativo (ou esconde, sem convite).</summary>
    public void Display(PendingInvite? invite, double now)
    {
        Visible = invite is not null;
        if (invite is null)
        {
            return;
        }

        _inviteId = invite.InviteId;
        var text = PartyText.Invitation(invite.FromName);
        if (_text.Text != text)
        {
            _text.Text = text;
        }

        _timer.Text = PartyText.Expires(invite.SecondsLeft(now));
    }

    private Button MakeButton(string text, bool accept)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(130, 36) };
        button.AddThemeFontSizeOverride("font_size", 15);
        HudTheme.StoneButton(button);
        button.Pressed += () =>
        {
            Visible = false;
            Responded?.Invoke(_inviteId, accept);
        };
        return button;
    }
}
