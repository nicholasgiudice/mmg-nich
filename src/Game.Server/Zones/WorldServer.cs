using System.Numerics;
using Game.Auth;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Interest;
using Game.Server.Parties;
using Game.Server.Persistence;
using Game.Server.Transport;
using Serilog;

namespace Game.Server.Zones;

/// <summary>
/// Mundo aberto (T-214): todas as zonas de <c>data/zones.json</c> num processo, cada uma com seu <see cref="OpenZone"/>
/// (mundo, tick e interesse próprios). Roda na thread do loop, como a <see cref="ZoneRoom"/>.
/// <list type="bullet">
/// <item>Entrada: token de jogo (personagem salvo, na zona e posição gravadas) ou, com <c>--dev-login</c>, nome e classe
/// (personagem nível 1 com progressão, não gravado). Quem não tem zona gravada nasce em <see cref="ServerOptions.StartZone"/>.</item>
/// <item>Portão: personagem vivo dentro da área vai para a outra zona, com o mesmo estado (build, itens, vida e Mana, recargas e
/// cargas gastas — T-301; status e ameaça se perdem); o
/// cliente recebe um <see cref="Welcome"/> novo com a arena da zona e época nova.</item>
/// <item>Morte: o personagem fica caído até mandar <see cref="RespawnRequest"/>; renasce na zona inicial, cheio, sem perder nada.</item>
/// <item>Só zonas com jogador avançam; zona vazia fica parada (monstros e renascimentos esperam).</item>
/// <item>Personagens com conta são gravados como na sala (T-212) e também ao trocar de zona; a gravação leva zona e posição.</item>
/// <item>Grupo (T-310): <see cref="PartyManager"/> acima das zonas; cada membro tem o <c>PartyMembership</c> na entidade
/// (refeito a cada troca de zona) e recebe o <see cref="PartyState"/> quando algo muda. Desconectar tira do grupo; o
/// grupo não é gravado.</item>
/// </list>
/// </summary>
public sealed class WorldServer : ITransportHandler, IGameHost, IPartyHost
{
    /// <summary>De quantos em quantos ticks o estado do grupo (vida, Mana, zona) é conferido e reenviado se mudou.</summary>
    public const int PartyStateIntervalTicks = 5;

    public const int MaxIntentsPerTick = 16;
    private const int RejectCloseDelayTicks = 10;
    private const string DevDefaultClass = "class.warrior";

    private readonly ContentCatalog _content;
    private readonly ServerOptions _options;
    private readonly IServerTransport _transport;
    private readonly ILogger _log;
    private readonly AccountLogin? _accounts;
    private readonly Dictionary<string, OpenZone> _zones = new(StringComparer.Ordinal);
    private readonly OpenZone _start;
    private readonly Dictionary<int, Player> _players = new();
    private readonly Dictionary<Guid, int> _online = new();
    private readonly Dictionary<int, long> _closeAt = new();
    private readonly PartyManager _parties;
    private long _tick;
    private long _ticksSinceSave;

    public WorldServer(ContentCatalog content, ServerOptions options, IServerTransport transport, ILogger log, AccountLogin? accounts = null)
    {
        _content = content;
        _options = options;
        _transport = transport;
        _log = log;
        _accounts = accounts;
        _parties = new PartyManager(content.Rules.Party, this);
        var seed = options.Seed;
        foreach (var zone in content.Zones.Values.OrderBy(z => z.Id, StringComparer.Ordinal))
        {
            _zones[zone.Id] = new OpenZone(content, zone, seed++, options);
        }

        _start = _zones.TryGetValue(options.StartZone, out var start)
            ? start
            : throw new ArgumentException($"Zona inicial desconhecida: {options.StartZone} (zonas: {string.Join(", ", _zones.Keys)})");
    }

    public IReadOnlyDictionary<string, OpenZone> Zones => _zones;

    public PartyManager Parties => _parties;

    public int PlayerCount => _players.Values.Count(p => p.Entity is not null);

