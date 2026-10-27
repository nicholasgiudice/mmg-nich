using System.Security.Claims;
using Game.Auth;
using Game.Persistence;

namespace Game.Api;

public sealed record CredentialsRequest(string? Email, string? Password);

public sealed record AccountResponse(Guid Id, string Email);

public sealed record SessionResponse(string Token, DateTimeOffset ExpiresAt);

public sealed record NewCharacterRequest(string? Name, string? ClassId);

public sealed record CharacterSummary(Guid Id, string Name, string ClassId, int Level, string ZoneId)
{
    public static CharacterSummary Of(Character c) => new(c.Id, c.Name, c.ClassId, c.Level, c.ZoneId);
}

public sealed record PlayResponse(string Ticket, DateTimeOffset ExpiresAt, string Server, string? Address);

/// <summary>Classes que o jogador pode criar: as de <c>data/classes.json</c> com atributos (progressão).</summary>
public sealed class PlayableClasses(IReadOnlySet<string> ids)
{
    public IReadOnlySet<string> Ids { get; } = ids;

    public static PlayableClasses Load(string dataDir)
    {
        var db = Game.Data.GameDataLoader.LoadDirectory(dataDir).GetDatabaseOrThrow();
        return new(db.Classes.Values.Where(c => c.Attributes is not null).Select(c => c.Id).ToHashSet(StringComparer.Ordinal));
    }
}

/// <summary>
/// Endpoints (T-211). Erros seguem o ProblemDetails com um <c>code</c> estável para o cliente (T-220) escolher a mensagem;
/// o <c>title</c> já vem em português.
/// </summary>
public static class Endpoints
{
    public const string AuthRateLimit = "auth";

    public static void Map(WebApplication app)
    {
        app.MapGet("/", () => Results.Ok(new { service = "mmo-api", status = "ok" }));

        app.MapPost("/accounts", CreateAccount).RequireRateLimiting(AuthRateLimit);
        app.MapPost("/sessions", Login).RequireRateLimiting(AuthRateLimit);

        var characters = app.MapGroup("/characters").RequireAuthorization();
        characters.MapGet("/", ListCharacters);
        characters.MapPost("/", CreateCharacter);
        characters.MapDelete("/{id:guid}", DeleteCharacter);
        characters.MapPost("/{id:guid}/play", Play);
    }

    public static IResult Error(int status, string code, string title) =>
        Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });

    private static async Task<IResult> CreateAccount(CredentialsRequest req, GameRepository repo, Passwords passwords, CancellationToken ct)
    {
        if (!Validation.IsValidEmail(req.Email))
        {
            return Error(400, "email_invalid", "E-mail inválido.");
        }

        if (!Validation.IsValidPassword(req.Password))
        {
            return Error(400, "password_invalid", $"A senha precisa ter de {Validation.MinPassword} a {Validation.MaxPassword} caracteres.");
        }

        var account = await repo.CreateAccountAsync(req.Email!, passwords.Hash(req.Password!), ct);
        return account is null
            ? Error(409, "email_taken", "Já existe uma conta com este e-mail.")
            : Results.Created($"/accounts/{account.Id}", new AccountResponse(account.Id, account.Email));
    }

    private static async Task<IResult> Login(CredentialsRequest req, GameRepository repo, Passwords passwords, ApiOptions options, TimeProvider time, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password) || req.Password.Length > Validation.MaxPassword)
        {
            return Error(401, "invalid_credentials", "E-mail ou senha incorretos.");
        }

        var account = await repo.FindAccountByEmailAsync(req.Email, ct);
        if (!passwords.Verify(account?.PasswordHash, req.Password) || account is null)
        {
            return Error(401, "invalid_credentials", "E-mail ou senha incorretos.");
        }

        await repo.RecordLoginAsync(account.Id, ct);
        var expiresAt = time.GetUtcNow() + options.SessionLifetime;
        var token = SignedToken.Sign(new SessionToken { AccountId = account.Id, ExpiresAtUnix = expiresAt.ToUnixTimeSeconds() }, options.SessionKey);
        return Results.Ok(new SessionResponse(token, DateTimeOffset.FromUnixTimeSeconds(expiresAt.ToUnixTimeSeconds())));
    }

    private static async Task<IResult> ListCharacters(ClaimsPrincipal user, GameRepository repo, CancellationToken ct)
    {
        var list = await repo.ListCharactersAsync(user.AccountId(), ct);
        return Results.Ok(list.Select(CharacterSummary.Of));
    }

    private static async Task<IResult> CreateCharacter(NewCharacterRequest req, ClaimsPrincipal user, GameRepository repo, PlayableClasses classes, ApiOptions options, CancellationToken ct)
    {
        if (Validation.NormalizeName(req.Name) is not { } name)
        {
            return Error(400, "name_invalid", $"O nome precisa ter de {Validation.MinName} a {Validation.MaxName} letras ou números e começar por letra.");
        }

        if (req.ClassId is null || !classes.Ids.Contains(req.ClassId))
        {
            return Error(400, "class_invalid", "Classe inexistente.");
        }

        var accountId = user.AccountId();
        if ((await repo.ListCharactersAsync(accountId, ct)).Count >= options.MaxCharactersPerAccount)
        {
            return Error(409, "character_limit", $"Limite de {options.MaxCharactersPerAccount} personagens por conta.");
        }

        // Progressão "{}" = personagem novo: nível 1, nada distribuído e o equipamento inicial da classe (quem carrega, T-212,
        // cria com Leveling.SpawnCharacter sem build). Posição (0, 0) = ponto de nascimento da zona (T-214).
        var (status, character) = await repo.CreateCharacterAsync(new NewCharacter(accountId, name, req.ClassId, options.StartZone), ct);
        return status switch
        {
            CreateCharacterStatus.Created => Results.Created($"/characters/{character!.Id}", CharacterSummary.Of(character)),
            CreateCharacterStatus.NameTaken => Error(409, "name_taken", "Este nome já está em uso."),
            _ => Error(401, "account_not_found", "Conta não encontrada."),
        };
    }

    private static async Task<IResult> DeleteCharacter(Guid id, ClaimsPrincipal user, GameRepository repo, CancellationToken ct) =>
        await repo.DeleteCharacterAsync(user.AccountId(), id, ct)
            ? Results.NoContent()
            : Error(404, "character_not_found", "Personagem não encontrado.");

    private static async Task<IResult> Play(Guid id, ClaimsPrincipal user, GameRepository repo, ApiOptions options, TimeProvider time, CancellationToken ct)
    {
        var character = await repo.LoadCharacterAsync(id, ct);
        if (character is null || character.AccountId != user.AccountId())
        {
            return Error(404, "character_not_found", "Personagem não encontrado.");
        }

        var ticket = GameTicket.Create(character.AccountId, character.Id, options.GameServerId, time.GetUtcNow() + options.TicketLifetime);
        return Results.Ok(new PlayResponse(ticket.Sign(options.TicketKey), ticket.ExpiresAt, options.GameServerId, options.GameServerAddress));
    }
}
