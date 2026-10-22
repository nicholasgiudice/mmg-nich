namespace Game.Server;

/// <summary>Tempo de tick acumulado entre relatórios (média e máximo), para o log a cada 10 s.</summary>
public sealed class TickStats
{
    private long _count;
    private double _sum;
    private double _max;

    public void Add(double milliseconds)
    {
        _count++;
        _sum += milliseconds;
        _max = Math.Max(_max, milliseconds);
    }

    public (long Count, double MeanMs, double MaxMs) TakeAndReset()
    {
        var result = (_count, _count == 0 ? 0 : _sum / _count, _max);
        _count = 0;
        _sum = 0;
        _max = 0;
        return result;
    }
}