    /// <summary>Zona e entidade de cada jogador em jogo (testes e console).</summary>
    public IReadOnlyDictionary<int, (string Zone, EntityId Entity)> Placement =>
        _players.Values.Where(p => p.Entity is not null).ToDictionary(p => p.Peer, p => (p.Zone!.Id, p.Entity!.Id));

    // ---------------- transporte ----------------

    public void OnConnected(int peer)
    {
        _players[peer] = new Player(peer, _options.MaxDeltaAgeTicks);
        _log.Information("Conexão {Peer} aberta", peer);
    }

    public void OnDisconnected(int peer)
    {
        _closeAt.Remove(peer);
        if (!_players.TryGetValue(peer, out var p))
        {
            return;
        }

        p.Leaving = true;
        _parties.Disconnected(peer);
        _players.Remove(peer);

        if (p.CharacterId is { } id && _online.TryGetValue(id, out var owner) && owner == peer)
        {
            _online.Remove(id);
        }

        if (p.Entity is { } e)
        {
            SaveNow(p, "saída");
            p.Zone!.World.Remove(e.Id);
            _log.Information("{Name} saiu de {Zone}", p.Name, p.Zone.Id);
        }
    }

    public void OnReceived(int peer, ReadOnlySpan<byte> data)
    {
        if (!_players.TryGetValue(peer, out var p))
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
                OnHello(p, hello);
                break;
            case IntentMessage intent when p.Entity is not null:
                if (p.Pending.Count < MaxIntentsPerTick * 4)
                {
                    p.Pending.Enqueue(intent);
                }

                break;
            case SnapshotAck ack:
                p.View.Ack(ack.Epoch, ack.Tick);
                break;
            case RespawnRequest:
                p.WantsRespawn = true;
                break;
            case PartyInvite invite when p.Entity is not null:
                _parties.Invite(peer, FindInvitee(p, invite), _tick);
                break;
            case PartyRespond respond when p.Entity is not null:
                _parties.Respond(peer, respond.InviteId, respond.Accept, _tick);
                break;
            case PartyLeave when p.Entity is not null:
                _parties.Leave(peer);
                break;
            case PartyKick kick when p.Entity is not null:
                _parties.Kick(peer, kick.MemberId);
                break;
            case Ping ping:
                Send(peer, new Pong(ping.ClientTimeMs, p.Zone?.World.Tick ?? 0));
                break;
            default:
                _log.Debug("Mensagem {Type} ignorada de {Peer}", message?.GetType().Name, peer);
                break;
        }
    }

    private void OnHello(Player p, Hello hello)
    {
        if (p.Entity is not null || p.CharacterId is not null)
        {
            return; // repetido
        }

        if (hello.ProtocolVersion != ProtocolVersion.Current)
        {
            Refuse(p, RejectReason.ProtocolVersion, $"servidor v{ProtocolVersion.Current}, cliente v{hello.ProtocolVersion}");
            return;
        }

        if (!string.Equals(hello.ContentFingerprint, _content.Fingerprint, StringComparison.Ordinal))
        {
            Refuse(p, RejectReason.Content, $"conteúdo do servidor {_content.Fingerprint}, do cliente {hello.ContentFingerprint}");
            return;
        }

        if (!string.IsNullOrEmpty(hello.Ticket))
        {
            BeginAccountLogin(p, hello.Ticket);
            return;
        }

        if (!_options.DevLogin)
        {
            Refuse(p, RejectReason.Unauthorized, "token de jogo obrigatório (servidor sem --dev-login)");
            return;
        }

        var classId = hello.ClassId ?? DevDefaultClass;
        if (!_content.Classes.TryGetValue(classId, out var cls) || cls.Attributes is null)
        {
            Refuse(p, RejectReason.BadRequest, $"classe sem progressão ou desconhecida: {classId}");
            return;
        }

        p.Name = string.IsNullOrWhiteSpace(hello.DevName) ? $"jogador{p.Peer}" : hello.DevName.Trim();
        p.ClassId = classId;
        Enter(p, CharacterState.New);
    }

    private void Refuse(Player p, RejectReason reason, string detail)
    {
        _log.Information("Conexão {Peer} recusada: {Reason} ({Detail})", p.Peer, reason, detail);
        Send(p.Peer, new Reject(reason, detail));
        _closeAt[p.Peer] = _tick + RejectCloseDelayTicks;
    }

    // ---------------- contas (T-212) ----------------

    private void BeginAccountLogin(Player p, string token)
    {
        if (_accounts is null)
        {
            Refuse(p, RejectReason.Unauthorized, "servidor sem contas (só --dev-login)");
            return;
        }

        var (result, ticket) = _accounts.Tickets.Consume(token);
        if (result != TicketCheck.Valid)
        {
            Refuse(p, RejectReason.Unauthorized, $"token de jogo recusado: {result}");
            return;
        }

        if (_online.ContainsKey(ticket!.CharacterId))
        {
            Refuse(p, RejectReason.AlreadyConnected, $"personagem {ticket.CharacterId} já está conectado");
            return;
        }

        _online[ticket.CharacterId] = p.Peer;
        p.CharacterId = ticket.CharacterId;
        _accounts.Persistence.BeginLoad(p.Peer, ticket.CharacterId, ticket.AccountId);
        _log.Information("Conexão {Peer}: token aceito, carregando o personagem {Character}", p.Peer, ticket.CharacterId);
    }

    private void ProcessLoads()
    {
        while (_accounts is not null && _accounts.Persistence.TryTakeLoaded(out var r))
        {
            if (!_players.TryGetValue(r.Peer, out var p) || p.CharacterId != r.CharacterId || p.Entity is not null)
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
                p.CharacterId = null;
                Refuse(p, RejectReason.Unavailable, $"personagem {r.CharacterId}: {error}");
                continue;
            }

            p.Name = r.Name;
            p.ClassId = r.ClassId;
            Enter(p, r.State!);
        }
    }

    // ---------------- zonas ----------------

    /// <summary>Entrada no mundo: zona e posição gravadas, se a zona existe; senão o nascimento da zona inicial.</summary>
    private void Enter(Player p, CharacterState state)
    {
        var (zone, position) = state.Location is { } at && _zones.TryGetValue(at.ZoneId, out var saved)
            ? (saved, new Vector2(at.X, at.Y))
            : (_start, _start.SpawnPosition);
        Place(p, zone, state, position, vitals: null);
        _log.Information("{Name} entrou em {Zone} como {Class} (entidade {Entity})", p.Name, zone.Id, p.ClassId, p.Entity!.Id);
    }

    private void Place(Player p, OpenZone zone, CharacterState state, Vector2 position, (float, float)? vitals)
    {
        var dropped = new List<string>();
        p.Zone = zone;
        p.Entity = zone.AddPlayer(state, _content.Classes[p.ClassId], position, vitals, dropped);
        p.Entity.Add(p.Entity.Get<UnitInfo>().WithName(p.Name)); // o cliente mostra o nome do personagem abaixo dele
        p.Saved = state;
        p.Epoch++;
        p.View.Reset(p.Epoch);
        p.LastPosition = null;
        p.Pending.Clear();
        p.AckDue = false;
        p.WantsRespawn = false;
        if (dropped.Count > 0)
        {
            _log.Warning("Itens salvos que o conteúdo não aceita mais foram descartados: {Items}", string.Join(", ", dropped));
        }

        if (_parties.PartyOf(p.Peer) is var party and not 0)
        {
            p.Entity.Add(new PartyMembership { PartyId = party }); // T-310: o grupo acompanha a troca de zona
            MarkParty(p.Peer);
        }

        Send(p.Peer, new Welcome(p.Entity.Id, zone.World.Tick, zone.Id, zone.Arena.Id, p.Epoch));
    }

    /// <summary>Tira o personagem da zona atual e põe em <paramref name="to"/>, com o estado capturado agora.</summary>
    private void Move(Player p, OpenZone to, Vector2 position, bool keepVitals, string reason)
    {
        var e = p.Entity!;
        var from = p.Zone!;
        var state = CharacterState.Capture(e) ?? CharacterState.New;
        if (e.TryGet<AbilityState>() is { } abilities)
        {
            state = state with { Timers = AbilityTimers.Capture(abilities, from.World.Tick) }; // T-301: recargas acompanham
        }

        (float, float)? vitals = keepVitals ? (e.Get<Health>().Current, e.TryGet<ResourcePool>()?.Current ?? 0) : null;
        from.World.Remove(e.Id);
        Place(p, to, state, position, vitals);
        _log.Information("{Name}: {From} → {To} ({Reason})", p.Name, from.Id, to.Id, reason);
        SaveNow(p, reason);
    }

    // ---------------- tick ----------------

    public void Step()
    {
        _tick++;
        CloseRefused();
        ProcessLoads();
        _parties.Expire(_tick);

        foreach (var p in _players.Values)
        {
            if (p.Entity is not { } entity)
            {
                p.Pending.Clear();
                continue;
            }

            for (var n = 0; n < MaxIntentsPerTick && p.Pending.TryDequeue(out var msg); n++)
            {
                // A entidade vem da conexão, nunca da mensagem (T-103).
                if (msg.Intent.ToIntent(entity.Id) is { } intent)
                {
                    p.Zone!.World.Enqueue(intent);
                }

                p.LastApplied = msg.Sequence;
                p.AckDue = true;
            }
        }

        var active = _players.Values.Where(p => p.Zone is not null && p.Entity is not null).Select(p => p.Zone!).Distinct().ToList();
        foreach (var zone in active)
        {
            zone.Step();
        }

        SaveAfterTick(active);
        MoveThroughGatesAndRespawn();
        foreach (var zone in _zones.Values)
        {
            Broadcast(zone);
        }

        SendPartyStates();
    }

    private void MoveThroughGatesAndRespawn()
    {
        foreach (var p in _players.Values.Where(p => p.Entity is not null).ToList())
        {
            var e = p.Entity!;
            if (!e.IsAlive)
            {
                if (p.WantsRespawn)
                {
                    Move(p, _start, _start.SpawnPosition, keepVitals: false, "renasceu");
                }

                continue;
            }

            p.WantsRespawn = false;
            if (e.TryGet<Transform>() is { } t && p.Zone!.GateAt(t.Position) is { } gate)
            {
                Move(p, _zones[gate.To], new Vector2((float)gate.Arrival.X, (float)gate.Arrival.Y), keepVitals: true, "portão");
            }
        }
    }

    private void Broadcast(OpenZone zone)
    {
        var players = _players.Values.Where(p => p.Zone == zone && p.Entity is not null).ToList();
        if (players.Count == 0)
        {
            return;
        }

        var world = zone.World;
        var snapshot = world.Snapshot(SnapshotScope.Replicated);
        var current = EntityState.FromSnapshot(snapshot);
        zone.Grid.Rebuild(snapshot);
        foreach (var p in players)
        {
            if (zone.Grid.PositionOf(p.Entity!.Id) is { } pos)
            {
                p.LastPosition = pos;
            }

            var visible = zone.Grid.Visible(p.LastPosition ?? zone.SpawnPosition);
            visible.Add(p.Entity.Id);

            var known = p.View.LastSent;
            var events = world.LastTickEvents.Where(ev => EventEntities.Relevant(ev, visible, known)).ToArray();
            if (events.Length > 0)
            {
                Send(p.Peer, new EventsMessage(world.Tick, events));
            }

            if (p.AckDue)
            {
                p.AckDue = false;
                Send(p.Peer, new IntentAck(p.LastApplied, world.Tick));
            }

            _transport.Send(p.Peer, ProtocolSerializer.Serialize(p.View.Build(world.Tick, current, visible, p.Entity.Id)), SendMode.Snapshot);
        }
    }

    private void CloseRefused()
    {
        foreach (var (peer, _) in _closeAt.Where(kv => _tick >= kv.Value).ToList())
        {
            _closeAt.Remove(peer);
            _transport.Disconnect(peer);
        }
    }

    // ---------------- gravação ----------------

    /// <summary>Captura e pede a gravação (só personagem com conta). Morto grava na zona inicial, no nascimento.</summary>
    private void SaveNow(Player p, string reason)
    {
        if (_accounts is null || p.CharacterId is not { } id || p.Entity is not { } e || CharacterState.Capture(e) is not { } state)
        {
            return;
        }

        var position = e.Get<Transform>().Position;
        var location = e.IsAlive ? new CharacterLocation(p.Zone!.Id, position.X, position.Y) : new CharacterLocation(_start.Id, 0, 0);
        p.Saved = state with { Location = location };
        _accounts.Persistence.Save(id, p.Saved, reason);
    }

    public void SaveAll(string reason)
    {
        foreach (var p in _players.Values)
        {
            SaveNow(p, reason);
        }
    }

    private void SaveAfterTick(List<OpenZone> active)
    {
        if (_accounts is null || _online.Count == 0)
        {
            return;
        }

        if (++_ticksSinceSave >= (long)Math.Round(_options.SaveIntervalSeconds * Core.Simulation.World.TickRate))
        {
            _ticksSinceSave = 0;
            SaveAll("periódica");
            return;
        }

        foreach (var zone in active)
        {
            foreach (var ev in zone.World.LastTickEvents)
            {
                var (entity, reason) = ev switch
                {
                    LevelUp l => (l.Entity, "nível"),
                    ItemPickedUp i when zone.World.Find(i.Entity)?.TryGet<Inventory>()?[i.InventorySlot] is { Rarity: >= ItemRarity.Rare } => (i.Entity, "item raro"),
                    Core.Combat.Shops.ItemBought b => (b.Entity, "compra"), // T-321: ouro e inventário gravados logo
                    Core.Combat.Shops.ItemSold s => (s.Entity, "venda"),
                    _ => (default(EntityId?), ""),
                };
                if (entity is { } id && _players.Values.FirstOrDefault(p => p.CharacterId is not null && p.Zone == zone && p.Entity?.Id == id) is { } owner)
                {
                    SaveNow(owner, reason);
                }
            }
        }
    }

    // ---------------- grupo (T-310) ----------------

    /// <summary>Convidado pela entidade (na zona de quem convida) ou pelo nome (em qualquer zona); null se não achou.</summary>
    private int? FindInvitee(Player from, PartyInvite invite)
    {
        var found = invite.Target is { } target
            ? _players.Values.FirstOrDefault(q => q.Zone == from.Zone && q.Entity?.Id == target)
            : string.IsNullOrWhiteSpace(invite.Name)
                ? null
                : _players.Values.Where(q => q.Entity is not null && string.Equals(q.Name, invite.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .OrderBy(q => q.Peer)
                    .FirstOrDefault();
        return found?.Peer;
    }

    string IPartyHost.NameOf(int member) => _players.TryGetValue(member, out var p) ? p.Name : "";

    bool IPartyHost.IsOnline(int member) => _players.TryGetValue(member, out var p) && !p.Leaving && p.Entity is not null;

    void IPartyHost.Send(int member, INetMessage message)
    {
        if (_players.TryGetValue(member, out var p) && !p.Leaving)
        {
            Send(member, message);
        }
    }

    void IPartyHost.PartyChanged(int member, int partyId)
    {
        if (_players.TryGetValue(member, out var p) && p.Entity is { } e)
        {
            if (partyId == 0)
            {
                e.Remove<PartyMembership>();
            }
            else
            {
                e.Add(new PartyMembership { PartyId = partyId });
            }

            p.PartyDirty = true;
        }

        MarkParty(member);
    }

    /// <summary>Todos do grupo do membro recebem o estado de novo no próximo envio.</summary>
    private void MarkParty(int member)
    {
        foreach (var m in _parties.Describe(member)?.Members ?? [])
        {
            if (_players.TryGetValue(m, out var q))
            {
                q.PartyDirty = true;
            }
        }
    }

    private void SendPartyStates()
    {
        var periodic = _tick % PartyStateIntervalTicks == 0;
        foreach (var p in _players.Values)
        {
            if (p.Entity is null || p.Leaving || !(p.PartyDirty || (periodic && p.LastPartyState is not null)))
            {
                continue;
            }

            p.PartyDirty = false;
            var state = PartyStateFor(p.Peer);
            if (state.PartyId == 0 && p.LastPartyState is null)
            {
                continue; // nunca esteve em grupo
            }

            var bytes = ProtocolSerializer.Serialize(state);
            if (p.LastPartyState is { } last && last.AsSpan().SequenceEqual(bytes))
            {
                continue;
            }

            p.LastPartyState = state.PartyId == 0 ? null : bytes;
            _transport.Send(p.Peer, bytes, SendMode.Reliable);
        }
    }

    /// <summary>Estado do grupo como o membro vê (sem grupo: id 0 e lista vazia).</summary>
    public PartyState PartyStateFor(int member)
    {
        if (_parties.Describe(member) is not { } party)
        {
            return new PartyState(0, 0, member, []);
        }

        var members = party.Members.Select(m =>
        {
            var q = _players[m];
            var e = q.Entity;
            var health = e?.TryGet<Health>();
            var mana = e?.TryGet<ResourcePool>();
            return new PartyMemberInfo(
                m,
                e?.Id ?? default,
                q.Name,
                q.ClassId,
                StatMath.LevelOf(e),
                health?.Current ?? 0,
                health?.Max ?? 0,
                mana?.Current ?? 0,
                mana?.Max ?? 0,
                q.Zone?.Id ?? "",
                e?.IsAlive ?? false);
        }).ToArray();
        return new PartyState(party.PartyId, party.Leader, member, members);
    }

    // ---------------- console ----------------

    public void Restart(string reason) => _log.Information("Mundo aberto não reinicia ({Reason}); use quit e suba de novo", reason);

    public string Status() =>
        $"mundo aberto, {PlayerCount} jogador(es)"
        + (_accounts is { } a ? $", gravações pendentes {a.Persistence.PendingSaves} (feitas {a.Persistence.SavesCompleted}, falhas {a.Persistence.SaveFailures})" : "")
        + "; zonas: "
        + string.Join("; ", _zones.Values.Select(z => $"{z.Id} tick {z.World.Tick}, {_players.Values.Count(p => p.Zone == z && p.Entity is not null)} jogador(es), {z.LivingMonsters} monstro(s)"));

    private void Send(int peer, INetMessage message) => _transport.Send(peer, ProtocolSerializer.Serialize(message), SendMode.Reliable);

    private sealed class Player(int peer, int maxDeltaAgeTicks)
    {
        public int Peer { get; } = peer;
        public ClientView View { get; } = new(maxDeltaAgeTicks);
        public string Name { get; set; } = "";
        public string ClassId { get; set; } = DevDefaultClass;
        public OpenZone? Zone { get; set; }
        public Entity? Entity { get; set; }
        public int Epoch { get; set; }
        public Vector2? LastPosition { get; set; }
        public Queue<IntentMessage> Pending { get; } = new();
        public uint LastApplied { get; set; }
        public bool AckDue { get; set; }
        public bool WantsRespawn { get; set; }
        public Guid? CharacterId { get; set; }
        public CharacterState? Saved { get; set; }

        /// <summary>Desconectando: não recebe mais mensagens nem conta como em jogo para o grupo.</summary>
        public bool Leaving { get; set; }

        public bool PartyDirty { get; set; }

        /// <summary>Último <see cref="PartyState"/> enviado (null = sem grupo).</summary>
        public byte[]? LastPartyState { get; set; }
    }
}
