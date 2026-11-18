using System.Numerics;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Zones;

namespace Game.Server.Tests;

/// <summary>
/// T-391: <see cref="PartyPositions"/> com a posição dos outros membros do grupo na mesma zona (e cópia), a cada
/// <see cref="WorldServer.PartyPositionIntervalTicks"/>, mesmo fora da área de interesse; lista vazia uma vez ao ficar sozinho.
/// </summary>
public class PartyPositionTests
{
    private const int Interval = WorldServer.PartyPositionIntervalTicks;

    private static TestClient[] All(params TestClient[] c) => c;

    private static void Group(WorldRig w, TestClient leader, TestClient guest, string guestName, TestClient[] all)
    {
        leader.Send(new PartyInvite(null, guestName));
        w.Tick(1, all);
        guest.Send(new PartyRespond(guest.All<PartyInviteReceived>().Last().InviteId, true));
        w.Tick(1, all);
    }

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

    /// <summary>Um tile livre (sem parede nem portão) a pelo menos <paramref name="distance"/> tiles de <paramref name="from"/>.</summary>
    private static Vector2 FarFreeTile(OpenZone zone, Vector2 from, float distance)
    {
        var map = zone.World.Map!;
        for (var y = map.Height - 2; y > 0; y--)
        {
            for (var x = map.Width - 2; x > 0; x--)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                if (Vector2.Distance(p, from) >= distance && !map.IsBlocked(p) && zone.GateAt(p) is null)
                {
                    return p;
                }
            }
        }

