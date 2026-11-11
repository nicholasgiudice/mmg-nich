using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Objectives;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Zones;

namespace Game.Server.Tests;

/// <summary>
/// T-352: a Cripta do Vigia com o conteúdo real. Da Cidade, pelo portão oeste, até as Ruínas e, pela escada da entrada da
/// Cripta, até a instância do grupo. Os selos da Sala do Vigia (pontos <c>objective</c> do mapa) nascem como objetivos; se
/// os três caem, o encontro falha: todos saem para a entrada nas Ruínas e a cópia acaba.
/// </summary>
public class CryptInstanceTests
{
    private const string Crypt = "zone.watcher_crypt";

    private static WorldRig Rig() => new(null, devLogin: true, TestRig.RealContent.Value, new ServerOptions());

    private static Vector2 GateTo(OpenZone zone, string to)
    {
        var a = zone.Definition.Gates.First(g => g.To == to).Area;
        return new Vector2((float)(a.X + (a.Width / 2)), (float)(a.Y + (a.Height / 2)));
    }

    /// <summary>Põe o personagem no portão da zona atual para <paramref name="to"/> e roda um tick.</summary>
    private static void Through(WorldRig w, TestClient c, string to, TestClient[] all)
    {
        w.Where(c).Entity.Get<Transform>().Position = GateTo(w.Where(c).Zone, to);
        w.Tick(1, all);
        Assert.Equal(to, w.Where(c).Zone.Id);
    }

    private static void IntoCrypt(WorldRig w, TestClient c, TestClient[] all)
    {
        Through(w, c, "zone.old_road_ruins", all);
        Through(w, c, Crypt, all);
    }

    private static List<Entity> Seals(OpenZone zone) => zone.World.Entities.Where(e => e.IsAlive && ObjectiveService.IsObjective(e)).ToList();

    private static TestClient[] GroupOfTwo(WorldRig w)
    {
        var a = w.Join("aria");
        var b = w.Join("bruno");
        TestClient[] all = [a, b];
        a.Send(new PartyInvite(null, "bruno"));
        w.Tick(1, all);
        b.Send(new PartyRespond(b.All<PartyInviteReceived>().Last().InviteId, true));
        w.Tick(1, all);
        return all;
    }

    [Fact]
    public void The_party_walks_from_the_city_into_the_same_crypt_copy_with_the_watcher_and_three_seals()
    {
        using var w = Rig();
        var all = GroupOfTwo(w);
        IntoCrypt(w, all[0], all);
        IntoCrypt(w, all[1], all);

        var copy = Assert.Single(w.Server.Instances);
        Assert.Equal(Crypt, copy.Zone.Id);
        Assert.Same(copy.Zone, w.Where(all[0]).Zone);
        Assert.Same(copy.Zone, w.Where(all[1]).Zone);

        var seals = Seals(copy.Zone);
        Assert.Equal(3, seals.Count);
        Assert.All(seals, s => Assert.Equal("monster.watcher_seal", s.Get<UnitInfo>().DefinitionId));
        Assert.Single(copy.Zone.World.Entities, e => e.TryGet<UnitInfo>()?.DefinitionId == "monster.faceless_watcher");
    }

    [Fact]
    public void Losing_the_three_seals_takes_everyone_out_to_the_crypt_entrance_and_ends_the_copy()
    {
        using var w = Rig();
        var all = GroupOfTwo(w);
        IntoCrypt(w, all[0], all);
        IntoCrypt(w, all[1], all);
        var copy = Assert.Single(w.Server.Instances);

        foreach (var seal in Seals(copy.Zone))
        {
            copy.Zone.World.Kill(seal);
        }

        w.Tick(1, all);

        Assert.Empty(w.Server.Instances);
        var ruins = w.Server.Zones["zone.old_road_ruins"];
        var arrival = ruins.Arena.Spawns.Single(s => s.Name == "from_crypt").Position;
        foreach (var c in all)
        {
            Assert.Same(ruins, w.Where(c).Zone);
            Assert.Equal(new Vector2((float)arrival.X, (float)arrival.Y), w.Where(c).Entity.Get<Transform>().Position);
            Assert.Contains(c.All<InstanceNotice>(), n => n.Kind == InstanceNoticeKind.Removed);
        }

        // De volta pela escada: cópia nova, com os três selos.
        Through(w, all[0], Crypt, all);
        var second = Assert.Single(w.Server.Instances);
        Assert.NotSame(copy, second);
        Assert.Equal(3, Seals(second.Zone).Count);
    }
}
