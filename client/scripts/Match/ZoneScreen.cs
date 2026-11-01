using Game.Core.Combat;
using Game.Core.Definitions;
using Godot;

namespace MmoRpgClient.Match;

/// <summary>
/// Mundo aberto (T-223), por cima do jogo: transição curta ao entrar numa zona (tela escura com o nome, que some),
/// nome da zona no canto e, com o personagem morto, o aviso com o botão "Renascer na Cidade".
/// </summary>
public partial class ZoneScreen : Control
{
    /// <summary>Tempo com a tela escura antes de começar a clarear (s).</summary>
    private const double HoldSeconds = 0.35;

    /// <summary>Tempo para clarear (s).</summary>
    private const double FadeSeconds = 0.6;

    private readonly ColorRect _curtain = new() { Color = Colors.Black, AnchorRight = 1, AnchorBottom = 1, MouseFilter = MouseFilterEnum.Ignore };
    private readonly Label _title = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _subtitle = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _corner = new() { HorizontalAlignment = HorizontalAlignment.Right };
    private readonly PanelContainer _death = new() { Visible = false };
    private readonly Label _deathText = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private double _transition = -1;

    /// <summary>Botão "Renascer na Cidade".</summary>
    public event Action? RespawnRequested;

    /// <summary>A tela escura da transição ainda está aparecendo.</summary>
    public bool InTransition => _transition >= 0;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        _curtain.Visible = false;
        AddChild(_curtain);

        foreach (var (label, size) in new[] { (_title, 34), (_subtitle, 16), (_corner, 15) })
        {
            label.MouseFilter = MouseFilterEnum.Ignore;
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeConstantOverride("outline_size", 6);
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            AddChild(label);
        }

        _title.AddThemeColorOverride("font_color", new Color(0.98f, 0.88f, 0.6f));
        _subtitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        _corner.AddThemeColorOverride("font_color", new Color(0.98f, 0.88f, 0.6f));
        _title.Visible = _subtitle.Visible = false;

        _death.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.04f, 0.04f, 0.92f),
            BorderColor = new Color(0.8f, 0.25f, 0.2f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            ContentMarginLeft = 14,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        });

        // Faixa compacta no alto: o resumo da morte (HUD) fica no centro da tela.
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 14);
        _death.AddChild(row);
        _deathText.VerticalAlignment = VerticalAlignment.Center;
        _deathText.AddThemeFontSizeOverride("font_size", 17);
        _deathText.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.5f));
        row.AddChild(_deathText);

        var respawn = new Button { Text = "Renascer na Cidade", TooltipText = "Volta à Cidade com vida e Mana cheias.", FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(190, 34) };
        respawn.Pressed += () => RespawnRequested?.Invoke();
        row.AddChild(respawn);
        AddChild(_death);
    }

    /// <summary>Entrou numa zona (Welcome novo): começa a transição e troca o nome do canto.</summary>
    public void Enter(ZoneDefinition zone, ContentCatalog content)
    {
        var subtitle = Subtitle(zone, content);
        _title.Text = zone.Name;
        _subtitle.Text = subtitle;
        _corner.Text = $"{zone.Name}\n{subtitle}";
        _transition = 0;
        _curtain.Visible = _title.Visible = _subtitle.Visible = true;
        SetDeath(false);
        Visible = true;
        Apply();
    }

    /// <summary>Fora do mundo aberto (sala de encontro): nada aparece.</summary>
    public void Leave()
    {
        _transition = -1;
        Visible = false;
    }

    /// <summary>Mostra ou esconde o aviso de morte.</summary>
    public void SetDeath(bool dead, string zoneName = "")
    {
        if (dead)
        {
            _deathText.Text = $"Você morreu em {zoneName}.";
        }

        _death.Visible = dead;
    }

    public bool DeathVisible => _death.Visible;

    public override void _Process(double delta)
    {
        if (_transition >= 0)
        {
            _transition += delta;
            if (_transition >= HoldSeconds + FadeSeconds)
            {
                _transition = -1;
                _curtain.Visible = _title.Visible = _subtitle.Visible = false;
            }
        }

        Apply();
    }

    private void Apply()
    {
        var screen = GetViewportRect().Size;
        if (_transition >= 0)
        {
            var alpha = (float)Math.Clamp(1 - (_transition - HoldSeconds) / FadeSeconds, 0, 1);
            _curtain.Color = new Color(0, 0, 0, alpha);
            _title.Modulate = _subtitle.Modulate = new Color(1, 1, 1, alpha);
            _title.ResetSize();
            _subtitle.ResetSize();
            _title.Position = new Vector2((screen.X - _title.Size.X) / 2, screen.Y * 0.42f);
            _subtitle.Position = new Vector2((screen.X - _subtitle.Size.X) / 2, _title.Position.Y + _title.Size.Y + 2);
        }

        _corner.ResetSize();
        _corner.Position = new Vector2(screen.X - _corner.Size.X - 12, 8);
        if (_death.Visible)
        {
            _death.ResetSize();
            _death.Position = new Vector2((screen.X - _death.Size.X) / 2, 8);
        }
    }

    /// <summary>"Zona segura" na Cidade; nos campos, a faixa de nível dos monstros.</summary>
    public static string Subtitle(ZoneDefinition zone, ContentCatalog content)
    {
        if (zone.Kind == ZoneKind.Safe)
        {
            return "Zona segura";
        }

        var levels = zone.MonsterSpawns
            .Select(s => s.Level ?? (content.Monsters.TryGetValue(s.Monster, out var m) ? m.Level : 1))
            .ToList();
        if (levels.Count == 0)
        {
            return "Área aberta";
        }

        var (min, max) = (levels.Min(), levels.Max());
        return min == max ? $"Monstros nível {min}" : $"Monstros nível {min} a {max}";
    }
}
