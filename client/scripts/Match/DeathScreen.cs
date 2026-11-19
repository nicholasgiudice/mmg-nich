using System.Globalization;
using System.Text;
using Game.Core.Combat;
using Game.Core.CombatLog;
using Godot;
using MmoRpgClient.Hud;

namespace MmoRpgClient.Match;

/// <summary>
/// Tela de morte do mundo aberto (T-397): a tela escurece e perde a cor aos poucos e, no centro, uma janela de pedra e ouro
/// com o emblema, "Você morreu", a zona, o golpe final, o resumo de dano e o botão "Retornar à Cidade". O renascimento é
/// do servidor (<c>RespawnRequest</c>); a tela só mostra e pede.
/// </summary>
public partial class DeathScreen : Control
{
    /// <summary>Tempo para escurecer e tirar a cor (s).</summary>
    private const double FadeSeconds = 1.0;

    /// <summary>Quanto a tela escurece no fim (0 a 1).</summary>
    private const float DarkAlpha = 0.45f;

    /// <summary>Fontes de dano mostradas no resumo.</summary>
    private const int MaxSources = 4;

    private const string EmblemFile = "death_emblem.png";

    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Color TitleColor = new(1f, 0.62f, 0.55f);

    private readonly ColorRect _gray = new() { AnchorRight = 1, AnchorBottom = 1, MouseFilter = MouseFilterEnum.Ignore };
    private readonly ColorRect _dark = new() { AnchorRight = 1, AnchorBottom = 1, MouseFilter = MouseFilterEnum.Ignore };
    private readonly PanelContainer _window = new() { MouseFilter = MouseFilterEnum.Stop };
    private readonly Label _zone = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _killer = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly RichTextLabel _summary = new();
    private readonly Button _respawn = new() { Text = "Retornar à Cidade" };
    private ShaderMaterial? _grayMaterial;
    private double _shownFor = -1;

    /// <summary>Botão "Retornar à Cidade".</summary>
    public event Action? RespawnRequested;

    /// <summary>Nome exibido de uma habilidade/status/área pelo id.</summary>
    public Func<string, string> SourceLabel { get; set; } = id => id;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;

        // Tira a cor do que está por baixo (mundo e HUD) com a textura da tela.
        _grayMaterial = new ShaderMaterial { Shader = new Shader { Code = GrayShader } };
        _gray.Material = _grayMaterial;
        AddChild(_gray);
        _dark.Color = new Color(0.08f, 0f, 0f, 0f);
        AddChild(_dark);

