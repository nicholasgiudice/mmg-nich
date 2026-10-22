using LiteNetLib;
using Serilog;

namespace Game.Server.Transport;

/// <summary>
/// Transporte UDP com LiteNetLib (T-105). Canal 0 = confiável e ordenado; canal 1 = snapshots (não confiável, ou
/// confiável sem ordem quando o snapshot passa do tamanho de um pacote). Conexões são aceitas sem chave: quem entra
/// ou não decide o <c>Hello</c>.
/// </summary>
public sealed class LiteNetTransport(int port, ILogger log, int maxConnections = 16) : IServerTransport
{
    private const byte ReliableChannel = 0;
    private const byte SnapshotChannel = 1;

    private readonly EventBasedNetListener _listener = new();
    private readonly Dictionary<int, NetPeer> _peers = new();
    private NetManager? _net;
    private long _fragmentedSnapshots;

    /// <summary>Snapshots que passaram do tamanho de um pacote e foram como confiáveis (indicador para a T-106).</summary>
    public long FragmentedSnapshots => _fragmentedSnapshots;

    public void Start(ITransportHandler handler)
    {
        _listener.ConnectionRequestEvent += request =>
        {
            if (_peers.Count >= maxConnections)
            {
                request.Reject();
                return;
            }

            request.Accept();
        };
        _listener.PeerConnectedEvent += peer =>
        {
            _peers[peer.Id] = peer;
            handler.OnConnected(peer.Id);
        };
        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            if (_peers.Remove(peer.Id))
            {
                log.Information("Conexão {Peer} caiu: {Reason}", peer.Id, info.Reason);
                handler.OnDisconnected(peer.Id);
            }
        };
        _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
        {
            handler.OnReceived(peer.Id, reader.GetRemainingBytesSpan());
            reader.Recycle();
        };
        _listener.NetworkErrorEvent += (endPoint, error) => log.Warning("Erro de rede de {EndPoint}: {Error}", endPoint, error);

        _net = new NetManager(_listener)
        {
            ChannelsCount = 2,
            AutoRecycle = false,
            DisconnectTimeout = 10_000,
        };
        if (!_net.Start(port))
        {
            throw new InvalidOperationException($"Não foi possível abrir a porta UDP {port}.");
        }

        log.Information("Escutando UDP na porta {Port}", _net.LocalPort);
    }

    public void Poll() => _net?.PollEvents();

    public void Send(int peer, ReadOnlySpan<byte> data, SendMode mode)
    {
        if (!_peers.TryGetValue(peer, out var p))
        {
            return;
        }

        if (mode == SendMode.Reliable)
        {
            p.Send(data, ReliableChannel, DeliveryMethod.ReliableOrdered);
            return;
        }

        if (data.Length <= p.GetMaxSinglePacketSize(DeliveryMethod.Unreliable))
        {
            p.Send(data, SnapshotChannel, DeliveryMethod.Unreliable);
            return;
        }

        _fragmentedSnapshots++;
        p.Send(data, SnapshotChannel, DeliveryMethod.ReliableUnordered);
    }

    public void Disconnect(int peer)
    {
        if (_peers.TryGetValue(peer, out var p))
        {
            p.Disconnect();
        }
    }

    public void Dispose() => _net?.Stop();
}
