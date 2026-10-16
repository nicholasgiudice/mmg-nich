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
using MmoRpgClient.Presentation;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient;

/// <summary>
/// Raiz do protótipo: escolhe a autoridade, liga input, apresentação e câmera.
/// Argumentos (depois de <c>--</c> na linha de comando):
/// <c>--encounter=id</c>, <c>--seed=n</c>, <c>--fake-authority</c>,
/// <c>--auto-move=x,y</c> (mantém as teclas de movimento pelo sinal de x e y),
/// <c>--demo-input=attack,ability_1,...</c> (aperta ações em sequência mirando o inimigo mais próximo e
/// imprime as respostas da autoridade) e <c>--capture=caminho.png</c> (salva a tela e fecha; após 2 s, ou
/// <c>--capture-after=segundos</c>, ou ao fim do demo).
/// </summary>
public partial class Main : Node2D
{
    private const string DefaultEncounter = "encounter.three_wolves";
    private const double CaptureDelaySeconds = 2.0;

    private readonly Dictionary<string, string> _args = new(StringComparer.Ordinal);
    private IGameAuthority? _authority;
    private PlayerInput? _input;
    private DebugInputScript? _demo;
    private WorldView _worldView = null!;
    private CameraRig _camera = null!;
    private Label _info = null!;
    private Hud.Hud _hud = null!;
    private double _elapsed;
    private double _demoFinishedAt = -1;

    public override void _Ready()
    {
        ParseArgs();
        _worldView = GetNode<WorldView>("World");
        _camera = GetNode<CameraRig>("Camera");
        _info = GetNode<Label>("Hud/InfoLabel");
        _hud = GetNode<Hud.Hud>("Hud/HudRoot");

        foreach (var action in InputActions.All.Where(a => !InputMap.HasAction(a)))
        {
            GD.PushError($"Ação ausente no Input Map: {action}");
        }

        try
        {
            var (authority, map, content) = CreateAuthority();
            _authority = authority;
            _input = new PlayerInput(content.Abilities);
            _hud.SetContent(content);
            GetNode<ArenaView>("Arena").Map = map;
            _camera.SetBounds(map);
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            _info.Text = $"Core {CoreInfo.Version}\nErro ao iniciar: {ex.Message}";
        }

        if (_args.ContainsKey("demo-input") || _args.ContainsKey("auto-move"))
        {
            _demo = new DebugInputScript(_args.GetValueOrDefault("demo-input", ""), AutoMoveActions());
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_authority is not null && _input is not null)
        {
            _demo?.Update(delta);
            var mousePx = GetGlobalMousePosition();
            var aim = Units.ToWorld(mousePx);
            var hovered = _worldView.PickAt(mousePx);
            if (_args.ContainsKey("demo-input") && NearestEnemyPosition(_authority) is { } enemy)
            {
                aim = Units.ToWorld(enemy.Pixels);
                hovered = enemy.Id;
            }

            _input.Update(_authority, aim, hovered, _hud.IsPointerOverUi());
            _authority.Advance(delta);

            var events = _authority.TakeEvents();
            if (_demo is not null)
            {
                LogAbilityEvents(_authority, events);
            }

            _worldView.SelectedEntity = _input.SelectedTarget;
            _worldView.Render(_authority, events);
            _hud.Render(_authority, _input.SelectedTarget, aim);
            if (_authority.ControlledEntity is { } player && _worldView.TryGetDrawnPosition(player, out var playerPx))
            {
                _camera.Follow(playerPx, Units.ToPixels(aim), delta);
            }

            _info.Text = $"Core {CoreInfo.Version} · {Engine.GetFramesPerSecond()} FPS · tick {_authority.CurrentTick}\n"
                + "WASD mover · clique atacar · 1-4 habilidades · Q poção · Tab alvo · Esc limpar";
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
        var encounterId = _args.GetValueOrDefault("encounter", DefaultEncounter);
        if (!content.Encounters.TryGetValue(encounterId, out var encounter))
        {
            throw new InvalidOperationException($"Encontro desconhecido: {encounterId}");
        }

        var seed = _args.TryGetValue("seed", out var s) && ulong.TryParse(s, out var parsed) ? parsed : 1UL;
        var setup = EncounterSetup.Create(seed, content, encounter);
        var map = setup.World.Map ?? EntityFactory.BuildMap(content.Arenas[encounter.Arena]);
        return (new LocalAuthority(setup.World, setup.Players.FirstOrDefault()?.Id), map, content);
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

        var due = _demo is not null && _args.ContainsKey("demo-input")
            ? _demoFinishedAt >= 0 && _elapsed >= _demoFinishedAt + 0.3
            : _elapsed >= (_args.TryGetValue("capture-after", out var after)
                && double.TryParse(after, System.Globalization.CultureInfo.InvariantCulture, out var secs) ? secs : CaptureDelaySeconds);
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
