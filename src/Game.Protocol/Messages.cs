using Game.Core.Events;
using Game.Core.Simulation;
using MemoryPack;

namespace Game.Protocol;

/// <summary>
/// Toda mensagem de rede (T-103). Serialize sempre por <see cref="ProtocolSerializer"/>, que registra os formatadores
/// dos tipos do Core. Canais (T-105): <see cref="SnapshotMessage"/> vai no não confiável (o mais novo substitui o
/// anterior); o resto vai no confiável e ordenado.
/// </summary>
[MemoryPackable]
[MemoryPackUnion(0, typeof(Hello))]
[MemoryPackUnion(1, typeof(IntentMessage))]
[MemoryPackUnion(2, typeof(Ping))]
[MemoryPackUnion(3, typeof(Welcome))]
[MemoryPackUnion(4, typeof(Reject))]
[MemoryPackUnion(5, typeof(SnapshotMessage))]
[MemoryPackUnion(6, typeof(EventsMessage))]
[MemoryPackUnion(7, typeof(Pong))]
[MemoryPackUnion(8, typeof(IntentAck))]
public partial interface INetMessage;

// ---------------- cliente → servidor ----------------

/// <summary>Primeira mensagem do cliente. <paramref name="ClassId"/> null = a classe que o servidor escolher.</summary>
[MemoryPackable]
public sealed partial record Hello(int ProtocolVersion, string ContentFingerprint, string DevName, string? ClassId) : INetMessage;

/// <summary>
/// Uma intenção do jogador. <paramref name="Sequence"/> cresce a cada intenção enviada (o servidor responde com
/// <see cref="IntentAck"/> para a predição da T-108); <paramref name="ClientTick"/> é o tick que o cliente estimava.
/// </summary>
[MemoryPackable]
public sealed partial record IntentMessage(uint Sequence, long ClientTick, NetIntent Intent) : INetMessage;

/// <summary>Medição de latência: o servidor devolve o mesmo valor num <see cref="Pong"/>.</summary>
[MemoryPackable]
public sealed partial record Ping(long ClientTimeMs) : INetMessage;

// ---------------- servidor → cliente ----------------

/// <summary>Entrada aceita: entidade controlada, tick atual e onde o jogo acontece.</summary>
[MemoryPackable]
public sealed partial record Welcome(EntityId ControlledEntity, long Tick, string EncounterId, string ArenaId) : INetMessage;

public enum RejectReason : byte
{
    /// <summary><see cref="Hello.ProtocolVersion"/> diferente de <see cref="ProtocolVersion.Current"/>.</summary>
    ProtocolVersion,

    /// <summary>Conteúdo (<c>data/</c>) diferente: fingerprint não bate.</summary>
    Content,

    /// <summary>Sala cheia.</summary>
    Full,

    /// <summary>Mensagem inválida ou fora de ordem (ex.: intenção antes do <see cref="Hello"/>).</summary>
    BadRequest,
}

/// <summary>Entrada recusada; o servidor fecha a conexão em seguida. <paramref name="Detail"/> é texto para log.</summary>
[MemoryPackable]
public sealed partial record Reject(RejectReason Reason, string Detail) : INetMessage;

/// <summary>Estado replicável do mundo no <paramref name="Tick"/> (componentes <c>IReplicated</c>).</summary>
[MemoryPackable]
public sealed partial record SnapshotMessage(long Tick, EntitySnapshot[] Entities) : INetMessage
{
    public static SnapshotMessage From(WorldSnapshot snapshot) => new(snapshot.Tick, snapshot.Entities.ToArray());

    public WorldSnapshot ToWorldSnapshot() => new(Tick, Entities);
}

/// <summary>Eventos publicados no <paramref name="Tick"/> (o <c>Tick</c> de cada evento chega igual ao do servidor).</summary>
[MemoryPackable]
public sealed partial record EventsMessage(long Tick, GameEvent[] Events) : INetMessage;

/// <summary>Resposta ao <see cref="Ping"/>, com o tick do servidor no momento da resposta.</summary>
[MemoryPackable]
public sealed partial record Pong(long ClientTimeMs, long ServerTick) : INetMessage;

/// <summary>Última intenção (<see cref="IntentMessage.Sequence"/>) aplicada pelo servidor e em que tick.</summary>
[MemoryPackable]
public sealed partial record IntentAck(uint LastSequence, long Tick) : INetMessage;
