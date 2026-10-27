using System.Globalization;
using Game.Auth;

namespace Game.Api;

/// <summary>
/// Configuração da API (T-211), lida das variáveis de ambiente (ou de qualquer fonte do <see cref="IConfiguration"/>).
/// Chaves e conexão não têm padrão: sem elas a API não sobe.
/// </summary>
public sealed record ApiOptions
{
    public const string SessionKeyEnv = "MMO_SESSION_KEY";
    public const string GameServerIdEnv = "MMO_GAME_SERVER_ID";
    public const string GameServerAddressEnv = "MMO_GAME_SERVER_ADDRESS";
    public const string AuthRateLimitEnv = "MMO_AUTH_RATE_LIMIT";
    public const string DataDirEnv = "MMO_DATA_DIR";

    public required string ConnectionString { get; init; }
    public required string DataDir { get; init; }

    /// <summary>Assina os tokens de sessão; só a API conhece.</summary>
    public required byte[] SessionKey { get; init; }

    /// <summary>Assina os tokens de jogo; compartilhada com o servidor de jogo (<see cref="GameTicket.KeyEnv"/>).</summary>
    public required byte[] TicketKey { get; init; }

    public TimeSpan SessionLifetime { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan TicketLifetime { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>Servidor de jogo para o qual os tokens de jogo valem.</summary>
    public string GameServerId { get; init; } = "game.dev";

    /// <summary>Endereço que o cliente usa para conectar (host:porta); null se o cliente já sabe.</summary>
    public string? GameServerAddress { get; init; }

    /// <summary>Zona onde o personagem novo nasce (a Cidade da T-214).</summary>
    public string StartZone { get; init; } = "zone.city";

    public int MaxCharactersPerAccount { get; init; } = 5;

    /// <summary>Cadastros e logins por IP por minuto.</summary>
    public int AuthRequestsPerMinute { get; init; } = 10;

    public static ApiOptions FromConfiguration(IConfiguration c) => new()
    {
        ConnectionString = c[Persistence.GameDatabase.ConnectionEnv] is { Length: > 0 } cs
            ? cs
            : throw new InvalidOperationException($"Defina {Persistence.GameDatabase.ConnectionEnv} (string de conexão do PostgreSQL)."),
        DataDir = c[DataDirEnv] is { Length: > 0 } dir ? dir : DefaultDataDir(),
        SessionKey = SignedToken.KeyFromBase64(c[SessionKeyEnv], SessionKeyEnv),
        TicketKey = SignedToken.KeyFromBase64(c[GameTicket.KeyEnv], GameTicket.KeyEnv),
        GameServerId = c[GameServerIdEnv] is { Length: > 0 } server ? server : "game.dev",
        GameServerAddress = c[GameServerAddressEnv] is { Length: > 0 } address ? address : null,
        AuthRequestsPerMinute = c[AuthRateLimitEnv] is { Length: > 0 } limit ? int.Parse(limit, CultureInfo.InvariantCulture) : 10,
    };

    /// <summary>data/ do repositório: sobe a partir do executável até achar <c>MmoRpg.sln</c>; senão <c>./data</c> (contêiner).</summary>
    private static string DefaultDataDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
            {
                return Path.Combine(dir.FullName, "data");
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "data");
    }
}
