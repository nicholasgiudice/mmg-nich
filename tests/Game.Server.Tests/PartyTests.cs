using System.Numerics;
using Game.Core.Combat.Progression;
using Game.Core.Intents;
using Game.Protocol;
using Game.Server.Zones;

namespace Game.Server.Tests;

/// <summary>T-310: grupo no mundo aberto (convite, resposta, expiração, cheio, saída, líder, zona, desconexão).</summary>
public class PartyTests
{
    private static PartyState LastState(TestClient c) => c.All<PartyState>().Last();

    private static PartyInviteReceived LastInvite(TestClient c) => c.All<PartyInviteReceived>().Last();

    private static int PartyIdOf(WorldRig w, TestClient c) => w.Where(c).Entity.TryGet<PartyMembership>()?.PartyId ?? 0;

    private static TestClient[] All(params TestClient[] c) => c;

    /// <summary><paramref name="leader"/> convida pelo nome e <paramref name="guest"/> aceita.</summary>
    private static void Group(WorldRig w, TestClient leader, TestClient guest, string guestName, params TestClient[] everyone)
    {
        leader.Send(new PartyInvite(null, guestName));
        w.Tick(1, everyone);
        guest.Send(new PartyRespond(LastInvite(guest).InviteId, true));
        w.Tick(1, everyone);
    }

    [Fact]
    public void Invite_by_entity_and_accept_forms_a_party_led_by_the_inviter()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno", "class.cleric");
        var all = All(a, b);

        a.Send(new PartyInvite(b.Me, null));
        w.Tick(1, all);

        var invite = LastInvite(b);
        Assert.Equal("aria", invite.FromName);
        Assert.Equal(a.Raw.Peer, invite.FromMemberId);
        Assert.Equal(600, invite.ExpiresInTicks);
        Assert.Contains(a.All<PartyNotice>(), n => n.Kind == PartyNoticeKind.InviteSent && n.Name == "bruno");
        Assert.Equal(0, PartyIdOf(w, a));

        b.Send(new PartyRespond(invite.InviteId, true));
        w.Tick(1, all);

        Assert.NotEqual(0, PartyIdOf(w, a));
        Assert.Equal(PartyIdOf(w, a), PartyIdOf(w, b));
        var state = LastState(b);
        Assert.Equal(a.Raw.Peer, state.LeaderId);
        Assert.Equal(b.Raw.Peer, state.YouId);
        Assert.Equal(["aria", "bruno"], state.Members.Select(m => m.Name));
        var cleric = state.Members[1];
        Assert.Equal(("class.cleric", "zone.city", b.Me, true), (cleric.ClassId, cleric.ZoneId, cleric.Entity, cleric.Alive));
        Assert.Equal(1, cleric.Level);
        Assert.True(cleric.Health > 0 && cleric.MaxHealth >= cleric.Health && cleric.MaxMana > 0);
        Assert.Contains(a.All<PartyNotice>(), n => n.Kind == PartyNoticeKind.Joined && n.Name == "bruno");

