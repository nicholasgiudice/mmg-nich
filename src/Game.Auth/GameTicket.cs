using System.Text.Json.Serialization;

namespace Game.Auth;

/// <summary>
/// Token de jogo (T-211): a API emite em <c>POST /characters/{id}/play</c> e o servidor de jogo valida sem chamar a API, com a
/// chave compartilhada <see cref="KeyEnv"/>. Vale para um personagem, um servidor e por pouco tempo; o uso único é garantido
/// pelo servidor com <see cref="GameTicketReplayGuard"/>.
/// </summary>
public sealed record GameTicket
{
    /// <summary>Variável de ambiente com a chave (base64, ≥ 32 bytes), a mesma na API e no servidor de jogo.</summary>
    public const string KeyEnv = "MMO_GAME_TICKET_KEY";

    internal const string Type = "game";

    [JsonPropertyName("typ")]
    public string Typ { get; init; } = Type;

    /// <summary>Identificador único do token (para o uso único).</summary>
    [JsonPropertyName("jti")]
    public Guid TicketId { get; init; }

    [JsonPropertyName("acc")]
    public Guid AccountId { get; init; }

    [JsonPropertyName("chr")]
    public Guid CharacterId { get; init; }

    /// <summary>Servidor de jogo para o qual o token vale.</summary>
    [JsonPropertyName("srv")]
    public string Server { get; init; } = "";

    /// <summary>Expiração em segundos Unix.</summary>
    [JsonPropertyName("exp")]
    public long ExpiresAtUnix { get; init; }

    [JsonIgnore]
    public DateTimeOffset ExpiresAt => DateTimeOffset.FromUnixTimeSeconds(ExpiresAtUnix);

    public static GameTicket Create(Guid accountId, Guid characterId, string server, DateTimeOffset expiresAt) => new()
    {
        TicketId = Guid.NewGuid(),
        AccountId = accountId,
        CharacterId = characterId,
        Server = server,
        ExpiresAtUnix = expiresAt.ToUnixTimeSeconds(),
    };

    public string Sign(byte[] key) => SignedToken.Sign(this, key);
}

public enum TicketCheck
{
    Valid,

    /// <summary>Malformado, assinatura errada ou de outro tipo.</summary>
    Invalid,
    Expired,
    WrongServer,

    /// <summary>Já foi usado (só com <see cref="GameTicketReplayGuard"/>).</summary>
    AlreadyUsed,
}

public static class GameTickets
{
    /// <summary>Valida assinatura, tipo, servidor e expiração (sem uso único: veja <see cref="GameTicketReplayGuard"/>).</summary>
    public static (TicketCheck Result, GameTicket? Ticket) Validate(string? token, byte[] key, string server, DateTimeOffset now)
    {
        var ticket = SignedToken.Read<GameTicket>(token, key);
        if (ticket is null || ticket.Typ != GameTicket.Type || ticket.TicketId == Guid.Empty)
        {
            return (TicketCheck.Invalid, null);
        }

        if (!string.Equals(ticket.Server, server, StringComparison.Ordinal))
        {
            return (TicketCheck.WrongServer, null);
        }

        return now >= ticket.ExpiresAt ? (TicketCheck.Expired, null) : (TicketCheck.Valid, ticket);
    }
}

/// <summary>
/// Uso único dos tokens de jogo, em memória no servidor de jogo: lembra cada token aceito até ele expirar. Seguro entre
/// threads. Basta um por processo; reiniciar o processo esquece os usados, mas eles expiram em segundos.
/// </summary>
public sealed class GameTicketReplayGuard(byte[] key, string server, TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly Dictionary<Guid, DateTimeOffset> _used = [];
    private readonly Lock _lock = new();

    /// <summary>Valida e consome o token; uma segunda chamada com o mesmo token devolve <see cref="TicketCheck.AlreadyUsed"/>.</summary>
    public (TicketCheck Result, GameTicket? Ticket) Consume(string? token)
    {
        var now = _time.GetUtcNow();
        var (result, ticket) = GameTickets.Validate(token, key, server, now);
        if (result != TicketCheck.Valid)
        {
            return (result, null);
        }

        lock (_lock)
        {
            foreach (var (id, expiresAt) in _used)
            {
                if (now >= expiresAt)
                {
                    _used.Remove(id);
                }
            }

            return _used.TryAdd(ticket!.TicketId, ticket.ExpiresAt) ? (TicketCheck.Valid, ticket) : (TicketCheck.AlreadyUsed, null);
        }
    }
}
