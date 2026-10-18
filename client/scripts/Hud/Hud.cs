using System.Globalization;
using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Combat.Phases;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;
using MmoRpgClient.Authority;
using Vec2 = System.Numerics.Vector2;

namespace MmoRpgClient.Hud;

/// <summary>
/// HUD do protótipo. Só lê estado (snapshots) e eventos da autoridade; nunca altera a simulação.
/// Quadro do jogador, barra de habilidades, conjuração, quadro do alvo, quadros de grupo, log e resumo de morte.
/// </summary>
public partial class Hud : Control
{
    private const float FrameWidth = 240f;
    private const double BannerSeconds = 3.0;

    /// <summary>Largura reservada à linha de ajuda no canto inferior esquerdo.</summary>
    private const float HelpTextWidth = 410f;

    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    private readonly HashSet<string> _removableTags = new(StringComparer.Ordinal);
    private ContentCatalog _content = ContentCatalog.Empty;

    private UnitFrame _player = null!;
    private UnitFrame _target = null!;
    private VBoxContainer _party = null!;
    private readonly List<UnitFrame> _partyFrames = new();
    private HBoxContainer _abilityBar = null!;
    private readonly List<AbilitySlot> _slots = new();
    private HudBar _playerCast = null!;
    private CombatLogPanel _log = null!;
    private DeathSummaryPanel _death = null!;
    private Label _banner = null!;
    private double _bannerLeft;
    private double _castFlashLeft;
    private string _castFlashText = "";

    /// <summary>Quanto tempo a barra de conjuração fica vermelha depois de uma interrupção.</summary>
    private const double CastFlashSeconds = 0.8;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _player = new UnitFrame(FrameWidth, showResource: true) { Position = new Vector2(8, 8) };
        AddChild(_player);

