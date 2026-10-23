using Game.Server;
using Serilog.Core;
using Serilog.Events;

namespace Game.Server.Tests;

/// <summary>T-105: relógio de 20 Hz sem deriva, recuperação limitada e pulo de ticks perdidos.</summary>
public class TickLoopTests
{
    private const long Frequency = 1_000_000; // relógio falso em microssegundos
    private const long Period = Frequency / 20;

    [Fact]
    public void Jittery_wakeups_do_not_accumulate_drift()
    {
        long now = 0;
        var ticks = 0;
        var loop = new TickLoop(() => now, Frequency, 20, 5, () => ticks++, Logger.None);

        // Acorda sempre um pouco atrasado (até 7 ms): em 60 s devem ser exatamente 1200 ticks.
        var rng = new Random(3);
        while (now < 60 * Frequency)
        {
            now += rng.Next(1_000, 7_000);
            loop.Advance();
        }

        Assert.Equal(1200, ticks);
        Assert.Equal(0, loop.Skipped);
    }

    [Fact]
    public void Late_server_catches_up_at_most_n_ticks_then_skips_the_rest()
    {
        long now = 0;
        var ticks = 0;
        var loop = new TickLoop(() => now, Frequency, 20, 5, () => ticks++, Logger.None);

        now = 3 * Period;
        Assert.Equal(3, loop.Advance()); // atraso pequeno: recupera tudo

        now = 20 * Period; // travou ~0,85 s
        Assert.Equal(5, loop.Advance());
        Assert.Equal(12, loop.Skipped);
        Assert.Equal(0, loop.Advance()); // em dia de novo

        now += Period;
        Assert.Equal(1, loop.Advance());
        Assert.Equal(9, ticks);
    }

    [Fact]
    public void Options_parse_arguments_and_environment()
    {
        var env = new Dictionary<string, string> { ["MMO_PORT"] = "9000", ["MMO_DATA_DIR"] = "/srv/data" };
        var o = ServerOptions.Parse(["--encounter", "encounter.four_rats", "--bots", "false", "--disconnect-grace", "5"], k => env.GetValueOrDefault(k));
        Assert.Equal(9000, o.Port);
        Assert.Equal("/srv/data", o.DataDir);
        Assert.Equal("encounter.four_rats", o.EncounterId);
        Assert.False(o.FillWithBots);
        Assert.Equal(5, o.DisconnectGraceSeconds);
        Assert.Throws<ArgumentException>(() => ServerOptions.Parse(["--nope"], _ => null));
    }

    [Fact]
    public void Log_level_comes_from_option_or_environment_and_option_wins()
    {
        Assert.Equal(LogEventLevel.Information, ServerOptions.Parse([], _ => null).LogLevel);
        Assert.Equal(LogEventLevel.Debug, ServerOptions.Parse([], k => k == "MMO_LOG_LEVEL" ? "debug" : null).LogLevel);
        Assert.Equal(LogEventLevel.Warning, ServerOptions.Parse(["--log-level", "warn"], k => k == "MMO_LOG_LEVEL" ? "debug" : null).LogLevel);
        Assert.Equal(LogEventLevel.Error, ServerOptions.Parse(["--log-level", "ERROR"], _ => null).LogLevel);
        Assert.Equal(LogEventLevel.Information, ServerOptions.ParseLogLevel("info"));
        Assert.Throws<ArgumentException>(() => ServerOptions.Parse(["--log-level", "loud"], _ => null));
        Assert.Throws<ArgumentException>(() => ServerOptions.ParseLogLevel("3"));
    }
}
