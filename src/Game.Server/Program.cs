using System.Collections.Concurrent;
using System.Diagnostics;
using Game.Protocol;
using Game.Server;
using Game.Server.Transport;
using Serilog;

Console.OutputEncoding = System.Text.Encoding.UTF8; // acentos no console do Windows

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateLogger();

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine(ServerOptions.Usage);
    return 0;
}

try
{
    var options = ServerOptions.Parse(args);
    var content = options.LoadContent();
    Log.Information("Servidor de zona — protocolo v{Version}, conteúdo {Fingerprint}, dados em {Data}", ProtocolVersion.Current, content.Fingerprint, options.DataDir);

    using var transport = new LiteNetTransport(options.Port, Log.Logger, options.MaxConnections);
    var room = new ZoneRoom(content, options, transport, Log.Logger);
    transport.Start(room);
    Log.Information("Sala: {Encounter} ({Slots} vagas, bots {Bots})", options.EncounterId, room.Encounter.Players.Count, options.FillWithBots);

    // Console: comandos lidos em outra thread e executados na thread do loop (a sala não é thread-safe).
    var commands = new ConcurrentQueue<string>();
    using var stop = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        stop.Cancel();
    };
    var input = new Thread(() =>
    {
        while (!stop.IsCancellationRequested && Console.ReadLine() is { } line)
        {
            commands.Enqueue(line.Trim().ToLowerInvariant());
        }
    })
    { IsBackground = true, Name = "console" };
    input.Start();

    var stats = new TickStats();
    var tickWatch = new Stopwatch();
    var loop = new TickLoop(Stopwatch.GetTimestamp, Stopwatch.Frequency, Game.Core.Simulation.World.TickRate, options.MaxCatchUpTicks,
        () =>
        {
            tickWatch.Restart();
            room.Step();
            stats.Add(tickWatch.Elapsed.TotalMilliseconds);
        },
        Log.Logger);
    var lastReport = Stopwatch.GetTimestamp();

    loop.Run(
        () =>
        {
            transport.Poll();
            while (commands.TryDequeue(out var cmd))
            {
                switch (cmd)
                {
                    case "restart":
                        room.Restart("comando do console");
                        break;
                    case "status":
                        Log.Information("{Status}", room.Status());
                        break;
                    case "quit" or "exit":
                        stop.Cancel();
                        break;
                    case "":
                        break;
                    default:
                        Log.Information("Comandos: restart · status · quit");
                        break;
                }
            }

            if (Stopwatch.GetElapsedTime(lastReport) >= TimeSpan.FromSeconds(10))
            {
                lastReport = Stopwatch.GetTimestamp();
                var (count, mean, max) = stats.TakeAndReset();
                Log.Information(
                    "Tick: média {Mean:0.000} ms, máx {Max:0.000} ms em {Count} ticks · {Players} jogador(es) · pulados {Skipped} · snapshots fragmentados {Fragmented}",
                    mean, max, count, room.PlayerCount, loop.Skipped, transport.FragmentedSnapshots);
            }
        },
        stop.Token);

    Log.Information("Servidor parado");
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException or DirectoryNotFoundException or InvalidDataException)
{
    Log.Fatal("{Message}", ex.Message);
    Console.WriteLine(ServerOptions.Usage);
    return 2;
}
finally
{
    Log.CloseAndFlush();
}
