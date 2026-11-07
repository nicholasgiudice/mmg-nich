using Game.Core.Definitions;
using Game.Protocol;

namespace Game.Server.Parties;

/// <summary>O que o <see cref="PartyManager"/> precisa do servidor: nomes, quem está em jogo e envio de mensagens.</summary>
public interface IPartyHost
{
    /// <summary>Nome do personagem do membro (para avisos).</summary>
    string NameOf(int member);

    /// <summary>O membro ainda está em jogo (conectado e com personagem).</summary>
    bool IsOnline(int member);

    void Send(int member, INetMessage message);

    /// <summary>O grupo do membro mudou (0 = sem grupo): o servidor atualiza o <c>PartyMembership</c> e o estado do grupo.</summary>
    void PartyChanged(int member, int partyId);
}

/// <summary>
/// Grupos (party, T-310) do mundo aberto: convites, membros, líder. Vive no servidor, acima das zonas (trocar de zona não
/// muda nada aqui) e não é gravado. O membro é identificado por um id estável enquanto a conexão durar (a conexão).
/// Ids de grupo e de convite crescem a partir de 1 (determinísticos). Tempo em ticks do servidor.
/// </summary>
public sealed class PartyManager(PartyRules rules, IPartyHost host)
{
    private readonly Dictionary<int, Party> _byMember = new();
    private readonly SortedDictionary<int, PendingInvite> _invites = new();
    private int _nextParty = 1;
    private int _nextInvite = 1;

    public PartyRules Rules => rules;

    /// <summary>Id do grupo do membro; 0 sem grupo.</summary>
    public int PartyOf(int member) => _byMember.TryGetValue(member, out var p) ? p.Id : 0;

    /// <summary>Líder e membros (em ordem de entrada) do grupo do membro; null sem grupo.</summary>
    public (int PartyId, int Leader, IReadOnlyList<int> Members)? Describe(int member) =>
        _byMember.TryGetValue(member, out var p) ? (p.Id, p.Leader, p.Members) : null;

    public int PendingInvites => _invites.Count;

    /// <summary>Convite de <paramref name="from"/> para <paramref name="to"/> (null = ninguém com esse id ou nome em jogo).</summary>
    public void Invite(int from, int? to, long now)
    {
        if (to is not { } target || !host.IsOnline(target))
        {
            Reject(from, PartyRejectReason.TargetNotFound, "ninguém com essa entidade ou nome em jogo");
            return;
        }

        if (target == from)
        {
            Reject(from, PartyRejectReason.Self, "não dá para convidar a si mesmo");
            return;
        }

        if (_byMember.TryGetValue(from, out var party))
        {
            if (party.Leader != from)
            {
                Reject(from, PartyRejectReason.NotLeader, "só o líder convida");
                return;
            }

            if (party.Members.Count >= rules.MaxMembers)
            {
                Reject(from, PartyRejectReason.PartyFull, $"grupo cheio ({rules.MaxMembers})");
                return;
            }
        }

        if (_byMember.ContainsKey(target))
        {
            Reject(from, PartyRejectReason.AlreadyInParty, $"{host.NameOf(target)} já está em grupo");
            return;
        }

        // Um convite pendente por par: convidar de novo renova.
        foreach (var old in _invites.Where(kv => kv.Value.From == from && kv.Value.To == target).Select(kv => kv.Key).ToList())
        {
            _invites.Remove(old);
        }

        var id = _nextInvite++;
        _invites[id] = new PendingInvite(id, from, target, now + rules.InviteTimeoutTicks);
        host.Send(target, new PartyInviteReceived(id, from, host.NameOf(from), rules.InviteTimeoutTicks));
        host.Send(from, new PartyNotice(PartyNoticeKind.InviteSent, host.NameOf(target)));
    }

