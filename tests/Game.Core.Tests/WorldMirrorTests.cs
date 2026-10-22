using System.Numerics;
using Game.Core.Combat;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-109: <see cref="World.Mirror"/> copia ids, componentes e tick de um snapshot.</summary>
public class WorldMirrorTests
{
    [Fact]
    public void Mirror_reproduces_ids_components_and_tick_without_events()
    {
        var source = WorldBuilder.CreateDefault(1, null, ContentCatalog.Empty);
        var a = source.Spawn(new Transform { Position = new Vector2(1, 2) }, new Health(50), new FactionTag(Faction.Player));
        source.Spawn(new Transform(), new Health(10), new FactionTag(Faction.Monster));
        var c = source.Spawn(new Transform { Position = new Vector2(7, 7) }, new Health(30), new FactionTag(Faction.Monster));
        source.Remove(source.Entities[1].Id); // buraco nos ids
        for (var i = 0; i < 12; i++)
        {
            source.Step();
        }

        var mirror = WorldBuilder.CreateDefault(2, null, ContentCatalog.Empty);
        var old = mirror.Spawn(new Transform());
        var snap = source.Snapshot(SnapshotScope.Replicated);
        var eventsBefore = mirror.Events.Current.Count;
        mirror.Mirror(snap);

        Assert.True(old.IsRemoved);
        Assert.Equal(12, mirror.Tick);
        Assert.Equal([a.Id, c.Id], mirror.Entities.Select(e => e.Id));
        Assert.Equal(new Vector2(7, 7), mirror.Find(c.Id)!.Get<Transform>().Position);
        Assert.Equal(eventsBefore, mirror.Events.Current.Count); // o espelho não publica nada

        // O espelho é uma cópia: mexer nele não muda o snapshot.
        mirror.Find(a.Id)!.Get<Transform>().Position = Vector2.Zero;
        Assert.Equal(new Vector2(1, 2), snap.Entities[0].Get<Transform>().Position);

        // Entidade nova no espelho não colide com as ids copiadas.
        Assert.True(mirror.Spawn(new Transform()).Id.Value > c.Id.Value);
    }
}
