using System.Numerics;
using Game.Core.Combat;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;
using MmoRpgClient.Authority;

namespace Game.Client.Tests;

/// <summary>
/// T-108: o preditor do cliente contra o mundo do Core de verdade, com atraso (e perda) de rede simulados nas duas
/// direções. O "servidor" aplica as intenções quando chegam e devolve snapshot + IntentAck como o ZoneRoom.
/// </summary>
public class MovementPredictorTests
{
    private static readonly PredictionParams Params = new(4.5f, 1f, 0.45f, false);

    private sealed class Net
    {
        private readonly World _world;
        private readonly Entity _player;
        private readonly int _up;
        private readonly int _down;
        private readonly Random _rng = new(3);
        private readonly List<(long At, uint Seq, Intent Intent)> _upQueue = new();
        private readonly List<(long At, long Tick, Vector2 Pos, (uint Seq, long Tick)? Ack)> _downQueue = new();
        private uint _seq;

        public Net(GridMap map, Vector2 start, int upTicks, int downTicks, double loss = 0)
        {
            _up = upTicks;
            _down = downTicks;
            Loss = loss;
            _world = WorldBuilder.CreateDefault(1, map, ContentCatalog.Empty);
            _player = _world.Spawn(new Transform { Position = start }, new Health(100), new FactionTag(Faction.Player), new Collider(Params.Radius), new Movement(Params.Speed));
            Predictor = new MovementPredictor(map);
            Predictor.Reset(start);
        }

        public MovementPredictor Predictor { get; }

        public double Loss { get; }

        public Vector2 ServerPosition => _player.Get<Transform>().Position;

        public float MaxCorrection { get; private set; }

        public int Snaps { get; private set; }

        public long Tick => _world.Tick;

        public void Teleport(Vector2 by) => _player.Get<Transform>().Position += by;

        /// <summary>O jogador aperta/solta (cliente): intenção vai para o servidor com atraso; o preditor aplica já.</summary>
        public void Input(Vector2 dir)
        {
            _seq++;
            Predictor.Sent(_seq, dir);
            Intent intent = dir == Vector2.Zero ? new StopIntent(_player.Id) : new MoveIntent(_player.Id, dir);
            _upQueue.Add((_world.Tick + _up, _seq, intent));
        }

        /// <summary>Um tick: servidor aplica o que chegou, avança e manda; cliente recebe o que chegou e prevê um tick.</summary>
        public void Step(double clientDt = World.TickSeconds)
        {
            uint? applied = null;
            foreach (var m in _upQueue.Where(m => m.At <= _world.Tick).ToList())
            {
                _upQueue.Remove(m);
                _world.Enqueue(m.Intent);
                applied = m.Seq;
            }

            _world.Step();
            if (_rng.NextDouble() >= Loss || applied is not null)
            {
                _downQueue.Add((_world.Tick + _down, _world.Tick, ServerPosition, applied is { } s ? (s, _world.Tick) : null));
            }

            foreach (var m in _downQueue.Where(m => m.At <= _world.Tick).ToList())
            {
                _downQueue.Remove(m);
                if (m.Ack is { } ack)
                {
                    Predictor.Acked(ack.Seq, ack.Tick);
                }

                Predictor.Reconcile(m.Tick, m.Pos, Params);
                MaxCorrection = Math.Max(MaxCorrection, Predictor.LastCorrection.Size);
                Snaps += Predictor.LastCorrection.Snapped ? 1 : 0;
            }

            Predictor.Advance(clientDt, Params);
        }

