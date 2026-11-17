using System.Numerics;

namespace MmoRpgClient.Hud;

/// <summary>Uma marca no mini mapa (T-392): um membro do grupo, na posição em tiles.</summary>
public readonly record struct MapMark(int Id, string Name, Vector2 Position, bool Alive);

/// <summary>
/// Recorte do mapa mostrado num painel (T-392): <see cref="Origin"/> é o tile do canto superior esquerdo,
/// <see cref="Size"/> quantos tiles aparecem e <see cref="Scale"/> quantos pixels cada tile ocupa.
/// </summary>
public readonly record struct MapFrame(Vector2 Origin, Vector2 Size, float Scale)
{
    /// <summary>Tamanho do recorte na tela, em pixels.</summary>
    public Vector2 PixelSize => Size * Scale;

    /// <summary>Posição em tiles para pixels dentro do recorte (0,0 = canto superior esquerdo do recorte).</summary>
    public Vector2 ToPixels(Vector2 tiles) => (tiles - Origin) * Scale;

    /// <summary>Pixels dentro do recorte para posição em tiles.</summary>
    public Vector2 ToTiles(Vector2 pixels) => pixels / Scale + Origin;
}

/// <summary>
/// Contas do mini mapa e do mapa grande da zona (T-392), sem Godot para serem testadas: o recorte em volta do jogador,
/// a escala inteira (pixel art sem borrar) e a marca presa na borda quando está fora do recorte. Só exibe: nada daqui
/// decide gameplay.
/// </summary>
public static class MiniMapView
{
    /// <summary>Lado do mini mapa na tela, em pixels.</summary>
    public const int PanelPixels = 168;

    /// <summary>Tiles mostrados em volta do jogador num mapa grande (56 × 56 com 3 pixels por tile).</summary>
    public const int ViewTiles = 56;

    /// <summary>Escala máxima do mapa grande: mapas pequenos não viram blocos enormes.</summary>
    public const int MaxWholeScale = 10;

    /// <summary>
    /// Recorte do mini mapa: <paramref name="viewTiles"/> tiles em volta de <paramref name="center"/>, parando nas bordas
    /// do mapa. Mapa que cabe inteiro aparece inteiro, ampliado pela maior escala inteira que caiba no painel.
    /// </summary>
    public static MapFrame Mini(int mapWidth, int mapHeight, Vector2 center, int viewTiles = ViewTiles, int panelPixels = PanelPixels)
    {
        var largest = Math.Max(1, Math.Max(mapWidth, mapHeight));
        var scale = Math.Max(Math.Max(1, panelPixels / viewTiles), panelPixels / largest);
        var visible = (float)panelPixels / scale;
        var size = new Vector2(Math.Min(mapWidth, visible), Math.Min(mapHeight, visible));
        var origin = new Vector2(
            Math.Clamp(center.X - size.X / 2, 0, mapWidth - size.X),
            Math.Clamp(center.Y - size.Y / 2, 0, mapHeight - size.Y));
        return new MapFrame(origin, size, scale);
    }

    /// <summary>Mapa grande: a zona inteira, na maior escala inteira que caiba em <paramref name="available"/> (pixels).</summary>
    public static MapFrame Whole(int mapWidth, int mapHeight, Vector2 available, int maxScale = MaxWholeScale)
    {
        var fit = Math.Min(available.X / Math.Max(1, mapWidth), available.Y / Math.Max(1, mapHeight));
        var scale = Math.Clamp((int)Math.Floor(fit), 1, Math.Max(1, maxScale));
        return new MapFrame(Vector2.Zero, new Vector2(mapWidth, mapHeight), scale);
    }

    /// <summary>Deslocamento para centralizar um recorte menor que o painel (mapa pequeno no mini mapa).</summary>
    public static Vector2 CenterOffset(MapFrame frame, Vector2 panel) =>
        Vector2.Max(Vector2.Zero, (panel - frame.PixelSize) / 2);

    /// <summary>
    /// Marca fora da área (<paramref name="area"/>, pixels) fica presa na borda, a <paramref name="margin"/> pixels dela,
    /// na linha que vai do centro até a posição real: assim ela aponta para onde a pessoa está. Dentro, fica onde está.
    /// </summary>
    public static (Vector2 Pixel, bool Inside) PinToEdge(Vector2 pixel, Vector2 area, float margin)
    {
        var low = new Vector2(margin);
        var high = area - new Vector2(margin);
        if (pixel.X >= low.X && pixel.X <= high.X && pixel.Y >= low.Y && pixel.Y <= high.Y)
        {
            return (pixel, true);
        }

        var center = area / 2;
        var half = Vector2.Max(Vector2.Zero, area / 2 - new Vector2(margin));
        var d = pixel - center;
        var tx = Math.Abs(d.X) > 1e-4f ? half.X / Math.Abs(d.X) : float.PositiveInfinity;
        var ty = Math.Abs(d.Y) > 1e-4f ? half.Y / Math.Abs(d.Y) : float.PositiveInfinity;
        var t = Math.Min(1f, Math.Min(tx, ty));
        return (center + d * t, false);
    }
}
