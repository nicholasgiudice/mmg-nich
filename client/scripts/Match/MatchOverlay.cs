using System.Globalization;
using Godot;

namespace MmoRpgClient.Match;

/// <summary>Telas sobre a partida: pausa (Esc) e resultado (vitória/derrota com o resumo).</summary>
public partial class MatchOverlay : Control
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    private readonly PanelContainer _panel = new();
    private readonly Label _title = new();
    private readonly Label _body = new();
    private readonly Button _continue = new() { Text = "Continuar" };
    private readonly Button _next = new() { Text = "Próximo encontro" };

    public event Action? ContinueRequested;
    public event Action? RestartRequested;
    public event Action? NextRequested;
    public event Action? MenuRequested;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        AddChild(new ColorRect { Color = new Color(0, 0, 0, 0.45f), AnchorRight = 1, AnchorBottom = 1, MouseFilter = MouseFilterEnum.Ignore });

        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.10f, 0.95f),
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 18,
            ContentMarginBottom = 18,
        });
        AddChild(_panel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        _panel.AddChild(box);

        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _title.AddThemeFontSizeOverride("font_size", 28);
        box.AddChild(_title);

        _body.CustomMinimumSize = new Vector2(340, 0);
        box.AddChild(_body);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 8);
        box.AddChild(buttons);

        var restart = new Button { Text = "Reiniciar (R)" };
        var menu = new Button { Text = "Menu" };
        _continue.Pressed += () => ContinueRequested?.Invoke();
        restart.Pressed += () => RestartRequested?.Invoke();
        _next.Pressed += () => NextRequested?.Invoke();
        menu.Pressed += () => MenuRequested?.Invoke();
        foreach (var b in new[] { _continue, restart, _next, menu })
        {
            b.FocusMode = FocusModeEnum.None;
            b.CustomMinimumSize = new Vector2(110, 34);
            buttons.AddChild(b);
        }
    }

    public void ShowPause(MatchSettings settings, string encounterName)
    {
        Open("Pausado", new Color(0.9f, 0.9f, 0.9f), $"{encounterName}\nSeed: {settings.Seed}", canContinue: true, canAdvance: false);
    }

    public void ShowResult(MatchTracker t, MatchSettings settings, string encounterName, bool hasNext)
    {
        var (title, color) = t.Outcome switch
        {
            MatchOutcome.Victory => ("Vitória", new Color(0.45f, 1f, 0.5f)),
            MatchOutcome.TimeUp => ("Derrota — tempo esgotado", new Color(1f, 0.55f, 0.35f)),
            _ => ("Derrota", new Color(1f, 0.4f, 0.35f)),
        };
        var absorbed = t.Absorbed > 0 ? $" ({N(t.Absorbed)} absorvido)" : "";
        var group = Math.Abs(t.GroupDamageDealt - t.DamageDealt) > 0.5 ? $"\nDano do grupo: {N(t.GroupDamageDealt)}" : "";
        var body =
            $"{encounterName} · seed {settings.Seed}\n\n" +
            $"Tempo: {Time(t.Seconds)}\n" +
            $"Dano causado: {N(t.DamageDealt)}{group}\n" +
            $"Dano recebido: {N(t.DamageTaken)}{absorbed}\n" +
            $"Cura feita: {N(t.Healing)}\n" +
            $"Mortes de jogadores: {t.PlayerDeaths}\n" +
            $"Inimigos derrotados: {t.EnemiesKilled}";
        Open(title, color, body, canContinue: false, canAdvance: hasNext);
    }

    public void Close() => Visible = false;

    public override void _Process(double delta)
    {
        if (Visible)
        {
            _panel.Position = (GetViewportRect().Size - _panel.Size) / 2;
        }
    }

    private void Open(string title, Color color, string body, bool canContinue, bool canAdvance)
    {
        _title.Text = title;
        _title.AddThemeColorOverride("font_color", color);
        _body.Text = body;
        _continue.Visible = canContinue;
        _next.Visible = canAdvance;
        Visible = true;
        _panel.ResetSize();
        _panel.Position = (GetViewportRect().Size - _panel.Size) / 2;
    }

    private static string N(double v) => v.ToString("0", Pt);

    private static string Time(double seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";
}
