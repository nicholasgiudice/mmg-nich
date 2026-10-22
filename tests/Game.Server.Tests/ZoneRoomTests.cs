using System.Numerics;
using Game.Core.Combat;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Transport;

namespace Game.Server.Tests;

/// <summary>T-105: sala com clientes falsos no transporte em memória (sem rede real).</summary>
public class ZoneRoomTests
{
    private static Vector2 Pos(TestClient c, EntityId id) => c.Entity(id)!.Get<Transform>().Position;

    [Fact]
    public void Two_players_join_with_their_classes_and_the_other_slots_play_as_bots()
    {
        var rig = new TestRig();
        var warrior = rig.Join("ana", "class.warrior").Welcome!;
        var mage = rig.Join("bia", "class.mage").Welcome!;

        Assert.NotEqual(warrior.ControlledEntity, mage.ControlledEntity);
        Assert.Equal("encounter.forest_matriarch", warrior.EncounterId);
        Assert.Equal(rig.Content.Encounters["encounter.forest_matriarch"].Arena, warrior.ArenaId);
        Assert.Equal("class.warrior", rig.Room.World.Find(warrior.ControlledEntity)!.Get<UnitInfo>().DefinitionId);
        Assert.Equal("class.mage", rig.Room.World.Find(mage.ControlledEntity)!.Get<UnitInfo>().DefinitionId);
        Assert.Equal(2, rig.Room.PlayerCount);
        Assert.Equal(2, rig.Room.Slots.Count(s => s.Bot));
    }

    [Fact]
    public void Intents_move_only_the_own_entity_and_are_acknowledged()
    {
        var rig = new TestRig(bots: false);
        var a = rig.Join("ana", "class.warrior");
        var b = rig.Join("bia", "class.mage");
        var both = new[] { a, b };
        rig.Tick(2, both);
        var startA = Pos(a, a.Me);
        var startB = Pos(a, b.Me);

        a.Send(new IntentMessage(1, rig.Room.World.Tick, NetIntent.From(new MoveIntent(b.Me, new Vector2(0, -1)))));
        rig.Tick(10, both);
        a.Send(new IntentMessage(2, rig.Room.World.Tick, NetIntent.From(new StopIntent(b.Me))));
        rig.Tick(3, both);

        // A intenção dizia "entidade de bia", mas o servidor usa sempre a entidade da conexão de ana.
        Assert.True(Pos(a, a.Me).Y < startA.Y - 1, "ana devia ter andado para cima");
        Assert.Equal(startB, Pos(a, b.Me));
        Assert.Equal(2u, a.All<IntentAck>().Last().LastSequence);
        Assert.Empty(b.All<IntentAck>());

        // Os dois veem o mesmo estado, e os ticks chegam em ordem.
        Assert.Equal(Pos(a, a.Me), Pos(b, a.Me));
        var ticks = a.Applied.Select(d => d.Tick).ToList();
        Assert.Equal(ticks.OrderBy(t => t), ticks);
        Assert.All(a.Raw.Received.Where(r => ProtocolSerializer.Deserialize(r.Data) is SnapshotDelta), r => Assert.Equal(SendMode.Snapshot, r.Mode));
        Assert.All(a.Raw.Received.Where(r => ProtocolSerializer.Deserialize(r.Data) is EventsMessage), r => Assert.Equal(SendMode.Reliable, r.Mode));
    }

    [Fact]
    public void Events_of_each_tick_reach_the_players()
    {
        var rig = new TestRig();
        var a = rig.Join("ana", "class.archer");
        rig.Tick(20 * 20, [a]); // bots lutam

        var events = a.All<EventsMessage>();
        Assert.NotEmpty(events);
        Assert.Contains(events.SelectMany(e => e.Events), e => e is DamageDealt);
        Assert.All(events, m => Assert.All(m.Events, e => Assert.Equal(m.Tick, e.Tick)));
    }

