using Game.Core.Simulation;
using MemoryPack;

namespace Game.Protocol;

// Grupo (party, T-310). Só no mundo aberto; a sala de encontro ignora. Os membros são identificados por um id de membro
// estável enquanto a conexão durar (a entidade muda a cada troca de zona). Toda recusa chega como PartyRejected.

// ---------------- cliente → servidor ----------------

/// <summary>
/// Convida um personagem: pela entidade (<paramref name="Target"/>, na zona de quem convida) ou pelo nome
/// (<paramref name="Name"/>, em qualquer zona; sem diferenciar maiúsculas). Sem grupo, quem convida vira líder quando o
/// convite for aceito; em grupo, só o líder convida.
/// </summary>
[MemoryPackable]
public sealed partial record PartyInvite(EntityId? Target, string? Name) : INetMessage;

/// <summary>Resposta a um <see cref="PartyInviteReceived"/>.</summary>
[MemoryPackable]
public sealed partial record PartyRespond(int InviteId, bool Accept) : INetMessage;

/// <summary>Sai do grupo. Se era o líder, a liderança passa para o membro mais antigo.</summary>
[MemoryPackable]
public sealed partial record PartyLeave : INetMessage;

/// <summary>O líder tira um membro (<see cref="PartyMemberInfo.MemberId"/>).</summary>
[MemoryPackable]
public sealed partial record PartyKick(int MemberId) : INetMessage;

// ---------------- servidor → cliente ----------------

/// <summary>Convite recebido; vale por <paramref name="ExpiresInTicks"/> (20 por segundo).</summary>
[MemoryPackable]
public sealed partial record PartyInviteReceived(int InviteId, int FromMemberId, string FromName, int ExpiresInTicks) : INetMessage;

/// <summary>
/// Um membro do grupo. <paramref name="Entity"/> só vale na zona de quem recebe (mesma <paramref name="ZoneId"/>);
/// a entidade muda a cada troca de zona.
/// </summary>
[MemoryPackable]
public sealed partial record PartyMemberInfo(
    int MemberId,
    EntityId Entity,
    string Name,
    string ClassId,
    int Level,
    float Health,
    float MaxHealth,
    float Mana,
    float MaxMana,
    string ZoneId,
    bool Alive);

/// <summary>
/// Estado do grupo para um membro: mandado quando muda (entrada, saída, líder, vida, Mana, zona). <paramref name="PartyId"/>
/// 0 e lista vazia = sem grupo. <paramref name="YouId"/> é o id de membro de quem recebe. Membros em ordem de entrada.
/// </summary>
[MemoryPackable]
public sealed partial record PartyState(int PartyId, int LeaderId, int YouId, PartyMemberInfo[] Members) : INetMessage;

public enum PartyNoticeKind : byte
{
    /// <summary>Alguém entrou no grupo.</summary>
    Joined,

    /// <summary>Alguém saiu (ou caiu).</summary>
    Left,

    /// <summary>Alguém foi expulso (para o expulso: você foi expulso).</summary>
    Kicked,

    /// <summary>Novo líder.</summary>
    LeaderChanged,

    /// <summary>O grupo se desfez (ficou com 1 membro).</summary>
    Disbanded,

    /// <summary>Para quem convidou: o convidado recusou.</summary>
    InviteDeclined,

    /// <summary>Para quem convidou: o convite expirou sem resposta.</summary>
    InviteExpired,

    /// <summary>Para quem convidou: o convite foi entregue.</summary>
    InviteSent,
}

/// <summary>Aviso de grupo para mostrar ao jogador; <paramref name="Name"/> é o personagem envolvido.</summary>
[MemoryPackable]
public sealed partial record PartyNotice(PartyNoticeKind Kind, string Name) : INetMessage;

public enum PartyRejectReason : byte
{
    /// <summary>O grupo já tem o máximo de membros.</summary>
    PartyFull,

    /// <summary>O convidado já está em grupo (ou quem aceita já entrou em outro).</summary>
    AlreadyInParty,

    /// <summary>O convite expirou ou não existe mais.</summary>
    InviteExpired,

    /// <summary>Ninguém com essa entidade ou nome está em jogo (ou quem convidou saiu).</summary>
    TargetNotFound,

    /// <summary>Só o líder convida e expulsa.</summary>
    NotLeader,

    /// <summary>Sair ou expulsar sem estar em grupo, ou expulsar quem não é do grupo.</summary>
    NotInParty,

    /// <summary>Convidar ou expulsar a si mesmo.</summary>
    Self,
}

/// <summary>Pedido de grupo recusado. <paramref name="Detail"/> é texto para log.</summary>
[MemoryPackable]
public sealed partial record PartyRejected(PartyRejectReason Reason, string Detail) : INetMessage;
