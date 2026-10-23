using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Game.Persistence;

/// <summary>Configuração e migrations do banco (T-210).</summary>
public static class GameDatabase
{
    /// <summary>Variável de ambiente com a string de conexão (Npgsql), usada pelo deploy e pelas ferramentas do EF.</summary>
    public const string ConnectionEnv = "MMO_DB_CONNECTION";

    public static DbContextOptions<GameDbContext> Options(string connectionString) =>
        new DbContextOptionsBuilder<GameDbContext>().UseNpgsql(connectionString).Options;

    /// <summary>Aplica as migrations pendentes (cria o banco se não existir).</summary>
    public static async Task MigrateAsync(DbContextOptions<GameDbContext> options, CancellationToken ct = default)
    {
        await using var db = new GameDbContext(options);
        await db.Database.MigrateAsync(ct);
    }
}

/// <summary>
/// Usado por <c>dotnet ef</c> (migrations e bundle). Lê <see cref="GameDatabase.ConnectionEnv"/>; sem ela, usa uma conexão
/// local que só serve para gerar migrations (gerar não conecta).
/// </summary>
public sealed class DesignTimeGameDbContextFactory : IDesignTimeDbContextFactory<GameDbContext>
{
    public GameDbContext CreateDbContext(string[] args) =>
        new(GameDatabase.Options(Environment.GetEnvironmentVariable(GameDatabase.ConnectionEnv) is { Length: > 0 } cs
            ? cs
            : "Host=localhost;Database=mmo;Username=mmo"));
}