        public void Run(int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                Step();
            }
        }
    }

    private static GridMap Open() => GridMap.FromAscii(Enumerable.Repeat(new string('.', 40), 30).ToArray());

    /// <summary>Sala com uma parede no meio (coluna 20, linhas 5..24).</summary>
    private static GridMap Walled() =>
        GridMap.FromAscii(Enumerable.Range(0, 30).Select(y => new string('.', 20) + (y is >= 5 and < 25 ? "#" : ".") + new string('.', 19)).ToArray());

    [Fact]
    public void With_150_ms_of_latency_the_player_moves_on_the_very_next_tick()
    {
        var net = new Net(Open(), new Vector2(5, 15), upTicks: 2, downTicks: 2); // ~100 ms cada lado + 1 tick de servidor
        net.Run(5);
        var before = net.Predictor.RenderPosition;
        net.Input(new Vector2(1, 0));
        net.Step();

        Assert.True(net.Predictor.Position.X > before.X + 0.2f, "o preditor devia andar já no próximo tick");
        Assert.Equal(before, net.ServerPosition); // o servidor ainda nem recebeu
    }

    [Fact]
    public void Prediction_matches_the_server_and_needs_no_correction()
    {
        var net = new Net(Open(), new Vector2(5, 15), upTicks: 3, downTicks: 3);
        net.Run(5);
        net.Input(new Vector2(1, 0));
        net.Run(30);
        net.Input(new Vector2(0.6f, -0.8f));
        net.Run(20);
        net.Input(new Vector2(-1, 1)); // diagonal sem normalizar: o World normaliza, o preditor também
        net.Run(15);
        net.Input(Vector2.Zero);
        net.Run(20);

        Assert.True(net.MaxCorrection < 1e-3f, $"correção {net.MaxCorrection}");
        Assert.Equal(0, net.Snaps);
        Assert.True(Vector2.Distance(net.ServerPosition, net.Predictor.Position) < 1e-4f);
    }

    [Fact]
    public void Sliding_along_a_wall_is_predicted_like_the_server()
    {
        var net = new Net(Walled(), new Vector2(17, 15), upTicks: 3, downTicks: 3);
        net.Input(Vector2.Normalize(new Vector2(1, -0.3f))); // contra a parede e escorregando
        net.Run(60);
        net.Input(Vector2.Zero);
        net.Run(10);

        Assert.True(net.ServerPosition.X < 20, "a parede segura");
        Assert.True(net.MaxCorrection < 1e-3f, $"correção {net.MaxCorrection}");
        Assert.True(Vector2.Distance(net.ServerPosition, net.Predictor.Position) < 1e-4f);
    }

    [Fact]
    public void With_5_percent_loss_and_uneven_frames_the_drawn_position_never_jumps()
    {
        var net = new Net(Open(), new Vector2(5, 5), upTicks: 3, downTicks: 3, loss: 0.05);
        var rng = new Random(9);
        var last = net.Predictor.RenderPosition;
        var maxFrameJump = 0f;
        var dirs = new[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0.5f), Vector2.Zero, new Vector2(0.7f, -0.7f) };
        for (var t = 0; t < 400; t++)
        {
            if (t % 40 == 5)
            {
                net.Input(dirs[(t / 40) % dirs.Length]);
            }

            // Frames de 1 a 3 por tick (60 Hz com variação): o desenho entre ticks é interpolado.
            var frames = rng.Next(1, 4);
            for (var f = 0; f < frames; f++)
            {
                if (f == 0)
                {
                    net.Step(World.TickSeconds / frames);
                }
                else
                {
                    net.Predictor.Advance(World.TickSeconds / frames, Params);
                }

                var now = net.Predictor.RenderPosition;
                maxFrameJump = Math.Max(maxFrameJump, Vector2.Distance(now, last));
                last = now;
            }
        }

        // 4,5 tiles/s a 60 FPS anda ~0,075 tile por frame; um salto visível seria vários pixels a mais (32 px por tile).
        Assert.True(maxFrameJump < 0.25f, $"maior passo entre frames: {maxFrameJump} tiles");
        Assert.True(net.MaxCorrection < 1e-3f, $"correção {net.MaxCorrection}");
    }

    [Fact]
    public void Small_server_corrections_are_smoothed_and_big_ones_snap()
    {
        var net = new Net(Open(), new Vector2(10, 10), upTicks: 2, downTicks: 2);
        net.Run(5);
        net.Teleport(new Vector2(0.6f, 0)); // empurrão pequeno do servidor (separação, por exemplo)
        net.Run(3);
        Assert.Equal(0, net.Snaps);
        Assert.InRange(net.MaxCorrection, 0.5f, 0.7f); // a correção de 0,6 foi vista e suavizada, não aplicada de uma vez
        Assert.True(net.Predictor.Offset.Length() > 0.1f, "o desenho devia escorregar até a posição certa");
        net.Run(10);
        Assert.True(net.Predictor.Offset.Length() < 0.01f, "o deslocamento devia sumir");
        Assert.True(Vector2.Distance(net.ServerPosition, net.Predictor.RenderPosition) < 0.01f);

        net.Teleport(new Vector2(5, 0)); // investida / teleporte
        net.Run(3);
        Assert.Equal(1, net.Snaps);
        Assert.Equal(Vector2.Zero, net.Predictor.Offset);
        Assert.True(Vector2.Distance(net.ServerPosition, net.Predictor.Position) < 1e-4f);
    }

    [Fact]
    public void Rooted_player_does_not_predict_movement()
    {
        var map = Open();
        var p = new MovementPredictor(map);
        p.Reset(new Vector2(3, 3));
        p.Sent(1, new Vector2(1, 0));
        p.Advance(0.5, Params with { SpeedMultiplier = 0 });
        Assert.Equal(new Vector2(3, 3), p.Position);
    }
}
