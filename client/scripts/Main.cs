using Game.Core;
using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Intents;
using Godot;
using MmoRpgClient.Authority;
using MmoRpgClient.Data;
using MmoRpgClient.Presentation;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient;

/// <summary>
/// Raiz do protótipo: escolhe a autoridade, liga a apresentação e lê o input temporário de movimento.
/// Argumentos (depois de <c>--</c> na linha de comando):
/// <c>--encounter=id</c>, <c>--seed=n</c>, <c>--fake-authority</c>,
/// <c>--auto-move=x,y</c> (move o jogador sem teclado) e <c>--capture=caminho.png</c> (salva a tela após 2 s e fecha).
/// </summary>
public partial class Main : Node2D
{
    private const string DefaultEncounter = "encounter.three_wolves";
    private const double CaptureDelaySeconds = 2.0;

    private readonly Dictionary<string, string> _args = new(StringComparer.Ordinal);
    private IGameAuthority? _authority;
    private WorldView _worldView = null!;
    private Label _info = null!;
    private System.Numerics.Vector2 _lastMove;
    private System.Numerics.Vector2? _autoMove;
    private double _elapsed;

    public override void _Ready()
    {
        ParseArgs();
        _worldView = GetNode<WorldView>("World");
        _worldView.Camera = GetNode<Camera2D>("Camera");
        _info = GetNode<Label>("Hud/InfoLabel");
        _autoMove = _args.TryGetValue("auto-move", out var move) ? ParseVector(move) : null;

        try
        {
            var (authority, map) = CreateAuthority();
            _authority = authority;
            GetNode<ArenaView>("Arena").Map = map;
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            _info.Text = $"Core {CoreInfo.Version}\nErro ao iniciar: {ex.Message}";
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_authority is not null)
        {
            SendMoveIntent(_authority);
            _authority.Advance(delta);
            _worldView.Render(_authority, _authority.TakeEvents());
            _info.Text = $"Core {CoreInfo.Version} · {Engine.GetFramesPerSecond()} FPS · tick {_authority.CurrentTick}\nWASD/setas: mover (temporário)";
        }

        if (_args.TryGetValue("capture", out var path) && _elapsed >= CaptureDelaySeconds)
        {
            _args.Remove("capture");
            var error = GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"capture {path}: {error}");
            GetTree().Quit();
        }
    }

    private (IGameAuthority Authority, GridMap Map) CreateAuthority()
    {
        if (_args.ContainsKey("fake-authority"))
        {
            return (new FakeAuthority(), FakeAuthority.Map);
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
        return (new LocalAuthority(setup.World, setup.Players.FirstOrDefault()?.Id), map);
    }

    /// <summary>Input temporário (a T-021 substitui): envia intenção só quando a direção muda.</summary>
    private void SendMoveIntent(IGameAuthority authority)
    {
        if (authority.ControlledEntity is not { } player)
        {
            return;
        }

        var dir = _autoMove ?? ReadMoveKeys();
        if (dir == _lastMove)
        {
            return;
        }

        _lastMove = dir;
        authority.Send(dir == System.Numerics.Vector2.Zero
            ? new StopIntent(player)
            : new MoveIntent(player, System.Numerics.Vector2.Normalize(dir)));
    }

    private static System.Numerics.Vector2 ReadMoveKeys()
    {
        var x = (Pressed(Key.D, Key.Right) ? 1 : 0) - (Pressed(Key.A, Key.Left) ? 1 : 0);
        var y = (Pressed(Key.S, Key.Down) ? 1 : 0) - (Pressed(Key.W, Key.Up) ? 1 : 0);
        return new System.Numerics.Vector2(x, y);
    }

    private static bool Pressed(Key a, Key b) => Input.IsPhysicalKeyPressed(a) || Input.IsPhysicalKeyPressed(b);

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

    private static System.Numerics.Vector2? ParseVector(string text)
    {
        var parts = text.Split(',');
        return parts.Length == 2
            && float.TryParse(parts[0], System.Globalization.CultureInfo.InvariantCulture, out var x)
            && float.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var y)
            ? new System.Numerics.Vector2(x, y)
            : null;
    }
}
