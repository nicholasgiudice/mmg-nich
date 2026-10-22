using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Transport;
using Xunit.Abstractions;

namespace Game.Server.Tests;

/// <summary>T-106: interesse por grade, deltas por cliente com perda, eventos filtrados e banda por jogador.</summary>
public class InterestDeltaTests(ITestOutputHelper output)
{
    /// <summary>Corredor de 220 tiles com um boneco a cada 6 tiles (y = 4) e o Guerreiro na ponta esquerda (y = 10).</summary>
    private static TestRig Corridor(out List<(float X, float Y)> dummies)
    {
        dummies = Enumerable.Range(0, 35).Select(i => (10f + (i * 6), 4f)).ToList();
        var content = TestRig.Synthetic(220, 20, (3, 10), dummies.Select(d => ("monster.test_dummy", d.X, d.Y)));
        return new TestRig("encounter.test_map", bots: false, content: content);
    }

    /// <summary>
    /// Estado esperado do jogador agora: snapshot replicado só com as entidades que ele vê, em bytes por componente;
    /// <c>Movement</c> só na entidade dele (T-106).
    /// </summary>
    private static Dictionary<EntityId, Dictionary<Type, byte[]>> Expected(TestRig rig, TestClient client)
    {
        var visible = rig.Room.VisibleToPeer(client.Raw.Peer);
        return rig.Room.World.Snapshot(SnapshotScope.Replicated).Entities
            .Where(e => visible.Contains(e.Id))
            .ToDictionary(
                e => e.Id,
                e => e.Components
                    .Where(c => e.Id == client.Me || c is not Movement)
                    .ToDictionary(c => c.GetType(), ProtocolSerializer.SerializeComponent));
    }

    private static void AssertSameState(Dictionary<EntityId, Dictionary<Type, byte[]>> expected, WorldSnapshot actual, string when)
    {
        Assert.True(expected.Keys.ToHashSet().SetEquals(actual.Entities.Select(e => e.Id)),
            $"{when}: entidades esperadas {string.Join(",", expected.Keys.Order())} vs {string.Join(",", actual.Entities.Select(e => e.Id))}");
        foreach (var e in actual.Entities)
        {
            var exp = expected[e.Id];
            Assert.Equal(exp.Keys.OrderBy(t => t.Name), e.Components.Select(c => c.GetType()).OrderBy(t => t.Name));
            foreach (var c in e.Components)
            {
                Assert.True(exp[c.GetType()].AsSpan().SequenceEqual(ProtocolSerializer.SerializeComponent(c)), $"{when}: {e.Id} {c.GetType().Name} diferente");
            }
        }
    }

    [Fact]
    public void Walking_down_a_long_map_receives_and_removes_entities_as_they_enter_and_leave_interest()
    {
        var rig = Corridor(out var dummies);
        var me = rig.Join("ana", "class.warrior");
        me.Send(new IntentMessage(1, 0, NetIntent.From(new MoveIntent(default, new Vector2(1, 0)))));

        var seen = new HashSet<EntityId>();
        var removedSeen = 0;
        var checkedTicks = 0;
        for (var t = 0; t < 1100; t++)
        {
            rig.Tick(1, [me]);
            if (me.State.Latest is not { } latest || latest.Tick != rig.Room.World.Tick)
            {
                continue;
            }

            var visible = rig.Room.VisibleToPeer(me.Raw.Peer);
            Assert.True(visible.SetEquals(latest.Entities.Select(e => e.Id)), $"tick {latest.Tick}: interesse diferente do que o cliente tem");
            seen.UnionWith(latest.Entities.Select(e => e.Id));
            removedSeen += me.Applied[^1].Removed.Length;
            checkedTicks++;
        }

        var x = me.Entity(me.Me)!.Get<Transform>().Position.X;
        Assert.True(x > 200, $"o Guerreiro devia ter chegado ao fim (x = {x})");
        Assert.Equal(dummies.Count + 1, seen.Count); // viu todos os bonecos em algum momento
        Assert.True(removedSeen >= 25, $"bonecos que ficaram para trás deviam ter saído ({removedSeen})");
        Assert.All(me.State.Latest!.Entities.Where(e => e.Id != me.Me),
            e => Assert.True(Math.Abs(e.Get<Transform>().Position.X - x) <= 3 * 16 + 1, "só entidades perto do jogador"));
        Assert.True(me.State.Latest.Entities.Count < 20, "nunca o mapa inteiro");
        Assert.True(checkedTicks > 1000);
    }

