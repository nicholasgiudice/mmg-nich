using Game.Core.Simulation;
using Game.Protocol;
using MmoRpgClient.Hud;

namespace Game.Client.Tests;

/// <summary>T-311: estado do grupo no cliente (membros, líder, zona) e convites recebidos.</summary>
public class PartyModelTests
{
    private static PartyMemberInfo Member(int id, string name, string zone = "zone.city", int entity = 0) =>
        new(id, new EntityId(entity == 0 ? id + 100 : entity), name, "class.warrior", 3, 80, 100, 20, 40, zone, true);

    private static PartyState State(int leader, int you, params PartyMemberInfo[] members) => new(7, leader, you, members);

    [Fact]
    public void Without_state_there_is_no_party()
    {
        var model = new PartyModel();
        Assert.False(model.InParty);
        Assert.False(model.IsLeader);
        Assert.Empty(model.Others);
        Assert.Null(model.You);

        model.Apply(new PartyState(0, 0, 3, []));
        Assert.False(model.InParty);
    }

    [Fact]
    public void Others_skip_the_viewer_and_keep_the_join_order()
    {
        var model = new PartyModel();
        model.Apply(State(leader: 1, you: 2, Member(1, "Thorin"), Member(2, "Lyra"), Member(3, "Kael")));

        Assert.True(model.InParty);
        Assert.False(model.IsLeader);
        Assert.Equal(["Thorin", "Kael"], model.Others.Select(m => m.Name).ToArray());
        Assert.True(model.IsLeaderMember(model.Others[0]));
        Assert.False(model.IsLeaderMember(model.Others[1]));
        Assert.Equal("Lyra", model.You!.Name);
    }

    [Fact]
    public void Viewer_can_be_the_leader()
    {
        var model = new PartyModel();
        model.Apply(State(leader: 2, you: 2, Member(1, "Thorin"), Member(2, "Lyra")));
        Assert.True(model.IsLeader);
    }

    [Fact]
    public void Same_zone_compares_with_the_viewer_zone()
    {
        var model = new PartyModel();
        model.Apply(State(1, 1, Member(1, "Thorin", "zone.vigil_fields"), Member(2, "Lyra", "zone.vigil_fields"), Member(3, "Kael", "zone.city")));

        Assert.True(model.SameZone(model.Others[0]));
        Assert.False(model.SameZone(model.Others[1]));
    }

    [Fact]
    public void Invite_counts_down_and_expires()
    {
        var model = new PartyModel();
        model.Receive(new PartyInviteReceived(5, 9, "Thorin", ExpiresInTicks: 30 * World.TickRate), now: 10);

        var invite = model.ActiveInvite(now: 10.2);
        Assert.NotNull(invite);
        Assert.Equal("Thorin", invite.FromName);
        Assert.Equal(30, invite.SecondsLeft(10.2));
        Assert.Equal(1, invite.SecondsLeft(39.5));
        Assert.Null(model.ActiveInvite(now: 40));
    }

    [Fact]
    public void Oldest_valid_invite_is_shown_first_and_answering_removes_it()
    {
        var model = new PartyModel();
        model.Receive(new PartyInviteReceived(5, 9, "Thorin", 600), now: 0);
        model.Receive(new PartyInviteReceived(6, 8, "Kael", 600), now: 1);

        Assert.Equal(5, model.ActiveInvite(2)!.InviteId);
        model.Answered(5);
        Assert.Equal(6, model.ActiveInvite(2)!.InviteId);
    }

    [Fact]
    public void A_renewed_invite_from_the_same_person_replaces_the_old_one()
    {
        var model = new PartyModel();
        model.Receive(new PartyInviteReceived(5, 9, "Thorin", 20), now: 0);
        model.Receive(new PartyInviteReceived(6, 9, "Thorin", 600), now: 0.5);

        var invite = model.ActiveInvite(2);
        Assert.Equal(6, invite!.InviteId);
        model.Answered(6);
        Assert.Null(model.ActiveInvite(2));
    }

    [Fact]
    public void Joining_a_party_drops_pending_invites()
    {
        var model = new PartyModel();
        model.Receive(new PartyInviteReceived(5, 9, "Thorin", 600), now: 0);
        model.Apply(State(1, 2, Member(1, "Kael"), Member(2, "Lyra")));
        Assert.Null(model.ActiveInvite(1));
    }

    [Fact]
    public void Kick_notice_with_the_viewer_name_reads_as_you()
    {
        Assert.Equal("Você foi expulso do grupo.", PartyText.Notice(PartyNoticeKind.Kicked, "Lyra", selfName: "lyra"));
        Assert.Equal("Kael foi expulso do grupo.", PartyText.Notice(PartyNoticeKind.Kicked, "Kael", selfName: "Lyra"));
        Assert.Equal("Convite enviado para Kael.", PartyText.Notice(PartyNoticeKind.InviteSent, "Kael", "Lyra"));
    }

    [Fact]
    public void Every_rejection_reason_has_its_own_text()
    {
        var texts = Enum.GetValues<PartyRejectReason>().Select(PartyText.Rejection).ToArray();
        Assert.Equal(texts.Length, texts.Distinct().Count());
        Assert.DoesNotContain("Pedido de grupo recusado.", texts);
    }

    [Fact]
    public void Only_membership_changes_go_to_the_log()
    {
        Assert.True(PartyText.GoesToLog(PartyNoticeKind.Joined));
        Assert.True(PartyText.GoesToLog(PartyNoticeKind.Disbanded));
        Assert.False(PartyText.GoesToLog(PartyNoticeKind.InviteSent));
        Assert.False(PartyText.GoesToLog(PartyNoticeKind.InviteDeclined));
    }
}
