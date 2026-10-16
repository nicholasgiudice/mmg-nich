using System.Globalization;
using Game.Core.Simulation;

namespace Game.Bots;

/// <summary>
/// Tempo de reação de um bot (T-018). Uma condição só vira ação depois de ficar verdadeira por um atraso sorteado
/// (padrão 0,2 a 0,4 s) desde que apareceu. O sorteio usa um <see cref="SimRandom"/> próprio do bot, com seed derivada da seed
/// do encontro (<see cref="BotContext.Seed"/>) e do id da entidade: mesma seed, mesmas reações. Nunca toca o RNG da simulação.
/// Opção <c>reaction</c> (global ou <c>class.x:reaction</c>): <c>0.2-0.4</c> (faixa em segundos), <c>0.3</c> (fixo) ou <c>0</c> (desliga).
/// </summary>
public sealed class Reactor
{
    public const string OptionKey = "reaction";
    public const double DefaultMin = 0.2;
    public const double DefaultMax = 0.4;

    private readonly Dictionary<string, long> _due = [];
    private SimRandom? _rng;
    private (double Min, double Max)? _range;

    /// <summary>Faixa (min, max) em segundos lida das opções; 0 desliga.</summary>
    public static (double Min, double Max) ParseRange(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (DefaultMin, DefaultMax);
        }

        var parts = text.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var a)
            && a >= 0)
        {
            var b = parts.Length >= 2 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var bb) ? bb : a;
            return (Math.Min(a, b), Math.Max(a, b));
        }

        return (DefaultMin, DefaultMax);
    }

    /// <summary>Atraso sorteado em ticks (0 se desligado).</summary>
    public int DrawTicks(BotContext ctx)
    {
        _range ??= ParseRange(ctx.Options.Get(ctx.ClassId, OptionKey));
        var (min, max) = _range.Value;
        if (max <= 0)
        {
            return 0;
        }

        var lo = (int)Math.Round(min * World.TickRate);
        var hi = (int)Math.Round(max * World.TickRate);
        if (hi <= lo)
        {
            return lo;
        }

        _rng ??= new SimRandom(ctx.Seed ^ unchecked((ulong)ctx.Self.Id.Value * 0x9E3779B97F4A7C15UL));
        return lo + _rng.NextInt(hi - lo + 1);
    }

    /// <summary>
    /// Devolve true quando a condição já dura o atraso de reação. Condição falsa limpa o cronômetro.
    /// Depois de agir, chame <see cref="Reset"/> para a próxima reação sortear um atraso novo.
    /// </summary>
    public bool Ready(BotContext ctx, string key, bool condition)
    {
        if (!condition)
        {
            _due.Remove(key);
            return false;
        }

        if (!_due.TryGetValue(key, out var due))
        {
            due = ctx.World.Tick + DrawTicks(ctx);
            _due[key] = due;
        }

        return ctx.World.Tick >= due;
    }

    public void Reset(string key) => _due.Remove(key);
}
