using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Game.Persistence.Tests;

/// <summary>
/// Testes que precisam de PostgreSQL real (T-210). Sem a variável <see cref="PostgresFixture.ConnectionEnv"/> são pulados
/// com a mensagem abaixo, para o <c>check.sh</c> passar onde não há banco. Na VPS: <c>deploy/test-db.sh</c>.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(PostgresFixture.AdminConnection))
        {
            Skip = $"Sem PostgreSQL: defina {PostgresFixture.ConnectionEnv} (veja deploy/test-db.sh).";
        }
    }
}

/// <summary>
/// Cria um banco descartável (<c>mmo_test_*</c>), aplica as migrations e apaga o banco no fim. A conexão de
/// <see cref="ConnectionEnv"/> precisa poder criar bancos (o usuário do contêiner de teste é superusuário).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string ConnectionEnv = "MMO_TEST_DB";

    public static string? AdminConnection => Environment.GetEnvironmentVariable(ConnectionEnv);

    private string _database = "";

    public DbContextOptions<GameDbContext> Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(AdminConnection))
        {
            return; // testes pulados
        }

        _database = $"mmo_test_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(AdminConnection))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        var cs = new NpgsqlConnectionStringBuilder(AdminConnection) { Database = _database, Pooling = false }.ConnectionString;
        Options = GameDatabase.Options(cs);
        await GameDatabase.MigrateAsync(Options);
    }

    public async Task DisposeAsync()
    {
        if (_database.Length == 0)
        {
            return;
        }

        await using var admin = new NpgsqlConnection(AdminConnection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
