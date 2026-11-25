using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Threat;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Zones;

namespace Game.Server.Tests;

/// <summary>
/// T-533: as Minas Fundas com o conteúdo real. Da Cidade, pelo Brejo, o Forte do Passo e a Serra Partida, até a instância do
/// grupo pela boca da mina (<c>mines_gate</c>). Quem morre nas Minas renasce no Forte (T-501), e o portão da Serra leva de
/// volta à mesma cópia. Se o grupo inteiro cai na Fornalha, o Capataz Forjado recomeça (vida, fase e Fornalha).
/// </summary>
public class MinesInstanceTests
{
    private const string Mines = "zone.deep_mines";
    private const string Ridge = "zone.broken_ridge";
    private const string Fort = "zone.pass_fort";
    private const string Overseer = "monster.forged_overseer";

    private static WorldRig Rig() => new(null, devLogin: true, TestRig.RealContent.Value, new ServerOptions());

    private static Vector2 GateTo(OpenZone zone, string to)
    {
        var a = zone.Definition.Gates.First(g => g.To == to).Area;
        return new Vector2((float)(a.X + (a.Width / 2)), (float)(a.Y + (a.Height / 2)));
    }

    private static void Through(WorldRig w, TestClient c, string to, TestClient[] all)
    {
        w.Where(c).Entity.Get<Transform>().Position = GateTo(w.Where(c).Zone, to);
        w.Tick(1, all);
        Assert.Equal(to, w.Where(c).Zone.Id);
    }

    /// <summary>Do Forte até a cópia das Minas: portão leste do Forte, Serra, boca da mina.</summary>
    private static void FromFortIntoMines(WorldRig w, TestClient c, TestClient[] all)
    {
        Through(w, c, Ridge, all);
        Through(w, c, Mines, all);
    }

    private static void IntoMines(WorldRig w, TestClient c, TestClient[] all)
    {
        Through(w, c, "zone.drowned_marsh", all);
        Through(w, c, Fort, all);
        FromFortIntoMines(w, c, all);
    }

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

    private static Entity Boss(OpenZone zone) =>
        Assert.Single(zone.World.Entities, e => e.IsAlive && e.TryGet<UnitInfo>()?.DefinitionId == Overseer);

    [Fact]
    public void The_party_walks_from_the_city_into_the_same_mines_copy_with_the_overseer_and_the_exit_leads_back_to_the_ridge()
    {
        using var w = Rig();
        var all = GroupOfTwo(w);
        IntoMines(w, all[0], all);
        IntoMines(w, all[1], all);

        var copy = Assert.Single(w.Server.Instances);
        Assert.Equal(Mines, copy.Zone.Id);
        Assert.Same(copy.Zone, w.Where(all[0]).Zone);
        Assert.Same(copy.Zone, w.Where(all[1]).Zone);
        Assert.Equal(27, Boss(copy.Zone).Get<UnitInfo>().Level);

        Through(w, all[0], Ridge, all);
        var arrival = w.Server.Zones[Ridge].Arena.Spawns.Single(s => s.Name == "from_mines").Position;
        Assert.Equal(new Vector2((float)arrival.X, (float)arrival.Y), w.Where(all[0]).Entity.Get<Transform>().Position);
    }

    [Fact]
    public void Death_in_the_mines_respawns_at_the_fort_and_the_ridge_gate_leads_back_to_the_same_copy()
    {
        using var w = Rig();
        var all = GroupOfTwo(w);
        IntoMines(w, all[0], all);
        IntoMines(w, all[1], all);
        var copy = Assert.Single(w.Server.Instances);

        var (zone, hero) = w.Where(all[0]);
        zone.World.Kill(hero);
        w.Tick(5, all);
        all[0].Send(new RespawnRequest());
        w.Tick(1, all);

        Assert.Same(w.Server.Zones[Fort], w.Where(all[0]).Zone);
        FromFortIntoMines(w, all[0], all);
        Assert.Same(copy, Assert.Single(w.Server.Instances));
        Assert.Same(copy.Zone, w.Where(all[0]).Zone);
    }

    [Fact]
    public void When_the_whole_party_falls_the_overseer_starts_over_and_not_before()
    {
        using var w = Rig();
        var all = GroupOfTwo(w);
        IntoMines(w, all[0], all);
        IntoMines(w, all[1], all);
        var zone = Assert.Single(w.Server.Instances).Zone;
        var boss = Boss(zone);
        var home = boss.Get<Transform>().Position;

        // O grupo entra em luta: os dois na tabela de ameaça, o chefe pela metade e com um Mineiro invocado.
        var heroes = all.Select(c => w.Where(c).Entity).ToList();
        foreach (var hero in heroes)
        {
            hero.Get<Transform>().Position = home + new Vector2(-2, 0);
            ThreatService.Add(zone.World, boss, hero, 100, AggroReason.Damage);
        }

        boss.Get<Health>().Current = boss.Get<Health>().Max / 2;
        var crew = EntityFactory.SpawnMonster(zone.World, zone.World.Content.Monsters["monster.possessed_miner"], home + new Vector2(3, 0), 27);
        crew.Add(new SummonedBy { Owner = boss.Id, MonsterId = "monster.possessed_miner" });
        w.Tick(1, all);

        // Um cai: o chefe continua como está.
        zone.World.Kill(heroes[0]);
        w.Tick(1, all);
        Assert.Same(boss, Boss(zone));
        Assert.True(boss.Get<Health>().Current < boss.Get<Health>().Max);

        // O último cai: o chefe recomeça no ponto, com a vida cheia, e sem ninguém na tabela.
        zone.World.Kill(heroes[1]);
        w.Tick(1, all);
        var fresh = Boss(zone);
        Assert.NotSame(boss, fresh);
        Assert.Equal(fresh.Get<Health>().Max, fresh.Get<Health>().Current);
        Assert.Empty(fresh.Get<ThreatTable>().Entries);
        Assert.Equal(home, fresh.Get<Transform>().Position);
        Assert.Null(zone.World.Find(crew.Id)); // a equipe invocada some junto
    }
}
