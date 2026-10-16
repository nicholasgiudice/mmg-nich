namespace Game.Data;

/// <summary>Conversão de segundos (JSON) para ticks de simulação.</summary>
public static class TickConversion
{
    public const int TicksPerSecond = 20;

    /// <summary>Arredonda para o tick mais próximo; metades arredondam para longe do zero.</summary>
    public static int SecondsToTicks(double seconds) =>
        (int)Math.Round(seconds * TicksPerSecond, MidpointRounding.AwayFromZero);
}
