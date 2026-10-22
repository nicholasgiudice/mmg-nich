using System.Numerics;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Transport;
using Serilog;

namespace Game.Server;

/// <summary>
/// Uma sala com um encontro do protótipo (T-105). Roda na thread do loop: mensagens chegam por <see cref="ITransportHandler"/>
/// durante o <c>Poll</c> do transporte e são aplicadas em <see cref="Step"/>.
/// <list type="bullet">
/// <item>Cada jogador ocupa uma <b>vaga</b> de jogador do encontro com a classe pedida (mesma regra do <c>MatchSetup</c> do cliente:
/// vaga da mesma classe; senão a primeira livre, que troca de classe).</item>
/// <item>Intenções valem só para a entidade do próprio jogador e entram no próximo tick; <see cref="IntentAck"/> diz a última aplicada.</item>
/// <item>A cada tick: snapshot completo para todos (não confiável) e os eventos do tick (confiável).</item>
/// <item>Quem cai: a entidade para (ou vira bot) e some depois de <see cref="ServerOptions.DisconnectGraceSeconds"/>;
/// voltando com o mesmo nome antes disso, retoma a vaga. Sem jogadores, a sala reinicia o encontro.</item>
/// </list>
/// </summary>
public sealed class ZoneRoom : ITransportHandler
{
    /// <summary>Intenções aceitas por jogador por tick (o resto é descartado: cliente com defeito ou abuso).</summary>
    public const int MaxIntentsPerTick = 16;

    /// <summary>Ticks entre o <see cref="Reject"/> e o fechamento da conexão (dá tempo de a recusa chegar).</summary>
    private const int RejectCloseDelayTicks = 10;

    private readonly ContentCatalog _content;
    private readonly ServerOptions _options;
    private readonly IServerTransport _transport;
    private readonly ILogger _log;
    private readonly EncounterDefinition _encounter;
    private readonly Dictionary<int, Connection> _connections = new();
    private readonly List<Slot> _slots = new();
    private readonly Dictionary<int, long> _closeAt = new();
    private BotDirector? _bots;
    private ulong _seed;

    public ZoneRoom(ContentCatalog content, ServerOptions options, IServerTransport transport, ILogger log)
    {
        _content = content;
        _options = options;
        _transport = transport;
        _log = log;
        _encounter = content.Encounters.TryGetValue(options.EncounterId, out var enc)
            ? enc
            : throw new ArgumentException($"Encontro desconhecido: {options.EncounterId}");
        _seed = options.Seed;
        World = CreateWorld();
    }

    public World World { get; private set; }

    public EncounterDefinition Encounter => _encounter;

    /// <summary>Jogadores com vaga (conectados).</summary>
    public int PlayerCount => _connections.Values.Count(c => c.Slot is not null);

    public int Restarts { get; private set; }

    /// <summary>Entidade de cada vaga (null = removida depois da desconexão) e quem a controla.</summary>
    public IReadOnlyList<(EntityId? Entity, string Class, string? Player, bool Bot)> Slots =>
        _slots.Select(s => (s.Entity?.Id, s.ClassId, s.Peer is { } p ? _connections[p].Name : s.ReservedFor, IsBotSlot(s))).ToList();

    // ---------------- transporte ----------------

    public void OnConnected(int peer)
    {
        _connections[peer] = new Connection(peer);
        _log.Information("Conexão {Peer} aberta", peer);
    }

    public void OnDisconnected(int peer)
    {
        _closeAt.Remove(peer);
        if (!_connections.Remove(peer, out var c) || c.Slot is not { } index)
        {
            return;
        }

        var slot = _slots[index];
        slot.Peer = null;
        slot.ReservedFor = c.Name;
        slot.LeftAtTick = World.Tick;
        if (slot.Entity is { IsAlive: true } e && !_options.FillWithBots)
        {
            World.Enqueue(new StopIntent(e.Id));
        }

        _log.Information("{Name} saiu (vaga {Slot}); entidade removida em {Grace} s se não voltar", c.Name, index, _options.DisconnectGraceSeconds);
        RebuildBots();
        if (PlayerCount == 0)
        {
            Restart("todos os jogadores saíram");
        }
    }

    public void OnReceived(int peer, ReadOnlySpan<byte> data)
    {
        if (!_connections.TryGetValue(peer, out var c))
        {
            return;
        }

        if (!ProtocolSerializer.TryDeserialize(data, out var message))
        {
            _log.Warning("Mensagem inválida de {Peer} ({Bytes} B)", peer, data.Length);
            return;
        }

        switch (message)
        {
            case Hello hello:
                OnHello(c, hello);
                break;
            case IntentMessage intent when c.Slot is not null:
                if (c.Pending.Count < MaxIntentsPerTick * 4)
                {
                    c.Pending.Enqueue(intent);
                }

                break;
            case Ping ping:
                Send(peer, new Pong(ping.ClientTimeMs, World.Tick));
                break;
            default:
                _log.Debug("Mensagem {Type} ignorada de {Peer}", message?.GetType().Name, peer);
                break;
        }
    }

