namespace MmoRpgClient.Presentation;

/// <summary>Ordem de desenho no mundo (maior = por cima). A arena fica em 0.</summary>
public static class ZLayers
{
    public const int Areas = 1;
    public const int Telegraphs = 2;
    public const int Trails = 3;
    public const int DeadUnits = 4;
    public const int Units = 5;

    /// <summary>Camada <c>above</c> do mapa do Tiled (copas, telhados): por cima das unidades (T-052).</summary>
    public const int Above = 6;
}
