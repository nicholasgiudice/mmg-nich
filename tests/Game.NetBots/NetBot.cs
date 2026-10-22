using System.Diagnostics;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using LiteNetLib;

namespace Game.NetBots;

public enum BotOutcome
{
    Playing,
    Won,
    Lost,
}

/// <summary>
/// Um jogador de rede sem Godot (T-109): conecta, faz o <see cref="Hello"/>, monta o estado com os deltas da T-106 e,
/// a cada snapshot novo, espelha o estado num <see cref="World"/> (<see cref="World.Mirror"/>) para rodar uma estratégia
/// do <c>Game.Bots</c>, exatamente como no simulador. As intenções vão para o servidor; o servidor decide tudo.
/// </summary>
public sealed class NetBot : IDisposable
{
    private const byte ReliableChannel = 0;
    private const byte SnapshotChannel = 1;

    private readonly ContentCatalog _content;
    private readonly string _strategy;
    private readonly BotOptions _options;
    private readonly EventBasedNetListener _listener = new();
    private readonly NetManager _net;
    private readonly SnapshotReconstructor _state = new();
    private readonly List<Intent> _buffer = new();
    private readonly Stopwatch _clock;
    private readonly Queue<bool> _lossWindow = new();
    private NetPeer? _peer;
    private World? _mirror;
    private IBotController? _controller;
    private readonly IHazardSource _hazards = new SnapshotHazardSource();
    private long _lastThought;
    private long _lastTick;
    private uint _sequence;
    private double _nextPingAt;

    public NetBot(int index, string host, int port, string? classId, string strategy, ContentCatalog content, BotOptions options, Stopwatch clock)
    {
        Index = index;
        Server = $"{host}:{port}";
        ClassId = classId;
        _content = content;
        _strategy = strategy;
        _options = options;
        _clock = clock;
        _listener.PeerConnectedEvent += peer =>
        {
            _peer = peer;
            Send(new Hello(ProtocolVersion.Current, content.Fingerprint, $"netbot{index}", classId), ReliableChannel, DeliveryMethod.ReliableOrdered);
        };
        _listener.PeerDisconnectedEvent += (_, info) =>
        {
            Disconnects++;
            Connected = false;
            LastError ??= info.Reason.ToString();
        };
        _listener.NetworkReceiveEvent += (_, reader, _, _) =>
        {
            BytesReceived += reader.AvailableBytes;
            OnReceived(reader.GetRemainingBytesSpan());
            reader.Recycle();
        };
        _net = new NetManager(_listener) { ChannelsCount = 2, AutoRecycle = false, UpdateTime = 5, DisconnectTimeout = 10_000 };
        _net.Start();
        _net.Connect(host, port, "");
        Connected = true;
    }

    public int Index { get; }
    public string Server { get; }
    public string? ClassId { get; }
    public bool Connected { get; private set; }
    public Welcome? Welcome { get; private set; }
    public string? LastError { get; private set; }

    // ---------- métricas ----------

    public long BytesReceived { get; private set; }
    public long IntentsSent { get; private set; }
    public long SnapshotsApplied { get; private set; }
    public int Disconnects { get; private set; }
    public double PingMs { get; private set; } = -1;
    public double LossPercent => _lossWindow.Count == 0 ? 0 : 100.0 * _lossWindow.Count(x => !x) / _lossWindow.Count;
    public BotOutcome Outcome { get; private set; }

    /// <summary>Segundos de jogo do encontro até o fim (vitória/derrota), pelo tick do servidor.</summary>
    public double? EndedAfterSeconds { get; private set; }

    /// <summary>Recusado pelo servidor (motivo) — erro de configuração do teste, não de carga.</summary>
    public RejectReason? Rejected { get; private set; }

    /// <summary>Recebe o que chegou e, se há snapshot novo, decide e manda as intenções.</summary>
    public void Update()
    {
        _net.PollEvents();
        if (_peer is null || Welcome is null || _state.Latest is not { } latest)
        {
            return;
        }

        PingIfDue();
        if (latest.Tick == _lastThought || _mirror is null || _controller is null)
        {
            return;
        }

        _lastThought = latest.Tick;
        _mirror.Mirror(latest);
        TrackOutcome(latest);
        if (_mirror.Find(Welcome.ControlledEntity) is not { IsAlive: true } self || Outcome != BotOutcome.Playing)
        {
            return;
        }

        _buffer.Clear();
        _controller.Think(new BotContext(_mirror, self, _options, _hazards, (ulong)Welcome.Epoch), _buffer);
        foreach (var intent in _buffer.Where(i => i.Entity == self.Id))
        {
            Send(new IntentMessage(++_sequence, latest.Tick, NetIntent.From(intent)), ReliableChannel, DeliveryMethod.ReliableOrdered);
            IntentsSent++;
        }
    }

    public void Dispose()
    {
        _peer?.Disconnect();
        _net.Stop();
    }

    private void OnReceived(ReadOnlySpan<byte> data)
    {
        if (!ProtocolSerializer.TryDeserialize(data, out var message))
        {
            return;
        }

        switch (message)
        {
            case Welcome w:
                Welcome = w;
                _state.Reset(w.Epoch);
                _mirror = WorldBuilder.CreateDefault((ulong)w.Epoch, EntityFactory.BuildMap(_content.Arenas[w.ArenaId]), _content);
                _controller = BotStrategies.Create(_strategy);
                _lastThought = 0;
                _lastTick = 0;
                Outcome = BotOutcome.Playing;
                EndedAfterSeconds = null;
                break;
            case Reject r:
                Rejected = r.Reason;
                LastError = $"{r.Reason}: {r.Detail}";
                break;
            case SnapshotDelta d when _state.Apply(d) is not null:
                SnapshotsApplied++;
                for (var t = _lastTick + 1; _lastTick > 0 && t < d.Tick; t++)
                {
                    Window(false);
                }

                Window(true);
                _lastTick = d.Tick;
                Send(new SnapshotAck(d.Epoch, d.Tick), SnapshotChannel, DeliveryMethod.Unreliable);
                break;
            case Pong p:
                PingMs = _clock.Elapsed.TotalMilliseconds - p.ClientTimeMs;
                break;
        }
    }

    private void Window(bool received)
    {
        _lossWindow.Enqueue(received);
        while (_lossWindow.Count > 200)
        {
            _lossWindow.Dequeue();
        }
    }

    private void TrackOutcome(WorldSnapshot s)
    {
        if (Outcome != BotOutcome.Playing)
        {
            return;
        }

        bool Alive(Faction f) => s.Entities.Any(e => e.TryGet<FactionTag>()?.Faction == f && e.TryGet<Health>() is { IsAlive: true });
        if (!Alive(Faction.Monster))
        {
            Outcome = BotOutcome.Won;
        }
        else if (!Alive(Faction.Player))
        {
            Outcome = BotOutcome.Lost;
        }

        if (Outcome != BotOutcome.Playing)
        {
            EndedAfterSeconds = s.Tick / (double)World.TickRate;
        }
    }

    private void PingIfDue()
    {
        var now = _clock.Elapsed.TotalMilliseconds;
        if (now >= _nextPingAt)
        {
            _nextPingAt = now + 1000;
            Send(new Ping((long)now), ReliableChannel, DeliveryMethod.ReliableOrdered);
        }
    }

    private void Send(INetMessage message, byte channel, DeliveryMethod method) =>
        _peer?.Send(ProtocolSerializer.Serialize(message), channel, method);
}