        throw new InvalidOperationException($"sem tile livre a {distance} tiles em {zone.Id}");
    }

    private static Vector2 PositionOf(WorldRig w, TestClient c) => w.Where(c).Entity.Get<Transform>().Position;

    private static PartyPositions Last(TestClient c) => c.All<PartyPositions>().Last();

    private static int Empties(TestClient c) => c.All<PartyPositions>().Count(m => m.Members.Length == 0);

    [Fact]
    public void Members_far_apart_in_the_same_zone_get_each_others_position_twice_a_second()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var all = All(a, b);
        Group(w, a, b, "bruno", all);
        Through(w, a, "zone.vigil_fields", all);
        Through(w, b, "zone.vigil_fields", all);

        var far = FarFreeTile(w.Fields, PositionOf(w, a), 80);
        w.Where(b).Entity.Get<Transform>().Position = far;
        w.Tick(Interval * 2, all);

        // Bem fora do raio da área de interesse (2 células de 16 tiles). Hoje o InterestGrid manda todo jogador da zona
        // (facção Player é sempre relevante, T-106), mas a posição do grupo não depende disso.
        Assert.True(Vector2.Distance(PositionOf(w, a), PositionOf(w, b)) > 3 * w.Fields.Grid.CellSize);
        var seen = Assert.Single(Last(a).Members);
        Assert.Equal(b.Raw.Peer, seen.MemberId);
        Assert.Equal(PositionOf(w, b), new Vector2(seen.X, seen.Y));
        Assert.Contains(a.All<PartyState>().Last().Members, m => m.MemberId == seen.MemberId && m.Name == "bruno");
        var back = Assert.Single(Last(b).Members);
        Assert.Equal(a.Raw.Peer, back.MemberId);
        Assert.Equal(PositionOf(w, a), new Vector2(back.X, back.Y));

        // Ritmo: uma a cada 10 ticks.
        var before = a.All<PartyPositions>().Count;
        w.Tick(Interval * 3, all);
        Assert.Equal(before + 3, a.All<PartyPositions>().Count);
    }

    [Fact]
    public void Without_a_party_nothing_is_sent()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        w.Tick(Interval * 3, a, b);

        Assert.Empty(a.All<PartyPositions>());
        Assert.Empty(b.All<PartyPositions>());
    }

    [Fact]
    public void A_member_in_another_zone_is_left_out_and_leaving_sends_one_empty_list()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var c = w.Join("caio");
        var all = All(a, b, c);
        Group(w, a, b, "bruno", all);
        Group(w, a, c, "caio", all);
        w.Tick(Interval, all);
        Assert.Equal(2, Last(a).Members.Length);

        // caio vai para os Campos: some da lista de aria e bruno; sozinho lá, recebe a lista vazia uma vez.
        Through(w, c, "zone.vigil_fields", all);
        w.Tick(Interval * 3, all);
        Assert.Equal(b.Raw.Peer, Assert.Single(Last(a).Members).MemberId);
        Assert.Empty(Last(c).Members);
        Assert.Equal(1, Empties(c));

        // bruno também vai: aria fica sozinha na Cidade e recebe a lista vazia uma vez; bruno e caio se veem nos Campos.
        Through(w, b, "zone.vigil_fields", all);
        w.Tick(Interval * 3, all);
        Assert.Empty(Last(a).Members);
        Assert.Equal(1, Empties(a));
        Assert.Equal(c.Raw.Peer, Assert.Single(Last(b).Members).MemberId);
        Assert.Equal(b.Raw.Peer, Assert.Single(Last(c).Members).MemberId);

        // Sem ninguém na zona, nada mais chega para aria.
        var count = a.All<PartyPositions>().Count;
        w.Tick(Interval * 3, all);
        Assert.Equal(count, a.All<PartyPositions>().Count);
    }

    [Fact]
    public void Leaving_the_party_sends_one_empty_list()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var all = All(a, b);
        Group(w, a, b, "bruno", all);
        w.Tick(Interval, all);
        Assert.Single(Last(a).Members);

        b.Send(new PartyLeave());
        w.Tick(Interval * 3, all);
        Assert.Empty(Last(a).Members);
        Assert.Empty(Last(b).Members);
        Assert.Equal(1, Empties(a));
        Assert.Equal(1, Empties(b));
    }

    [Fact]
    public void A_dead_member_stays_where_it_fell_until_it_respawns()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var all = All(a, b);
        Group(w, a, b, "bruno", all);
        Through(w, a, "zone.vigil_fields", all);
        Through(w, b, "zone.vigil_fields", all);

        var (fields, hero) = w.Where(b);
        var fell = PositionOf(w, b);
        fields.World.Kill(hero);
        w.Tick(Interval * 2, all);
        var seen = Assert.Single(Last(a).Members);
        Assert.Equal(fell, new Vector2(seen.X, seen.Y));
        Assert.False(a.All<PartyState>().Last().Members.Single(m => m.MemberId == seen.MemberId).Alive);

        // Renasce na Cidade: sai da lista de aria, que fica sozinha nos Campos.
        b.Send(new RespawnRequest());
        w.Tick(Interval * 2, all);
        Assert.Same(w.City, w.Where(b).Zone);
        Assert.Empty(Last(a).Members);
        Assert.Equal(1, Empties(a));
    }

    [Fact]
    public void Two_parties_in_different_crypt_copies_do_not_see_each_other()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var c = w.Join("caio");
        var d = w.Join("davi");
        var all = All(a, b, c, d);
        Group(w, a, b, "bruno", all);
        Group(w, c, d, "davi", all);
        foreach (var x in all)
        {
            Through(w, x, "zone.old_road_ruins", all);
            Through(w, x, "zone.watcher_crypt", all);
        }

        Assert.Equal(2, w.Server.Instances.Count);
        Assert.NotSame(w.Where(a).Zone, w.Where(c).Zone);
        w.Tick(Interval * 2, all);

        Assert.Equal(b.Raw.Peer, Assert.Single(Last(a).Members).MemberId);
        Assert.Equal(a.Raw.Peer, Assert.Single(Last(b).Members).MemberId);
        Assert.Equal(d.Raw.Peer, Assert.Single(Last(c).Members).MemberId);
        Assert.Equal(c.Raw.Peer, Assert.Single(Last(d).Members).MemberId);
    }
}