        // O componente é replicado: o cliente vê o grupo nos snapshots.
        w.Tick(2, all);
        Assert.Equal(PartyIdOf(w, a), a.Entity(b.Me)!.Get<PartyMembership>().PartyId);
    }

    [Fact]
    public void Declining_tells_the_inviter_and_creates_nothing()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var all = All(a, b);

        a.Send(new PartyInvite(null, "BRUNO"));
        w.Tick(1, all);
        b.Send(new PartyRespond(LastInvite(b).InviteId, false));
        w.Tick(1, all);

        Assert.Contains(a.All<PartyNotice>(), n => n.Kind == PartyNoticeKind.InviteDeclined && n.Name == "bruno");
        Assert.Equal(0, PartyIdOf(w, a));
        Assert.Equal(0, PartyIdOf(w, b));
        Assert.Empty(a.All<PartyState>());

        // Responder de novo ao mesmo convite: não existe mais.
        b.Send(new PartyRespond(LastInvite(b).InviteId, true));
        w.Tick(1, all);
        Assert.Equal(PartyRejectReason.InviteExpired, b.All<PartyRejected>().Last().Reason);
    }

    [Fact]
    public void Invites_expire_after_thirty_seconds()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var all = All(a, b);

        a.Send(new PartyInvite(null, "bruno"));
        w.Tick(1, all);
        var invite = LastInvite(b);
        w.Tick(w.Content.Rules.Party.InviteTimeoutTicks + 1, all);

        Assert.Contains(a.All<PartyNotice>(), n => n.Kind == PartyNoticeKind.InviteExpired && n.Name == "bruno");
        b.Send(new PartyRespond(invite.InviteId, true));
        w.Tick(1, all);

        Assert.Equal(PartyRejectReason.InviteExpired, b.All<PartyRejected>().Last().Reason);
        Assert.Equal(0, PartyIdOf(w, b));
    }

    [Fact]
    public void Unknown_targets_and_players_already_grouped_are_refused()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var c = w.Join("carla");
        var all = All(a, b, c);

        a.Send(new PartyInvite(null, "ninguem"));
        w.Tick(1, all);
        Assert.Equal(PartyRejectReason.TargetNotFound, a.All<PartyRejected>().Last().Reason);

        a.Send(new PartyInvite(new Core.Simulation.EntityId(9999), null));
        w.Tick(1, all);
        Assert.Equal(PartyRejectReason.TargetNotFound, a.All<PartyRejected>().Last().Reason);

        Group(w, a, b, "bruno", all);
        c.Send(new PartyInvite(null, "bruno"));
        w.Tick(1, all);
        Assert.Equal(PartyRejectReason.AlreadyInParty, c.All<PartyRejected>().Last().Reason);

        // Só o líder convida.
        b.Send(new PartyInvite(null, "carla"));
        w.Tick(1, all);
        Assert.Equal(PartyRejectReason.NotLeader, b.All<PartyRejected>().Last().Reason);
    }

    [Fact]
    public void A_fifth_member_is_refused()
    {
        using var w = new WorldRig();
        var names = new[] { "aria", "bruno", "carla", "davi", "elisa" };
        var c = names.Select(n => w.Join(n)).ToArray();

        // Os convites saem todos antes de alguém aceitar: o 5º chega a ser convidado, mas não cabe.
        for (var i = 1; i < 5; i++)
        {
            c[0].Send(new PartyInvite(null, names[i]));
        }

        w.Tick(1, c);
        for (var i = 1; i < 5; i++)
        {
            c[i].Send(new PartyRespond(LastInvite(c[i]).InviteId, true));
            w.Tick(1, c);
        }

        Assert.Equal(4, LastState(c[0]).Members.Length);
        Assert.Equal(PartyRejectReason.PartyFull, c[4].All<PartyRejected>().Last().Reason);
        Assert.Equal(0, PartyIdOf(w, c[4]));

        // Com o grupo cheio, o líder não consegue nem convidar.
        c[0].Send(new PartyInvite(null, "elisa"));
        w.Tick(1, c);
        Assert.Equal(PartyRejectReason.PartyFull, c[0].All<PartyRejected>().Last().Reason);
    }

    [Fact]
    public void Leaving_passes_the_lead_to_the_oldest_member_and_one_member_disbands()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var c = w.Join("carla");
        var all = All(a, b, c);
        Group(w, a, b, "bruno", all);
        Group(w, a, c, "carla", all);
        var party = PartyIdOf(w, a);

        a.Send(new PartyLeave());
        w.Tick(WorldServer.PartyStateIntervalTicks, all);

        Assert.Equal(0, PartyIdOf(w, a));
        Assert.Equal(0, LastState(a).PartyId);
        Assert.Empty(LastState(a).Members);
        Assert.Equal(party, PartyIdOf(w, b));
        Assert.Equal(b.Raw.Peer, LastState(c).LeaderId);
        Assert.Equal(["bruno", "carla"], LastState(c).Members.Select(m => m.Name));
        Assert.Contains(c.All<PartyNotice>(), n => n.Kind == PartyNoticeKind.LeaderChanged && n.Name == "bruno");

        // O novo líder expulsa; sobra 1 e o grupo se desfaz.
        b.Send(new PartyKick(c.Raw.Peer));
        w.Tick(WorldServer.PartyStateIntervalTicks, all);

        Assert.Contains(c.All<PartyNotice>(), n => n.Kind == PartyNoticeKind.Kicked && n.Name == "carla");
        Assert.Contains(b.All<PartyNotice>(), n => n.Kind == PartyNoticeKind.Disbanded);
        Assert.Equal(0, PartyIdOf(w, b));
        Assert.Equal(0, PartyIdOf(w, c));
        Assert.Equal(0, LastState(b).PartyId);
        Assert.Equal(0, w.Server.Parties.PartyOf(b.Raw.Peer));

        b.Send(new PartyLeave());
        w.Tick(1, all);
        Assert.Equal(PartyRejectReason.NotInParty, b.All<PartyRejected>().Last().Reason);
    }

    [Fact]
    public void Only_the_leader_kicks()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var c = w.Join("carla");
        var all = All(a, b, c);
        Group(w, a, b, "bruno", all);
        Group(w, a, c, "carla", all);

        b.Send(new PartyKick(c.Raw.Peer));
        w.Tick(1, all);

        Assert.Equal(PartyRejectReason.NotLeader, b.All<PartyRejected>().Last().Reason);
        Assert.Equal(3, LastState(a).Members.Length);
    }

    [Fact]
    public void Changing_zones_keeps_the_party()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var all = All(a, b);
        Group(w, a, b, "bruno", all);
        var party = PartyIdOf(w, a);

        // Anda para leste até o portão dos Campos.
        var hero = w.Where(b).Entity;
        b.Send(new IntentMessage(1, 0, NetIntent.From(new MoveIntent(hero.Id, Vector2.UnitX))));
        for (var i = 0; i < 200 && b.Welcome!.EncounterId == "zone.city"; i++)
        {
            w.Tick(1, all);
        }

        Assert.Equal("zone.vigil_fields", b.Welcome!.EncounterId);
        w.Tick(WorldServer.PartyStateIntervalTicks, all);

        Assert.Equal(party, PartyIdOf(w, b));
        Assert.Equal(party, w.Server.Parties.PartyOf(b.Raw.Peer));
        var seen = LastState(a).Members.Single(m => m.Name == "bruno");
        Assert.Equal("zone.vigil_fields", seen.ZoneId);
        Assert.Equal(b.Me, seen.Entity);
    }

    [Fact]
    public void Disconnecting_leaves_the_party()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var c = w.Join("carla");
        var all = All(a, b, c);
        Group(w, a, b, "bruno", all);
        Group(w, a, c, "carla", all);

        a.Raw.Leave();
        w.Tick(WorldServer.PartyStateIntervalTicks, b, c);

        Assert.Equal(0, w.Server.Parties.PartyOf(a.Raw.Peer));
        Assert.Equal(["bruno", "carla"], LastState(b).Members.Select(m => m.Name));
        Assert.Equal(b.Raw.Peer, LastState(c).LeaderId);
        Assert.Contains(b.All<PartyNotice>(), n => n.Kind == PartyNoticeKind.Left && n.Name == "aria");

        c.Raw.Leave();
        w.Tick(WorldServer.PartyStateIntervalTicks, b);
        Assert.Equal(0, PartyIdOf(w, b));
        Assert.Equal(0, LastState(b).PartyId);
    }
}
