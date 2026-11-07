using Game.Core.Simulation;
using Game.Protocol;

namespace MmoRpgClient.Hud;

/// <summary>
/// Estado do grupo no cliente (T-311): o último <see cref="PartyState"/> do servidor e os convites recebidos ainda
/// válidos. Só apresentação: o cliente nunca decide quem está no grupo, só mostra o que o servidor mandou.
/// Sem Godot, para os testes.
/// </summary>
public sealed class PartyModel
{
    private readonly List<PendingInvite> _invites = new();

    /// <summary>Último estado do servidor; null ou sem membros = sem grupo.</summary>
    public PartyState? State { get; private set; }

    /// <summary>Em grupo (o servidor desfaz o grupo de 1, então são sempre 2 ou mais).</summary>
    public bool InParty => State is { PartyId: > 0, Members.Length: > 0 };

    /// <summary>Quem recebe é o líder.</summary>
    public bool IsLeader => InParty && State!.LeaderId == State.YouId;

    /// <summary>Membros na ordem de entrada, sem o próprio jogador (o quadro dele já está no topo da tela).</summary>
    public IReadOnlyList<PartyMemberInfo> Others =>
        InParty ? State!.Members.Where(m => m.MemberId != State.YouId).ToArray() : Array.Empty<PartyMemberInfo>();

    /// <summary>O próprio jogador no grupo (zona atual dele); null sem grupo.</summary>
    public PartyMemberInfo? You => InParty ? State!.Members.FirstOrDefault(m => m.MemberId == State.YouId) : null;

    public bool IsLeaderMember(PartyMemberInfo member) => InParty && member.MemberId == State!.LeaderId;

    /// <summary>
    /// O membro está na mesma zona de quem vê? Só então a entidade dele vale aqui (clicar no quadro seleciona).
    /// </summary>
    public bool SameZone(PartyMemberInfo member) => You is { } me && member.ZoneId == me.ZoneId;

    public void Apply(PartyState state)
    {
        State = state;
        if (InParty)
        {
            // Quem entrou num grupo não responde mais a convites antigos (o servidor recusaria com "já em grupo").
            _invites.Clear();
        }
    }

    /// <summary>Guarda um convite; <paramref name="now"/> é o relógio do cliente em segundos.</summary>
    public void Receive(PartyInviteReceived invite, double now)
    {
        _invites.RemoveAll(i => i.InviteId == invite.InviteId || i.FromMemberId == invite.FromMemberId);
        _invites.Add(new PendingInvite(invite.InviteId, invite.FromMemberId, invite.FromName,
            now + invite.ExpiresInTicks / (double)World.TickRate));
    }

    /// <summary>O convite mostrado na janela: o mais antigo ainda válido; os vencidos somem.</summary>
    public PendingInvite? ActiveInvite(double now)
    {
        _invites.RemoveAll(i => i.ExpiresAt <= now);
        return _invites.Count > 0 ? _invites[0] : null;
    }

    /// <summary>Tira o convite depois de responder (aceitar ou recusar).</summary>
    public void Answered(int inviteId) => _invites.RemoveAll(i => i.InviteId == inviteId);

    /// <summary>Conexão nova (entrada no jogo): nada do grupo anterior vale mais.</summary>
    public void Reset()
    {
        State = null;
        _invites.Clear();
    }
}

/// <summary>Convite recebido que ainda não foi respondido.</summary>
public sealed record PendingInvite(int InviteId, int FromMemberId, string FromName, double ExpiresAt)
{
    /// <summary>Segundos inteiros restantes, arredondados para cima (a janela mostra "Expira em N s").</summary>
    public int SecondsLeft(double now) => Math.Max(0, (int)Math.Ceiling(ExpiresAt - now));
}

/// <summary>Textos do grupo exibidos ao jogador (T-311).</summary>
public static class PartyText
{
    public static string Invitation(string fromName) => $"{fromName} convidou você para o grupo.";

    public static string Expires(int seconds) => $"Expira em {seconds} s";

    public static string Rejection(PartyRejectReason reason) => reason switch
    {
        PartyRejectReason.PartyFull => "O grupo está cheio.",
        PartyRejectReason.AlreadyInParty => "Esse personagem já está em um grupo.",
        PartyRejectReason.InviteExpired => "O convite expirou.",
        PartyRejectReason.TargetNotFound => "Ninguém com esse nome está em jogo.",
        PartyRejectReason.NotLeader => "Só o líder do grupo pode fazer isso.",
        PartyRejectReason.NotInParty => "Você não está em um grupo.",
        PartyRejectReason.Self => "Você não pode convidar a si mesmo.",
        _ => "Pedido de grupo recusado.",
    };

    /// <summary>
    /// Texto do aviso. <paramref name="selfName"/> é o nome do próprio personagem: o expulso recebe o aviso com o próprio
    /// nome, e lê "Você foi expulso".
    /// </summary>
    public static string Notice(PartyNoticeKind kind, string name, string? selfName)
    {
        var isYou = selfName is not null && string.Equals(name, selfName, StringComparison.OrdinalIgnoreCase);
        return kind switch
        {
            PartyNoticeKind.Joined => isYou ? "Você entrou no grupo." : $"{name} entrou no grupo.",
            PartyNoticeKind.Left => isYou ? "Você saiu do grupo." : $"{name} saiu do grupo.",
            PartyNoticeKind.Kicked => isYou ? "Você foi expulso do grupo." : $"{name} foi expulso do grupo.",
            PartyNoticeKind.LeaderChanged => isYou ? "Agora você é o líder do grupo." : $"{name} agora é o líder do grupo.",
            PartyNoticeKind.Disbanded => "O grupo se desfez.",
            PartyNoticeKind.InviteDeclined => $"{name} recusou o convite.",
            PartyNoticeKind.InviteExpired => $"O convite para {name} expirou.",
            PartyNoticeKind.InviteSent => $"Convite enviado para {name}.",
            _ => name,
        };
    }

    /// <summary>Avisos que mudam o grupo vão para o log (categoria Sistema); os de convite só aparecem na tela.</summary>
    public static bool GoesToLog(PartyNoticeKind kind) =>
        kind is PartyNoticeKind.Joined or PartyNoticeKind.Left or PartyNoticeKind.Kicked
            or PartyNoticeKind.LeaderChanged or PartyNoticeKind.Disbanded;
}
