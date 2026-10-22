using System.Diagnostics;
using Serilog;

namespace Game.Server;

/// <summary>
/// Relógio do servidor (T-105): roda <c>tick</c> a <c>tickRate</c> Hz sem deriva acumulada — o tick <i>n</i> vence em
/// <c>início + n × período</c>, calculado sempre a partir do início, nunca somando atrasos. Se o servidor atrasar,
/// recupera até <c>maxCatchUp</c> ticks de uma vez; além disso pula os ticks perdidos e registra um aviso.
/// O relógio é injetável (<see cref="Stopwatch.GetTimestamp"/> em produção) para testar sem esperar.
/// </summary>
public sealed class TickLoop
{
    private readonly Func<long> _now;
    private readonly long _period;
    private readonly int _maxCatchUp;
    private readonly Action _tick;
    private readonly ILogger _log;
    private long _start;
    private long _done;

    /// <param name="now">Relógio em unidades de <paramref name="frequency"/> por segundo.</param>
    public TickLoop(Func<long> now, long frequency, int tickRate, int maxCatchUp, Action tick, ILogger log)
    {
        _now = now;
        _period = frequency / tickRate;
        _maxCatchUp = Math.Max(1, maxCatchUp);
        _tick = tick;
        _log = log;
        _start = now();
    }

    /// <summary>Ticks pulados por atraso desde o início.</summary>
    public long Skipped { get; private set; }

    /// <summary>Ticks executados desde o início.</summary>
    public long Executed => _done;

    /// <summary>Quanto falta para o próximo tick vencer (0 se já venceu), em unidades do relógio.</summary>
    public long TimeUntilNext => Math.Max(0, Due(_done + 1) - _now());

    /// <summary>Roda os ticks vencidos (no máximo <c>maxCatchUp</c>) e devolve quantos rodou.</summary>
    public int Advance()
    {
        var now = _now();
        var ran = 0;
        while (ran < _maxCatchUp && now >= Due(_done + 1))
        {
            _tick();
            _done++;
            ran++;
            now = _now();
        }

        // Ainda atrasado depois de recuperar o máximo: pula os perdidos (o mundo anda mais devagar, mas não trava).
        var behind = (now - _start) / _period - _done;
        if (behind > 0 && ran == _maxCatchUp)
        {
            _start += behind * _period;
            Skipped += behind;
            _log.Warning("Servidor atrasado: {Behind} tick(s) pulado(s) depois de recuperar {CatchUp}", behind, _maxCatchUp);
        }

        return ran;
    }

    /// <summary>Laço principal: <paramref name="poll"/> sempre, ticks quando vencem, dorme no resto.</summary>
    public void Run(Action poll, CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            poll();
            Advance();
            var wait = TimeSpan.FromSeconds(TimeUntilNext / (double)Stopwatch.Frequency);
            if (wait > TimeSpan.FromMilliseconds(2))
            {
                Thread.Sleep(1); // Sleep(1) no Windows pode durar ~1-15 ms: acordar cedo e conferir de novo
            }
            else if (wait > TimeSpan.Zero)
            {
                Thread.Yield();
            }
        }
    }

    private long Due(long n) => _start + (n * _period);
}
