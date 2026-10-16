using Game.Core.Combat;
using Game.Core.Simulation;

namespace Game.Sim;

/// <summary>Estatísticas agregadas de N execuções de (encontro × estratégia).</summary>
public sealed record BatchSummary
{
    public required string Encounter { get; init; }
    public required string Strategy { get; init; }
    public required int Runs { get; init; }
    public required ulong BaseSeed { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int Timeouts { get; init; }
    public double WinRate { get; init; }

    /// <summary>Tempo até a vitória em segundos (só execuções vencidas); 0 se não houve vitória.</summary>
    public double TimeToWinMean { get; init; }
    public double TimeToWinP50 { get; init; }
    public double TimeToWinP90 { get; init; }

    // Médias por execução.
    public double DamageTaken { get; init; }
    public double DamageDealt { get; init; }
    public double Healing { get; init; }
    public double Overheal { get; init; }
    public double ResourceGained { get; init; }
    public double ResourceSpent { get; init; }
    public double ResourceWasted { get; init; }
    public double SecondsAtResourceCap { get; init; }
    public double PlayerDeaths { get; init; }
    public double EnemyDeaths { get; init; }
    public double PhaseChanges { get; init; }
    public double DistanceMoved { get; init; }
    public double StationaryUseShare { get; init; }

    /// <summary>Usos médios por execução de cada habilidade (ordenado por id).</summary>
    public IReadOnlyDictionary<string, double> AbilityUses { get; init; } = new Dictionary<string, double>();

    /// <summary>Habilidades distintas usadas por execução, em média (indicador de profundidade: "só provocação + básico" = baixo).</summary>
    public double DistinctAbilities { get; init; }

    public double WallSeconds { get; init; }
    public IReadOnlyList<RunResult>? PerRun { get; init; }
}

public static class Batch
{
    /// <summary>Roda <c>spec.Runs</c> execuções (seeds BaseSeed..BaseSeed+Runs-1), em paralelo entre seeds; resultado independe do paralelismo.</summary>
    public static BatchSummary Run(ContentCatalog content, RunSpec spec, int parallelism = 0, bool keepPerRun = false)
    {
        var results = new RunResult[spec.Runs];
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var options = new ParallelOptions { MaxDegreeOfParallelism = parallelism > 0 ? parallelism : Environment.ProcessorCount };
        Parallel.For(0, spec.Runs, options, i => results[i] = EncounterRunner.Run(content, spec, spec.BaseSeed + (ulong)i));
        sw.Stop();
        return Summarize(spec, results, sw.Elapsed.TotalSeconds, keepPerRun);
    }

    public static BatchSummary Summarize(RunSpec spec, IReadOnlyList<RunResult> results, double wallSeconds = 0, bool keepPerRun = false)
    {
        var n = Math.Max(1, results.Count);
        var wins = results.Where(r => r.Outcome == RunOutcome.Win).Select(r => r.Seconds).Order().ToList();
        double Avg(Func<RunResult, double> f) => results.Count == 0 ? 0 : results.Sum(f) / n;

        var uses = new SortedDictionary<string, double>(StringComparer.Ordinal);
        foreach (var r in results)
        {
            foreach (var (id, count) in r.AbilityUses)
            {
                uses[id] = uses.GetValueOrDefault(id) + count;
            }
        }

        foreach (var k in uses.Keys.ToList())
        {
            uses[k] /= n;
        }

        return new BatchSummary
        {
            Encounter = spec.EncounterId,
            Strategy = spec.Label,
            Runs = results.Count,
            BaseSeed = spec.BaseSeed,
            Wins = wins.Count,
            Losses = results.Count(r => r.Outcome == RunOutcome.Loss),
            Timeouts = results.Count(r => r.Outcome == RunOutcome.Timeout),
            WinRate = wins.Count / (double)n,
            TimeToWinMean = wins.Count == 0 ? 0 : wins.Average(),
            TimeToWinP50 = Percentile(wins, 0.50),
            TimeToWinP90 = Percentile(wins, 0.90),
            DamageTaken = Avg(r => r.DamageTaken),
            DamageDealt = Avg(r => r.DamageDealt),
            Healing = Avg(r => r.Healing),
            Overheal = Avg(r => r.Overheal),
            ResourceGained = Avg(r => r.ResourceGained),
            ResourceSpent = Avg(r => r.ResourceSpent),
            ResourceWasted = Avg(r => r.ResourceWasted),
            SecondsAtResourceCap = Avg(r => r.TicksAtResourceCap) / World.TickRate,
            PlayerDeaths = Avg(r => r.PlayerDeaths),
            EnemyDeaths = Avg(r => r.EnemyDeaths),
            PhaseChanges = Avg(r => r.PhaseChanges),
            DistanceMoved = Avg(r => r.DistanceMoved),
            StationaryUseShare = Avg(r => r.StationaryUseShare),
            AbilityUses = uses,
            DistinctAbilities = Avg(r => r.AbilityUses.Count),
            WallSeconds = wallSeconds,
            PerRun = keepPerRun ? results : null,
        };
    }

    /// <summary>Percentil por posto mais próximo (nearest-rank) de uma lista ordenada.</summary>
    public static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        var rank = (int)Math.Ceiling(p * sorted.Count);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Count - 1)];
    }
}
