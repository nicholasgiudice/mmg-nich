using Game.Persistence;

namespace Game.Api;

/// <summary>Resposta de <c>GET /status</c>: jogadores online agora, somando os servidores de jogo que relataram há pouco.</summary>
public sealed record StatusResponse(int Online);

/// <summary>
/// Contador de jogadores online da tela de login. Lê <c>server_status</c> (cada servidor de jogo grava o seu número de tempos
/// em tempos) e guarda o resultado por <see cref="ApiOptions.OnlineCacheLifetime"/>: o pedido é público e sem login, então
/// muitos clientes abertos na tela de login não viram uma consulta ao banco cada um.
/// </summary>
public sealed class OnlineCounter(GameRepository repo, ApiOptions options, TimeProvider time)
{
    private readonly Lock _lock = new();
    private (int Online, DateTimeOffset At)? _cached;

    public async Task<int> CountAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        lock (_lock)
        {
            if (_cached is { } c && now - c.At < options.OnlineCacheLifetime)
            {
                return c.Online;
            }
        }

        var online = await repo.CountOnlinePlayersAsync(now - options.ServerStatusMaxAge, ct);
        lock (_lock)
        {
            _cached = (online, now);
        }

        return online;
    }
}
