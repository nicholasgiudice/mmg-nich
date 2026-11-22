using Game.Persistence;
using Serilog;

namespace Game.Server.Persistence;

/// <summary>
/// Relata ao banco quantos jogadores o servidor tem (contador de online da tela de login, lido pela API em <c>GET /status</c>).
/// O loop chama <see cref="Report"/> de tempos em tempos e nunca espera o banco: o relato roda no thread pool e, se o anterior
/// ainda não acabou, o novo é pulado (o próximo leva o número atualizado). Falha do banco só vira aviso no log; a API ignora
/// relatos velhos, então um servidor sem relatar some do contador sozinho.
/// </summary>
public sealed class ServerStatusReporter(string serverId, Func<string, int, CancellationToken, Task> write, ILogger log)
{
    /// <summary>Intervalo entre relatos; a API descarta relatos com mais de 60 s (<c>ApiOptions.ServerStatusMaxAge</c>).</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private Task _inFlight = Task.CompletedTask;
    private bool _failing;

    public ServerStatusReporter(string serverId, GameRepository repository, ILogger log)
        : this(serverId, repository.ReportServerStatusAsync, log)
    {
    }

    public string ServerId { get; } = serverId;

    /// <summary>Começa um relato com <paramref name="onlinePlayers"/>; false se o anterior ainda está em andamento.</summary>
    public bool Report(int onlinePlayers)
    {
        if (!_inFlight.IsCompleted)
        {
            return false;
        }

        _inFlight = Task.Run(() => WriteAsync(onlinePlayers));
        return true;
    }

    /// <summary>Parada do servidor: espera o relato em andamento e grava 0 jogadores, com prazo.</summary>
    public bool ReportStopped(TimeSpan timeout)
    {
        try
        {
            return _inFlight.ContinueWith(_ => WriteAsync(0), TaskScheduler.Default).Unwrap().Wait(timeout);
        }
        catch (AggregateException)
        {
            return false;
        }
    }

    private async Task WriteAsync(int onlinePlayers)
    {
        try
        {
            await write(ServerId, onlinePlayers, CancellationToken.None);
            if (_failing)
            {
                _failing = false;
                log.Information("Contador de online voltou a gravar ({Players} jogador(es))", onlinePlayers);
            }
        }
        catch (Exception ex)
        {
            // Só avisa na primeira falha seguida: com o banco fora, o aviso se repetiria a cada relato.
            if (!_failing)
            {
                _failing = true;
                log.Warning("Falha ao gravar o contador de online: {Error}", ex.Message);
            }
        }
    }
}
