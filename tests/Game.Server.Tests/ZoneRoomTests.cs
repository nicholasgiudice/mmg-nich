using System.Numerics;
using Game.Core.Combat;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server;
using Game.Server.Transport;
using Serilog.Core;

namespace Game.Server.Tests;

/// <summary>T-105: sala com clientes falsos no transporte em memória (sem rede real).</summary>
public class ZoneRoomTests
{
    private static readonly Lazy<ContentCatalog> Content = new(() => new ServerOptions().LoadContent());

    private sealed class Rig
    {
        public Rig(string encounter = "encounter.forest_matriarch", bool bots = true, double grace = 30)
        {
            Room = new ZoneRoom(Content.Value, new ServerOptions { EncounterId = encounter, FillWithBots = bots, DisconnectGraceSeconds = grace }, Transport, Logger.None);
            Transport.Start(Room);
        }

        public InMemoryTransport Transport { get; } = new();

        public ZoneRoom Room { get; }

        /// <summary>Um tick do servidor: entrega o que chegou e avança.</summary>
        public void Tick(int n = 1)
        {
            for (var i = 0; i < n; i++)
            {
                Transport.Poll();
                Room.Step();
            }
        }

        public (InMemoryClient Client, Welcome Welcome) Join(string name, string? cls)
        {
            var client = Transport.Connect();
            client.Send(ProtocolSerializer.Serialize(new Hello(ProtocolVersion.Current, Content.Value.Fingerprint, name, cls)));
            Tick();
            return (client, Messages<Welcome>(client).Single());
        }
    }

    private static List<T> Messages<T>(InMemoryClient c)
        where T : INetMessage =>
        c.Received.Select(r => ProtocolSerializer.Deserialize(r.Data)).OfType<T>().ToList();

    private static Vector2 PositionIn(SnapshotMessage s, EntityId id) =>
        s.Entities.Single(e => e.Id == id).Get<Transform>().Position;

    [Fact]
    public void Two_players_join_with_their_classes_and_the_other_slots_play_as_bots()
    {
        var rig = new Rig();
        var (_, warrior) = rig.Join("ana", "class.warrior");
        var (_, mage) = rig.Join("bia", "class.mage");

        Assert.NotEqual(warrior.ControlledEntity, mage.ControlledEntity);
        Assert.Equal("encounter.forest_matriarch", warrior.EncounterId);
        Assert.Equal(Content.Value.Encounters["encounter.forest_matriarch"].Arena, warrior.ArenaId);
        Assert.Equal("class.warrior", rig.Room.World.Find(warrior.ControlledEntity)!.Get<UnitInfo>().DefinitionId);
        Assert.Equal("class.mage", rig.Room.World.Find(mage.ControlledEntity)!.Get<UnitInfo>().DefinitionId);
        Assert.Equal(2, rig.Room.PlayerCount);
        Assert.Equal(2, rig.Room.Slots.Count(s => s.Bot));
    }

    [Fact]
    public void Intents_move_only_the_own_entity_and_are_acknowledged()
    {
        var rig = new Rig(bots: false);
        var (a, wa) = rig.Join("ana", "class.warrior");
        var (b, wb) = rig.Join("bia", "class.mage");
        rig.Tick();
        var startA = rig.Room.World.Find(wa.ControlledEntity)!.Get<Transform>().Position;
        var startB = rig.Room.World.Find(wb.ControlledEntity)!.Get<Transform>().Position;

        a.Send(ProtocolSerializer.Serialize(new IntentMessage(1, rig.Room.World.Tick, NetIntent.From(new Core.Intents.MoveIntent(wb.ControlledEntity, new Vector2(0, -1))))));
        rig.Tick(10);
        a.Send(ProtocolSerializer.Serialize(new IntentMessage(2, rig.Room.World.Tick, NetIntent.From(new Core.Intents.StopIntent(wb.ControlledEntity)))));
        rig.Tick(2);

        // A intenção dizia "entidade de bia", mas o servidor usa sempre a entidade da conexão de ana.
        var lastA = Messages<SnapshotMessage>(a).Last();
        Assert.True(PositionIn(lastA, wa.ControlledEntity).Y < startA.Y - 1, "ana devia ter andado para cima");
        Assert.Equal(startB, PositionIn(lastA, wb.ControlledEntity));
        Assert.Equal(2u, Messages<IntentAck>(a).Last().LastSequence);
        Assert.Empty(Messages<IntentAck>(b));

        // Os dois recebem o mesmo estado a cada tick, em ordem.
        var ticksA = Messages<SnapshotMessage>(a).Select(s => s.Tick).ToList();
        Assert.Equal(ticksA.OrderBy(t => t), ticksA);
        Assert.Equal(PositionIn(lastA, wa.ControlledEntity), PositionIn(Messages<SnapshotMessage>(b).Last(), wa.ControlledEntity));
        Assert.All(a.Received.Where(r => ProtocolSerializer.Deserialize(r.Data) is SnapshotMessage), r => Assert.Equal(SendMode.Snapshot, r.Mode));
        Assert.All(a.Received.Where(r => ProtocolSerializer.Deserialize(r.Data) is EventsMessage), r => Assert.Equal(SendMode.Reliable, r.Mode));
    }

