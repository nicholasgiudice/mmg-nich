using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Game.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Game.Api;

/// <summary>Token de sessão (T-211): emitido no login, curto, enviado como <c>Authorization: Bearer</c>. Só a API valida.</summary>
public sealed record SessionToken
{
    internal const string Type = "session";

    [JsonPropertyName("typ")]
    public string Typ { get; init; } = Type;

    [JsonPropertyName("acc")]
    public Guid AccountId { get; init; }

    [JsonPropertyName("exp")]
    public long ExpiresAtUnix { get; init; }

    [JsonIgnore]
    public DateTimeOffset ExpiresAt => DateTimeOffset.FromUnixTimeSeconds(ExpiresAtUnix);

    /// <summary>Conta do token, se a assinatura confere e não expirou.</summary>
    public static Guid? Validate(string? token, byte[] key, DateTimeOffset now) =>
        SignedToken.Read<SessionToken>(token, key) is { Typ: Type } s && s.AccountId != Guid.Empty && now < s.ExpiresAt ? s.AccountId : null;
}

/// <summary>Autenticação pelo token de sessão. A conta autenticada fica em <see cref="ClaimTypes.NameIdentifier"/>.</summary>
public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiOptions api,
    TimeProvider time) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Session";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (SessionToken.Validate(header["Bearer ".Length..].Trim(), api.SessionKey, time.GetUtcNow()) is not { } accountId)
        {
            return Task.FromResult(AuthenticateResult.Fail("Sessão inválida ou expirada."));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, accountId.ToString())], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

public static class SessionPrincipal
{
    public static Guid AccountId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