    private void OnHello(Connection c, Hello hello)
    {
        if (c.Slot is not null)
        {
            return; // repetido
        }

        if (hello.ProtocolVersion != ProtocolVersion.Current)
        {
            Refuse(c, RejectReason.ProtocolVersion, $"servidor v{ProtocolVersion.Current}, cliente v{hello.ProtocolVersion}");
            return;
        }

        if (!string.Equals(hello.ContentFingerprint, _content.Fingerprint, StringComparison.Ordinal))
        {
            Refuse(c, RejectReason.Content, $"conteúdo do servidor {_content.Fingerprint}, do cliente {hello.ContentFingerprint}");
            return;
        }

        var name = string.IsNullOrWhiteSpace(hello.DevName) ? $"jogador{c.Peer}" : hello.DevName.Trim();
        if (hello.ClassId is { } cls && !_content.Classes.ContainsKey(cls))
        {
            Refuse(c, RejectReason.BadRequest, $"classe desconhecida: {cls}");
            return;
        }

        var index = ChooseSlot(name, hello.ClassId);
        if (index < 0)
        {
            Refuse(c, RejectReason.Full, $"as {_slots.Count} vagas do encontro estão ocupadas");
            return;
        }

        c.Name = name;
        Occupy(c, index, hello.ClassId);
        _log.Information("{Name} entrou na vaga {Slot} como {Class} (entidade {Entity})", name, index, _slots[index].ClassId, _slots[index].Entity!.Id);
    }

    private void Refuse(Connection c, RejectReason reason, string detail)
    {
        _log.Information("Conexão {Peer} recusada: {Reason} ({Detail})", c.Peer, reason, detail);
        Send(c.Peer, new Reject(reason, detail));
        _closeAt[c.Peer] = World.Tick + RejectCloseDelayTicks;
    }

    // ---------------- vagas ----------------

    /// <summary>Vaga retomada pelo nome; senão livre da mesma classe; senão a primeira livre. -1 = cheia.</summary>
    private int ChooseSlot(string name, string? classId)
    {
        var free = Enumerable.Range(0, _slots.Count).Where(i => _slots[i].Peer is null).ToList();
        if (free.Count == 0)
        {
            return -1;
        }

        var reclaim = free.Where(i => _slots[i].ReservedFor == name && (classId is null || _slots[i].ClassId == classId)).ToList();
        if (reclaim.Count > 0)
        {
            return reclaim[0];
        }

        // Vagas reservadas para quem caiu ficam por último (podem voltar).
        var open = free.OrderBy(i => _slots[i].ReservedFor is null ? 0 : 1).ThenBy(i => i).ToList();
        return classId is null ? open[0] : open.FirstOrDefault(i => _slots[i].ClassId == classId, open[0]);
    }

    private void Occupy(Connection c, int index, string? classId)
    {
        var slot = _slots[index];
        if (slot.Entity is null || (classId is not null && classId != slot.ClassId))
        {
            Respawn(slot, classId ?? slot.ClassId);
        }

        slot.Peer = c.Peer;
        slot.ReservedFor = null;
        slot.LeftAtTick = null;
        c.Slot = index;
        c.LastApplied = 0;
        c.Pending.Clear();
        RebuildBots();
        Send(c.Peer, new Welcome(slot.Entity!.Id, World.Tick, _encounter.Id, _encounter.Arena));
    }

    /// <summary>Troca a entidade da vaga (classe diferente ou removida): nasce onde a anterior estava, ou no ponto da vaga.</summary>
    private void Respawn(Slot slot, string classId)
    {
        var position = slot.Entity?.TryGet<Transform>()?.Position ?? slot.SpawnPosition;
        if (slot.Entity is { } old)
        {
            World.Remove(old.Id);
        }

        slot.ClassId = classId;
        slot.Entity = EntityFactory.SpawnPlayer(World, _content.Classes[classId], position);
    }

    private bool IsBotSlot(Slot s) => _options.FillWithBots && s.Peer is null && s.Entity is not null;

    /// <summary>O <c>BotDirector</c> não remove bots: recria com as vagas que hoje são de bot.</summary>
    private void RebuildBots()
    {
        _bots = null;
        var botSlots = _slots.Where(IsBotSlot).ToList();
        if (botSlots.Count == 0)
        {
            return;
        }

        _bots = new BotDirector(World, new TelegraphHazardSource()) { Seed = _seed };
        var options = new BotOptions(new Dictionary<string, string> { ["class.mage:explosion"] = "mage.elemental_blast" });
        foreach (var s in botSlots)
        {
            _bots.Add(s.Entity!, BotStrategies.Create(_options.BotStrategy), options);
        }
    }

    // ---------------- tick ----------------

