namespace Game.Server;

/// <summary>Tempo de tick acumulado entre relatórios (média, p99 e máximo), para o log a cada 10 s.</summary>
public sealed class TickStats
{
    private readonly List<double> _samples = new(256);

    public void Add(double milliseconds) => _samples.Add(milliseconds);

    /// <summary>Estatísticas desde a última chamada (T-109: p99 para o teste de carga).</summary>
    public (long Count, double MeanMs, double P99Ms, double MaxMs) TakeAndReset()
    {
        if (_samples.Count == 0)
        {
            return (0, 0, 0, 0);
        }

        _samples.Sort();
        var p99 = _samples[Math.Min(_samples.Count - 1, (int)Math.Ceiling(_samples.Count * 0.99) - 1)];
        var result = ((long)_samples.Count, _samples.Average(), p99, _samples[^1]);
        _samples.Clear();
        return result;
    }
}
