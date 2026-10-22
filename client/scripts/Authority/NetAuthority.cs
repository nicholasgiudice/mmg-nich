using System.Diagnostics;
using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.CombatLog;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Protocol;
using LiteNetLib;

namespace MmoRpgClient.Authority;

/// <summary>Estado da conexão com o servidor.</summary>
public enum NetStatus
{
    Connecting,

    /// <summary>Conectado, esperando o <see cref="Welcome"/>.</summary>
    Joining,

    Playing,

    /// <summary>O servidor recusou (<see cref="NetAuthority.Message"/> diz por quê).</summary>
    Rejected,

    /// <summary>A conexão caiu ou não abriu.</summary>
    Disconnected,
}

/// <summary>
/// Autoridade de rede (T-107): o servidor decide; o cliente manda intenções e desenha o que chega.
/// <list type="bullet">
/// <item>Snapshots chegam como deltas (T-106), montados com <see cref="SnapshotReconstructor"/> e confirmados com <see cref="SnapshotAck"/>.</item>
/// <item>Entidades são desenhadas ~<see cref="InterpolationDelayTicks"/> ticks atrás do snapshot mais novo, interpolando entre os
/// dois snapshots em volta do relógio de desenho: <see cref="Previous"/>/<see cref="Latest"/>/<see cref="InterpolationFraction"/>
/// vêm do buffer, não de um tick local.</item>
/// <item>Eventos são entregues quando o relógio de desenho chega no tick deles (o golpe e o número aparecem juntos).</item>
/// </list>
/// Não depende do Godot: o <c>_Process</c> chama <see cref="Advance"/>.
/// </summary>
public sealed class NetAuthority : IGameAuthority, ICombatLogSource, IDisposable
{
    /// <summary>Atraso de desenho em ticks (100 ms): absorve a variação de chegada e um pacote perdido.</summary>
    public const double InterpolationDelayTicks = 2;

    private const byte ReliableChannel = 0;
    private const byte SnapshotChannel = 1;
    private const int BufferSize = 32;

    private readonly ContentCatalog _content;
    private readonly string _name;
    private readonly string? _classId;
    private readonly EventBasedNetListener _listener = new();
    private readonly NetManager _net;
    private readonly SnapshotReconstructor _state = new();
    private readonly List<WorldSnapshot> _buffer = new();
    private readonly SortedDictionary<long, List<GameEvent>> _eventsByTick = new();
    private readonly List<GameEvent> _ready = new();
    private CombatLogRecorder _log;
    private readonly List<DeathSummary> _pendingDeaths = new();
    private readonly Queue<(long Tick, bool Received)> _lossWindow = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private NetPeer? _peer;
    private uint _sequence;
    private double _renderTick = -1;
    private long _newestTick;
    private double _nextPingAt;

    public NetAuthority(string host, int port, string name, string? classId, ContentCatalog content)
    {
        _content = content;
        _name = name;
        _classId = classId;
        _log = new CombatLogRecorder(viewer: null);
        Server = $"{host}:{port}";
        _listener.PeerConnectedEvent += OnConnected;
        _listener.PeerDisconnectedEvent += (_, info) => Fail(NetStatus.Disconnected, info.Reason switch
        {
            DisconnectReason.ConnectionFailed => $"O servidor {Server} não respondeu.",
            DisconnectReason.Timeout => "Conexão perdida: o servidor parou de responder.",
            DisconnectReason.RemoteConnectionClose => "O servidor fechou a conexão.",
            DisconnectReason.HostUnreachable or DisconnectReason.NetworkUnreachable => $"Não foi possível chegar a {Server}.",
            _ => $"Conexão perdida ({info.Reason}).",
        });
        _listener.NetworkReceiveEvent += (_, reader, _, _) =>
        {
            OnReceived(reader.GetRemainingBytesSpan());
            reader.Recycle();
        };
        _net = new NetManager(_listener) { ChannelsCount = 2, AutoRecycle = false, DisconnectTimeout = 8_000, UpdateTime = 5 };
        _net.Start();
        if (_net.Connect(host, port, "") is null)
        {
            Fail(NetStatus.Disconnected, $"Não foi possível conectar a {Server}.");
        }
    }

    public string Server { get; }

    public NetStatus Status { get; private set; } = NetStatus.Connecting;

