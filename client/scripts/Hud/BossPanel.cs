using System.Globalization;
using Game.Core.Combat;
using Game.Core.Combat.Objectives;
using Game.Core.Combat.Phases;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Tela do chefe (T-345), no topo da tela, enquanto há um chefe vivo (rank <c>boss</c>) no snapshot: nome e fase, vida,
/// a conjuração dele e, se o encontro tem objetivos a proteger (os selos do Vigia, T-351), uma barra para cada um. A
/// conjuração que mira um objetivo (o Último Comando) fica destacada e pulsando: precisa ser interrompida.
/// </summary>
public partial class BossPanel : PanelContainer
{
    public const float PanelWidth = 440f;

    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Color MustInterrupt = new(1f, 0.25f, 0.2f);
    private static readonly Color SealIntact = new(0.85f, 0.72f, 0.35f);
    private static readonly Color SealCrumbling = new(0.95f, 0.55f, 0.25f);
    private static readonly Color SealBroken = new(0.35f, 0.35f, 0.38f);

    private readonly Label _name = new();
    private readonly HudBar _health = new() { CustomMinimumSize = new Vector2(PanelWidth, 18), FontSize = 12, MouseFilter = MouseFilterEnum.Ignore };
    private readonly HudBar _cast = new() { CustomMinimumSize = new Vector2(PanelWidth, 16), FontSize = 11, MouseFilter = MouseFilterEnum.Ignore };
    private readonly HBoxContainer _seals = new();
    private readonly List<(Label Label, HudBar Bar)> _sealViews = new();
    private double _time;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", HudTheme.StonePanel());
        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 4);
        AddChild(box);

        _name.HorizontalAlignment = HorizontalAlignment.Center;
        _name.AddThemeFontSizeOverride("font_size", 15);
        _name.AddThemeColorOverride("font_color", HudTheme.StoneText);
        _name.AddThemeConstantOverride("outline_size", 3);
        _name.AddThemeColorOverride("font_outline_color", Colors.Black);
        box.AddChild(_name);
        _health.BorderColor = HudTheme.BarBorderGold;
        box.AddChild(_health);
        _cast.BorderColor = HudTheme.BarBorderGold;
        box.AddChild(_cast);

        _seals.Alignment = BoxContainer.AlignmentMode.Center;
        _seals.AddThemeConstantOverride("separation", 10);
        _seals.MouseFilter = MouseFilterEnum.Ignore;
        box.AddChild(_seals);
    }

    public override void _Process(double delta) => _time += delta;

    /// <summary>Atualiza a partir do snapshot; esconde o painel sem chefe vivo à vista.</summary>
    public void Update(WorldSnapshot? latest, ContentCatalog content, double now)
    {
        var boss = latest?.Entities.FirstOrDefault(e =>
            e.TryGet<Health>() is { IsAlive: true }
            && e.TryGet<FactionTag>()?.Faction == Faction.Monster
            && e.TryGet<UnitInfo>() is { } info
            && content.Monsters.TryGetValue(info.DefinitionId, out var def) && def.Rank == MonsterRank.Boss);
        Visible = boss is not null;
        if (boss is null || latest is null)
        {
            return;
        }

        var unit = boss.Get<UnitInfo>();
        var monster = content.Monsters[unit.DefinitionId];
        var phase = boss.TryGet<PhaseState>()?.Index ?? -1;
        _name.Text = monster.Phases.Count > 1 && phase >= 0 ? $"{unit.Name} — Fase {phase + 1}" : unit.Name;
        var hp = boss.Get<Health>();
        _health.Set(hp.Max > 0 ? hp.Current / hp.Max : 0, HudTheme.CastEnemy, $"{N(hp.Current)} / {N(hp.Max)}");

        var objectives = latest.Entities.Where(e => e.TryGet<EncounterObjective>() is not null).OrderBy(e => e.Id.Value).ToList();
        UpdateCast(boss, objectives, content, now);
        UpdateSeals(objectives, content, now);
    }

    private void UpdateCast(EntitySnapshot boss, IReadOnlyList<EntitySnapshot> objectives, ContentCatalog content, double now)
    {
        var cast = boss.TryGet<AbilityState>()?.Cast;
        _cast.Visible = cast is not null && cast.EndTick > cast.StartTick;
        if (cast is null || !_cast.Visible)
        {
            return;
        }

        var fraction = (float)((now - cast.StartTick) / (cast.EndTick - cast.StartTick));
        var name = content.Abilities.TryGetValue(cast.AbilityId, out var def) ? def.Name : cast.AbilityId;
        var left = Math.Max(0, (cast.EndTick - now) / World.TickRate);
        if (cast.Target is { } target && objectives.Any(o => o.Id == target))
        {
            // Mira um selo: vermelho pulsando, com o pedido de interromper.
            var pulse = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin((float)_time * 6f));
            _cast.Set(fraction, new Color(MustInterrupt, pulse), $"INTERROMPA!  {name} — {left.ToString("0.0", Pt)} s");
        }
        else
        {
            _cast.Set(fraction, HudTheme.Cast, $"{name} — {left.ToString("0.0", Pt)} s");
        }
    }

    private void UpdateSeals(IReadOnlyList<EntitySnapshot> objectives, ContentCatalog content, double now)
    {
        _seals.Visible = objectives.Count > 0;
        while (_sealViews.Count < objectives.Count)
        {
            var cell = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            var label = new Label { HorizontalAlignment = HorizontalAlignment.Center };
            label.AddThemeFontSizeOverride("font_size", 11);
            label.AddThemeColorOverride("font_color", HudTheme.StoneText);
            var bar = new HudBar { CustomMinimumSize = new Vector2(130, 12), FontSize = 9, MouseFilter = MouseFilterEnum.Ignore, BorderColor = HudTheme.BarBorderGold };
            cell.AddChild(label);
            cell.AddChild(bar);
            _seals.AddChild(cell);
            _sealViews.Add((label, bar));
        }

        for (var i = 0; i < _sealViews.Count; i++)
        {
            var (label, bar) = _sealViews[i];
            var visible = i < objectives.Count;
            label.GetParent<Control>().Visible = visible;
            if (!visible)
            {
                continue;
            }

            var seal = objectives[i];
            var name = seal.TryGet<UnitInfo>()?.Name ?? "Selo";
            var hp = seal.TryGet<Health>();
            if (hp is not { IsAlive: true })
            {
                label.Text = $"{name} {i + 1}";
                bar.Set(1, SealBroken, "Quebrado");
                continue;
            }

            var intact = seal.TryGet<StatusEffects>()?.Active.Any(s => s.ExpiresAtTick > now
                && content.Statuses.TryGetValue(s.StatusId, out var st) && st.Tags.Contains("seal_intact")) ?? false;
            label.Text = $"{name} {i + 1}";
            var fill = hp.Max > 0 ? hp.Current / hp.Max : 0;
            bar.Set(fill, intact ? SealIntact : SealCrumbling, intact ? "Intacto" : $"{N(hp.Current)} / {N(hp.Max)}");
        }
    }

    private static string N(double v) => v.ToString("N0", Pt);
}
