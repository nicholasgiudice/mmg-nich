using System.Diagnostics;
using System.Numerics;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Shops;
using Game.Core.Definitions;
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
    private long _respawnAskedAt = long.MinValue / 2;
    private bool _respawnPending;
    private bool _reachedSpawn;
    private double _nextPingAt;
    private double _nextInviteAt;
    private string[] _partyNames = [];
    private readonly Dictionary<string, int> _zoneVisits = new(StringComparer.Ordinal);
    private List<Vector2> _shopPath = [];
    private long _nextShopTick;

    public NetBot(int index, string host, int port, string? classId, string strategy, ContentCatalog content, BotOptions options, Stopwatch clock, string? ticket = null)
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
            Send(new Hello(ProtocolVersion.Current, content.Fingerprint, $"netbot{index}", classId, ticket), ReliableChannel, DeliveryMethod.ReliableOrdered);
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

    /// <summary>Mundo aberto (T-214): anda do nascimento ao portão de cada zona em vez de lutar.</summary>
    public bool Roam { get; init; }

    /// <summary>No mundo aberto (o <see cref="Welcome"/> é de uma zona de <c>data/zones.json</c>).</summary>
    public bool InWorld => Zone is not null;

    /// <summary><see cref="Welcome"/> de zona recebidos depois do primeiro (portão ou renascimento).</summary>
    public int ZoneChanges { get; private set; }

    public int Respawns { get; private set; }

    /// <summary>Zonas em que o bot já esteve (T-331: o <c>--roam</c> passa pelas três).</summary>
    public IReadOnlyCollection<string> ZonesVisited => _zoneVisits.Keys;

    // ---------- grupo (T-310) ----------

    /// <summary>Líder: nomes dos bots que ele convida (de novo a cada 5 s, enquanto não estão no grupo).</summary>
    public IReadOnlyList<string> PartyInvites { get; init; } = [];

    /// <summary>Aceita todo convite de grupo que chegar.</summary>
    public bool AcceptPartyInvites { get; init; }

    /// <summary>Membros no último <see cref="PartyState"/> (0 = sem grupo).</summary>
    public int PartySize { get; private set; }

    public int PartyStates { get; private set; }

    public int PartyRejections { get; private set; }

    // ---------- loja (T-321) ----------

    /// <summary>Mundo aberto: anda até o NPC da loja <see cref="ShopId"/> e compra 1 unidade de <see cref="ShopItem"/>.</summary>
    public bool Shop { get; init; }

    public string ShopId { get; init; } = "shop.temple";
    public string ShopItem { get; init; } = "consumable.minor_healing_potion";

    /// <summary>Compras e vendas confirmadas (eventos do servidor) e recusas da loja.</summary>
    public int ShopBuys { get; private set; }

    public int ShopSales { get; private set; }

    public int ShopRejections { get; private set; }

    /// <summary>Chegou perto do NPC da loja.</summary>
    public bool ReachedShop { get; private set; }

    private ZoneDefinition? Zone => Welcome is { } w && _content.Zones.TryGetValue(w.EncounterId, out var z) ? z : null;

    /// <summary>Recebe o que chegou e, se há snapshot novo, decide e manda as intenções.</summary>
    public void Update()
    {
        _net.PollEvents();
        if (_peer is null || Welcome is null || _state.Latest is not { } latest)
        {
            return;
        }

        PingIfDue();
        InviteIfDue();
        if (latest.Tick == _lastThought || _mirror is null || _controller is null)
        {
            return;
        }

        _lastThought = latest.Tick;
        _mirror.Mirror(latest);
        if (Zone is { } zone)
        {
            if (_mirror.Find(Welcome.ControlledEntity) is { IsAlive: false } && latest.Tick >= _respawnAskedAt + World.TickRate)
            {
                _respawnAskedAt = latest.Tick;
                _respawnPending = true;
                Send(new RespawnRequest(), ReliableChannel, DeliveryMethod.ReliableOrdered);
            }
        }
        else
        {
            TrackOutcome(latest); // mundo aberto não termina
        }

        if (_mirror.Find(Welcome.ControlledEntity) is not { IsAlive: true } self || Outcome != BotOutcome.Playing)
        {
            return;
        }

        _buffer.Clear();
        if (Shop && Zone is { } shopZone)
        {
            ShopIntent(shopZone, self, latest.Tick);
        }
        else if (Roam && Zone is { } here)
        {
            RoamIntent(here, self);
        }
        else
        {
            _controller.Think(new BotContext(_mirror, self, _options, _hazards, (ulong)Welcome.Epoch), _buffer);
        }

        foreach (var intent in _buffer.Where(i => i.Entity == self.Id))
        {
            Send(new IntentMessage(++_sequence, latest.Tick, NetIntent.From(intent)), ReliableChannel, DeliveryMethod.ReliableOrdered);
            IntentsSent++;
        }
    }

    /// <summary>
    /// Primeiro vai ao nascimento da zona; de lá, ao centro de um portão. A cada visita à zona usa o portão seguinte
    /// (T-331), para passar por todas as zonas e não só ir e voltar pelo primeiro.
    /// </summary>
    private void RoamIntent(ZoneDefinition zone, Entity self)
    {
        var position = self.Get<Transform>().Position;
        var spawn = new Vector2((float)zone.Spawn.X, (float)zone.Spawn.Y);
        if (Vector2.Distance(position, spawn) < 1)
        {
            _reachedSpawn = true;
        }

        var gate = zone.Gates[(_zoneVisits.GetValueOrDefault(zone.Id, 1) - 1) % zone.Gates.Count].Area;
        var target = _reachedSpawn ? new Vector2((float)(gate.X + (gate.Width / 2)), (float)(gate.Y + (gate.Height / 2))) : spawn;
        var to = target - position;
        _buffer.Add(to.LengthSquared() < 0.01f ? new StopIntent(self.Id) : new MoveIntent(self.Id, Vector2.Normalize(to)));
    }

    /// <summary>
    /// T-321: segue um caminho (A*) até o NPC da loja na zona e, ao alcance, compra 1 unidade do item. Sem ouro, vende antes
    /// as pilhas de outros itens (o bot de <c>--dev-login</c> nasce sem ouro, só com as poções iniciais) e depois compra.
    /// Um pedido por segundo; para depois da primeira compra.
    /// </summary>
    private void ShopIntent(ZoneDefinition zone, Entity self, long tick)
    {
        if (ShopBuys > 0 || zone.Npcs.FirstOrDefault(n => n.Shop == ShopId) is not { } placement)
        {
            _buffer.Add(new StopIntent(self.Id));
            return;
        }

        var position = self.Get<Transform>().Position;
        var target = new Vector2((float)placement.Position.X, (float)placement.Position.Y);
        var range = (float)_content.Rules.Shops.InteractRange - 0.5f;
        if (Vector2.Distance(position, target) > range)
        {
            if (_shopPath.Count == 0 && _mirror?.Map is { } map)
            {
                _shopPath = Game.Core.Spatial.Pathfinder.FindPath(map, position, target, self.TryGet<Collider>()?.Radius ?? 0).ToList();
            }

            while (_shopPath.Count > 0 && Vector2.Distance(position, _shopPath[0]) < 0.3f)
            {
                _shopPath.RemoveAt(0);
            }

            var to = (_shopPath.Count > 0 ? _shopPath[0] : target) - position;
            _buffer.Add(to.LengthSquared() < 0.01f ? new StopIntent(self.Id) : new MoveIntent(self.Id, Vector2.Normalize(to)));
            return;
        }

        ReachedShop = true;
        _buffer.Add(new StopIntent(self.Id));
        var npc = _mirror!.Entities.FirstOrDefault(e => e.TryGet<NpcInfo>()?.Shop == ShopId);
        if (npc is null || tick < _nextShopTick || !_content.Shops.TryGetValue(ShopId, out var shop))
        {
            return;
        }

        _nextShopTick = tick + World.TickRate;
        var offer = shop.Offers.Select((o, i) => (o, i)).First(x => x.o.Item == ShopItem);
        var price = Game.Core.Combat.Shops.Shop.PriceOf(_content, offer.o);
        if ((self.TryGet<Wallet>()?.Gold ?? 0) >= price)
        {
            _buffer.Add(new BuyIntent(self.Id, npc.Id, offer.i, 1));
            return;
        }

        var inv = self.TryGet<Inventory>();
        var sell = inv?.Slots.Select((item, slot) => (item, slot)).FirstOrDefault(x => x.item is not null);
        if (sell is { item: { } item } found)
        {
            _buffer.Add(new SellIntent(self.Id, npc.Id, found.slot, item.Count));
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
                if (Welcome is not null && _content.Zones.ContainsKey(w.EncounterId))
                {
                    ZoneChanges++;
                    Respawns += _respawnPending ? 1 : 0;
                }

                _respawnPending = false;
                _reachedSpawn = false;
                _shopPath = [];
                Welcome = w;
                _zoneVisits[w.EncounterId] = _zoneVisits.GetValueOrDefault(w.EncounterId) + 1;
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
            case PartyInviteReceived invite when AcceptPartyInvites:
                Send(new PartyRespond(invite.InviteId, true), ReliableChannel, DeliveryMethod.ReliableOrdered);
                break;
            case PartyState party:
                PartyStates++;
                PartySize = party.Members.Length;
                _partyNames = party.Members.Select(m => m.Name).ToArray();
                break;
            case PartyRejected:
                PartyRejections++;
                break;
            case EventsMessage events when Welcome is { } me:
                foreach (var ev in events.Events)
                {
                    switch (ev)
                    {
                        case ItemBought b when b.Entity == me.ControlledEntity: ShopBuys++; break;
                        case ItemSold x when x.Entity == me.ControlledEntity: ShopSales++; break;
                        case ShopRejected r when r.Entity == me.ControlledEntity: ShopRejections++; break;
                    }
                }

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

    /// <summary>Líder no mundo aberto: convida pelo nome quem ainda não está no grupo.</summary>
    private void InviteIfDue()
    {
        var now = _clock.Elapsed.TotalMilliseconds;
        if (PartyInvites.Count == 0 || !InWorld || now < _nextInviteAt)
        {
            return;
        }

        _nextInviteAt = now + 5000;
        foreach (var name in PartyInvites.Where(n => !_partyNames.Contains(n)))
        {
            Send(new PartyInvite(null, name), ReliableChannel, DeliveryMethod.ReliableOrdered);
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
