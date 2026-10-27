using System.Numerics;
using Game.Auth;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Interest;
using Game.Server.Persistence;
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
/// <item>A cada tick (T-106): para cada jogador, só as entidades da sua área de interesse (grade), como delta em relação ao
/// último tick que ele confirmou (não confiável), e só os eventos dessas entidades (confiável).</item>
/// <item>Quem cai: a entidade para (ou vira bot) e some depois de <see cref="ServerOptions.DisconnectGraceSeconds"/>;
/// voltando com o mesmo nome antes disso, retoma a vaga. Sem jogadores, a sala reinicia o encontro.</item>
/// <item>Ciclo da luta: sala sem jogadores fica <b>parada</b> no começo (os bots não lutam sozinhos); quem entra numa sala vazia
/// começa a luta do zero; no fim (vitória, derrota ou tempo esgotado) a sala mostra o resultado por
/// <see cref="ServerOptions.RestartAfterSeconds"/> e recomeça para quem está nela.</item>
/// <item>Contas (T-212): com token de jogo, o personagem salvo é carregado fora do loop e entra quando chega; nasce com o
/// build, o equipamento e o inventário salvos. É gravado (também fora do loop) ao sair, a cada
/// <see cref="ServerOptions.SaveIntervalSeconds"/>, ao subir de nível, ao pegar item raro, no reinício da sala e na parada
/// (<see cref="SaveAll"/>). Um personagem só entra uma vez; ao sair, a entidade some na hora (sem vaga reservada).</item>
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
    private int _epoch = 1;
    private long? _endedAtTick;
    private readonly InterestGrid _grid;
    private readonly AccountLogin? _accounts;

    // Personagens com conta conectados ou carregando, e a conexão de cada um.
    private readonly Dictionary<Guid, int> _online = new();
    private long _ticksSinceSave;

    public ZoneRoom(ContentCatalog content, ServerOptions options, IServerTransport transport, ILogger log, AccountLogin? accounts = null)
    {
        _content = content;
        _accounts = accounts;
        _options = options;
        _transport = transport;
        _log = log;
        _encounter = content.Encounters.TryGetValue(options.EncounterId, out var enc)
            ? enc
            : throw new ArgumentException($"Encontro desconhecido: {options.EncounterId}");
        _seed = options.Seed;
        _grid = new InterestGrid(options.InterestCellSize, options.InterestRadiusCells);
        World = CreateWorld();
    }

    public World World { get; private set; }

    public EncounterDefinition Encounter => _encounter;

    /// <summary>Jogadores com vaga (conectados).</summary>
    public int PlayerCount => _connections.Values.Count(c => c.Slot is not null);

    public int Restarts { get; private set; }

    /// <summary>Como o encontro terminou (null = em andamento).</summary>
    public string? Outcome { get; private set; }

    /// <summary>Entidade de cada vaga (null = removida depois da desconexão) e quem a controla.</summary>
    public IReadOnlyList<(EntityId? Entity, string Class, string? Player, bool Bot)> Slots =>
        _slots.Select(s => (s.Entity?.Id, s.ClassId, s.Peer is { } p ? _connections[p].Name : s.ReservedFor, IsBotSlot(s))).ToList();

    // ---------------- transporte ----------------

    public void OnConnected(int peer)
    {
        _connections[peer] = new Connection(peer, _options.MaxDeltaAgeTicks);
        _log.Information("Conexão {Peer} aberta", peer);
    }

    public void OnDisconnected(int peer)
    {
        _closeAt.Remove(peer);
        if (!_connections.Remove(peer, out var c))
        {
            return;
        }

        if (c.CharacterId is { } characterId && _online.TryGetValue(characterId, out var owner) && owner == peer)
        {
            _online.Remove(characterId);
        }

        if (c.Slot is not { } index)
        {
            return;
        }

        var slot = _slots[index];
        if (c.CharacterId is not null)
        {
            // Com conta: grava e tira a entidade na hora (voltar exige token novo e recarrega o salvo).
            SaveNow(c, "saída");
            if (slot.Entity is { } entity)
            {
                World.Remove(entity.Id);
            }

            slot.Entity = null;
            slot.Peer = null;
            _log.Information("{Name} saiu (vaga {Slot}); personagem gravado", c.Name, index);
            RebuildBots();
            if (PlayerCount == 0)
            {
                Restart("todos os jogadores saíram");
            }

            return;
        }

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
            case SnapshotAck ack:
                c.View.Ack(ack.Epoch, ack.Tick);
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
        if (c.Slot is not null || c.CharacterId is not null)
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

        if (!string.IsNullOrEmpty(hello.Ticket))
        {
            BeginAccountLogin(c, hello.Ticket);
            return;
        }

        if (!_options.DevLogin)
        {
            Refuse(c, RejectReason.Unauthorized, "token de jogo obrigatório (servidor sem --dev-login)");
            return;
        }

        var name = string.IsNullOrWhiteSpace(hello.DevName) ? $"jogador{c.Peer}" : hello.DevName.Trim();
        if (hello.ClassId is { } cls && !_content.Classes.ContainsKey(cls))
        {
            Refuse(c, RejectReason.BadRequest, $"classe desconhecida: {cls}");
            return;
        }

        Enter(c, name, hello.ClassId);
    }

    /// <summary>Ocupa uma vaga (ou recusa com sala cheia). Primeiro a entrar numa sala vazia recomeça a luta.</summary>
    private void Enter(Connection c, string name, string? classId)
    {
        var index = ChooseSlot(name, classId);
        if (index < 0)
        {
            Refuse(c, RejectReason.Full, $"as {_slots.Count} vagas do encontro estão ocupadas");
            return;
        }

        c.Name = name;
        if (PlayerCount == 0 && (World.Tick > 0 || Outcome is not null))
        {
            // Primeiro a entrar numa sala vazia: luta do zero (não a que sobrou).
            Restart("primeiro jogador entrou");
            index = ChooseSlot(name, classId);
        }

        Occupy(c, index, classId);
        _log.Information("{Name} entrou na vaga {Slot} como {Class} (entidade {Entity})", name, index, _slots[index].ClassId, _slots[index].Entity!.Id);
    }

    // ---------------- contas (T-212) ----------------

    private void BeginAccountLogin(Connection c, string token)
    {
        if (_accounts is null)
        {
            Refuse(c, RejectReason.Unauthorized, "servidor sem contas (só --dev-login)");
            return;
        }

        var (result, ticket) = _accounts.Tickets.Consume(token);
        if (result != TicketCheck.Valid)
        {
            Refuse(c, RejectReason.Unauthorized, $"token de jogo recusado: {result}");
            return;
        }

        if (_online.ContainsKey(ticket!.CharacterId))
        {
            Refuse(c, RejectReason.AlreadyConnected, $"personagem {ticket.CharacterId} já está conectado");
            return;
        }

        _online[ticket.CharacterId] = c.Peer;
        c.CharacterId = ticket.CharacterId;
        _accounts.Persistence.BeginLoad(c.Peer, ticket.CharacterId, ticket.AccountId);
        _log.Information("Conexão {Peer}: token aceito, carregando o personagem {Character}", c.Peer, ticket.CharacterId);
    }

    /// <summary>Personagens que terminaram de carregar entram (na thread do loop).</summary>
    private void ProcessLoads()
    {
        while (_accounts is not null && _accounts.Persistence.TryTakeLoaded(out var r))
        {
            if (!_connections.TryGetValue(r.Peer, out var c) || c.CharacterId != r.CharacterId || c.Slot is not null)
            {
                continue; // caiu enquanto carregava
            }

            string? error = r.Outcome switch
            {
                LoadOutcome.Loaded when !_content.Classes.TryGetValue(r.ClassId, out var cls) || cls.Attributes is null => $"classe {r.ClassId} sem progressão",
                LoadOutcome.Loaded => Leveling.Validate(_content, _content.Classes[r.ClassId], r.State!.Build),
                _ => r.Error ?? r.Outcome.ToString(),
            };
            if (error is not null)
            {
                _online.Remove(r.CharacterId);
                c.CharacterId = null;
                Refuse(c, RejectReason.Unavailable, $"personagem {r.CharacterId}: {error}");
                continue;
            }

            c.Saved = r.State;
            Enter(c, r.Name, r.ClassId);
            if (c.Slot is null)
            {
                _online.Remove(r.CharacterId);
                c.CharacterId = null;
                c.Saved = null;
            }
        }
    }

    /// <summary>Captura o estado do personagem com conta e pede a gravação (não espera o banco).</summary>
    private void SaveNow(Connection c, string reason)
    {
        if (_accounts is null || c.CharacterId is not { } id || c.Slot is not { } index
            || _slots[index].Entity is not { } e || CharacterState.Capture(e) is not { } state)
        {
            return;
        }

        c.Saved = state;
        _accounts.Persistence.Save(id, state, reason);
    }

    /// <summary>Grava todos os personagens com conta conectados (gravação periódica e parada do servidor).</summary>
    public void SaveAll(string reason)
    {
        foreach (var c in _connections.Values)
        {
            SaveNow(c, reason);
        }
    }

    /// <summary>Gravação periódica e por evento (subir de nível, pegar item raro ou melhor).</summary>
    private void SaveAfterTick()
    {
        if (_accounts is null || _online.Count == 0)
        {
            return;
        }

        if (++_ticksSinceSave >= (long)Math.Round(_options.SaveIntervalSeconds * World.TickRate))
        {
            _ticksSinceSave = 0;
            SaveAll("periódica");
            return;
        }

        foreach (var ev in World.LastTickEvents)
        {
            var (entity, reason) = ev switch
            {
                LevelUp l => (l.Entity, "nível"),
                ItemPickedUp p when World.Find(p.Entity)?.TryGet<Inventory>()?[p.InventorySlot] is { Rarity: >= ItemRarity.Rare } => (p.Entity, "item raro"),
                _ => (default(EntityId?), ""),
            };
            if (entity is { } id && _connections.Values.FirstOrDefault(c => c.CharacterId is not null && c.Slot is { } i && _slots[i].Entity?.Id == id) is { } owner)
            {
                SaveNow(owner, reason);
            }
        }
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
        if (slot.Entity is null || (classId is not null && classId != slot.ClassId) || c.Saved is not null)
        {
            Respawn(slot, classId ?? slot.ClassId, c.Saved);
        }

        slot.Peer = c.Peer;
        slot.ReservedFor = null;
        slot.LeftAtTick = null;
        c.Slot = index;
        c.LastApplied = 0;
        c.Pending.Clear();
        c.View.Reset(_epoch);
        c.LastPosition = null;
        RebuildBots();
        Send(c.Peer, new Welcome(slot.Entity!.Id, World.Tick, _encounter.Id, _encounter.Arena, _epoch));
    }

    /// <summary>
    /// Troca a entidade da vaga (classe diferente, removida ou personagem com conta): nasce onde a anterior estava, ou no ponto
    /// da vaga. Com <paramref name="saved"/>, nasce com o estado salvo.
    /// </summary>
    private void Respawn(Slot slot, string classId, CharacterState? saved = null)
    {
        var position = slot.Entity?.TryGet<Transform>()?.Position ?? slot.SpawnPosition;
        if (slot.Entity is { } old)
        {
            World.Remove(old.Id);
        }

        slot.ClassId = classId;
        if (saved is null)
        {
            slot.Entity = EntityFactory.SpawnPlayer(World, _content.Classes[classId], position);
            return;
        }

        var dropped = new List<string>();
        slot.Entity = saved.Spawn(World, _content.Classes[classId], position, dropped);
        if (dropped.Count > 0)
        {
            _log.Warning("Itens salvos que o conteúdo não aceita mais foram descartados: {Items}", string.Join(", ", dropped));
        }
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
        ProcessLoads();
        if (PlayerCount == 0)
        {
            return; // sala vazia fica parada: ninguém vê, ninguém luta (nem os bots)
        }

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
        SaveAfterTick();
        Broadcast();
        CheckEnd();
    }

    /// <summary>Vitória (inimigos mortos), derrota (jogadores mortos) ou tempo esgotado; recomeça depois do intervalo.</summary>
    private void CheckEnd()
    {
        if (Outcome is null)
        {
            bool Alive(Faction f) => World.Entities.Any(e => e.IsAlive && e.TryGet<FactionTag>()?.Faction == f && e.Has<Health>());
            Outcome = !Alive(Faction.Monster) ? "vitória"
                : !Alive(Faction.Player) ? "derrota"
                : _encounter.Defeat.TimeLimitTicks is { } limit && World.Tick >= limit ? "tempo esgotado"
                : null;
            if (Outcome is not null)
            {
                _endedAtTick = World.Tick;
                _log.Information("Encontro terminou: {Outcome} em {Seconds:0} s", Outcome, World.Tick / (double)World.TickRate);
            }

            return;
        }

        if (_options.RestartAfterSeconds > 0 && World.Tick - _endedAtTick >= (long)Math.Round(_options.RestartAfterSeconds * World.TickRate))
        {
            Restart($"fim do encontro ({Outcome})");
        }
    }

    private void Broadcast()
    {
        var players = _connections.Values.Where(c => c.Slot is not null).ToList();
        if (players.Count == 0)
        {
            return;
        }

        // Uma vez por tick: estado em bytes (para os deltas) e a grade de interesse.
        var snapshot = World.Snapshot(SnapshotScope.Replicated);
        var current = EntityState.FromSnapshot(snapshot);
        _grid.Rebuild(snapshot);
        foreach (var c in players)
        {
            var visible = VisibleTo(c);

            // Eventos: de quem ele vê agora ou tinha no último envio (morreu, saiu do mundo neste tick).
            var known = c.View.LastSent;
            var events = World.LastTickEvents.Where(e => EventEntities.Relevant(e, visible, known)).ToArray();
            if (events.Length > 0)
            {
                Send(c.Peer, new EventsMessage(World.Tick, events));
            }

            if (c.AckDue)
            {
                c.AckDue = false;
                Send(c.Peer, new IntentAck(c.LastApplied, World.Tick));
            }

            var delta = c.View.Build(World.Tick, current, visible, _slots[c.Slot!.Value].Entity?.Id);
            var bytes = ProtocolSerializer.Serialize(delta);
            c.SnapshotBytes += bytes.Length;
            _transport.Send(c.Peer, bytes, SendMode.Snapshot);
        }
    }

    /// <summary>Entidades que o jogador vê: em volta da própria entidade (ou da última posição dela).</summary>
    private HashSet<EntityId> VisibleTo(Connection c)
    {
        var slot = _slots[c.Slot!.Value];
        if (slot.Entity is { } e && _grid.PositionOf(e.Id) is { } pos)
        {
            c.LastPosition = pos;
        }

        var visible = _grid.Visible(c.LastPosition ?? slot.SpawnPosition);
        if (slot.Entity is { } own)
        {
            visible.Add(own.Id);
        }

        return visible;
    }

    /// <summary>Só para testes: o que o jogador da conexão deveria ver agora (mesma regra do envio).</summary>
    internal HashSet<EntityId> VisibleToPeer(int peer)
    {
        _grid.Rebuild(World.Snapshot(SnapshotScope.Replicated));
        return VisibleTo(_connections[peer]);
    }

    /// <summary>Bytes de snapshot (delta) mandados para cada jogador desde que entrou (medição da T-106).</summary>
    public IReadOnlyDictionary<int, long> SnapshotBytesByPeer =>
        _connections.Values.Where(c => c.Slot is not null).ToDictionary(c => c.Peer, c => c.SnapshotBytes);

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
        _epoch++;
        Restarts++;
        Outcome = null;
        _endedAtTick = null;
        SaveAll("reinício da sala"); // também guarda em c.Saved o estado com que o personagem volta
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
        $"tick {World.Tick}, {PlayerCount} jogador(es), seed {_seed}, reinícios {Restarts}"
        + (_accounts is { } a ? $", gravações pendentes {a.Persistence.PendingSaves} (feitas {a.Persistence.SavesCompleted}, falhas {a.Persistence.SaveFailures})" : "")
        + "; vagas: "
        + string.Join("; ", Slots.Select((s, i) => $"{i}:{s.Class} {(s.Player ?? (s.Bot ? "bot" : "livre"))}{(s.Entity is null ? " (sem entidade)" : "")}"));

    private void Send(int peer, INetMessage message) => _transport.Send(peer, ProtocolSerializer.Serialize(message), SendMode.Reliable);

    private sealed class Connection(int peer, int maxDeltaAgeTicks)
    {
        public int Peer { get; } = peer;
        public ClientView View { get; } = new(maxDeltaAgeTicks);
        public Vector2? LastPosition { get; set; }
        public long SnapshotBytes { get; set; }
        public string Name { get; set; } = "";
        public int? Slot { get; set; }
        public Queue<IntentMessage> Pending { get; } = new();
        public uint LastApplied { get; set; }
        public bool AckDue { get; set; }

        /// <summary>Personagem com conta (T-212); null = login de desenvolvimento.</summary>
        public Guid? CharacterId { get; set; }

        /// <summary>Último estado carregado ou capturado do personagem com conta (com ele a entidade renasce no reinício).</summary>
        public CharacterState? Saved { get; set; }
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