        _party = new VBoxContainer { Position = new Vector2(8, 104) };
        _party.AddThemeConstantOverride("separation", 6);
        _party.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_party);

        _target = new UnitFrame(FrameWidth + 40, showResource: false, showCast: true);
        AddChild(_target);

        _abilityBar = new HBoxContainer();
        _abilityBar.AddThemeConstantOverride("separation", 4);
        AddChild(_abilityBar);

        _playerCast = new HudBar { Size = new Vector2(260, 16), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        AddChild(_playerCast);

        _log = new CombatLogPanel();
        AddChild(_log);

        _death = new DeathSummaryPanel { Visible = false, SourceLabel = SourceLabel };
        AddChild(_death);

        _banner = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        _banner.AddThemeFontSizeOverride("font_size", 26);
        _banner.AddThemeConstantOverride("outline_size", 8);
        _banner.AddThemeColorOverride("font_outline_color", Colors.Black);
        _banner.AddThemeColorOverride("font_color", new Color(1f, 0.55f, 0.95f));
        AddChild(_banner);
    }

    /// <summary>Aviso grande no topo da tela (ex.: mudança de fase do chefe), some sozinho.</summary>
    public void Announce(string text, double seconds = BannerSeconds)
    {
        _banner.Text = text;
        _banner.Visible = true;
        _bannerLeft = seconds;
        _banner.ResetSize();
    }

    public override void _Process(double delta)
    {
        _castFlashLeft = Math.Max(0, _castFlashLeft - delta);
        if (_bannerLeft <= 0)
        {
            return;
        }

        _bannerLeft -= delta;
        _banner.Modulate = new Color(1, 1, 1, Mathf.Clamp((float)_bannerLeft, 0f, 1f));
        _banner.Visible = _bannerLeft > 0;
    }

    /// <summary>Posiciona os elementos pelo tamanho atual da tela (chamado a cada frame; barato).</summary>
    private void Layout()
    {
        var screen = GetViewportRect().Size;
        var bar = _abilityBar.GetCombinedMinimumSize();

        // Centro do espaço livre entre a ajuda (canto inferior esquerdo) e o log (canto inferior direito).
        var freeLeft = Mathf.Min(HelpTextWidth, screen.X / 3);
        var freeRight = screen.X - _log.Size.X - 16;
        var x = freeRight - freeLeft >= bar.X ? freeLeft + (freeRight - freeLeft - bar.X) / 2 : (screen.X - bar.X) / 2;
        _abilityBar.Position = new Vector2(x, screen.Y - bar.Y - 8);
        _playerCast.Position = new Vector2((screen.X - _playerCast.Size.X) / 2, _abilityBar.Position.Y - _playerCast.Size.Y - 6);
        _target.Position = new Vector2((screen.X - _target.Size.X) / 2, 8);
        _log.Position = new Vector2(screen.X - _log.Size.X - 8, screen.Y - _log.Size.Y - 8);
        _death.Position = (screen - _death.Size) / 2;
        _banner.Position = new Vector2((screen.X - _banner.Size.X) / 2, screen.Y * 0.18f);
    }

    /// <summary>Conteúdo para nomes, custos, alcances e status removíveis (pode ser vazio).</summary>
    public void SetContent(ContentCatalog content)
    {
        _content = content;
        _removableTags.Clear();
        foreach (var cls in content.Classes.Values)
        {
            foreach (var id in cls.Abilities.Append(cls.BasicAttack))
            {
                if (!content.Abilities.TryGetValue(id, out var ability))
                {
                    continue;
                }

                foreach (var remove in ability.Effects.OfType<RemoveStatusEffect>())
                {
                    if (remove.Tag.Length > 0)
                    {
                        _removableTags.Add(remove.Tag);
                    }

                    _removableTags.UnionWith(remove.Tags);
                }
            }
        }
    }

    /// <summary>Atualiza o HUD. Chamar uma vez por frame, depois de <c>Advance</c>.</summary>
    public void Render(IGameAuthority authority, EntityId? selected, Vec2 aimPoint, IReadOnlyList<GameEvent> events)
    {
        var latest = authority.Latest;
        foreach (var cut in events.OfType<CastInterrupted>().Where(c => c.Entity == authority.ControlledEntity))
        {
            var ability = _content.Abilities.TryGetValue(cut.AbilityId, out var def) ? def.Name : cut.AbilityId;
            _castFlashText = cut.Reason == CastInterruptReason.Moved ? $"{ability} — interrompido (você se moveu)" : $"{ability} — interrompido";
            _castFlashLeft = CastFlashSeconds;
        }

        foreach (var phase in events.OfType<PhaseChanged>().Where(p => p.Previous >= 0))
        {
            var boss = latest?.Entities.FirstOrDefault(e => e.Id == phase.Entity)?.TryGet<UnitInfo>()?.Name
                ?? (_content.Monsters.TryGetValue(phase.MonsterId, out var m) ? m.Name : phase.MonsterId);
            Announce($"{boss} — Fase {phase.Index + 1}");
        }

        if (latest is null)
        {
            return;
        }

        Layout();
        var now = authority.CurrentTick + authority.InterpolationFraction;
        var byId = latest.Entities.ToDictionary(e => e.Id);
        EntitySnapshot? self = authority.ControlledEntity is { } pid && byId.TryGetValue(pid, out var s) ? s : null;

        if (self is not null)
        {
            _player.Visible = true;
            _player.Display(self, StatusText(self, now), now, _content);
            UpdateAbilityBar(self, byId, selected, aimPoint, now);
            UpdateCast(_playerCast, self, now, HudTheme.Cast);
            if (!_playerCast.Visible && _castFlashLeft > 0)
            {
                _playerCast.Visible = true;
                _playerCast.Set(1f, new Color(HudTheme.CastEnemy, (float)Math.Min(1, _castFlashLeft / 0.3)), _castFlashText);
            }
        }
        else
        {
            _player.Visible = false;
            _playerCast.Visible = false;
        }

        if (selected is { } tid && byId.TryGetValue(tid, out var target))
        {
            _target.Visible = true;
            _target.Display(target, StatusText(target, now), now, _content);
        }
        else
        {
            _target.Visible = false;
        }

        UpdateParty(latest, self, now);

        if (authority is ICombatLogSource source)
        {
            _log.Refresh(source.LogEntries);
            foreach (var death in source.TakeDeathSummaries())
            {
                if (death.Victim == authority.ControlledEntity)
                {
                    _death.ShowSummary(death);
                }
            }
        }
    }

    /// <summary>True se o mouse está sobre um controle do HUD que recebe clique (o clique não deve virar ataque).</summary>
    public bool IsPointerOverUi()
    {
        var hovered = GetViewport().GuiGetHoveredControl();
        return hovered is not null && hovered != this && hovered.MouseFilter != MouseFilterEnum.Ignore;
    }

    private void UpdateParty(WorldSnapshot latest, EntitySnapshot? self, double now)
    {
        var faction = self?.TryGet<FactionTag>()?.Faction ?? Faction.Player;
        var allies = latest.Entities
            .Where(e => e.Id != self?.Id && e.TryGet<FactionTag>()?.Faction == faction && e.TryGet<Health>() is not null
                && e.TryGet<ResourcePool>() is not null)
            .ToList();

        while (_partyFrames.Count < allies.Count)
        {
            var frame = new UnitFrame(FrameWidth - 40, showResource: true, compact: true);
            _partyFrames.Add(frame);
            _party.AddChild(frame);
        }

        for (var i = 0; i < _partyFrames.Count; i++)
        {
            _partyFrames[i].Visible = i < allies.Count;
            if (i < allies.Count)
            {
                _partyFrames[i].Display(allies[i], StatusText(allies[i], now), now, _content);
            }
        }
    }

    private void UpdateAbilityBar(EntitySnapshot self, IReadOnlyDictionary<EntityId, EntitySnapshot> byId, EntityId? selected,
        Vec2 aimPoint, double now)
    {
        var abilities = self.TryGet<AbilityState>();
        if (abilities is null)
        {
            return;
        }

        var entries = new List<(string Id, string Key)>();
        if (abilities.BasicAttackId is { } basic)
        {
            entries.Add((basic, "M1"));
        }

        var known = abilities.Known.Where(a => a != abilities.BasicAttackId).ToList();
        for (var i = 0; i < known.Count && i < Input.InputActions.AbilitySlots.Length; i++)
        {
            entries.Add((known[i], (i + 1).ToString(CultureInfo.InvariantCulture)));
        }

        if (abilities.Consumables.Count > 0)
        {
            entries.Add((abilities.Consumables[0], "Q"));
        }

        while (_slots.Count < entries.Count)
        {
            var slot = new AbilitySlot { MouseFilter = MouseFilterEnum.Pass };
            _slots.Add(slot);
            _abilityBar.AddChild(slot);
        }

        for (var i = 0; i < _slots.Count; i++)
        {
            _slots[i].Visible = i < entries.Count;
            if (i < entries.Count)
            {
                var (id, key) = entries[i];
                var (state, tooltip) = SlotState(self, abilities, byId, selected, aimPoint, now, id, key);
                _slots[i].SetState(state, tooltip);
            }
        }

    }

    private (AbilitySlotState, string) SlotState(EntitySnapshot self, AbilityState abilities,
        IReadOnlyDictionary<EntityId, EntitySnapshot> byId, EntityId? selected, Vec2 aimPoint, double now, string id, string key)
    {
        _content.Abilities.TryGetValue(id, out var def);
        var name = def?.Name ?? id;
        var remaining = abilities.ReadyAtTick.TryGetValue(id, out var ready) ? Math.Max(0, ready - now) : 0;
        var cdFraction = def is { CooldownTicks: > 0 } ? (float)Math.Clamp(remaining / def.CooldownTicks, 0, 1) : 0f;
        var pool = self.TryGet<ResourcePool>();
        var noResource = def?.Cost is { } cost && (pool is null || pool.Type != cost.Resource || pool.Current < cost.Amount);
        var charges = def?.Charges is { } max
            ? Math.Max(0, max - (abilities.ChargesUsed.TryGetValue(id, out var used) ? used : 0)).ToString(CultureInfo.InvariantCulture)
            : null;

        var state = new AbilitySlotState(
            Abbreviate(name),
            key,
            def?.Cost is { } c ? ((int)c.Amount).ToString(CultureInfo.InvariantCulture) : "",
            cdFraction,
            (float)(remaining / World.TickRate),
            noResource,
            IsOutOfRange(def, self, byId, selected, aimPoint),
            abilities.Cast?.AbilityId == id,
            charges);

        var tooltip = name;
        if (def is not null)
        {
            tooltip += def.Cost is { } tc ? $"\nCusto: {tc.Amount.ToString("0", Pt)} {HudTheme.ResourceName(tc.Resource)}" : "";
            tooltip += def.CooldownTicks > 0 ? $"\nRecarga: {(def.CooldownTicks / (double)World.TickRate).ToString("0.#", Pt)} s" : "";
            tooltip += def.CastTimeTicks > 0 ? $"\nConjuração: {(def.CastTimeTicks / (double)World.TickRate).ToString("0.#", Pt)} s" : "";
        }

        return (state, tooltip);
    }

    /// <summary>Só indicativo (a autoridade decide): alvo único contra o alvo selecionado. Área em ponto nunca fica fora de alcance (T-028: a mira é limitada ao alcance).</summary>
    private static bool IsOutOfRange(AbilityDefinition? def, EntitySnapshot self, IReadOnlyDictionary<EntityId, EntitySnapshot> byId,
        EntityId? selected, Vec2 aimPoint)
    {
        var origin = self.Get<Transform>().Position;
        switch (def?.Targeting)
        {
            case UnitTargeting { Faction: not FactionFilter.Ally } unit
                when selected is { } sid && byId.TryGetValue(sid, out var t) && t.TryGet<Transform>() is { } tt:
                var radius = t.TryGet<Collider>()?.Radius ?? 0f;
                return Vec2.Distance(origin, tt.Position) - radius > unit.Range;
            default:
                return false;
        }
    }

    private void UpdateCast(HudBar bar, EntitySnapshot unit, double now, Color color)
    {
        var cast = unit.TryGet<AbilityState>()?.Cast;
        if (cast is null || cast.EndTick <= cast.StartTick)
        {
            bar.Visible = false;
            return;
        }

        bar.Visible = true;
        var fraction = (float)((now - cast.StartTick) / (cast.EndTick - cast.StartTick));
        var name = _content.Abilities.TryGetValue(cast.AbilityId, out var def) ? def.Name : cast.AbilityId;
        var left = Math.Max(0, (cast.EndTick - now) / World.TickRate);
        bar.Set(fraction, color, $"{name}  {left.ToString("0.0", Pt)} s");
    }

    /// <summary>Status ativos em BBCode; os que o grupo pode remover aparecem destacados.</summary>
    private string StatusText(EntitySnapshot unit, double now)
    {
        var active = unit.TryGet<StatusEffects>()?.Active;
        if (active is null || active.Count == 0)
        {
            return "";
        }

        var parts = new List<string>();
        foreach (var st in active.Where(a => a.ExpiresAtTick > now))
        {
            _content.Statuses.TryGetValue(st.StatusId, out var def);
            var name = def?.Name ?? st.StatusId;
            var secs = ((st.ExpiresAtTick - now) / World.TickRate).ToString("0.0", Pt);
            var stacks = st.Stacks > 1 ? $" ×{st.Stacks}" : "";
            var text = Escape($"{name}{stacks} {secs}s");
            parts.Add(def is not null && def.Tags.Any(_removableTags.Contains)
                ? $"[color=#{HudTheme.Removable.ToHtml(false)}][b]◆ {text}[/b][/color]"
                : text);
        }

        return string.Join("  ", parts);
    }

    private static string Abbreviate(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 || char.IsUpper(w[0]))
            .ToList();
        return words.Count switch
        {
            0 => "?",
            1 => words[0][..Math.Min(3, words[0].Length)],
            _ => string.Concat(words.Take(3).Select(w => char.ToUpperInvariant(w[0]))),
        };
    }

    private string SourceLabel(string id) =>
        _content.Abilities.TryGetValue(id, out var a) ? a.Name
        : _content.Statuses.TryGetValue(id, out var s) ? s.Name
        : id;

    internal static string Escape(string text) => text.Replace("[", "[lb]", StringComparison.Ordinal);

    /// <summary>Quadro de uma unidade: nome, vida (+escudo), recurso, status e conjuração.</summary>
    private sealed partial class UnitFrame : PanelContainer
    {
        private readonly Label _name = new();
        private readonly HudBar _health = new();
        private readonly HudBar? _resource;
        private readonly HudBar? _cast;
        private readonly RichTextLabel _status = new();

        public UnitFrame(float width, bool showResource, bool showCast = false, bool compact = false)
        {
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = HudTheme.Panel,
                ContentMarginLeft = 6,
                ContentMarginRight = 6,
                ContentMarginTop = 4,
                ContentMarginBottom = 4,
            });
            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", 2);
            AddChild(box);

            _name.AddThemeFontSizeOverride("font_size", compact ? 11 : 13);
            box.AddChild(_name);

            _health.CustomMinimumSize = new Vector2(width, compact ? 12 : 18);
            _health.FontSize = compact ? 10 : 12;
            _health.MouseFilter = MouseFilterEnum.Ignore;
            box.AddChild(_health);

            if (showResource)
            {
                _resource = new HudBar { CustomMinimumSize = new Vector2(width, compact ? 8 : 12), FontSize = compact ? 0 : 10, MouseFilter = MouseFilterEnum.Ignore };
                box.AddChild(_resource);
            }

            if (showCast)
            {
                _cast = new HudBar { CustomMinimumSize = new Vector2(width, 14), FontSize = 10, MouseFilter = MouseFilterEnum.Ignore };
                box.AddChild(_cast);
            }

            _status.BbcodeEnabled = true;
            _status.FitContent = true;
            _status.ScrollActive = false;
            _status.CustomMinimumSize = new Vector2(width, 0);
            _status.AddThemeFontSizeOverride("normal_font_size", compact ? 10 : 11);
            _status.AddThemeFontSizeOverride("bold_font_size", compact ? 10 : 11);
            _status.MouseFilter = MouseFilterEnum.Ignore;
            box.AddChild(_status);
        }

        public void Display(EntitySnapshot unit, string statusBbcode, double now, ContentCatalog content)
        {
            var info = unit.TryGet<UnitInfo>();
            var rank = info?.Rank switch
            {
                MonsterRank.Elite => " (Elite)",
                MonsterRank.Boss => " (Chefe)",
                _ => "",
            };
            var name = (info?.Name ?? $"Entidade {unit.Id.Value}") + rank;
            if (_name.Text != name)
            {
                _name.Text = name;
            }

            var hp = unit.TryGet<Health>();
            if (hp is not null)
            {
                var shield = unit.TryGet<ShieldState>()?.Total ?? 0f;
                var frac = hp.Max > 0 ? hp.Current / hp.Max : 0f;
                var text = !hp.IsAlive ? "Morto"
                    : shield > 0 ? $"{hp.Current:0} / {hp.Max:0}  (+{shield:0})"
                    : $"{hp.Current:0} / {hp.Max:0}";
                _health.Set(frac, frac < 0.3f ? HudTheme.HealthLow : HudTheme.Health, text, hp.Max > 0 ? shield / hp.Max : 0f);
            }

            if (_resource is not null && unit.TryGet<ResourcePool>() is { } pool)
            {
                _resource.Visible = true;
                var label = _resource.FontSize > 0 ? $"{HudTheme.ResourceName(pool.Type)} {pool.Current:0}/{pool.Max:0}" : "";
                _resource.Set(pool.Max > 0 ? pool.Current / pool.Max : 0f, HudTheme.Resource(pool.Type), label);
            }
            else if (_resource is not null)
            {
                _resource.Visible = false;
            }

            if (_cast is not null)
            {
                var cast = unit.TryGet<AbilityState>()?.Cast;
                _cast.Visible = cast is not null && cast.EndTick > cast.StartTick;
                if (cast is not null && _cast.Visible)
                {
                    var abilityName = content.Abilities.TryGetValue(cast.AbilityId, out var def) ? def.Name : cast.AbilityId;
                    var left = Math.Max(0, (cast.EndTick - now) / World.TickRate);
                    _cast.Set((float)((now - cast.StartTick) / (cast.EndTick - cast.StartTick)), HudTheme.CastEnemy,
                        $"Conjurando: {abilityName}  {left.ToString("0.0", Pt)} s");
                }
            }

            _status.Visible = statusBbcode.Length > 0;
            if (_status.Text != statusBbcode)
            {
                _status.Text = statusBbcode;
            }
        }
    }
}
