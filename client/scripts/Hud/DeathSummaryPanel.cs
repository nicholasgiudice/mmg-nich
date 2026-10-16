using System.Globalization;
using System.Text;
using Game.Core.CombatLog;
using Game.Core.Simulation;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>Resumo exibido quando o jogador morre: dano por fonte e últimas entradas que o afetaram.</summary>
public partial class DeathSummaryPanel : PanelContainer
{
    private const float PanelWidth = 440f;

    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    private readonly RichTextLabel _text = new();

    /// <summary>Nome exibido de uma habilidade/status/área pelo id.</summary>
    public Func<string, string> SourceLabel { get; set; } = id => id;

    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.04f, 0.04f, 0.92f),
            BorderColor = new Color(0.8f, 0.25f, 0.2f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        });

        var box = new VBoxContainer();
        AddChild(box);

        _text.BbcodeEnabled = true;
        _text.FitContent = true;
        _text.CustomMinimumSize = new Vector2(PanelWidth, 0);
        _text.AddThemeFontSizeOverride("normal_font_size", 12);
        _text.AddThemeFontSizeOverride("bold_font_size", 15);
        box.AddChild(_text);

        var close = new Button { Text = "Fechar", FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        close.Pressed += Hide;
        box.AddChild(close);
    }

    public void ShowSummary(DeathSummary summary)
    {
        var sb = new StringBuilder();
        sb.Append("[center][b]Você morreu[/b][/center]\n\n");
        sb.Append("[b]Dano recebido nos últimos 5 s[/b]\n");
        if (summary.DamageBySources.Count == 0)
        {
            sb.Append("(nenhum)\n");
        }

        var total = summary.DamageBySources.Sum(d => d.Amount);
        var repeated = summary.DamageBySources
            .GroupBy(d => d.SourceName)
            .Where(g => g.Select(d => d.Source).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();
        foreach (var d in summary.DamageBySources)
        {
            // Várias fontes com o mesmo nome (3 lobos): o número da entidade separa cada uma.
            var sourceName = repeated.Contains(d.SourceName) && !d.Source.IsNone ? $"{d.SourceName} {d.Source}" : d.SourceName;
            var share = total > 0 ? d.Amount / total : 0;
            var absorbed = d.Absorbed > 0 ? $", {d.Absorbed.ToString("0", Pt)} absorvido" : "";
            var hits = d.Hits == 1 ? "1 golpe" : $"{d.Hits} golpes";
            sb.Append($"  [color=#ff9c8c]{Hud.Escape(sourceName)}[/color] — {Hud.Escape(SourceLabel(d.SourceId))}: ")
                .Append($"{d.Amount.ToString("0", Pt)} ({share.ToString("P0", Pt)}) — {hits}{absorbed}\n");
        }

        sb.Append("\n[b]Últimos eventos[/b]\n");
        foreach (var e in summary.Entries)
        {
            var time = (e.Tick / (double)World.TickRate).ToString("0.0", Pt);
            sb.Append($"  [color=#888888]{time}s[/color] [color=#{HudTheme.Category(e.Category).ToHtml(false)}]{Hud.Escape(e.Text)}[/color]\n");
        }

        _text.Text = sb.ToString();
        Visible = true;
        ResetSize();
    }
}