    /// <summary>Texto para o jogador (recusa, queda) ou vazio.</summary>
    public string Message { get; private set; } = "";

    /// <summary>Último <see cref="Welcome"/> (encontro, arena, época); null antes de entrar.</summary>
    public Welcome? Welcome { get; private set; }

    /// <summary>Incrementa a cada <see cref="Welcome"/> (entrada e reinício da sala): o mapa e o resultado recomeçam.</summary>
    public int WelcomeCount { get; private set; }

    /// <summary>Ida e volta (ms) medida com <see cref="Ping"/>/<see cref="Pong"/>; -1 antes da primeira.</summary>
    public double PingMs { get; private set; } = -1;

    /// <summary>Snapshots que não chegaram, em % dos últimos 100 ticks.</summary>
    public double LossPercent => _lossWindow.Count == 0 ? 0 : 100.0 * _lossWindow.Count(x => !x.Received) / _lossWindow.Count;

    // ---------------- IGameAuthority ----------------

    public EntityId? ControlledEntity => Status == NetStatus.Playing ? Welcome?.ControlledEntity : null;

    public long CurrentTick => Latest?.Tick ?? 0;

    public float InterpolationFraction { get; private set; }

    public WorldSnapshot? Latest { get; private set; }

    public WorldSnapshot? Previous { get; private set; }

    public void Advance(double deltaSeconds)
    {
        _net.PollEvents();
        if (Status != NetStatus.Playing || _buffer.Count == 0)
        {
            return;
        }

        PingIfDue();
        UpdateRenderClock(deltaSeconds);
        SelectSnapshots();
        ReleaseEvents();
    }

    public void Send(Intent intent)
    {
        // Mesma regra da LocalAuthority (e do servidor): só a entidade controlada.
        if (_peer is null || ControlledEntity is not { } id || intent.Entity != id)
        {
            return;
        }

        var message = new IntentMessage(++_sequence, (long)Math.Floor(_renderTick), NetIntent.From(intent));
        _peer.Send(ProtocolSerializer.Serialize(message), ReliableChannel, DeliveryMethod.ReliableOrdered);
    }

    public IReadOnlyList<GameEvent> TakeEvents()
    {
        if (_ready.Count == 0)
        {
            return Array.Empty<GameEvent>();
        }

        var events = _ready.ToArray();
        _ready.Clear();
        return events;
    }

    // ---------------- ICombatLogSource ----------------

    public IReadOnlyList<CombatLogEntry> LogEntries => _log.Entries;

    public IReadOnlyList<DeathSummary> TakeDeathSummaries()
    {
        if (_pendingDeaths.Count == 0)
        {
            return Array.Empty<DeathSummary>();
        }

        var deaths = _pendingDeaths.ToArray();
        _pendingDeaths.Clear();
        return deaths;
    }

    public void Dispose() => _net.Stop();

    // ---------------- rede ----------------

