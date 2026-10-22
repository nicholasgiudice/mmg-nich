using Game.Core;
using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Godot;
using MmoRpgClient.Authority;
using MmoRpgClient.Data;
using MmoRpgClient.Hud;
using MmoRpgClient.Input;
using MmoRpgClient.Match;
using MmoRpgClient.Presentation;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient;

/// <summary>
/// Cena da partida: monta a autoridade a partir do menu (<see cref="MatchSettings"/>) e liga input, apresentação,
/// câmera, pausa (Esc), reinício (R) e tela de resultado.
/// Argumentos de depuração (depois de <c>--</c> na linha de comando; pulam o menu):
/// <c>--encounter=id</c>, <c>--class=id</c>, <c>--seed=n</c>, <c>--time-scale=n</c>, <c>--fake-authority</c>,
/// <c>--ranks=nh1|focado|espalhado</c> e <c>--rank=habilidade:NH,...</c> (NH do jogador),
/// <c>--auto-move=x,y</c> (mantém as teclas de movimento pelo sinal de x e y), <c>--hold=attack,...</c> (mantém ações),
/// <c>--demo-input=attack,ability_1,...</c> (aperta ações em sequência mirando o inimigo mais próximo e
/// imprime as respostas da autoridade) e <c>--capture=caminho.png</c> (salva a tela e fecha; após 2 s, ou
/// <c>--capture-after=segundos</c>, ou ao fim do demo).
/// </summary>
public partial class Main : Node2D
{
    public const string ScenePath = "res://scenes/Main.tscn";

    private const string DefaultEncounter = "encounter.three_wolves";
    private const double CaptureDelaySeconds = 2.0;

    /// <summary>Tempo para ver o desfecho antes da tela de resultado.</summary>
    private const double ResultDelaySeconds = 1.5;

    private readonly Dictionary<string, string> _args = new(StringComparer.Ordinal);
    private IGameAuthority? _authority;
    private PlayerInput? _input;
    private DebugInputScript? _demo;
    private WorldView _worldView = null!;
    private CameraRig _camera = null!;
    private AimPreview _aimPreview = null!;
    private Label _info = null!;
    private Hud.Hud _hud = null!;
    private double _elapsed;
    private double _demoFinishedAt = -1;
    private ContentCatalog _content = ContentCatalog.Empty;
    private MatchSettings? _settings;
    private MatchTracker? _tracker;
    private MatchOverlay _overlay = null!;
    private string _encounterName = "";
    private bool _paused;
    private double _endedAt = -1;