    [Fact]
    public void Applying_the_deltas_with_packet_loss_reproduces_the_full_state()
    {
        var rig = new TestRig(); // Matriarca com bots: tudo se mexe
        var lossy = rig.Join("ana", "class.archer");
        var clean = rig.Join("bia", "class.cleric");
        var rng = new Random(11);
        rig.Transport.Drop = (peer, mode) => peer == lossy.Raw.Peer && mode == SendMode.Snapshot && rng.NextDouble() < 0.3;
        lossy.AutoAck = false;

        var compared = 0;
        for (var t = 0; t < 40 * 20; t++)
        {
            rig.Tick(1, [clean]);
            lossy.Sync();
            if (lossy.State.Latest is { } latest && latest.Tick == rig.Room.World.Tick)
            {
                AssertSameState(Expected(rig, lossy), latest, $"perda, tick {latest.Tick}");
                compared++;
                if (rng.NextDouble() < 0.5)
                {
                    lossy.Send(new SnapshotAck(lossy.State.Epoch, latest.Tick)); // confirmações também se perdem
                }
            }

            if (clean.State.Latest is { } c && c.Tick == rig.Room.World.Tick)
            {
                AssertSameState(Expected(rig, clean), c, $"sem perda, tick {c.Tick}");
            }
        }

        var deltas = lossy.Applied.Count(d => !d.IsFull);
        output.WriteLine($"com perda: {compared} estados comparados, {deltas} deltas e {lossy.Applied.Count - deltas} completos aplicados");
        Assert.True(compared > 400);
        Assert.True(deltas > compared / 2, "a maioria devia ser delta");
    }

    [Fact]
    public void Without_acks_every_snapshot_is_full_and_with_acks_they_become_small_deltas()
    {
        var rig = new TestRig();
        var silent = rig.Join("ana", "class.warrior");
        var acking = rig.Join("bia", "class.mage");
        silent.AutoAck = false;
        rig.Tick(200, [silent, acking]);

        // O Join confirmou só o 1º delta: a base fica nele até passar de MaxDeltaAgeTicks (40); depois, tudo completo.
        var first = silent.Applied[0].Tick;
        Assert.All(silent.Applied.Where(d => d.Tick > first + 40), d => Assert.True(d.IsFull));
        Assert.All(silent.Applied.Where(d => !d.IsFull), d => Assert.Equal(first, d.BaseTick));
        var last = acking.Applied.TakeLast(100).ToList();
        Assert.True(last.Count(d => !d.IsFull) >= 95);
        var bytesSilent = rig.Room.SnapshotBytesByPeer[silent.Raw.Peer];
        var bytesAcking = rig.Room.SnapshotBytesByPeer[acking.Raw.Peer];
        output.WriteLine($"200 ticks: sem confirmação {bytesSilent} B, com confirmação {bytesAcking} B");
        Assert.True(bytesAcking < bytesSilent / 2);
    }

    [Fact]
    public void Events_of_entities_out_of_interest_are_not_sent()
    {
        var rig = Corridor(out var dummies);
        var me = rig.Join("ana", "class.warrior");
        rig.Tick(2, [me]);
        var world = rig.Room.World;
        var monsters = world.Entities.Where(e => e.TryGet<FactionTag>()?.Faction == Faction.Monster).OrderBy(e => e.Get<Transform>().Position.X).ToList();
        var near = monsters[0];
        var far = monsters[^1];

        DamageResolver.Deal(world, far, far, "test.far", 5, DamageSchool.Physical);
        DamageResolver.Deal(world, near, near, "test.near", 5, DamageSchool.Physical);
        rig.Tick(1, [me]);

        var damage = me.All<EventsMessage>().SelectMany(m => m.Events).OfType<DamageDealt>().ToList();
        Assert.Contains(damage, d => d.SourceId == "test.near");
        Assert.DoesNotContain(damage, d => d.SourceId == "test.far");
    }

