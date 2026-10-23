using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Game.Persistence;

public enum CreateCharacterStatus
{
    Created,
    NameTaken,
    AccountNotFound,
}

public enum SaveStatus
{
    Saved,

    /// <summary>Alguém salvou antes (a versão no banco é outra); recarregue e tente de novo.</summary>
    Conflict,
    NotFound,
}

/// <summary>Dados para criar um personagem (nível 1, XP 0; progressão inicial vazia se não informada).</summary>
public sealed record NewCharacter(Guid AccountId, string Name, string ClassId, string ZoneId, float PositionX = 0, float PositionY = 0)
{
    public int ProgressionVersion { get; init; } = 1;
    public string Progression { get; init; } = "{}";
}

/// <summary>
/// Operações de contas e personagens (T-210). Cada chamada usa seu próprio <see cref="GameDbContext"/>, então o
/// repositório pode ser compartilhado entre threads. As entidades devolvidas não são rastreadas.
/// </summary>
public sealed class GameRepository(DbContextOptions<GameDbContext> options, TimeProvider? time = null)
{
    private const string UniqueViolation = "23505";
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Cria a conta; null se o e-mail já existe.</summary>
    public async Task<Account?> CreateAccountAsync(string email, string passwordHash, CancellationToken ct = default)
    {
        var account = new Account { Id = Guid.NewGuid(), Email = NormalizeEmail(email), PasswordHash = passwordHash, CreatedAt = Now() };
        await using var db = Db();
        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(ct);
            return account;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return null;
        }
    }

    public async Task<Account?> FindAccountByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = NormalizeEmail(email);
        await using var db = Db();
        return await db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Email == normalized, ct);
    }

    /// <summary>Marca o último login; false se a conta não existe.</summary>
    public async Task<bool> RecordLoginAsync(Guid accountId, CancellationToken ct = default)
    {
        var now = Now();
        await using var db = Db();
        return await db.Accounts.Where(a => a.Id == accountId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.LastLoginAt, now), ct) == 1;
    }

    public async Task<(CreateCharacterStatus Status, Character? Character)> CreateCharacterAsync(NewCharacter request, CancellationToken ct = default)
    {
        var now = Now();
        var character = new Character
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            Name = request.Name.Trim(),
            NormalizedName = Character.Normalize(request.Name),
            ClassId = request.ClassId,
            ZoneId = request.ZoneId,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            ProgressionVersion = request.ProgressionVersion,
            Progression = request.Progression,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await using var db = Db();
        if (!await db.Accounts.AnyAsync(a => a.Id == request.AccountId, ct))
        {
            return (CreateCharacterStatus.AccountNotFound, null);
        }

        db.Characters.Add(character);
        try
        {
            await db.SaveChangesAsync(ct);
            return (CreateCharacterStatus.Created, character);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return (CreateCharacterStatus.NameTaken, null);
        }
    }

    /// <summary>Personagens da conta, do mais antigo para o mais novo.</summary>
    public async Task<IReadOnlyList<Character>> ListCharactersAsync(Guid accountId, CancellationToken ct = default)
    {
        await using var db = Db();
        return await db.Characters.AsNoTracking()
            .Where(c => c.AccountId == accountId)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.NormalizedName)
            .ToListAsync(ct);
    }

    public async Task<Character?> LoadCharacterAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var db = Db();
        return await db.Characters.AsNoTracking().SingleOrDefaultAsync(c => c.Id == characterId, ct);
    }

    /// <summary>Apaga o personagem se ele for da conta; false se não existe ou é de outra conta.</summary>
    public async Task<bool> DeleteCharacterAsync(Guid accountId, Guid characterId, CancellationToken ct = default)
    {
        await using var db = Db();
        return await db.Characters.Where(c => c.Id == characterId && c.AccountId == accountId).ExecuteDeleteAsync(ct) == 1;
    }

    /// <summary>
    /// Grava o estado do personagem se a <see cref="Character.Version"/> ainda for a do banco. Se der certo, a entidade recebe a
    /// versão nova e o novo <see cref="Character.UpdatedAt"/>. Conta e data de criação não mudam por aqui.
    /// </summary>
    public async Task<SaveStatus> SaveCharacterAsync(Character character, CancellationToken ct = default)
    {
        var previousUpdatedAt = character.UpdatedAt;
        character.NormalizedName = Character.Normalize(character.Name);
        character.UpdatedAt = Now();

        await using var db = Db();
        var entry = db.Characters.Attach(character);
        foreach (var p in entry.Properties)
        {
            p.IsModified = p.Metadata.Name is not (nameof(Character.Id) or nameof(Character.AccountId) or nameof(Character.CreatedAt) or nameof(Character.Version));
        }

        try
        {
            await db.SaveChangesAsync(ct);
            return SaveStatus.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            character.UpdatedAt = previousUpdatedAt;
            await using var check = Db();
            return await check.Characters.AnyAsync(c => c.Id == character.Id, ct) ? SaveStatus.Conflict : SaveStatus.NotFound;
        }
    }

    private GameDbContext Db() => new(options);

    // timestamptz guarda microssegundos; truncar aqui mantém a entidade devolvida igual à que volta do banco.
    private DateTimeOffset Now()
    {
        var now = _time.GetUtcNow();
        return now.AddTicks(-(now.Ticks % (TimeSpan.TicksPerMillisecond / 1000)));
    }

    private static bool IsUniqueViolation(DbUpdateException ex) => ex.InnerException is PostgresException { SqlState: UniqueViolation };
}
