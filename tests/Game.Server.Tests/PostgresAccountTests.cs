using Game.Core.Combat.Items;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Persistence;
using Game.Server.Persistence;
using Npgsql;

namespace Game.Server.Tests;

/// <summary>Testes contra PostgreSQL real; pulados sem <c>MMO_TEST_DB</c> (veja <c>deploy/test-db.sh</c>).</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MMO_TEST_DB")))
        {
            Skip = "Sem PostgreSQL: defina MMO_TEST_DB (veja deploy/test-db.sh).";
        }
    }
}

public sealed class PostgresAccountTests : IAsyncLifetime
{
    private string _database = "";
    private GameRepository _repo = null!;

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("MMO_TEST_DB") is not { Length: > 0 } admin)
        {
            return;
        }

        _database = $"mmo_test_{Guid.NewGuid():N}";
        await using (var conn = new NpgsqlConnection(admin))
        {
            await conn.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", conn);
            await create.ExecuteNonQueryAsync();
        }

        var options = GameDatabase.Options(new NpgsqlConnectionStringBuilder(admin) { Database = _database, Pooling = false }.ConnectionString);
        await GameDatabase.MigrateAsync(options);
        _repo = new GameRepository(options);
    }

    public async Task DisposeAsync()
    {
        if (_database.Length == 0)
        {
            return;
        }

        await using var conn = new NpgsqlConnection(Environment.GetEnvironmentVariable("MMO_TEST_DB"));
        await conn.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", conn);
        await drop.ExecuteNonQueryAsync();
    }

    [PostgresFact]
    public async Task Character_created_by_the_api_plays_levels_up_and_comes_back_from_the_database()
    {
        var account = (await _repo.CreateAccountAsync("pg@exemplo.com", "hash"))!;
        var (_, row) = await _repo.CreateCharacterAsync(new NewCharacter(account.Id, "Persistente", "class.mage", "zone.city"));
        var ring = new ItemInstance("accessory.ring", 5, ItemRarity.Epic, ["affix.int"]);

        using (var a = new AccountRig(new RepositoryCharacterStore(_repo)))
        {
            var client = a.Join(AccountRig.Ticket(row!));
            var hero = a.Entity(client);
            Game.Core.Combat.Progression.Leveling.GrantExperience(a.Room.World, hero, 300, default, "test");
            hero.Get<Inventory>()[0] = ring;
            a.Rig.Tick(1, [client]);
            client.Raw.Leave();
            a.Rig.Tick(1);
            a.Flush();
        }

        var stored = (await _repo.LoadCharacterAsync(row!.Id))!;
        Assert.True(stored.Level >= 2);
        Assert.Equal(CharacterDocument.Version, stored.ProgressionVersion);
        Assert.Equal("zone.city", stored.ZoneId);

        using var b = new AccountRig(new RepositoryCharacterStore(_repo));
        var back = b.Entity(b.Join(AccountRig.Ticket(stored)));
        Assert.Equal(stored.Level, back.Get<CharacterStats>().Level);
        Assert.Equal(ring.BaseId, back.Get<Inventory>()[0]!.BaseId);
        Assert.Equal(["affix.int"], back.Get<Inventory>()[0]!.Affixes);
        Assert.Equal("weapon.staff", back.Get<Equipment>()[EquipSlot.Weapon]!.BaseId);
    }
}
