using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Protocol;
using Game.Server;
using Game.Server.Transport;
using Serilog.Core;

namespace Game.Server.Tests;

/// <summary>Sala + transporte em memória + clientes de teste que montam o estado com os deltas e confirmam (T-105/T-106).</summary>
internal sealed class TestRig
{
    public static readonly Lazy<ContentCatalog> RealContent = new(() => new ServerOptions().LoadContent());

    public TestRig(string encounter = "encounter.forest_matriarch", bool bots = true, double grace = 30, ContentCatalog? content = null, ServerOptions? options = null,
        bool devLogin = true, Game.Server.Persistence.AccountLogin? accounts = null)
    {
        Content = content ?? RealContent.Value;
        var o = (options ?? new ServerOptions()) with { EncounterId = encounter, FillWithBots = bots, DisconnectGraceSeconds = grace, DevLogin = devLogin };
        Room = new ZoneRoom(Content, o, Transport, Logger.None, accounts);
        Transport.Start(Room);
    }

    public ContentCatalog Content { get; }

    public InMemoryTransport Transport { get; } = new();

    public ZoneRoom Room { get; }

    /// <summary>Ticks do servidor: entrega o que chegou, avança e deixa os clientes processarem o que receberam.</summary>
    public void Tick(int n = 1, IEnumerable<TestClient>? clients = null)
    {
        for (var i = 0; i < n; i++)
        {
            Transport.Poll();
            Room.Step();
            foreach (var c in clients ?? [])
            {
                c.Sync();
            }
        }
    }

    public TestClient Join(string name, string? cls)
    {
        var client = new TestClient(Transport.Connect(), Content);
        client.Send(new Hello(ProtocolVersion.Current, Content.Fingerprint, name, cls));
        Tick();
        client.Sync();
        return client;
    }

    /// <summary>
    /// Conteúdo real mais uma arena aberta de <paramref name="width"/> × <paramref name="height"/>, um boneco parado
    /// (<c>monster.test_dummy</c>, IA <c>ai.none</c>) e o encontro <c>encounter.test_map</c> com um Guerreiro em
    /// <paramref name="player"/> e os inimigos dados.
    /// </summary>
    public static ContentCatalog Synthetic(int width, int height, (float X, float Y) player, IEnumerable<(string Monster, float X, float Y)> enemies)
    {
        var c = RealContent.Value;
        var arena = new ArenaDefinition { Id = "arena.test_map", Lines = Enumerable.Repeat(new string('.', width), height).ToList() };
        var dummy = new MonsterDefinition
        {
            Id = "monster.test_dummy",
            Name = "Boneco",
            Health = 1000,
            Speed = 0,
            Radius = 0.4,
            Ai = new AiProfileDefinition { Id = "ai.none" },
        };
        var encounter = new EncounterDefinition
        {
            Id = "encounter.test_map",
            Name = "Mapa de teste",
            Arena = arena.Id,
            Players = [new PlayerSpawnDefinition { Class = "class.warrior", Position = new PositionDefinition(player.X, player.Y) }],
            Enemies = enemies.Select(e => new EnemySpawnDefinition { Monster = e.Monster, Position = new PositionDefinition(e.X, e.Y) }).ToList(),
        };
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values.Append(dummy), c.Arenas.Values.Append(arena), c.Encounters.Values.Append(encounter), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
        };
    }
}

/// <summary>Cliente de teste: lê o que chegou, monta o estado com <see cref="SnapshotReconstructor"/> e confirma com <see cref="SnapshotAck"/>.</summary>
internal sealed class TestClient(InMemoryClient client, ContentCatalog content)
{
    private int _read;

    public InMemoryClient Raw { get; } = client;

    public SnapshotReconstructor State { get; } = new();

    public Welcome? Welcome { get; private set; }

    /// <summary>Mensagens já lidas, em ordem.</summary>
    public List<INetMessage> Messages { get; } = new();

    /// <summary>Deltas aplicados (os ignorados ficam de fora).</summary>
    public List<SnapshotDelta> Applied { get; } = new();

    /// <summary>Bytes recebidos (tudo: snapshots, eventos, acks).</summary>
    public long BytesReceived { get; private set; }

    public bool AutoAck { get; set; } = true;

    public EntityId Me => Welcome!.ControlledEntity;

    public void Send(INetMessage message) => Raw.Send(ProtocolSerializer.Serialize(message));

    public void Sync()
    {
        for (; _read < Raw.Received.Count; _read++)
        {
            var data = Raw.Received[_read].Data;
            BytesReceived += data.Length;
            var msg = ProtocolSerializer.Deserialize(data);
            Messages.Add(msg);
            switch (msg)
            {
                case Welcome w:
                    Welcome = w;
                    State.Reset(w.Epoch);
                    break;
                case SnapshotDelta d when State.Apply(d) is not null:
                    Applied.Add(d);
                    if (AutoAck)
                    {
                        Send(new SnapshotAck(d.Epoch, d.Tick));
                    }

                    break;
            }
        }
    }

    public List<T> All<T>()
        where T : INetMessage => Messages.OfType<T>().ToList();

    public EntitySnapshot? Entity(EntityId id) => State.Latest?.Entities.FirstOrDefault(e => e.Id == id);

    internal ContentCatalog Content => content;
}