    public void Respond(int member, int inviteId, bool accept, long now)
    {
        if (!_invites.TryGetValue(inviteId, out var invite) || invite.To != member || now > invite.ExpiresAt)
        {
            Reject(member, PartyRejectReason.InviteExpired, $"convite {inviteId} expirado ou inexistente");
            return;
        }

        _invites.Remove(inviteId);
        if (!accept)
        {
            Notify(invite.From, PartyNoticeKind.InviteDeclined, host.NameOf(member));
            return;
        }

        if (!host.IsOnline(invite.From))
        {
            Reject(member, PartyRejectReason.TargetNotFound, "quem convidou não está mais em jogo");
            return;
        }

        if (_byMember.ContainsKey(member))
        {
            Reject(member, PartyRejectReason.AlreadyInParty, "você já está em grupo");
            return;
        }

        if (!_byMember.TryGetValue(invite.From, out var party))
        {
            party = new Party(_nextParty++, invite.From);
            party.Members.Add(invite.From);
            _byMember[invite.From] = party;
            host.PartyChanged(invite.From, party.Id);
        }
        else if (party.Members.Count >= rules.MaxMembers)
        {
            Reject(member, PartyRejectReason.PartyFull, $"grupo cheio ({rules.MaxMembers})");
            return;
        }

        party.Members.Add(member);
        _byMember[member] = party;
        host.PartyChanged(member, party.Id);
        foreach (var m in party.Members)
        {
            Notify(m, PartyNoticeKind.Joined, host.NameOf(member));
        }
    }

    /// <summary>Saída pedida pelo jogador.</summary>
    public void Leave(int member)
    {
        if (!_byMember.ContainsKey(member))
        {
            Reject(member, PartyRejectReason.NotInParty, "sem grupo");
            return;
        }

        Remove(member, PartyNoticeKind.Left);
    }

    public void Kick(int leader, int target)
    {
        if (!_byMember.TryGetValue(leader, out var party))
        {
            Reject(leader, PartyRejectReason.NotInParty, "sem grupo");
        }
        else if (party.Leader != leader)
        {
            Reject(leader, PartyRejectReason.NotLeader, "só o líder expulsa");
        }
        else if (target == leader)
        {
            Reject(leader, PartyRejectReason.Self, "para sair, use sair do grupo");
        }
        else if (!party.Members.Contains(target))
        {
            Reject(leader, PartyRejectReason.NotInParty, $"membro {target} não é do grupo");
        }
        else
        {
            Remove(target, PartyNoticeKind.Kicked);
        }
    }

    /// <summary>O membro saiu do jogo: sai do grupo e os convites dele (feitos e recebidos) caem, sem avisos para ele.</summary>
    public void Disconnected(int member)
    {
        foreach (var id in _invites.Where(kv => kv.Value.From == member || kv.Value.To == member).Select(kv => kv.Key).ToList())
        {
            _invites.Remove(id);
        }

        if (_byMember.ContainsKey(member))
        {
            Remove(member, PartyNoticeKind.Left);
        }
    }

    /// <summary>Descarta os convites vencidos e avisa quem convidou.</summary>
    public void Expire(long now)
    {
        foreach (var invite in _invites.Values.Where(i => now > i.ExpiresAt).ToList())
        {
            _invites.Remove(invite.Id);
            Notify(invite.From, PartyNoticeKind.InviteExpired, host.NameOf(invite.To));
        }
    }

    private void Remove(int member, PartyNoticeKind why)
    {
        var party = _byMember[member];
        var name = host.NameOf(member);
        party.Members.Remove(member);
        _byMember.Remove(member);
        host.PartyChanged(member, 0);
        Notify(member, why, name);
        foreach (var m in party.Members)
        {
            Notify(m, why, name);
        }

        if (party.Members.Count == 1)
        {
            var last = party.Members[0];
            party.Members.Clear();
            _byMember.Remove(last);
            host.PartyChanged(last, 0);
            Notify(last, PartyNoticeKind.Disbanded, host.NameOf(last));
            foreach (var id in _invites.Where(kv => kv.Value.From == last).Select(kv => kv.Key).ToList())
            {
                _invites.Remove(id); // o convite era para o grupo que acabou; quem aceitar depois recebe "expirado"
            }

            return;
        }

        if (party.Leader == member)
        {
            party.Leader = party.Members[0]; // o mais antigo
            foreach (var m in party.Members)
            {
                Notify(m, PartyNoticeKind.LeaderChanged, host.NameOf(party.Leader));
            }
        }
    }

    private void Notify(int member, PartyNoticeKind kind, string name)
    {
        if (host.IsOnline(member))
        {
            host.Send(member, new PartyNotice(kind, name));
        }
    }

    private void Reject(int member, PartyRejectReason reason, string detail) => host.Send(member, new PartyRejected(reason, detail));

    private sealed class Party(int id, int leader)
    {
        public int Id { get; } = id;
        public int Leader { get; set; } = leader;
        public List<int> Members { get; } = new();
    }

    private sealed record PendingInvite(int Id, int From, int To, long ExpiresAt);
}
