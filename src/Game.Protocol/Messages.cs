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
[MemoryPackUnion(9, typeof(SnapshotDelta))]
[MemoryPackUnion(10, typeof(SnapshotAck))]
[MemoryPackUnion(11, typeof(RespawnRequest))]
public partial interface INetMessage;

// ---------------- cliente → servidor ----------------

/// <summary>
/// Primeira mensagem do cliente. Com <paramref name="Ticket"/> (token de jogo da API, T-211/T-212) o servidor carrega o
/// personagem salvo e ignora nome e classe. Sem ele, login de desenvolvimento (só com <c>--dev-login</c>):
/// <paramref name="DevName"/> e <paramref name="ClassId"/> (null = a classe que o servidor escolher).
/// </summary>
[MemoryPackable]
public sealed partial record Hello(int ProtocolVersion, string ContentFingerprint, string DevName, string? ClassId, string? Ticket = null) : INetMessage;

/// <summary>
/// Uma intenção do jogador. <paramref name="Sequence"/> cresce a cada intenção enviada (o servidor responde com
/// <see cref="IntentAck"/> para a predição da T-108); <paramref name="ClientTick"/> é o tick que o cliente estimava.
/// </summary>
[MemoryPackable]
public sealed partial record IntentMessage(uint Sequence, long ClientTick, NetIntent Intent) : INetMessage;

/// <summary>
/// O cliente aplicou o <see cref="SnapshotDelta"/> do <paramref name="Tick"/> (T-106): o servidor passa a mandar deltas
/// a partir dele. Pode ir no canal não confiável; vale o maior tick confirmado da mesma <paramref name="Epoch"/>.
/// </summary>
[MemoryPackable]
public sealed partial record SnapshotAck(int Epoch, long Tick) : INetMessage;

/// <summary>
/// Mundo aberto (T-214): o personagem morto pede para renascer na Cidade (com vida cheia, sem perder nada). Ignorado se ele
/// está vivo ou numa sala de encontro.
/// </summary>
[MemoryPackable]
public sealed partial record RespawnRequest : INetMessage;

/// <summary>Medição de latência: o servidor devolve o mesmo valor num <see cref="Pong"/>.</summary>
[MemoryPackable]
public sealed partial record Ping(long ClientTimeMs) : INetMessage;

// ---------------- servidor → cliente ----------------

/// <summary>
/// Entrada aceita: entidade controlada, tick atual e onde o jogo acontece. <paramref name="Epoch"/> muda a cada reinício
/// da sala (T-106): ids e ticks recomeçam, então o cliente descarta todo o estado da época anterior.
/// No mundo aberto (T-214), <paramref name="EncounterId"/> é o id da zona (<c>zone.*</c>) e cada troca de zona (portão,
/// renascer) chega como um <c>Welcome</c> novo, com a arena da zona e época nova.
/// </summary>
[MemoryPackable]
public sealed partial record Welcome(EntityId ControlledEntity, long Tick, string EncounterId, string ArenaId, int Epoch) : INetMessage;

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

    /// <summary>Token de jogo ausente, inválido, expirado, de outro servidor ou já usado (T-212).</summary>
    Unauthorized,

    /// <summary>O personagem já está conectado (T-212).</summary>
    AlreadyConnected,

    /// <summary>O servidor não conseguiu carregar o personagem (banco fora do ar, personagem apagado ou corrompido).</summary>
    Unavailable,
}

/// <summary>Entrada recusada; o servidor fecha a conexão em seguida. <paramref name="Detail"/> é texto para log.</summary>
[MemoryPackable]
public sealed partial record Reject(RejectReason Reason, string Detail) : INetMessage;

/// <summary>
/// Estado replicável completo do mundo no <paramref name="Tick"/> (componentes <c>IReplicated</c>). Desde a T-106 o
/// servidor manda <see cref="SnapshotDelta"/>; este formato fica para ferramentas e testes.
/// </summary>
[MemoryPackable]
public sealed partial record SnapshotMessage(long Tick, EntitySnapshot[] Entities) : INetMessage
{
    public static SnapshotMessage From(WorldSnapshot snapshot) => new(snapshot.Tick, snapshot.Entities.ToArray());

    public WorldSnapshot ToWorldSnapshot() => new(Tick, Entities);
}

/// <summary>
/// Estado visível pelo jogador no <paramref name="Tick"/> (T-106), como diferença em relação ao tick
/// <paramref name="BaseTick"/> que o cliente confirmou com <see cref="SnapshotAck"/>. <paramref name="BaseTick"/> 0 =
/// completo: o cliente descarta o que tinha. <paramref name="Removed"/> = entidades que saíram do interesse ou do mundo.
/// Aplique com <see cref="SnapshotReconstructor"/>.
/// </summary>
[MemoryPackable]
public sealed partial record SnapshotDelta(int Epoch, long Tick, long BaseTick, EntityDelta[] Entities, EntityId[] Removed) : INetMessage
{
    public bool IsFull => BaseTick == 0;
}

/// <summary>
/// Uma entidade num <see cref="SnapshotDelta"/>. <paramref name="Full"/> = entrou no interesse (ou o snapshot é completo):
/// <paramref name="Components"/> é tudo que ela tem. Senão, só os componentes que mudaram, e
/// <paramref name="RemovedComponents"/> as tags (<see cref="ComponentCodecs.Tags"/>) dos que ela perdeu.
/// </summary>
/// <remarks>Formato compacto próprio (id e contagens em varint, um byte de flags): é a parte que mais se repete na rede.</remarks>
public sealed record EntityDelta(EntityId Id, bool Full, IComponent[] Components, byte[] RemovedComponents);

/// <summary>Eventos publicados no <paramref name="Tick"/> (o <c>Tick</c> de cada evento chega igual ao do servidor).</summary>
[MemoryPackable]
public sealed partial record EventsMessage(long Tick, GameEvent[] Events) : INetMessage;

/// <summary>Resposta ao <see cref="Ping"/>, com o tick do servidor no momento da resposta.</summary>
[MemoryPackable]
public sealed partial record Pong(long ClientTimeMs, long ServerTick) : INetMessage;

/// <summary>Última intenção (<see cref="IntentMessage.Sequence"/>) aplicada pelo servidor e em que tick.</summary>
[MemoryPackable]
public sealed partial record IntentAck(uint LastSequence, long Tick) : INetMessage;
