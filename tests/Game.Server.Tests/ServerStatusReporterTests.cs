using Game.Server.Persistence;
using Serilog;

namespace Game.Server.Tests;

/// <summary>Contador de online: o servidor relata sem esperar o banco e zera o número ao parar.</summary>
public class ServerStatusReporterTests
{
    private static readonly ILogger Silent = new LoggerConfiguration().CreateLogger();

    [Fact]
    public void Reports_the_player_count_with_the_server_id()
    {
        var written = new List<(string Server, int Online)>();
        var reporter = new ServerStatusReporter("game.test", (server, online, _) =>
        {
            lock (written)
            {
                written.Add((server, online));
            }

            return Task.CompletedTask;
        }, Silent);

        Assert.True(reporter.Report(4));
        Assert.True(reporter.ReportStopped(BackgroundWork.Timeout));

        Assert.Equal([("game.test", 4), ("game.test", 0)], written);
    }

    [Fact]
    public void Skips_a_report_while_the_previous_one_is_still_writing()
    {
        var release = new TaskCompletionSource();
        var written = new List<int>();
        var reporter = new ServerStatusReporter("game.test", async (_, online, _) =>
        {
            lock (written)
            {
                written.Add(online);
            }

            if (online == 1)
            {
                await release.Task;
            }
        }, Silent);

        Assert.True(reporter.Report(1));
        Assert.False(reporter.Report(2)); // o banco ainda não respondeu o primeiro
        release.SetResult();
        Assert.True(reporter.ReportStopped(BackgroundWork.Timeout));

        Assert.Equal([1, 0], written);
    }

    [Fact]
    public void Database_failure_does_not_reach_the_loop()
    {
        var calls = 0;
        var reporter = new ServerStatusReporter("game.test", (_, _, _) =>
        {
            Interlocked.Increment(ref calls);
            throw new InvalidOperationException("banco fora do ar");
        }, Silent);

        Assert.True(reporter.Report(3));
        Assert.True(reporter.ReportStopped(BackgroundWork.Timeout)); // a falha vira aviso no log, não exceção
        Assert.Equal(2, calls);
    }
}