    public override void _Ready()
    {
        ParseArgs();
        _worldView = GetNode<WorldView>("World");
        _aimPreview = new AimPreview { Name = "AimPreview" };
        _worldView.AddChild(_aimPreview);
        _camera = GetNode<CameraRig>("Camera");
        _info = GetNode<Label>("Hud/InfoLabel");
        _hud = GetNode<Hud.Hud>("Hud/HudRoot");
        _overlay = GetNode<MatchOverlay>("Hud/Overlay");
        _overlay.ContinueRequested += Resume;
        _overlay.RestartRequested += Restart;
        _overlay.NextRequested += NextEncounter;
        _overlay.MenuRequested += BackToMenu;

        foreach (var action in InputActions.All.Where(a => !InputMap.HasAction(a)))
        {
            GD.PushError($"Ação ausente no Input Map: {action}");
        }

        try
        {
            var (authority, map, content) = CreateAuthority();
            _authority = authority;
            _content = content;
            // T-064: a entrada usa as definições no NH do jogador (raio da prévia, alcance, cargas).
            var plan = _settings?.ClassId is { } cls && content.Classes.TryGetValue(cls, out var classDef)
                ? Match.SkillPlan.For(classDef, content).Ranks
                : new Dictionary<string, int>();
            _input = new PlayerInput(content.Abilities.ToDictionary(
                kv => kv.Key, kv => content.AbilityAt(kv.Key, plan.GetValueOrDefault(kv.Key, 1)) ?? kv.Value));
            _hud.SetContent(content);
            _worldView.SetContent(content);
            GetNode<ArenaView>("Arena").Map = map;
            _camera.SetBounds(map);
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            _info.Text = $"Core {CoreInfo.Version}\nErro ao iniciar: {ex.Message}";
        }

        if (_args.TryGetValue("time-scale", out var scale)
            && double.TryParse(scale, System.Globalization.CultureInfo.InvariantCulture, out var timeScale) && timeScale > 0)
        {
            Engine.TimeScale = timeScale; // só depuração: acelera a partida (a simulação continua em ticks fixos)
        }

        if (_args.ContainsKey("demo-input") || _args.ContainsKey("auto-move") || _args.ContainsKey("hold"))
        {
            var held = AutoMoveActions().Concat(
                _args.GetValueOrDefault("hold", "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            _demo = new DebugInputScript(_args.GetValueOrDefault("demo-input", ""), held);
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (!_paused && !_overlay.IsOpen)
        {
            _demo?.Update(delta); // antes de ler as teclas: o roteiro aperta ações neste frame
        }

        HandleMatchKeys();
        var running = !_paused && !_overlay.IsOpen;
        if (_authority is not null && _input is not null)
        {

            var mousePx = GetGlobalMousePosition();
            var aim = Units.ToWorld(mousePx);
            var hovered = _worldView.PickAt(mousePx);
            if ((_args.ContainsKey("demo-input") || _args.ContainsKey("hold")) && NearestEnemyPosition(_authority) is { } enemy)
            {
                aim = Units.ToWorld(enemy.Pixels);
                hovered = enemy.Id;
            }

            IReadOnlyList<GameEvent> events = Array.Empty<GameEvent>();
            if (running)
            {
                _input.Update(_authority, aim, hovered, _hud.IsPointerOverUi());
                _authority.Advance(delta);
                events = _authority.TakeEvents();
                UpdateOutcome(events);
            }

            if (_demo is not null)
            {
                LogAbilityEvents(_authority, events);
            }

            if (_args.ContainsKey("protocol-check"))
            {
                ProtocolCheck(_authority, events);
            }

            _worldView.SelectedEntity = _input.SelectedTarget;
            _worldView.Render(_authority, events);
            _hud.Render(_authority, _input.SelectedTarget, aim, events);
            if (_authority.ControlledEntity is { } player && _worldView.TryGetDrawnPosition(player, out var playerPx))
            {
                _camera.Follow(playerPx, Units.ToPixels(aim), delta);
                _aimPreview.Show(_input.PreviewAbility, playerPx, Units.ToPixels(_input.PreviewPoint));
            }

            _info.Text = $"Core {CoreInfo.Version} · {Engine.GetFramesPerSecond()} FPS · tick {_authority.CurrentTick}\n"
                + "WASD mover · Tab/clique seleciona e ataca sozinho · 1-5 habilidades · Q poção · R reiniciar · Esc pausa";
        }

        CaptureIfRequested();
    }

    private (IGameAuthority Authority, GridMap Map, ContentCatalog Content) CreateAuthority()
    {
        if (_args.ContainsKey("fake-authority"))
        {
            return (new FakeAuthority(), FakeAuthority.Map, ContentCatalog.Empty);
        }

        var content = ContentLoader.Load();
        var settings = MatchSettings.Current ?? new MatchSettings(
            _args.GetValueOrDefault("encounter", DefaultEncounter),
            _args.GetValueOrDefault("class"),
            _args.TryGetValue("seed", out var s) && ulong.TryParse(s, out var parsed) ? parsed : 1UL);
        // --ranks=perfil e --rank=id:n,... (depuração, T-064): ajustam o plano de NH da classe do jogador.
        var playerClass = settings.ClassId is { } c && content.Classes.ContainsKey(c)
            ? c
            : content.Encounters.TryGetValue(settings.EncounterId, out var enc) ? MatchSetup.SuggestedClass(enc) : null;
        if (playerClass is not null)
        {
            Match.SkillPlan.For(content.Classes[playerClass], content)
                .ApplyArgs(_args.GetValueOrDefault("ranks"), _args.GetValueOrDefault("rank"));
        }

        var match = MatchSetup.Create(content, settings);
        MatchSettings.Current = _settings = settings with { ClassId = match.PlayerClass };
        _encounterName = match.Encounter.Name;
        _tracker = new MatchTracker(match.Authority.ControlledEntity, match.Encounter.Defeat.TimeLimitTicks);
        return (match.Authority, match.Map, content);
    }

    /// <summary>Esc: limpa o alvo se houver (a ação do PlayerInput); senão pausa/continua. R: reinicia com a mesma seed.</summary>
    private void HandleMatchKeys()
    {
        if (_settings is null)
        {
            return;
        }

        if (Godot.Input.IsActionJustPressed(InputActions.Restart))
        {
            Restart();
            return;
        }

        if (Godot.Input.IsActionJustPressed(InputActions.Pause))
        {
            if (_paused)
            {
                Resume();
            }
            else if (!_overlay.IsOpen && _input?.SelectedTarget is null)
            {
                _paused = true;
                _overlay.ShowPause(_settings, _encounterName);
            }
        }
    }

    private void UpdateOutcome(IReadOnlyList<GameEvent> events)
    {
        if (_tracker is null || _settings is null || _authority is null)
        {
            return;
        }

        _tracker.Consume(events, _authority.Latest);
        if (_tracker.Outcome == MatchOutcome.InProgress)
        {
            return;
        }

        if (_endedAt < 0)
        {
            _endedAt = _elapsed;
            GD.Print($"[match] {_tracker.Outcome} em {_tracker.Seconds:0.0}s");
        }
        else if (_elapsed - _endedAt >= ResultDelaySeconds && _demo is null)
        {
            _overlay.ShowResult(_tracker, _settings, _encounterName, NextEncounterId() is not null);
        }
    }

    private void Resume()
    {
        _paused = false;
        _overlay.Close();
    }

    private void Restart() => GetTree().ReloadCurrentScene();

    private void BackToMenu() => GetTree().ChangeSceneToFile(MenuScreen.ScenePath);

    /// <summary>Próximo encontro da lista, com a classe sugerida dele e uma seed nova.</summary>
    private void NextEncounter()
    {
        if (NextEncounterId() is not { } next)
        {
            BackToMenu();
            return;
        }

        var encounter = _content.Encounters[next];
        MatchSettings.Current = new MatchSettings(next, MatchSetup.SuggestedClass(encounter), MatchSettings.RandomSeed());
        GetTree().ReloadCurrentScene();
    }

    private string? NextEncounterId()
    {
        var ordered = MenuScreen.OrderedEncounterIds(_content);
        var index = _settings is null ? -1 : ordered.IndexOf(_settings.EncounterId);
        return index >= 0 && index + 1 < ordered.Count ? ordered[index + 1] : null;
    }

    private bool _protocolChecked;

    /// <summary>
    /// <c>--protocol-check</c> (T-103): serializa o snapshot e os eventos do frame com o <c>Game.Protocol</c> dentro do
    /// Godot, lê de volta e imprime o tamanho — confirma que o MemoryPack funciona no runtime .NET do Godot.
    /// </summary>
    private void ProtocolCheck(IGameAuthority authority, IReadOnlyList<GameEvent> events)
    {
        if (_protocolChecked || _elapsed < 1.0 || authority.Latest is not { } latest)
        {
            return;
        }

        _protocolChecked = true;
        var snapshot = Game.Protocol.ProtocolSerializer.Serialize(Game.Protocol.SnapshotMessage.From(latest));
        var back = (Game.Protocol.SnapshotMessage)Game.Protocol.ProtocolSerializer.Deserialize(snapshot);
        var eventBytes = Game.Protocol.ProtocolSerializer.Serialize(new Game.Protocol.EventsMessage(latest.Tick, events.ToArray()));
        GD.Print($"[protocol] v{Game.Protocol.ProtocolVersion.Current}: snapshot {snapshot.Length} B, "
            + $"{back.Entities.Length}/{latest.Entities.Count} entidades de volta; {events.Count} eventos {eventBytes.Length} B");
    }

    private void CaptureIfRequested()
    {
        if (!_args.TryGetValue("capture", out var path))
        {
            return;
        }

        if (_demo is { Finished: true } && _demoFinishedAt < 0)
        {
            _demoFinishedAt = _elapsed;
        }

        // --capture-after manda; sem ele, com roteiro, fotografa logo depois do fim do roteiro.
        double? delay = _args.TryGetValue("capture-after", out var after)
            && double.TryParse(after, System.Globalization.CultureInfo.InvariantCulture, out var secs) ? secs : null;
        var due = delay is { } d ? _elapsed >= d
            : _demo is not null && _args.ContainsKey("demo-input") ? _demoFinishedAt >= 0 && _elapsed >= _demoFinishedAt + 0.3
            : _elapsed >= CaptureDelaySeconds;
        if (!due)
        {
            return;
        }

        _args.Remove("capture");
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"capture {path}: {error}");
        GetTree().Quit();
    }

    private (EntityId Id, Godot.Vector2 Pixels)? NearestEnemyPosition(IGameAuthority authority)
    {
        if (authority.ControlledEntity is not { } player || !_worldView.TryGetDrawnPosition(player, out var origin))
        {
            return null;
        }

        var faction = authority.Latest?.Entities.FirstOrDefault(e => e.Id == player)
            ?.TryGet<Game.Core.Simulation.Components.FactionTag>()?.Faction;
        return authority.Latest?.Entities
            .Where(e => e.Id != player
                && e.TryGet<Game.Core.Simulation.Components.FactionTag>()?.Faction is { } f && f != faction
                && e.TryGet<Game.Core.Simulation.Components.Health>() is { IsAlive: true })
            .Select(e => _worldView.TryGetDrawnPosition(e.Id, out var px) ? (e.Id, px) : ((EntityId, Godot.Vector2)?)null)
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .OrderBy(x => x.Item2.DistanceTo(origin))
            .Cast<(EntityId, Godot.Vector2)?>()
            .FirstOrDefault();
    }

    private static void LogAbilityEvents(IGameAuthority authority, IReadOnlyList<GameEvent> events)
    {
        foreach (var e in events)
        {
            var line = e switch
            {
                AbilityStarted s when s.Source == authority.ControlledEntity => $"started {s.AbilityId} target={s.Target} aim={s.AimPoint}",
                AbilityExecuted x when x.Source == authority.ControlledEntity => $"executed {x.AbilityId} targets={x.Targets.Count}",
                AbilityRejected r when r.Source == authority.ControlledEntity => $"rejected {r.AbilityId}: {r.Reason}",
                _ => null,
            };
            if (line is not null)
            {
                GD.Print($"[tick {e.Tick}] {line}");
            }
        }
    }

    private IEnumerable<string> AutoMoveActions()
    {
        if (!_args.TryGetValue("auto-move", out var text))
        {
            yield break;
        }

        var parts = text.Split(',');
        if (parts.Length != 2
            || !float.TryParse(parts[0], System.Globalization.CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var y))
        {
            yield break;
        }

        if (x > 0)
        {
            yield return InputActions.MoveRight;
        }
        else if (x < 0)
        {
            yield return InputActions.MoveLeft;
        }

        if (y > 0)
        {
            yield return InputActions.MoveDown;
        }
        else if (y < 0)
        {
            yield return InputActions.MoveUp;
        }
    }

    private void ParseArgs()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            var trimmed = arg.TrimStart('-');
            var eq = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (eq < 0)
            {
                _args[trimmed] = "";
            }
            else
            {
                _args[trimmed[..eq]] = trimmed[(eq + 1)..];
            }
        }
    }
}
