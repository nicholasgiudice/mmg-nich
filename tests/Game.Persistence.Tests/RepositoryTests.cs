using Microsoft.EntityFrameworkCore;

namespace Game.Persistence.Tests;

/// <summary>T-210: contas e personagens contra PostgreSQL real (pulados sem <c>MMO_TEST_DB</c>).</summary>
public class RepositoryTests(PostgresFixture db) : IClassFixture<PostgresFixture>
{
    private GameRepository Repo() => new(db.Options);

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private async Task<Account> NewAccount(GameRepository repo) =>
        (await repo.CreateAccountAsync($"{Unique("p")}@Example.com ", "hash"))!;

    [PostgresFact]
    public async Task Migrations_create_the_tables()
    {
        await using var ctx = new GameDbContext(db.Options);
        Assert.Empty(await ctx.Database.GetPendingMigrationsAsync());
        Assert.Contains("20261023020840_InitialCreate", await ctx.Database.GetAppliedMigrationsAsync());
    }

    [PostgresFact]
    public async Task Account_email_is_normalized_and_unique()
    {
        var repo = Repo();
        var email = $"{Unique("Ana")}@Example.com";
        var account = await repo.CreateAccountAsync($"  {email} ", "hash-1");

        Assert.NotNull(account);
        Assert.Equal(email.ToLowerInvariant(), account.Email);
        Assert.Null(await repo.CreateAccountAsync(email.ToUpperInvariant(), "hash-2"));

        var found = await repo.FindAccountByEmailAsync(email);
        Assert.Equal(account.Id, found!.Id);
        Assert.Equal("hash-1", found.PasswordHash);
        Assert.Null(found.LastLoginAt);

        Assert.True(await repo.RecordLoginAsync(account.Id));
        Assert.NotNull((await repo.FindAccountByEmailAsync(email))!.LastLoginAt);
        Assert.False(await repo.RecordLoginAsync(Guid.NewGuid()));
    }

    [PostgresFact]
    public async Task Characters_are_created_listed_and_loaded_with_progression_json()
    {
        var repo = Repo();
        var account = await NewAccount(repo);
        var name = Unique("Aria");

        var (status, created) = await repo.CreateCharacterAsync(new NewCharacter(account.Id, $" {name} ", "class.mage", "zone.town", 3.5f, 7)
        {
            ProgressionVersion = 2,
            Progression = """{"ranks": {"mage.fireball": 2}, "bag": []}""",
        });
        Assert.Equal(CreateCharacterStatus.Created, status);
        await repo.CreateCharacterAsync(new NewCharacter(account.Id, Unique("Bram"), "class.warrior", "zone.town"));

        var list = await repo.ListCharactersAsync(account.Id);
        Assert.Equal(2, list.Count);
        Assert.Equal(name, list[0].Name);

        var loaded = (await repo.LoadCharacterAsync(created!.Id))!;
        Assert.Equal(name, loaded.Name);
        Assert.Equal("class.mage", loaded.ClassId);
        Assert.Equal(1, loaded.Level);
        Assert.Equal(0, loaded.Experience);
        Assert.Equal((3.5f, 7f), (loaded.PositionX, loaded.PositionY));
        Assert.Equal(2, loaded.ProgressionVersion);
        Assert.Contains("\"mage.fireball\": 2", loaded.Progression); // jsonb normaliza espaços
        Assert.NotEqual(0u, loaded.Version);
        Assert.Null(await repo.LoadCharacterAsync(Guid.NewGuid()));
    }

    [PostgresFact]
    public async Task Character_names_are_unique_ignoring_case_and_need_an_account()
    {
        var repo = Repo();
        var a = await NewAccount(repo);
        var b = await NewAccount(repo);
        var name = Unique("Lia");

        Assert.Equal(CreateCharacterStatus.Created, (await repo.CreateCharacterAsync(new NewCharacter(a.Id, name, "class.cleric", "zone.town"))).Status);
        Assert.Equal(CreateCharacterStatus.NameTaken, (await repo.CreateCharacterAsync(new NewCharacter(b.Id, name.ToUpperInvariant(), "class.archer", "zone.town"))).Status);
        Assert.Equal(CreateCharacterStatus.AccountNotFound, (await repo.CreateCharacterAsync(new NewCharacter(Guid.NewGuid(), Unique("X"), "class.archer", "zone.town"))).Status);
    }

    [PostgresFact]
    public async Task Delete_only_removes_a_character_of_the_same_account()
    {
        var repo = Repo();
        var owner = await NewAccount(repo);
        var other = await NewAccount(repo);
        var (_, c) = await repo.CreateCharacterAsync(new NewCharacter(owner.Id, Unique("Tor"), "class.warrior", "zone.town"));

        Assert.False(await repo.DeleteCharacterAsync(other.Id, c!.Id));
        Assert.True(await repo.DeleteCharacterAsync(owner.Id, c.Id));
        Assert.False(await repo.DeleteCharacterAsync(owner.Id, c.Id));
        Assert.Empty(await repo.ListCharactersAsync(owner.Id));
    }

    [PostgresFact]
    public async Task Save_checks_the_version_and_reports_conflicts()
    {
        var repo = Repo();
        var account = await NewAccount(repo);
        var (_, created) = await repo.CreateCharacterAsync(new NewCharacter(account.Id, Unique("Kai"), "class.archer", "zone.town"));

        var first = (await repo.LoadCharacterAsync(created!.Id))!;
        var second = (await repo.LoadCharacterAsync(created.Id))!;

        first.Level = 2;
        first.Experience = 150;
        first.ZoneId = "zone.fields";
        first.Progression = """{"points": 5}""";
        var before = first.Version;
        Assert.Equal(SaveStatus.Saved, await repo.SaveCharacterAsync(first));
        Assert.NotEqual(before, first.Version);

        second.Level = 9;
        Assert.Equal(SaveStatus.Conflict, await repo.SaveCharacterAsync(second));

        var stored = (await repo.LoadCharacterAsync(created.Id))!;
        Assert.Equal(2, stored.Level);
        Assert.Equal(150, stored.Experience);
        Assert.Equal("zone.fields", stored.ZoneId);
        Assert.Equal(first.Version, stored.Version);
        Assert.Equal(created.CreatedAt, stored.CreatedAt);
        Assert.True(stored.UpdatedAt >= stored.CreatedAt);

        stored.Level = 3; // salvar de novo com a versão nova funciona
        Assert.Equal(SaveStatus.Saved, await repo.SaveCharacterAsync(stored));

        await repo.DeleteCharacterAsync(account.Id, created.Id);
        Assert.Equal(SaveStatus.NotFound, await repo.SaveCharacterAsync(stored));
    }

    [PostgresFact]
    public async Task Deleting_an_account_removes_its_characters()
    {
        var repo = Repo();
        var account = await NewAccount(repo);
        var (_, c) = await repo.CreateCharacterAsync(new NewCharacter(account.Id, Unique("Ivo"), "class.mage", "zone.town"));

        await using (var ctx = new GameDbContext(db.Options))
        {
            await ctx.Accounts.Where(a => a.Id == account.Id).ExecuteDeleteAsync();
        }

        Assert.Null(await repo.LoadCharacterAsync(c!.Id));
    }
}