    /// <summary>Um tick da sala: intenções, bots, simulação e envio.</summary>
    public void Step()
    {
        CloseRefused();
        RemoveAbandoned();

        foreach (var c in _connections.Values)
        {
            if (c.Slot is not { } index || _slots[index].Entity is not { } entity)
            {
                c.Pending.Clear();
                continue;
            }

            for (var n = 0; n < MaxIntentsPerTick && c.Pending.TryDequeue(out var msg); n++)
            {
                // A entidade vem da conexão, nunca da mensagem (T-103).
                if (msg.Intent.ToIntent(entity.Id) is { } intent)
                {
                    World.Enqueue(intent);
                }

                c.LastApplied = msg.Sequence;
                c.AckDue = true;
            }
        }

        _bots?.Tick();
        World.Step();
        Broadcast();
    }

    private void Broadcast()
    {
        var players = _connections.Values.Where(c => c.Slot is not null).ToList();
        if (players.Count == 0)
        {
            return;
        }

        var snapshot = ProtocolSerializer.Serialize(SnapshotMessage.From(World.Snapshot(SnapshotScope.Replicated)));
        var events = World.LastTickEvents.Count > 0
            ? ProtocolSerializer.Serialize(new EventsMessage(World.Tick, World.LastTickEvents.ToArray()))
            : null;
        foreach (var c in players)
        {
            if (events is not null)
            {
                _transport.Send(c.Peer, events, SendMode.Reliable);
            }

            if (c.AckDue)
            {
                c.AckDue = false;
                Send(c.Peer, new IntentAck(c.LastApplied, World.Tick));
            }

            _transport.Send(c.Peer, snapshot, SendMode.Snapshot);
        }
    }

    private void CloseRefused()
    {
        foreach (var (peer, at) in _closeAt.Where(kv => World.Tick >= kv.Value).ToList())
        {
            _closeAt.Remove(peer);
            _transport.Disconnect(peer);
        }
    }

    private void RemoveAbandoned()
    {
        var grace = (long)Math.Round(_options.DisconnectGraceSeconds * World.TickRate);
        var changed = false;
        foreach (var s in _slots.Where(s => s.LeftAtTick is { } left && World.Tick - left >= grace))
        {
            if (s.Entity is { } e)
            {
                World.Remove(e.Id);
                _log.Information("Entidade {Entity} de {Name} removida (não voltou)", e.Id, s.ReservedFor);
            }

            s.Entity = null;
            s.ReservedFor = null;
            s.LeftAtTick = null;
            changed = true;
        }

        if (changed)
        {
            RebuildBots();
        }
    }

    // ---------------- reinício ----------------

    /// <summary>Recomeça o encontro (próxima seed). Quem está conectado continua, na mesma vaga e classe, com um novo <see cref="Welcome"/>.</summary>
    public void Restart(string reason)
    {
        _seed++;
        Restarts++;
        var keep = _connections.Values.Where(c => c.Slot is not null).Select(c => (c, _slots[c.Slot!.Value].ClassId)).ToList();
        World = CreateWorld();
        _log.Information("Sala reiniciada ({Reason}); seed {Seed}", reason, _seed);
        foreach (var (c, cls) in keep)
        {
            var index = ChooseSlot(c.Name, cls);
            c.Slot = null;
            Occupy(c, index, cls);
        }
    }

    private World CreateWorld()
    {
        var setup = EncounterSetup.Create(_seed, _content, _encounter);
        _slots.Clear();
        for (var i = 0; i < setup.Players.Count; i++)
        {
            var p = _encounter.Players[i];
            _slots.Add(new Slot
            {
                ClassId = p.Class,
                Entity = setup.Players[i],
                SpawnPosition = new Vector2((float)p.Position.X, (float)p.Position.Y),
            });
        }

        World = setup.World;
        RebuildBots();
        return setup.World;
    }

    /// <summary>Linha de estado para o console.</summary>
    public string Status() =>
        $"tick {World.Tick}, {PlayerCount} jogador(es), seed {_seed}, reinícios {Restarts}; vagas: "
        + string.Join("; ", Slots.Select((s, i) => $"{i}:{s.Class} {(s.Player ?? (s.Bot ? "bot" : "livre"))}{(s.Entity is null ? " (sem entidade)" : "")}"));

    private void Send(int peer, INetMessage message) => _transport.Send(peer, ProtocolSerializer.Serialize(message), SendMode.Reliable);

    private sealed class Connection(int peer)
    {
        public int Peer { get; } = peer;
        public string Name { get; set; } = "";
        public int? Slot { get; set; }
        public Queue<IntentMessage> Pending { get; } = new();
        public uint LastApplied { get; set; }
        public bool AckDue { get; set; }
    }

    private sealed class Slot
    {
        public required string ClassId { get; set; }
        public Entity? Entity { get; set; }
        public Vector2 SpawnPosition { get; init; }
        public int? Peer { get; set; }

        /// <summary>Nome de quem caiu (retoma a vaga se voltar antes da remoção).</summary>
        public string? ReservedFor { get; set; }
        public long? LeftAtTick { get; set; }
    }
}
