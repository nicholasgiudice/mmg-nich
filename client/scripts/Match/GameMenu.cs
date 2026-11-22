using Godot;
using MmoRpgClient.Hud;

namespace MmoRpgClient.Match;

/// <summary>
/// Menu do Esc no mundo aberto (T-396): janela de pedra e ouro com Continuar, Trocar de personagem (ou Voltar ao menu, no
/// login de desenvolvimento), Sair do jogo e a opção de efeitos. Trocar e Sair pedem a saída ao servidor (T-395); em
/// combate, a janela mostra a contagem e o botão Cancelar. O mundo continua por trás: só a entrada do jogador para.
/// </summary>
public partial class GameMenu : Control
{
    private readonly ColorRect _dim = new() { Color = new Color(0, 0, 0, 0.35f), MouseFilter = MouseFilterEnum.Ignore };
    private readonly PanelContainer _window = new() { MouseFilter = MouseFilterEnum.Stop };
    private readonly Label _title = new() { Text = "Menu", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly VBoxContainer _choices = new();
    private readonly VBoxContainer _leaving = new();
    private readonly Label _leavingText = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _note = new() { HorizontalAlignment = HorizontalAlignment.Center, Visible = false };
    private readonly Button _switch = new();
    private readonly Button _effects = new();
    private readonly Button _cancel = new() { Text = "Cancelar" };

    // T-411: ferramenta de teste (só com o servidor em --dev-tools).
    private readonly Button _testEntry = new() { Text = "Teste (desenvolvimento)" };
    private readonly HSeparator _testSeparator = new();
    private readonly VBoxContainer _tests = new();
    private readonly VBoxContainer _confirm = new();
    private readonly Label _confirmText = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private Game.Protocol.DevToolAction _pending;

    public event Action? ContinueRequested;
    public event Action? SwitchRequested;
    public event Action? QuitRequested;
    public event Action? CancelRequested;

    /// <summary>T-411: um pedido da ferramenta de teste (já confirmado, quando pede confirmação).</summary>
    public event Action<Game.Protocol.DevToolAction>? DevToolRequested;

    public bool IsOpen => Visible;

    /// <summary>Uma saída foi pedida e ainda não terminou (a janela mostra a espera).</summary>
    public bool Leaving => Visible && _leaving.Visible;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        AddChild(_dim);

        _window.AddThemeStyleboxOverride("panel", HudTheme.WindowFrame() ?? new StyleBoxFlat
        {
            BgColor = HudTheme.Panel,
            ContentMarginLeft = 18,
            ContentMarginRight = 18,
            ContentMarginTop = 12,
            ContentMarginBottom = 14,
        });
        AddChild(_window);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        _window.AddChild(box);

        _title.AddThemeFontSizeOverride("font_size", 20);
        _title.AddThemeColorOverride("font_color", HudTheme.Suggested);
        _title.AddThemeColorOverride("font_outline_color", Colors.Black);
        _title.AddThemeConstantOverride("outline_size", 5);
        if (HudTheme.WindowTitlePlate() is { } plate)
        {
            var holder = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            holder.AddThemeStyleboxOverride("panel", plate);
            holder.AddChild(_title);
            box.AddChild(holder);
        }
        else
        {
            box.AddChild(_title);
        }

        _note.AddThemeFontSizeOverride("font_size", 13);
        _note.AddThemeColorOverride("font_color", HudTheme.Warning);
        box.AddChild(_note);

        _choices.AddThemeConstantOverride("separation", 8);
        box.AddChild(_choices);
        _choices.AddChild(MenuButton("Continuar", () => ContinueRequested?.Invoke()));
        _switch.Pressed += () => SwitchRequested?.Invoke();
        _choices.AddChild(Styled(_switch));
        _choices.AddChild(MenuButton("Sair do jogo", () => QuitRequested?.Invoke()));

        // T-054: intensidade dos efeitos (fica gravada para as próximas partidas).
        _effects.TooltipText = "Reduzidos: só os impactos e projéteis que envolvem você, e áreas e escudos mais transparentes. Telegráfos não mudam.";
        _effects.Pressed += () =>
        {
            Presentation.EffectSettings.Reduced = !Presentation.EffectSettings.Reduced;
            _effects.Text = Presentation.EffectSettings.Label;
        };
        _choices.AddChild(Styled(_effects, small: true));

        // T-411: separada das outras, por último; abre a lista de pedidos.
        _choices.AddChild(_testSeparator);
        _testEntry.Pressed += ShowTests;
        _choices.AddChild(Styled(_testEntry, small: true));

        _tests.AddThemeConstantOverride("separation", 6);
        _tests.Visible = false;
        box.AddChild(_tests);
        foreach (var action in DevToolText.Actions)
        {
            var button = new Button { Text = DevToolText.Label(action) };
            button.Pressed += () => Request(action);
            _tests.AddChild(Styled(button, small: true));
        }

        _tests.AddChild(Styled(MenuButton("Voltar", ShowChoices), small: true));

        _confirm.AddThemeConstantOverride("separation", 8);
        _confirm.Visible = false;
        box.AddChild(_confirm);
        _confirmText.AddThemeFontSizeOverride("font_size", 15);
        _confirmText.AddThemeColorOverride("font_color", HudTheme.StoneText);
        _confirm.AddChild(_confirmText);
        _confirm.AddChild(MenuButton("Desfazer", () =>
        {
            ShowTests();
            DevToolRequested?.Invoke(_pending);
        }));
        _confirm.AddChild(MenuButton("Cancelar", ShowTests));

        _leaving.AddThemeConstantOverride("separation", 10);
        _leaving.Visible = false;
        box.AddChild(_leaving);
        _leavingText.AddThemeFontSizeOverride("font_size", 15);
        _leavingText.AddThemeColorOverride("font_color", HudTheme.StoneText);
        _leavingText.CustomMinimumSize = new Vector2(260, 0);
        _leaving.AddChild(_leavingText);
        _cancel.Pressed += () => CancelRequested?.Invoke();
        _leaving.AddChild(Styled(_cancel));
    }

