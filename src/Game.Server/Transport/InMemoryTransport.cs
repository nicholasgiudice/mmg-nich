namespace Game.Server.Transport;

/// <summary>
/// Transporte em memória para testes (T-105): sem rede, sem perda, entrega na próxima chamada de <see cref="Poll"/>.
/// Os clientes são <see cref="InMemoryClient"/>.
/// </summary>
public sealed class InMemoryTransport : IServerTransport
{
    private readonly Queue<Action<ITransportHandler>> _inbox = new();
    private readonly Dictionary<int, InMemoryClient> _clients = new();
    private ITransportHandler? _handler;
    private int _nextPeer = 1;

    public void Start(ITransportHandler handler) => _handler = handler;

    /// <summary>Um cliente novo que se conecta (o servidor vê a conexão no próximo <see cref="Poll"/>).</summary>
    public InMemoryClient Connect()
    {
        var client = new InMemoryClient(this, _nextPeer++);
        _clients[client.Peer] = client;
        _inbox.Enqueue(h => h.OnConnected(client.Peer));
        return client;
    }

    internal void FromClient(int peer, byte[] data) => _inbox.Enqueue(h => h.OnReceived(peer, data));

    internal void ClientLeft(int peer)
    {
        if (_clients.Remove(peer))
        {
            _inbox.Enqueue(h => h.OnDisconnected(peer));
        }
    }

    public void Poll()
    {
        while (_handler is not null && _inbox.TryDequeue(out var e))
        {
            e(_handler);
        }
    }

    public void Send(int peer, ReadOnlySpan<byte> data, SendMode mode)
    {
        if (_clients.TryGetValue(peer, out var c))
        {
            c.Deliver(data.ToArray(), mode);
        }
    }

    public void Disconnect(int peer)
    {
        if (_clients.Remove(peer, out var c))
        {
            c.Closed = true;
            _inbox.Enqueue(h => h.OnDisconnected(peer));
        }
    }

    public void Dispose()
    {
    }
}

/// <summary>Um cliente do <see cref="InMemoryTransport"/>: manda bytes e guarda o que recebeu.</summary>
public sealed class InMemoryClient(InMemoryTransport transport, int peer)
{
    public int Peer { get; } = peer;

    /// <summary>Mensagens recebidas, em ordem, com o modo de envio.</summary>
    public List<(byte[] Data, SendMode Mode)> Received { get; } = new();

    /// <summary>O servidor fechou a conexão.</summary>
    public bool Closed { get; internal set; }

    public void Send(byte[] data) => transport.FromClient(Peer, data);

    public void Leave() => transport.ClientLeft(Peer);

    internal void Deliver(byte[] data, SendMode mode) => Received.Add((data, mode));
}
