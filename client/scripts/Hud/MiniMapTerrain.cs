using Godot;
using MmoRpgClient.Presentation;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient.Hud;

/// <summary>
/// Imagem do terreno do mini mapa (T-392): 1 pixel por tile, gerada uma vez por zona. Com mapa do Tiled, a cor de cada
/// tile é a média dos pixels visíveis dele, camada por camada (menos a de colisão), como o jogo desenha; sem mapa do Tiled
/// (arena ASCII), chão e parede em duas cores. Tiles bloqueados (paredes, árvores, água) ficam mais escuros, para o
/// caminho se destacar. Só exibe: a colisão continua sendo a do Core.
/// </summary>
public static class MiniMapTerrain
{
    private static readonly Color Empty = new(0.05f, 0.05f, 0.06f);
    private static readonly Color Floor = new(0.36f, 0.42f, 0.28f);
    private static readonly Color Wall = new(0.30f, 0.27f, 0.25f);

    /// <summary>Quanto um tile bloqueado escurece (0 a 1).</summary>
    private const float BlockedDarken = 0.45f;

    public static ImageTexture Build(GridMap map, TiledVisual? visual)
    {
        var image = Image.CreateEmpty(Math.Max(1, map.Width), Math.Max(1, map.Height), false, Image.Format.Rgba8);
        var layers = visual?.Layers.Where(l => l.Name != TiledVisual.Collision).ToList() ?? [];
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var blocked = map.IsBlocked(x, y);
                var color = layers.Count == 0 ? (blocked ? Wall : Floor) : Empty;
                foreach (var layer in layers)
                {
                    if (x < layer.Width && y < layer.Height && visual!.AverageColor(layer.Data[y * layer.Width + x]) is { } tile)
                    {
                        color = color.Lerp(tile.Color, Math.Clamp(tile.Coverage * layer.Opacity, 0f, 1f));
                    }
                }

                image.SetPixel(x, y, blocked && layers.Count > 0 ? color.Darkened(BlockedDarken) : color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}
