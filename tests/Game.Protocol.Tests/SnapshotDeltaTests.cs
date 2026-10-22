using System.Numerics;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;

namespace Game.Protocol.Tests;

/// <summary>T-106: formato do <see cref="SnapshotDelta"/> e aplicação no <see cref="SnapshotReconstructor"/>.</summary>
public class SnapshotDeltaTests
{
    private static readonly EntityId A = new(1);
    private static readonly EntityId B = new(2);

    private static Transform At(float x) => new() { Position = new Vector2(x, 1) };

    private static SnapshotDelta Full(long tick, params (EntityId Id, IComponent[] Components)[] entities) =>
        new(1, tick, 0, entities.Select(e => new EntityDelta(e.Id, true, e.Components, [])).ToArray(), []);

    private static float X(WorldSnapshot s, EntityId id) => s.Entities.Single(e => e.Id == id).Get<Transform>().Position.X;

    [Fact]
    public void Delta_round_trips_with_compact_entities()
    {
        var delta = new SnapshotDelta(4, 120, 117,
            [new EntityDelta(new EntityId(300), true, [At(2.5f), new Health(50) { Current = 20 }], []), new EntityDelta(A, false, [At(3)], [2, 9])],
            [B, new EntityId(77)]);

        var bytes = ProtocolSerializer.Serialize(delta);
        var back = Assert.IsType<SnapshotDelta>(ProtocolSerializer.Deserialize(bytes));

        Assert.Equal((4, 120L, 117L), (back.Epoch, back.Tick, back.BaseTick));
        Assert.Equal([B, new EntityId(77)], back.Removed);
        Assert.Equal([new EntityId(300), A], back.Entities.Select(e => e.Id));
        Assert.Equal([true, false], back.Entities.Select(e => e.Full));
        Assert.Equal([2, 9], back.Entities[1].RemovedComponents);
        Assert.Equal(20, Assert.IsType<Health>(back.Entities[0].Components[1]).Current);
        Assert.Equal(bytes, ProtocolSerializer.Serialize(back));
    }

    [Fact]
    public void Deltas_build_on_the_confirmed_base_and_entities_enter_and_leave()
    {
        var r = new SnapshotReconstructor();
        r.Reset(1);

        var s1 = r.Apply(Full(1, (A, [At(1), new Health(10)]), (B, [At(5)])))!;
        Assert.Equal([A, B], s1.Entities.Select(e => e.Id));

        // Tick 2 sobre a base 1: A anda; B sai; C entra.
        var c = new EntityId(3);
        var s2 = r.Apply(new SnapshotDelta(1, 2, 1, [new EntityDelta(A, false, [At(2)], []), new EntityDelta(c, true, [At(9)], [])], [B]))!;
        Assert.Equal([A, c], s2.Entities.Select(e => e.Id));
        Assert.Equal(2, X(s2, A));
        Assert.NotNull(s2.Entities.Single(e => e.Id == A).TryGet<Health>()); // componente que não mudou continua

        // O tick 3 se perdeu; o 4 ainda vem sobre a base 1 (a última confirmada) e traz tudo de novo.
        var s4 = r.Apply(new SnapshotDelta(1, 4, 1, [new EntityDelta(A, false, [At(4)], [2]), new EntityDelta(c, true, [At(9)], [])], [B]))!;
        Assert.Equal(4, X(s4, A));
        Assert.Null(s4.Entities.Single(e => e.Id == A).TryGet<Health>()); // tag 2 = Health removido
    }

    [Fact]
    public void Old_wrong_epoch_or_unknown_base_deltas_are_ignored()
    {
        var r = new SnapshotReconstructor();
        r.Reset(2);
        Assert.Null(r.Apply(Full(1, (A, [At(1)])) with { Epoch = 1 }));
        Assert.NotNull(r.Apply(Full(5, (A, [At(1)])) with { Epoch = 2 }));
        Assert.Null(r.Apply(Full(5, (A, [At(7)])) with { Epoch = 2 })); // repetido
        Assert.Null(r.Apply(new SnapshotDelta(2, 6, 4, [], []))); // base 4 nunca chegou
        Assert.Equal(5, r.Latest!.Tick);

        // Completo substitui tudo.
        var s = r.Apply(Full(8, (B, [At(3)])) with { Epoch = 2 })!;
        Assert.Equal([B], s.Entities.Select(e => e.Id));
    }

    [Fact]
    public void Only_recent_states_are_kept()
    {
        var r = new SnapshotReconstructor();
        r.Reset(1);
        r.Apply(Full(1, (A, [At(1)])));
        for (long t = 2; t <= SnapshotReconstructor.History + 5; t++)
        {
            Assert.NotNull(r.Apply(new SnapshotDelta(1, t, t - 1, [new EntityDelta(A, false, [At(t)], [])], [])));
        }

        Assert.Null(r.Apply(new SnapshotDelta(1, 200, 1, [], []))); // base 1 já foi descartada
    }
}
