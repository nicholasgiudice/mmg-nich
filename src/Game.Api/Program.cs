using System.Threading.RateLimiting;
using Game.Api;
using Game.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;

// API de contas e personagens (T-211). Configuração por variáveis de ambiente: veja ApiOptions e o README.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp => ApiOptions.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new GameRepository(
    Game.Persistence.GameDatabase.Options(sp.GetRequiredService<ApiOptions>().ConnectionString), sp.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton(sp => PlayableClasses.Load(sp.GetRequiredService<ApiOptions>().DataDir));
builder.Services.AddSingleton<Passwords>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Endpoints.AuthRateLimit, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = ctx.RequestServices.GetRequiredService<ApiOptions>().AuthRequestsPerMinute,
            Window = TimeSpan.FromMinutes(1),
        }));
});

// A API só é alcançável pela rede interna do Compose (Caddy, T-213): o IP do cliente vem no X-Forwarded-For.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Falha na subida (e não no primeiro pedido) se falta configuração ou os dados de jogo são inválidos.
app.Services.GetRequiredService<ApiOptions>();
app.Services.GetRequiredService<PlayableClasses>();

app.UseForwardedHeaders();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
Endpoints.Map(app);

app.Run();

public partial class Program;
