using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Intents;
using Game.Core.Simulation;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Base das telas de personagem e de habilidades (T-221): painel que abre e fecha, se refaz só quando o estado mostrado
/// muda e manda intenções (quem decide é a autoridade; a recusa volta como <see cref="ProgressionRejected"/>).
/// </summary>
public abstract partial class ProgressionPanel : PanelContainer
{
    /// <summary>Tempo para confirmar a redistribuição com o segundo clique.</summary>
    private const double ConfirmSeconds = 3.0;

    private readonly VBoxContainer _box = new();
    private string _key = "";
    private string _message = "";
    private double _confirmLeft;

    protected ContentCatalog Content { get; private set; } = ContentCatalog.Empty;

    /// <summary>Para onde vão as intenções (a autoridade); null = não manda.</summary>
    public Action<Intent>? Send { get; set; }

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = HudTheme.Panel,
            BorderColor = new Color(1, 1, 1, 0.15f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 10,
        });
        _box.AddThemeConstantOverride("separation", 6);
        AddChild(_box);
    }

    public override void _Process(double delta)
    {
        if (_confirmLeft > 0)
        {
            _confirmLeft -= delta;
            if (_confirmLeft <= 0)
            {
                _key = ""; // volta o botão de redistribuir ao normal
            }
        }
    }

    public void SetContent(ContentCatalog content)
    {
        Content = content;
        _key = "";
    }

    public void Toggle()
    {
        Visible = !Visible;
        _key = "";
    }

    /// <summary>Mensagem curta no rodapé (ex.: motivo de uma recusa); vazio limpa.</summary>
    public void SetMessage(string message)
    {
        _message = message;
        _key = "";
    }

    /// <summary>Atualiza com o estado do jogador (só refaz os controles se algo mostrado mudou).</summary>
    public void Render(EntitySnapshot self)
    {
        if (!Visible)
        {
            return;
        }

        var key = StateKey(self) + "|" + _message + "|" + (_confirmLeft > 0);
        if (key == _key)
        {
            return;
        }

        _key = key;
        foreach (var child in _box.GetChildren())
        {
            _box.RemoveChild(child);
            child.QueueFree();
        }

        Build(_box, self);
        if (_message.Length > 0)
        {
            var label = Small(_message, 12);
            label.AddThemeColorOverride("font_color", HudTheme.Warning);
            _box.AddChild(label);
        }

        ResetSize();
    }

    /// <summary>Texto que muda sempre que algo mostrado muda.</summary>
    protected abstract string StateKey(EntitySnapshot self);

    protected abstract void Build(VBoxContainer box, EntitySnapshot self);

    protected static Label Title(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", HudTheme.Suggested);
        return label;
    }

    /// <summary>Texto menor; a cor pode ser trocada depois com <c>font_color</c> (por isso sem <c>LabelSettings</c>).</summary>
    protected static Label Small(string text, int size = 11, float alpha = 0.75f, float width = 0)
    {
        var label = new Label
        {
            Text = text,
            Modulate = new Color(1, 1, 1, alpha),
            AutowrapMode = width > 0 ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
            CustomMinimumSize = new Vector2(width, 0),
        };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    protected static Button PlusButton(bool enabled, string tooltip, Action pressed)
    {
        var button = new Button
        {
            Text = "+",
            Disabled = !enabled,
            TooltipText = tooltip,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(28, 24),
        };
        button.Pressed += pressed;
        return button;
    }

    /// <summary>Botão de redistribuir com confirmação no segundo clique (devolve todos os pontos de atributo e de habilidade).</summary>
    protected Button ResetButton(EntitySnapshot self, bool anySpent)
    {
        var confirming = _confirmLeft > 0;
        var button = new Button
        {
            Text = confirming ? "Confirmar: devolver todos os pontos?" : "Redistribuir pontos",
            Disabled = !anySpent,
            FocusMode = FocusModeEnum.None,
            TooltipText = "Devolve todos os pontos de atributo e de habilidade (grátis por enquanto). Só fora de combate.\n"
                + "A barra fica só com o ataque básico até você gastar os pontos de novo.",
        };
        if (confirming)
        {
            button.AddThemeColorOverride("font_color", HudTheme.Warning);
        }

        button.Pressed += () =>
        {
            if (_confirmLeft > 0)
            {
                _confirmLeft = 0;
                Send?.Invoke(new ResetPointsIntent(self.Id));
            }
            else
            {
                _confirmLeft = ConfirmSeconds;
            }

            _key = "";
        };
        return button;
    }

    /// <summary>Algum ponto gasto (atributo ou habilidade)?</summary>
    protected static bool AnySpent(CharacterProgress p, CharacterStats s, ContentCatalog content) =>
        p.AllocatedTotal > 0 || p.SkillPoints < content.Rules.Progression.SkillPointsAt(s.Level);
}