    [Fact]
    public void Events_of_each_tick_reach_the_players()
    {
        var rig = new Rig();
        var (a, _) = rig.Join("ana", "class.archer");
        rig.Tick(20 * 20); // bots lutam

        var events = Messages<EventsMessage>(a);
        Assert.NotEmpty(events);
        Assert.Contains(events.SelectMany(e => e.Events), e => e is DamageDealt);
        Assert.All(events, m => Assert.All(m.Events, e => Assert.Equal(m.Tick, e.Tick)));
    }

    [Fact]
    public void Wrong_version_or_content_or_full_room_is_rejected_and_closed()
    {
        var rig = new Rig("encounter.three_wolves");
        var old = rig.Transport.Connect();
        old.Send(ProtocolSerializer.Serialize(new Hello(ProtocolVersion.Current + 1, Content.Value.Fingerprint, "x", null)));
        var other = rig.Transport.Connect();
        other.Send(ProtocolSerializer.Serialize(new Hello(ProtocolVersion.Current, "outro-conteudo", "y", null)));
        rig.Tick();
        rig.Join("ana", null);
        var late = rig.Transport.Connect();
        late.Send(ProtocolSerializer.Serialize(new Hello(ProtocolVersion.Current, Content.Value.Fingerprint, "z", null)));
        rig.Tick(15);

        Assert.Equal(RejectReason.ProtocolVersion, Messages<Reject>(old).Single().Reason);
        Assert.Equal(RejectReason.Content, Messages<Reject>(other).Single().Reason);
        Assert.Equal(RejectReason.Full, Messages<Reject>(late).Single().Reason);
        Assert.All([old, other, late], c => Assert.True(c.Closed));
        Assert.Empty(Messages<SnapshotMessage>(late));
    }

    [Fact]
    public void A_class_not_in_the_encounter_replaces_the_slot_class()
    {
        var rig = new Rig("encounter.three_wolves");
        var (_, w) = rig.Join("ana", "class.cleric");
        Assert.Equal("class.cleric", rig.Room.World.Find(w.ControlledEntity)!.Get<UnitInfo>().DefinitionId);
        Assert.Single(rig.Room.World.Entities, e => e.TryGet<FactionTag>()?.Faction == Faction.Player);
    }

    [Fact]
    public void Disconnected_entity_waits_for_the_player_then_is_removed()
    {
        var rig = new Rig(bots: false, grace: 2);
        var (a, wa) = rig.Join("ana", "class.warrior");
        var (b, _) = rig.Join("bia", "class.mage");

        a.Leave();
        rig.Tick(10);
        Assert.NotNull(rig.Room.World.Find(wa.ControlledEntity)); // ainda dentro do prazo

        var (_, again) = rig.Join("ana", "class.warrior");
        Assert.Equal(wa.ControlledEntity, again.ControlledEntity); // voltou para a mesma entidade

        b.Leave();
        rig.Tick(2 * 20 + 2);
        Assert.Contains(rig.Room.Slots, s => s.Class == "class.mage" && s.Entity is null);
        Assert.Equal(0, rig.Room.Restarts); // ana ainda está na sala
    }

    [Fact]
    public void When_everybody_leaves_the_room_restarts_and_console_restart_sends_a_new_welcome()
    {
        var rig = new Rig();
        var (a, _) = rig.Join("ana", "class.warrior");
        rig.Tick(30);
        rig.Room.Restart("teste");
        rig.Tick();

        var welcomes = Messages<Welcome>(a);
        Assert.Equal(2, welcomes.Count);
        Assert.True(welcomes[1].Tick < 5, "o mundo novo começa do zero");
        Assert.Equal("class.warrior", rig.Room.World.Find(welcomes[1].ControlledEntity)!.Get<UnitInfo>().DefinitionId);

        a.Leave();
        rig.Tick();
        Assert.Equal(2, rig.Room.Restarts);
        Assert.Equal(0, rig.Room.PlayerCount);
    }

    [Fact]
    public void Ping_gets_a_pong_with_the_server_tick()
    {
        var rig = new Rig();
        var (a, _) = rig.Join("ana", null);
        rig.Tick(5);
        a.Send(ProtocolSerializer.Serialize(new Ping(987)));
        rig.Tick();
        var pong = Messages<Pong>(a).Single();
        Assert.Equal(987, pong.ClientTimeMs);
        Assert.InRange(pong.ServerTick, 5, 7);
    }

    [Fact]
    public void Intent_flood_is_capped_per_tick_and_garbage_is_ignored()
    {
        var rig = new Rig(bots: false);
        var (a, _) = rig.Join("ana", "class.warrior");
        a.Send([1, 2, 3, 250]);
        for (uint i = 1; i <= 100; i++)
        {
            a.Send(ProtocolSerializer.Serialize(new IntentMessage(i, 0, NetIntent.From(new Core.Intents.StopIntent(default)))));
        }

        rig.Tick();
        Assert.Equal((uint)ZoneRoom.MaxIntentsPerTick, Messages<IntentAck>(a).Last().LastSequence);
        Assert.False(a.Closed);
    }
}