    private void OnConnected(NetPeer peer)
    {
        _peer = peer;
        Status = NetStatus.Joining;
        var hello = new Hello(ProtocolVersion.Current, _content.Fingerprint, _name, _classId);
        peer.Send(ProtocolSerializer.Serialize(hello), ReliableChannel, DeliveryMethod.ReliableOrdered);
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
                OnWelcome(w);
                break;
            case Reject r:
                Fail(NetStatus.Rejected, r.Reason switch
                {
                    RejectReason.ProtocolVersion => "Versão do jogo diferente da do servidor. Atualize o cliente.",
                    RejectReason.Content => "Os dados do jogo (data/) são diferentes dos do servidor.",
                    RejectReason.Full => "A sala está cheia.",
                    _ => "O servidor recusou a entrada.",
                } + $" ({r.Detail})");
                break;
            case SnapshotDelta d:
                OnSnapshot(d);
                break;
            case EventsMessage e when e.Events.Length > 0:
                if (!_eventsByTick.TryGetValue(e.Tick, out var list))
                {
                    _eventsByTick[e.Tick] = list = new List<GameEvent>();
                }

                list.AddRange(e.Events);
                break;
            case Pong p:
                PingMs = _clock.Elapsed.TotalMilliseconds - p.ClientTimeMs;
                break;
        }
    }

    private void OnWelcome(Welcome w)
    {
        Welcome = w;
        WelcomeCount++;
        Status = NetStatus.Playing;
        _state.Reset(w.Epoch);
        _buffer.Clear();
        _eventsByTick.Clear();
        _lossWindow.Clear();
        _renderTick = -1;
        _newestTick = 0;
        Latest = null;
        Previous = null;
        _log = new CombatLogRecorder(viewer: w.ControlledEntity); // mundo novo (entrada ou reinício da sala)
    }

    private void OnSnapshot(SnapshotDelta delta)
    {
        if (_state.Apply(delta) is not { } snapshot)
        {
            return;
        }

        _peer?.Send(ProtocolSerializer.Serialize(new SnapshotAck(delta.Epoch, delta.Tick)), SnapshotChannel, DeliveryMethod.Unreliable);
        TrackLoss(delta.Tick);
        _newestTick = Math.Max(_newestTick, delta.Tick);
        _buffer.Add(snapshot);
        _buffer.Sort((a, b) => a.Tick.CompareTo(b.Tick));
        while (_buffer.Count > BufferSize)
        {
            _buffer.RemoveAt(0);
        }
    }

    private void TrackLoss(long tick)
    {
        var last = _lossWindow.Count > 0 ? _lossWindow.Last().Tick : tick - 1;
        for (var t = last + 1; t < tick; t++)
        {
            _lossWindow.Enqueue((t, false));
        }

        _lossWindow.Enqueue((tick, true));
        while (_lossWindow.Count > 100)
        {
            _lossWindow.Dequeue();
        }
    }

    private void PingIfDue()
    {
        var now = _clock.Elapsed.TotalMilliseconds;
        if (now < _nextPingAt || _peer is null)
        {
            return;
        }

        _nextPingAt = now + 1000;
        _peer.Send(ProtocolSerializer.Serialize(new Ping((long)now)), ReliableChannel, DeliveryMethod.ReliableOrdered);
    }

    private void Fail(NetStatus status, string message)
    {
        if (Status is NetStatus.Rejected or NetStatus.Disconnected)
        {
            return; // a primeira causa é a que conta (recusa seguida da queda da conexão)
        }

        Status = status;
        Message = message;
    }

    // ---------------- interpolação ----------------

    /// <summary>
    /// O relógio de desenho anda em tempo real (20 ticks/s) e mira <c>tick mais novo − atraso</c>: longe demais (entrada,
    /// travada), pula; perto, corrige 10% por frame — o movimento fica suave mesmo com chegadas irregulares.
    /// </summary>
    private void UpdateRenderClock(double deltaSeconds)
    {
        var target = _newestTick - InterpolationDelayTicks;
        if (_renderTick < 0 || Math.Abs(target - _renderTick) > 6)
        {
            _renderTick = target;
            return;
        }

        _renderTick += deltaSeconds * World.TickRate;
        _renderTick += (target - _renderTick) * 0.1;
        _renderTick = Math.Min(_renderTick, _newestTick); // nunca à frente do que chegou (sem extrapolar)
    }

    private void SelectSnapshots()
    {
        WorldSnapshot? before = null;
        WorldSnapshot? after = null;
        foreach (var s in _buffer)
        {
            if (s.Tick <= _renderTick)
            {
                before = s;
            }
            else
            {
                after = s;
                break;
            }
        }

        if (before is null)
        {
            Previous = null;
            Latest = _buffer[0];
            InterpolationFraction = 0;
            return;
        }

        if (after is null)
        {
            Previous = _buffer.Count > 1 ? _buffer[^2] : null;
            Latest = before;
            InterpolationFraction = 1;
            return;
        }

        Previous = before;
        Latest = after;
        InterpolationFraction = (float)Math.Clamp((_renderTick - before.Tick) / (after.Tick - before.Tick), 0, 1);
    }

    /// <summary>Eventos cujo tick o desenho já alcançou, em ordem; o log é gravado com o snapshot daquele tick.</summary>
    private void ReleaseEvents()
    {
        while (_eventsByTick.Count > 0)
        {
            var (tick, events) = _eventsByTick.First();
            if (tick > _renderTick)
            {
                break;
            }

            _eventsByTick.Remove(tick);
            _ready.AddRange(events);
            var at = _buffer.LastOrDefault(s => s.Tick <= tick) ?? _buffer[0];
            _pendingDeaths.AddRange(_log.Record(events, new SnapshotLogLookup(new WorldSnapshot(tick, at.Entities), _content)));
        }
    }
}
