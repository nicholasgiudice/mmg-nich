using System.Diagnostics;
using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Combat.Status;
using Game.Core.CombatLog;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
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
/// <item>T-108: o próprio jogador é desenhado na posição <b>prevista</b> (<see cref="MovementPredictor"/>), não na interpolada,
/// e a barra de conjuração começa no aperto (desfeita se o servidor recusar).</item>
/// <item>Depuração: latência e perda artificiais no próprio cliente (<c>--net-lag</c>, <c>--net-loss</c>).</item>
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
    private readonly string? _ticket;
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
    private MovementPredictor? _predictor;
    private PredictionParams _params;
    private bool _ownAlive;
    private LocalCast? _localCast;
    private readonly double _lagMs;
    private readonly double _loss;
    private readonly Random _simRandom = new();
    private readonly List<(double At, byte[] Data)> _inbound = new();
    private readonly List<(double At, byte[] Data, byte Channel, DeliveryMethod Method)> _outbound = new();

    /// <summary>Conjuração mostrada antes da confirmação do servidor (T-108). Ticks no relógio de desenho.</summary>
    private sealed record LocalCast(string AbilityId, double StartTick, double EndTick, EntityId? Target, System.Numerics.Vector2? Aim, bool CastWhileMoving);

    /// <param name="lagMs">Latência artificial de ida e volta (metade em cada sentido), só para depuração.</param>
    /// <param name="lossPercent">Perda artificial de snapshots (e das confirmações), só para depuração.</param>
    public NetAuthority(string host, int port, string name, string? classId, ContentCatalog content, double lagMs = 0, double lossPercent = 0,
        string? ticket = null)
    {
        _ticket = ticket;
        _lagMs = Math.Max(0, lagMs);
        _loss = Math.Clamp(lossPercent, 0, 100) / 100.0;
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
        _listener.NetworkReceiveEvent += (_, reader, _, method) =>
        {
            if (_lagMs > 0 || _loss > 0)
            {
                // Simulação: perde só o que é não confiável (snapshots); atrasa tudo meia latência na chegada.
                if (!(method == DeliveryMethod.Unreliable && _simRandom.NextDouble() < _loss))
                {
                    _inbound.Add((_clock.Elapsed.TotalMilliseconds + (_lagMs / 2), reader.GetRemainingBytes()));
                }
            }
            else
            {
                OnReceived(reader.GetRemainingBytesSpan());
            }

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

    /// <summary>Latência e perda artificiais ligadas (depuração).</summary>
    public string Simulation => _lagMs > 0 || _loss > 0 ? $"simulando +{_lagMs:0} ms, {_loss * 100:0}% de perda" : "";

    /// <summary>Última correção da predição (tiles) e se foi aplicada direto (depuração).</summary>
    public (float Size, bool Snapped) LastCorrection => _predictor?.LastCorrection ?? default;

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
        FlushSimulated();
        if (Status != NetStatus.Playing || _buffer.Count == 0)
        {
            return;
        }

        PingIfDue();
        UpdateRenderClock(deltaSeconds);
        if (_ownAlive)
        {
            _predictor?.Advance(deltaSeconds, _params);
        }

        SelectSnapshots();
        ApplyPrediction();
        ReleaseEvents();
    }

    public void Send(Intent intent)
    {
        // Mesma regra da LocalAuthority (e do servidor): só a entidade controlada.
        if (_peer is null || ControlledEntity is not { } id || intent.Entity != id)
        {
            return;
        }

        var seq = ++_sequence;
        _predictor?.Sent(seq, intent switch
        {
            MoveIntent m => m.Direction,
            StopIntent => System.Numerics.Vector2.Zero,
            _ => null,
        });
        if (intent is MoveIntent { Direction: var d } && d != System.Numerics.Vector2.Zero && _localCast is { CastWhileMoving: false })
        {
            _localCast = null; // andar cancela a conjuração (o servidor também vai cancelar)
        }

        if (intent is UseAbilityIntent use)
        {
            StartLocalCast(use);
        }

        var message = new IntentMessage(seq, _predictor?.Tick ?? 0, NetIntent.From(intent));
        SendRaw(ProtocolSerializer.Serialize(message), ReliableChannel, DeliveryMethod.ReliableOrdered);
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
        var hello = new Hello(ProtocolVersion.Current, _content.Fingerprint, _name, _classId, _ticket);
        SendRaw(ProtocolSerializer.Serialize(hello), ReliableChannel, DeliveryMethod.ReliableOrdered);
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
                    RejectReason.Unauthorized => "Entrada recusada: o token de jogo é inválido ou expirou. Tente jogar de novo.",
                    RejectReason.AlreadyConnected => "Este personagem já está conectado.",
                    RejectReason.Unavailable => "O servidor não conseguiu carregar o personagem. Tente de novo em instantes.",
                    _ => "O servidor recusou a entrada.",
                } + $" ({r.Detail})");
                break;
            case SnapshotDelta d:
                OnSnapshot(d);
                break;
            case IntentAck ack:
                _predictor?.Acked(ack.LastSequence, ack.Tick);
                break;
            case EventsMessage e when e.Events.Length > 0:
                CancelLocalCastOn(e.Events);
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
        _predictor = _content.Arenas.TryGetValue(w.ArenaId, out var arena) ? new MovementPredictor(EntityFactory.BuildMap(arena)) : new MovementPredictor(null);
        _localCast = null;
        _ownAlive = false;
    }

    private void OnSnapshot(SnapshotDelta delta)
    {
        if (_state.Apply(delta) is not { } snapshot)
        {
            return;
        }

        SendRaw(ProtocolSerializer.Serialize(new SnapshotAck(delta.Epoch, delta.Tick)), SnapshotChannel, DeliveryMethod.Unreliable);
        TrackLoss(delta.Tick);
        Reconcile(snapshot);
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
        SendRaw(ProtocolSerializer.Serialize(new Ping((long)now)), ReliableChannel, DeliveryMethod.ReliableOrdered);
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

    // ---------------- latência e perda artificiais (depuração) ----------------

    private void SendRaw(byte[] data, byte channel, DeliveryMethod method)
    {
        if (_peer is null)
        {
            return;
        }

        if (_lagMs <= 0 && _loss <= 0)
        {
            _peer.Send(data, channel, method);
            return;
        }

        if (method == DeliveryMethod.Unreliable && _simRandom.NextDouble() < _loss)
        {
            return;
        }

        _outbound.Add((_clock.Elapsed.TotalMilliseconds + (_lagMs / 2), data, channel, method));
    }

    private void FlushSimulated()
    {
        if (_inbound.Count == 0 && _outbound.Count == 0)
        {
            return;
        }

        var now = _clock.Elapsed.TotalMilliseconds;
        for (var i = 0; i < _outbound.Count; i++)
        {
            if (_outbound[i].At <= now)
            {
                _peer?.Send(_outbound[i].Data, _outbound[i].Channel, _outbound[i].Method);
                _outbound.RemoveAt(i--);
            }
        }

        for (var i = 0; i < _inbound.Count; i++)
        {
            if (_inbound[i].At <= now)
            {
                var data = _inbound[i].Data;
                _inbound.RemoveAt(i--);
                OnReceived(data);
            }
        }
    }

    // ---------------- predição (T-108) ----------------

    /// <summary>Snapshot mais novo: parâmetros do movimento e reconciliação da posição prevista.</summary>
    private void Reconcile(WorldSnapshot snapshot)
    {
        if (_predictor is null || Welcome is null || snapshot.Entities.FirstOrDefault(e => e.Id == Welcome.ControlledEntity) is not { } own)
        {
            return;
        }

        _ownAlive = own.TryGet<Health>()?.IsAlive ?? true;
        if (own.TryGet<Transform>() is not { } t || own.TryGet<Movement>() is not { } mv)
        {
            return;
        }

        _params = new PredictionParams(mv.Speed, SpeedMultiplier(own, snapshot.Tick), own.TryGet<Collider>()?.Radius ?? 0f, mv.LockFacing);
        if (!_ownAlive)
        {
            _predictor.Reset(t.Position);
            return;
        }

        _predictor.Reconcile(snapshot.Tick, t.Position, _params);
    }

    /// <summary>Mesma conta do <c>StatusModifierSource</c> do Core, com os status que o servidor mandou.</summary>
    private float SpeedMultiplier(EntitySnapshot own, long tick)
    {
        var m = 1.0;
        foreach (var s in own.TryGet<StatusEffects>()?.Active ?? [])
        {
            if (s.ExpiresAtTick <= tick || !_content.Statuses.TryGetValue(s.StatusId, out var def))
            {
                continue;
            }

            if (def.Flags.Stunned || def.Flags.Rooted)
            {
                return 0;
            }

            m *= Math.Pow(def.Modifiers.SpeedMultiplier, s.Stacks);
        }

        return (float)Math.Max(0, m);
    }

    /// <summary>Troca, nos snapshots de desenho, a posição do próprio jogador pela prevista e acrescenta a conjuração local.</summary>
    private void ApplyPrediction()
    {
        if (Welcome is not { } w || Latest is null)
        {
            return;
        }

        var id = w.ControlledEntity;
        var predicted = _ownAlive && _predictor is { Ready: true } p ? p : null;
        var ownLatest = Latest.Entities.FirstOrDefault(e => e.Id == id);
        if (_localCast is { } lc && (_renderTick > lc.EndTick + 2 || ownLatest?.TryGet<AbilityState>()?.Cast is not null))
        {
            _localCast = null; // terminou, ou o servidor já mostra a conjuração dele
        }

        if (predicted is null && _localCast is null)
        {
            return;
        }

        IEnumerable<IComponent> Patch(EntitySnapshot e)
        {
            foreach (var c in e.Components)
            {
                if (predicted is not null && c is Transform t)
                {
                    var dir = predicted.Direction;
                    var facing = dir != System.Numerics.Vector2.Zero && !_params.LockFacing ? System.Numerics.Vector2.Normalize(dir) : t.Facing;
                    yield return new Transform { Position = predicted.RenderPosition, Facing = facing };
                }
                else if (_localCast is { } cast && c is AbilityState { Cast: null } st)
                {
                    var copy = (AbilityState)st.Clone();
                    copy.Cast = new CastState
                    {
                        AbilityId = cast.AbilityId,
                        StartTick = (long)Math.Floor(cast.StartTick),
                        EndTick = (long)Math.Ceiling(cast.EndTick),
                        Target = cast.Target,
                        AimPoint = cast.Aim,
                    };
                    yield return copy;
                }
                else
                {
                    yield return c;
                }
            }
        }

        WorldSnapshot Replace(WorldSnapshot s) =>
            new(s.Tick, s.Entities.Select(e => e.Id == id ? EntitySnapshot.Create(id, Patch(e).ToList()) : e).ToList());

        Latest = Replace(Latest);
        if (Previous is not null)
        {
            Previous = Replace(Previous);
        }
    }

    /// <summary>Barra de conjuração local no aperto (só visual; o servidor decide se a conjuração existe).</summary>
    private void StartLocalCast(UseAbilityIntent use)
    {
        var own = Latest?.Entities.FirstOrDefault(e => e.Id == use.Entity)?.TryGet<AbilityState>();
        if (own is null || own.IsCasting || _localCast is not null
            || _content.AbilityAt(use.AbilityId, own.RankOf(use.AbilityId)) is not { CastTimeTicks: > 0 } def)
        {
            return;
        }

        _localCast = new LocalCast(def.Id, _renderTick, _renderTick + def.CastTimeTicks, use.Target, use.AimPoint, def.CastWhileMoving);
    }

    /// <summary>Recusa ou interrupção chegou: desfaz a barra local na hora (não espera o relógio de desenho).</summary>
    private void CancelLocalCastOn(IEnumerable<GameEvent> events)
    {
        if (_localCast is not { } cast || Welcome is not { } w)
        {
            return;
        }

        foreach (var e in events)
        {
            if ((e is AbilityRejected r && r.Source == w.ControlledEntity && r.AbilityId == cast.AbilityId)
                || (e is CastInterrupted ci && ci.Entity == w.ControlledEntity))
            {
                _localCast = null;
                return;
            }
        }
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
