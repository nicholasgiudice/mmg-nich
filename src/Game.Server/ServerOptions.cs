using System.Globalization;
using Game.Core.Combat;
using Game.Data;
using Serilog.Events;

namespace Game.Server;

/// <summary>Configuração do servidor de zona (T-105). Argumentos <c>--chave valor</c>; algumas também por variável de ambiente.</summary>
public sealed record ServerOptions
{
    public const string Usage = """
        Servidor de zona (T-105).

        Uso: dotnet run --project src/Game.Server -- [opções]
          --data <dir>              pasta com os JSON (padrão: data/ do repositório; env MMO_DATA_DIR)
          --port <n>                porta UDP (padrão 7777; env MMO_PORT)
          --encounter <id>          encontro da sala (padrão encounter.forest_matriarch)
          --seed <n>                seed do encontro (padrão 1; cada reinício usa a próxima)
          --bots <true|false>       vagas sem jogador jogam como bots (padrão true)
          --disconnect-grace <s>    segundos até remover a entidade de quem caiu (padrão 30)
          --max-catch-up <n>        ticks recuperados de uma vez quando o servidor atrasa (padrão 5)
          --max-players <n>         conexões aceitas (padrão 8; vagas do encontro limitam quem joga)
          --restart-after <s>       segundos mostrando o fim do encontro antes de recomeçar (padrão 10; 0 = não recomeça)
          --interest-cell <tiles>   tamanho da célula da grade de interesse (padrão 16)
          --interest-radius <n>     células em volta do jogador que ele vê (padrão 2)
          --max-delta-age <ticks>   base de delta mais velha aceita; além disso vai snapshot completo (padrão 40)
          --log-level <nível>       verbose · debug · info · warning · error · fatal (padrão info; env MMO_LOG_LEVEL)

        Console: restart · status · quit
        """;

    public string DataDir { get; init; } = DefaultDataDir();
    public int Port { get; init; } = 7777;
    public string EncounterId { get; init; } = "encounter.forest_matriarch";
    public ulong Seed { get; init; } = 1;
    public bool FillWithBots { get; init; } = true;
    public double DisconnectGraceSeconds { get; init; } = 30;
    public int MaxCatchUpTicks { get; init; } = 5;
    public int MaxConnections { get; init; } = 8;

    /// <summary>Segundos mostrando o fim do encontro (vitória ou derrota) antes de recomeçar; 0 = fica no fim.</summary>
    public double RestartAfterSeconds { get; init; } = 10;

    /// <summary>Grade de interesse (T-106): célula em tiles e raio em células.</summary>
    public float InterestCellSize { get; init; } = 16;
    public int InterestRadiusCells { get; init; } = 2;

    /// <summary>Base de delta mais velha aceita (ticks; ≤ <c>SnapshotReconstructor.History</c>). Mais velha = snapshot completo.</summary>
    public int MaxDeltaAgeTicks { get; init; } = 40;

    /// <summary>Nível mínimo do log do servidor (T-207).</summary>
    public LogEventLevel LogLevel { get; init; } = LogEventLevel.Information;

    /// <summary>Estratégia dos bots nas vagas livres (a mesma dos companheiros do cliente: grupo que se move).</summary>
    public string BotStrategy { get; init; } = "kite+dodge";

    public static ServerOptions Parse(IReadOnlyList<string> args, Func<string, string?>? env = null)
    {
        env ??= Environment.GetEnvironmentVariable;
        var o = new ServerOptions();
        if (env("MMO_DATA_DIR") is { Length: > 0 } dataEnv)
        {
            o = o with { DataDir = dataEnv };
        }

        if (env("MMO_PORT") is { Length: > 0 } portEnv)
        {
            o = o with { Port = int.Parse(portEnv, CultureInfo.InvariantCulture) };
        }

        if (env("MMO_LOG_LEVEL") is { Length: > 0 } levelEnv)
        {
            o = o with { LogLevel = ParseLogLevel(levelEnv) };
        }

        for (var i = 0; i < args.Count; i++)
        {
            string Next() => i + 1 < args.Count ? args[++i] : throw new ArgumentException($"Falta o valor de {args[i]}.");
            o = args[i] switch
            {
                "--data" => o with { DataDir = Next() },
                "--port" => o with { Port = int.Parse(Next(), CultureInfo.InvariantCulture) },
                "--encounter" => o with { EncounterId = Next() },
                "--seed" => o with { Seed = ulong.Parse(Next(), CultureInfo.InvariantCulture) },
                "--bots" => o with { FillWithBots = bool.Parse(Next()) },
                "--disconnect-grace" => o with { DisconnectGraceSeconds = double.Parse(Next(), CultureInfo.InvariantCulture) },
                "--max-catch-up" => o with { MaxCatchUpTicks = int.Parse(Next(), CultureInfo.InvariantCulture) },
                "--max-players" => o with { MaxConnections = int.Parse(Next(), CultureInfo.InvariantCulture) },
                "--restart-after" => o with { RestartAfterSeconds = double.Parse(Next(), CultureInfo.InvariantCulture) },
                "--interest-cell" => o with { InterestCellSize = float.Parse(Next(), CultureInfo.InvariantCulture) },
                "--interest-radius" => o with { InterestRadiusCells = int.Parse(Next(), CultureInfo.InvariantCulture) },
                "--max-delta-age" => o with { MaxDeltaAgeTicks = Math.Clamp(int.Parse(Next(), CultureInfo.InvariantCulture), 1, Game.Protocol.SnapshotReconstructor.History) },
                "--log-level" => o with { LogLevel = ParseLogLevel(Next()) },
                _ => throw new ArgumentException($"Opção desconhecida: {args[i]}"),
            };
        }

        return o;
    }

    /// <summary>Nome do nível (Serilog, sem diferenciar maiúsculas) ou apelido curto: info, warn.</summary>
    public static LogEventLevel ParseLogLevel(string value) => value.Trim().ToLowerInvariant() switch
    {
        "info" => LogEventLevel.Information,
        "warn" => LogEventLevel.Warning,
        var v when Enum.TryParse<LogEventLevel>(v, ignoreCase: true, out var level) && Enum.IsDefined(level) && !int.TryParse(v, out _) => level,
        _ => throw new ArgumentException($"Nível de log inválido: {value}. Use verbose, debug, info, warning, error ou fatal."),
    };

    /// <summary>Carrega <c>data/</c> como o simulador e o cliente fazem (mesmo fingerprint).</summary>
    public ContentCatalog LoadContent()
    {
        var db = GameDataLoader.LoadDirectory(DataDir).GetDatabaseOrThrow();
        return new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values, db.Items.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
            Fingerprint = db.Fingerprint,
        };
    }

    /// <summary>data/ do repositório: sobe a partir do executável até achar <c>MmoRpg.sln</c>; senão <c>./data</c> (contêiner).</summary>
    private static string DefaultDataDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
            {
                return Path.Combine(dir.FullName, "data");
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "data");
    }
}