        _window.AddThemeStyleboxOverride("panel", HudTheme.WindowFrame() ?? new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.04f, 0.04f, 0.95f),
            ContentMarginLeft = 18,
            ContentMarginRight = 18,
            ContentMarginTop = 12,
            ContentMarginBottom = 14,
        });
        AddChild(_window);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        _window.AddChild(box);

        var title = new Label { Text = "Você morreu", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", TitleColor);
        title.AddThemeColorOverride("font_outline_color", Colors.Black);
        title.AddThemeConstantOverride("outline_size", 5);
        if (HudTheme.WindowTitlePlate() is { } plate)
        {
            var holder = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            holder.AddThemeStyleboxOverride("panel", plate);
            holder.AddChild(title);
            box.AddChild(holder);
        }
        else
        {
            box.AddChild(title);
        }

        if (HudTheme.UiTexture(EmblemFile) is { } emblem)
        {
            box.AddChild(new TextureRect
            {
                Texture = emblem,
                TextureFilter = TextureFilterEnum.Nearest,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = emblem.GetSize() * 2,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            });
        }

        foreach (var (label, size, color) in new[] { (_zone, 15, HudTheme.StoneText), (_killer, 14, new Color(1f, 0.72f, 0.66f)) })
        {
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeColorOverride("font_color", color);
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            label.AddThemeConstantOverride("outline_size", 4);
            box.AddChild(label);
        }

        _summary.BbcodeEnabled = true;
        _summary.FitContent = true;
        _summary.ScrollActive = false;
        _summary.CustomMinimumSize = new Vector2(380, 0);
        _summary.AddThemeFontSizeOverride("normal_font_size", 12);
        _summary.AddThemeFontSizeOverride("bold_font_size", 13);
        box.AddChild(_summary);

        var note = new Label
        {
            Text = "Você volta à Cidade com vida e Mana cheias. Nada é perdido.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(380, 0),
            Modulate = new Color(1, 1, 1, 0.8f),
        };
        note.AddThemeFontSizeOverride("font_size", 12);
        box.AddChild(note);

        HudTheme.StoneButton(_respawn);
        _respawn.FocusMode = FocusModeEnum.None;
        _respawn.CustomMinimumSize = new Vector2(200, 36);
        _respawn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _respawn.AddThemeFontSizeOverride("font_size", 15);
        _respawn.Pressed += () => RespawnRequested?.Invoke();
        box.AddChild(_respawn);
    }

    /// <summary>Abre a tela (a morte acabou de acontecer); o resumo chega depois por <see cref="SetSummary"/>.</summary>
    public void Open(string zoneName)
    {
        if (Visible)
        {
            return;
        }

        _zone.Text = $"em {zoneName}";
        _killer.Text = "";
        _killer.Visible = false;
        _summary.Text = "";
        _summary.Visible = false;
        _shownFor = 0;
        Visible = true;
        Apply();
    }

    public void Close()
    {
        Visible = false;
        _shownFor = -1;
    }

    /// <summary>Resumo de dano da morte do jogador (log de combate): golpe final e dano por fonte.</summary>
    public void SetSummary(DeathSummary summary)
    {
        var killer = KillingBlow(summary);
        _killer.Text = killer is null ? "" : $"Golpe final: {killer}";
        _killer.Visible = killer is not null;
        _summary.Text = Format(summary);
        _summary.Visible = _summary.Text.Length > 0;
        _window.ResetSize();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        _shownFor += delta;
        Apply();
    }

    private void Apply()
    {
        var t = (float)Math.Clamp(_shownFor / FadeSeconds, 0, 1);
        _grayMaterial?.SetShaderParameter("amount", t * 0.85f);
        _dark.Color = new Color(_dark.Color, t * DarkAlpha);
        _window.Modulate = new Color(1, 1, 1, Math.Clamp((float)(_shownFor - 0.3) / 0.5f, 0, 1));
        _window.ResetSize();
        var screen = GetViewportRect().Size;
        // A ZoneScreen fica numa CanvasLayer e pode não ter tamanho: a tela cobre a janela inteira por conta própria.
        Position = Vector2.Zero;
        Size = screen;
        _gray.Size = _dark.Size = screen;
        _window.Position = new Vector2((screen.X - _window.Size.X) / 2, Mathf.Max(8, (screen.Y - _window.Size.Y) / 2 - 30));
    }

    /// <summary>"Lobo Cinzento — Mordida": quem causou o último dano antes da morte.</summary>
    private string? KillingBlow(DeathSummary summary)
    {
        var hits = summary.Entries.Select(e => e.Event).OfType<DamageDealt>().Where(d => d.Target == summary.Victim).ToList();
        var last = hits.LastOrDefault(d => d.Killed) ?? hits.LastOrDefault();
        if (last is null)
        {
            return null;
        }

        var source = summary.DamageBySources.FirstOrDefault(s => s.Source == last.Source && s.SourceId == last.SourceId);
        var who = source?.SourceName ?? "";
        var what = SourceLabel(last.SourceId);
        return who.Length == 0 ? what : $"{who} — {what}";
    }

    private string Format(DeathSummary summary)
    {
        if (summary.DamageBySources.Count == 0)
        {
            return "";
        }

        var total = summary.DamageBySources.Sum(d => d.Amount);
        var sb = new StringBuilder("[b]Dano recebido nos últimos segundos[/b]\n");
        foreach (var d in summary.DamageBySources.Take(MaxSources))
        {
            var share = total > 0 ? d.Amount / total : 0;
            var hits = d.Hits == 1 ? "1 golpe" : $"{d.Hits} golpes";
            sb.Append($"  [color=#ff9c8c]{Hud.Hud.Escape(d.SourceName)}[/color] — {Hud.Hud.Escape(SourceLabel(d.SourceId))}: ")
                .Append($"{d.Amount.ToString("0", Pt)} ({share.ToString("P0", Pt)}) · {hits}\n");
        }

        var rest = summary.DamageBySources.Count - MaxSources;
        if (rest > 0)
        {
            sb.Append($"  [color=#888888]e mais {rest} fonte(s)[/color]\n");
        }

        return sb.ToString().TrimEnd('\n');
    }

    private const string GrayShader = """
        shader_type canvas_item;
        uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
        uniform float amount = 0.0;
        void fragment() {
            vec4 c = textureLod(screen_texture, SCREEN_UV, 0.0);
            float g = dot(c.rgb, vec3(0.299, 0.587, 0.114));
            COLOR = vec4(mix(c.rgb, vec3(g), amount), 1.0);
        }
        """;
}