    [Fact]
    public void Wrong_version_or_content_or_full_room_is_rejected_and_closed()
    {
        var rig = new TestRig("encounter.three_wolves");
        var old = new TestClient(rig.Transport.Connect(), rig.Content);
        old.Send(new Hello(ProtocolVersion.Current + 1, rig.Content.Fingerprint, "x", null));
        var other = new TestClient(rig.Transport.Connect(), rig.Content);
        other.Send(new Hello(ProtocolVersion.Current, "outro-conteudo", "y", null));
        rig.Tick();
        rig.Join("ana", null);
        var late = new TestClient(rig.Transport.Connect(), rig.Content);
        late.Send(new Hello(ProtocolVersion.Current, rig.Content.Fingerprint, "z", null));
        rig.Tick(15, [old, other, late]);

        Assert.Equal(RejectReason.ProtocolVersion, old.All<Reject>().Single().Reason);
        Assert.Equal(RejectReason.Content, other.All<Reject>().Single().Reason);
        Assert.Equal(RejectReason.Full, late.All<Reject>().Single().Reason);
        Assert.All([old, other, late], c => Assert.True(c.Raw.Closed));
        Assert.Empty(late.All<SnapshotDelta>());
    }

    [Fact]
    public void A_class_not_in_the_encounter_replaces_the_slot_class()
    {
        var rig = new TestRig("encounter.three_wolves");
        var me = rig.Join("ana", "class.cleric").Me;
        Assert.Equal("class.cleric", rig.Room.World.Find(me)!.Get<UnitInfo>().DefinitionId);
        Assert.Single(rig.Room.World.Entities, e => e.TryGet<FactionTag>()?.Faction == Faction.Player);
    }

    [Fact]
    public void Disconnected_entity_waits_for_the_player_then_is_removed()
    {
        var rig = new TestRig(bots: false, grace: 2);
        var a = rig.Join("ana", "class.warrior");
        var b = rig.Join("bia", "class.mage");

        a.Raw.Leave();
        rig.Tick(10);
        Assert.NotNull(rig.Room.World.Find(a.Me)); // ainda dentro do prazo

        var again = rig.Join("ana", "class.warrior");
        Assert.Equal(a.Me, again.Me); // voltou para a mesma entidade

        b.Raw.Leave();
        rig.Tick(2 * 20 + 2);
        Assert.Contains(rig.Room.Slots, s => s.Class == "class.mage" && s.Entity is null);
        Assert.Equal(0, rig.Room.Restarts); // ana ainda está na sala
    }

    [Fact]
    public void When_everybody_leaves_the_room_restarts_and_console_restart_sends_a_new_welcome_and_epoch()
    {
        var rig = new TestRig();
        var a = rig.Join("ana", "class.warrior");
        rig.Tick(30, [a]);
        rig.Room.Restart("teste");
        rig.Tick(2, [a]);

        var welcomes = a.All<Welcome>();
        Assert.Equal(2, welcomes.Count);
        Assert.Equal(welcomes[0].Epoch + 1, welcomes[1].Epoch);
        Assert.True(welcomes[1].Tick < 5, "o mundo novo começa do zero");
        Assert.Equal("class.warrior", rig.Room.World.Find(welcomes[1].ControlledEntity)!.Get<UnitInfo>().DefinitionId);

        // O primeiro delta da época nova é completo; os da época velha que chegarem depois são ignorados.
        var firstNew = a.All<SnapshotDelta>().First(d => d.Epoch == welcomes[1].Epoch);
        Assert.True(firstNew.IsFull);
        Assert.Equal(welcomes[1].Epoch, a.State.Epoch);
        Assert.Null(a.State.Apply(firstNew with { Epoch = welcomes[0].Epoch, Tick = 999 }));

        a.Raw.Leave();
        rig.Tick();
        Assert.Equal(2, rig.Room.Restarts);
        Assert.Equal(0, rig.Room.PlayerCount);
    }

    [Fact]
    public void Ping_gets_a_pong_with_the_server_tick()
    {
        var rig = new TestRig();
        var a = rig.Join("ana", null);
        rig.Tick(5);
        a.Send(new Ping(987));
        rig.Tick(1, [a]);
        var pong = a.All<Pong>().Single();
        Assert.Equal(987, pong.ClientTimeMs);
        Assert.InRange(pong.ServerTick, 5, 7);
    }

    [Fact]
    public void Intent_flood_is_capped_per_tick_and_garbage_is_ignored()
    {
        var rig = new TestRig(bots: false);
        var a = rig.Join("ana", "class.warrior");
        a.Raw.Send([1, 2, 3, 250]);
        for (uint i = 1; i <= 100; i++)
        {
            a.Send(new IntentMessage(i, 0, NetIntent.From(new StopIntent(default))));
        }

        rig.Tick(1, [a]);
        Assert.Equal((uint)ZoneRoom.MaxIntentsPerTick, a.All<IntentAck>().Last().LastSequence);
        Assert.False(a.Raw.Closed);
    }
}
