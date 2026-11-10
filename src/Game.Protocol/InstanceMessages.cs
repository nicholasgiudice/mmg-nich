using MemoryPack;

namespace Game.Protocol;

// Instâncias de dungeon (T-350). Só no mundo aberto. A entrada aceita chega como um Welcome com InstanceId; a recusa, como
// InstanceRejected (o personagem fica onde estava, no portão).

public enum InstanceRejectReason : byte
{
    /// <summary>O processo já tem o máximo de instâncias abertas.</summary>
    TooManyInstances,

    /// <summary>A cópia do grupo já tem o máximo de personagens (<c>maxPlayers</c> da zona).</summary>
    InstanceFull,
}

/// <summary>Entrada numa instância recusada. <paramref name="ZoneId"/> é a zona da instância; <paramref name="Detail"/> é texto para log.</summary>
[MemoryPackable]
public sealed partial record InstanceRejected(string ZoneId, InstanceRejectReason Reason, string Detail) : INetMessage;

public enum InstanceNoticeKind : byte
{
    /// <summary>Você não é mais do grupo dono desta cópia: será levado para fora em <c>Ticks</c>.</summary>
    LeavingSoon,

    /// <summary>Voltou a ser do grupo dono da cópia: a saída marcada foi cancelada.</summary>
    StayCancelled,

    /// <summary>Levado para fora da cópia (fim do prazo).</summary>
    Removed,
}

/// <summary>Aviso de instância para mostrar ao jogador. <paramref name="Ticks"/>: prazo restante (20 por segundo), quando houver.</summary>
[MemoryPackable]
public sealed partial record InstanceNotice(InstanceNoticeKind Kind, int Ticks) : INetMessage;
