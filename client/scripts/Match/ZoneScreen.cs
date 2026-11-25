using Game.Core.Combat;
using Game.Core.Definitions;
using Godot;

namespace MmoRpgClient.Match;

/// <summary>
/// Mundo aberto (T-223), por cima do jogo: transição curta ao entrar numa zona (tela escura com o nome, que some),
/// nome da zona no canto e, com o personagem morto, a tela de morte (<see cref="DeathScreen"/>, T-397).
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

    /// <summary>Onde termina o nome da zona no canto superior direito (px); 0 sem ele. A lista de batalha fica abaixo (T-312).</summary>
    public float CornerBottom => Visible && _corner.Visible && _corner.Text.Length > 0 ? _corner.Position.Y + _corner.Size.Y : 0;
    private readonly DeathScreen _death = new();
    private double _transition = -1;

    /// <summary>Botão "Retornar à Cidade" da tela de morte.</summary>
    public event Action? RespawnRequested
    {
        add => _death.RespawnRequested += value;
        remove => _death.RespawnRequested -= value;
    }

    /// <summary>Tela de morte (T-397), para o resumo de dano e o nome das fontes.</summary>
    public DeathScreen Death => _death;

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

    /// <summary>Mostra ou esconde a tela de morte.</summary>
    public void SetDeath(bool dead, string zoneName = "", string destination = "à Cidade")
    {
        if (dead)
        {
            _death.Open(zoneName, destination);
        }
        else
        {
            _death.Close();
        }
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
