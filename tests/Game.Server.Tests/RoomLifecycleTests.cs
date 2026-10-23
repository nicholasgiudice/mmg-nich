using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;

namespace Game.Server.Tests;

/// <summary>Ciclo da luta na sala: parada sem jogadores, do zero para quem entra, recomeço no fim.</summary>
public class RoomLifecycleTests
{
    [Fact]
    public void Empty_room_does_not_run_even_with_bots()
    {
        var rig = new TestRig(bots: true);
        rig.Tick(200);

        Assert.Equal(0, rig.Room.World.Tick); // nada andou: os bots não lutam sozinhos
        var me = rig.Join("ana", "class.warrior");
        Assert.True(me.Welcome!.Tick <= 1);
        rig.Tick(20, [me]);
        Assert.True(rig.Room.World.Tick >= 20);
    }

    [Fact]
    public void A_player_entering_an_empty_room_starts_a_fresh_fight()
    {
        var rig = new TestRig();
        var a = rig.Join("ana", "class.warrior");
        rig.Tick(100, [a]);
        a.Raw.Leave();
        rig.Tick(50);

        var b = rig.Join("bia", "class.mage");
        Assert.True(b.Welcome!.Tick <= 1, $"devia começar do zero, começou no tick {b.Welcome.Tick}");
        var boss = rig.Room.World.Entities.Single(e => e.TryGet<Game.Core.Combat.UnitInfo>()?.Rank == Game.Core.Definitions.MonsterRank.Boss);
        Assert.Equal(boss.Get<Health>().Max, boss.Get<Health>().Current);
    }

    [Fact]
    public void Victory_shows_the_result_then_restarts_for_who_is_there()
    {
        var rig = new TestRig(options: new ServerOptions { RestartAfterSeconds = 2 });
        var a = rig.Join("ana", "class.archer");
        rig.Tick(10, [a]);
        foreach (var e in rig.Room.World.Entities.Where(e => e.TryGet<FactionTag>()?.Faction == Faction.Monster && e.IsAlive).ToList())
        {
            rig.Room.World.Kill(e);
        }

        rig.Tick(1, [a]);
        Assert.Equal("vitória", rig.Room.Outcome);
        rig.Tick(30, [a]);
        Assert.Equal("vitória", rig.Room.Outcome); // ainda mostrando o fim (2 s = 40 ticks)
        rig.Tick(15, [a]);

        Assert.Null(rig.Room.Outcome);
        Assert.Equal(1, rig.Room.Restarts);
        var welcomes = a.All<Welcome>();
        Assert.Equal(2, welcomes.Count);
        Assert.Equal(welcomes[0].Epoch + 1, welcomes[1].Epoch);
        Assert.Contains(rig.Room.World.Entities, e => e.TryGet<FactionTag>()?.Faction == Faction.Monster && e.IsAlive);
    }

    [Fact]
    public void Defeat_also_restarts_and_zero_keeps_the_room_at_the_end()
    {
        var rig = new TestRig(bots: false, options: new ServerOptions { RestartAfterSeconds = 1 });
        var a = rig.Join("ana", "class.mage");
        foreach (var e in rig.Room.World.Entities.Where(e => e.TryGet<FactionTag>()?.Faction == Faction.Player).ToList())
        {
            rig.Room.World.Kill(e);
        }

        rig.Tick(1, [a]);
        Assert.Equal("derrota", rig.Room.Outcome);
        rig.Tick(25, [a]);
        Assert.Equal(1, rig.Room.Restarts);
        Assert.True(rig.Room.World.Find(a.All<Welcome>()[^1].ControlledEntity)!.IsAlive);

        var stay = new TestRig(bots: false, options: new ServerOptions { RestartAfterSeconds = 0 });
        var b = stay.Join("bia", "class.mage");
        stay.Room.World.Kill(stay.Room.World.Find(b.Me)!);
        foreach (var e in stay.Room.World.Entities.Where(e => e.TryGet<FactionTag>()?.Faction == Faction.Player).ToList())
        {
            stay.Room.World.Kill(e);
        }

        stay.Tick(100, [b]);
        Assert.Equal("derrota", stay.Room.Outcome);
        Assert.Equal(0, stay.Room.Restarts);
    }
}