    /// <summary>
    /// Abre com as escolhas. <paramref name="withAccount"/>: "Trocar de personagem"; sem conta, "Voltar ao menu".
    /// <paramref name="devTools"/> (T-411): o servidor tem a ferramenta de teste; sem ela, a entrada "Teste" nem aparece.
    /// </summary>
    public void Open(bool withAccount, bool devTools = false)
    {
        _switch.Text = withAccount ? "Trocar de personagem" : "Voltar ao menu";
        _effects.Text = Presentation.EffectSettings.Label;
        _testEntry.Visible = _testSeparator.Visible = devTools;
        _note.Visible = false;
        ShowChoices();
        Visible = true;
        Layout();
    }

    /// <summary>T-411: o pedido saiu e espera a resposta do servidor.</summary>
    public void ShowDevToolSending() => ShowNote("Enviando...", ok: true);

    /// <summary>T-411: a resposta do servidor à ferramenta de teste, no aviso de cima (verde se feito, laranja se recusado).</summary>
    public void ShowNote(string text, bool ok)
    {
        _note.Text = text;
        _note.AddThemeColorOverride("font_color", ok ? HudTheme.Heal : HudTheme.Warning);
        _note.Visible = true;
        Layout();
    }

    public void Close() => Visible = false;

    /// <summary>A saída foi pedida e o servidor ainda não respondeu.</summary>
    public void ShowSending()
    {
        _leavingText.Text = "Saindo...";
        _cancel.Visible = false;
        ShowLeaving();
    }

    /// <summary>Em combate (T-395): faltam <paramref name="seconds"/> para sair.</summary>
    public void ShowWaiting(double seconds)
    {
        _leavingText.Text = $"Em combate: saindo em {Math.Max(0, Math.Ceiling(seconds)):0} s.\nMover-se ou atacar cancela.";
        _cancel.Visible = true;
        ShowLeaving();
    }

    /// <summary>O servidor cancelou a espera (o jogador agiu ou pediu): volta às escolhas, com o aviso.</summary>
    public void ShowCancelled()
    {
        ShowChoices();
        ShowNote("Saída cancelada.", ok: false);
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            Layout();
        }
    }

    private void ShowChoices()
    {
        _choices.Visible = true;
        _tests.Visible = false;
        _confirm.Visible = false;
        _leaving.Visible = false;
        _title.Text = "Menu";
        Layout();
    }

    private void ShowTests()
    {
        _choices.Visible = false;
        _confirm.Visible = false;
        _tests.Visible = true;
        _title.Text = "Teste";
        Layout();
    }

    private void Request(Game.Protocol.DevToolAction action)
    {
        if (DevToolText.NeedsConfirmation(action))
        {
            _pending = action;
            _confirmText.Text = DevToolText.ConfirmUndo;
            _tests.Visible = false;
            _note.Visible = false;
            _confirm.Visible = true;
            Layout();
            return;
        }

        DevToolRequested?.Invoke(action);
    }

    private void ShowLeaving()
    {
        _note.Visible = false;
        _choices.Visible = false;
        _tests.Visible = false;
        _confirm.Visible = false;
        _leaving.Visible = true;
        Layout();
    }

    private void Layout()
    {
        var screen = GetViewportRect().Size;
        // Fica numa CanvasLayer, sem tamanho de pai: cobre a janela inteira por conta própria.
        Position = Vector2.Zero;
        Size = screen;
        _dim.Size = screen;
        _window.ResetSize();
        _window.Position = (screen - _window.Size) / 2;
    }

    private static Button MenuButton(string text, Action pressed)
    {
        var button = new Button { Text = text };
        button.Pressed += pressed;
        return Styled(button);
    }

    private static Button Styled(Button button, bool small = false)
    {
        HudTheme.StoneButton(button);
        button.FocusMode = FocusModeEnum.None;
        button.CustomMinimumSize = new Vector2(240, small ? 30 : 36);
        button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        button.AddThemeFontSizeOverride("font_size", small ? 13 : 15);
        return button;
    }
}
