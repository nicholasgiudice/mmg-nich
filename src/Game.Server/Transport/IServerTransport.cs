namespace Game.Server.Transport;

/// <summary>Como uma mensagem vai pela rede.</summary>
public enum SendMode
{
    /// <summary>Confiável e ordenado (canal 0): Welcome, Reject, Events, IntentAck, Pong.</summary>
    Reliable,

    /// <summary>
    /// Snapshot (canal 1): não confiável — o mais novo substitui o anterior. Se não couber num pacote, o transporte
    /// usa confiável sem ordem (o LiteNetLib só fragmenta no confiável); a T-106 encolhe os snapshots com delta.
    /// </summary>
    Snapshot,
}

/// <summary>Quem recebe os acontecimentos do transporte. Chamado só dentro de <see cref="IServerTransport.Poll"/>.</summary>
public interface ITransportHandler
{
    void OnConnected(int peer);

    void OnDisconnected(int peer);

    void OnReceived(int peer, ReadOnlySpan<byte> data);
}

/// <summary>
/// Rede do servidor por trás de uma interface (T-105): LiteNetLib em produção (<see cref="LiteNetTransport"/>) e
/// em memória nos testes (<see cref="InMemoryTransport"/>). Tudo roda na thread do loop: eventos só dentro de <see cref="Poll"/>.
/// </summary>
public interface IServerTransport : IDisposable
{
    void Start(ITransportHandler handler);

    /// <summary>Entrega os eventos pendentes (conexões, mensagens, desconexões) ao handler.</summary>
    void Poll();

    void Send(int peer, ReadOnlySpan<byte> data, SendMode mode);

    /// <summary>Fecha a conexão (as mensagens confiáveis já enviadas ainda são entregues quando o transporte permite).</summary>
    void Disconnect(int peer);
}
