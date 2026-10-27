using System.Net.Http.Headers;
using System.Net.Http.Json;
using Game.Auth;
using Game.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Game.Api.Tests;

/// <summary>Relógio controlado pelos testes (expiração de sessão e de token de jogo).</summary>
public sealed class ManualTime(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

/// <summary>
/// A API em memória (<see cref="WebApplicationFactory{TEntryPoint}"/>) com chaves de teste. Sem banco, a conexão aponta para
/// um endereço que nunca é usado (só os testes que não tocam o banco usam essa forma).
/// </summary>
public sealed class ApiFactory(string? connectionString = null, int authRequestsPerMinute = 1000) : WebApplicationFactory<Program>
{
    public static readonly byte[] SessionKey = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
    public static readonly byte[] TicketKey = Enumerable.Range(101, 32).Select(i => (byte)i).ToArray();
    public const string GameServer = "game.test";

    public ManualTime Time { get; } = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(GameDatabase.ConnectionEnv, connectionString ?? "Host=127.0.0.1;Port=1;Database=unused");
        builder.UseSetting(ApiOptions.SessionKeyEnv, Convert.ToBase64String(SessionKey));
        builder.UseSetting(GameTicket.KeyEnv, Convert.ToBase64String(TicketKey));
        builder.UseSetting(ApiOptions.GameServerIdEnv, GameServer);
        builder.UseSetting(ApiOptions.GameServerAddressEnv, "127.0.0.1:7777");
        builder.UseSetting(ApiOptions.AuthRateLimitEnv, authRequestsPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(Time));
    }

    public async Task<HttpClient> LoggedInAsync(string email, string password = "senha-forte-1")
    {
        var http = CreateClient();
        (await http.PostAsJsonAsync("/accounts", new { email, password })).EnsureSuccessStatusCode();
        var login = await http.PostAsJsonAsync("/sessions", new { email, password });
        login.EnsureSuccessStatusCode();
        var session = (await login.Content.ReadFromJsonAsync<SessionResponse>())!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return http;
    }
}

/// <summary>Testes que precisam de PostgreSQL real; pulados sem <see cref="ApiDatabase.ConnectionEnv"/> (como em Game.Persistence.Tests).</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(ApiDatabase.AdminConnection))
        {
            Skip = $"Sem PostgreSQL: defina {ApiDatabase.ConnectionEnv} (veja deploy/test-db.sh).";
        }
    }
}

/// <summary>Banco descartável <c>mmo_test_*</c> com as migrations e a API apontando para ele; apagado no fim.</summary>
public sealed class ApiDatabase : IAsyncLifetime
{
    public const string ConnectionEnv = "MMO_TEST_DB";

    public static string? AdminConnection => Environment.GetEnvironmentVariable(ConnectionEnv);

    private string _database = "";

    public string ConnectionString { get; private set; } = "";

    public ApiFactory Api { get; private set; } = null!;

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

        ConnectionString = new NpgsqlConnectionStringBuilder(AdminConnection) { Database = _database, Pooling = false }.ConnectionString;
        await GameDatabase.MigrateAsync(GameDatabase.Options(ConnectionString));
        Api = new ApiFactory(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (_database.Length == 0)
        {
            return;
        }

        await Api.DisposeAsync();
        await using var admin = new NpgsqlConnection(AdminConnection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