    // ---------- medição de banda (Resultado da T-106) ----------

    [Fact]
    public void Bandwidth_per_player_in_the_matriarch_fight()
    {
        var rig = new TestRig();
        var me = rig.Join("ana", "class.mage"); // + 3 bots lutando
        const int seconds = 60;
        rig.Tick(seconds * 20, [me]);

        var snapshotBytes = rig.Room.SnapshotBytesByPeer[me.Raw.Peer];
        var fullSize = ProtocolSerializer.Serialize(SnapshotMessage.From(rig.Room.World.Snapshot(SnapshotScope.Replicated))).Length;
        output.WriteLine($"Matriarca, {seconds} s: tudo {me.BytesReceived / seconds / 1024.0:0.00} KB/s por jogador; snapshots {snapshotBytes / seconds / 1024.0:0.00} KB/s "
            + $"(snapshot completo agora: {fullSize} B = {fullSize * 20 / 1024.0:0.0} KB/s se fosse mandado a cada tick)");
        Assert.True(me.BytesReceived / seconds < 8 * 1024, "acima de 8 KB/s");
    }

    [Fact]
    public void Bandwidth_per_player_with_100_spread_entities()
    {
        var rng = new Random(5);
        var rats = Enumerable.Range(0, 100).Select(_ => ("monster.field_rat", (float)(5 + (rng.NextDouble() * 190)), (float)(5 + (rng.NextDouble() * 190)))).ToList();
        var content = TestRig.Synthetic(200, 200, (100, 100), rats);
        var rig = new TestRig("encounter.test_map", bots: false, content: content);
        var me = rig.Join("ana", "class.warrior");
        me.Send(new IntentMessage(1, 0, NetIntent.From(new MoveIntent(default, Vector2.Normalize(new Vector2(1, 0.4f))))));
        const int seconds = 30;
        var maxVisible = 0;
        for (var t = 0; t < seconds * 20; t++)
        {
            rig.Tick(1, [me]);
            maxVisible = Math.Max(maxVisible, me.State.Latest?.Entities.Count ?? 0);
        }

        var full = ProtocolSerializer.Serialize(SnapshotMessage.From(rig.Room.World.Snapshot(SnapshotScope.Replicated))).Length;
        output.WriteLine($"100 ratos em 200×200, {seconds} s: tudo {me.BytesReceived / seconds / 1024.0:0.00} KB/s; "
            + $"no máximo {maxVisible} entidades visíveis; snapshot completo do mapa: {full} B = {full * 20 / 1024.0:0} KB/s a cada tick");

        // Onde vai a banda (Resultado da T-106).
        var byType = me.Applied.SelectMany(d => d.Entities).SelectMany(e => e.Components)
            .GroupBy(c => c.GetType().Name)
            .Select(g => (g.Key, Bytes: g.Sum(c => (long)ProtocolSerializer.SerializeComponent(c).Length)))
            .OrderByDescending(x => x.Bytes);
        output.WriteLine("componentes: " + string.Join(", ", byType.Select(x => $"{x.Key} {x.Bytes / seconds / 1024.0:0.0} KB/s")));
        output.WriteLine($"eventos: {me.All<EventsMessage>().Sum(m => ProtocolSerializer.Serialize(m).Length) / seconds / 1024.0:0.0} KB/s");

        // Pior caso de propósito: os ratos perseguem o jogador e se juntam em volta dele. O interesse ainda corta o
        // mapa e o delta fica muito abaixo do snapshot completo a cada tick.
        Assert.True(maxVisible < rats.Count, "o interesse devia cortar parte do mapa");
        Assert.True(me.BytesReceived / seconds < full * 20 / 10, "o delta devia ser bem menor que o completo a cada tick");
    }
}

