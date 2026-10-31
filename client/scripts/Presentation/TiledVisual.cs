using System.Text.Json.Nodes;
using System.Xml;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Parte visual de um mapa do Tiled (T-052): camadas de tiles e tilesets, lidas do mesmo <c>.tmj</c> que o Core usa.
/// O <c>.tsx</c> aponta para a imagem fora de <c>data/</c> (<c>client/assets/art/tiles/</c>, convenção da T-051); aqui
/// vale só o nome do arquivo, procurado em <c>res://assets/art/tiles/</c>. Tileset sem imagem (ex.: o de colisão) é
/// ignorado. Espelhamento horizontal e vertical são desenhados; a rotação (bit diagonal) não.
/// </summary>
public sealed class TiledVisual
{
    public const string Collision = "collision";
    public const string Above = "above";
    public const string TilesDirectory = "res://assets/art/tiles";

    private const uint FlipH = 0x80000000;
    private const uint FlipV = 0x40000000;
    private const uint FlagMask = 0xE0000000;

    public sealed record Layer(string Name, int Width, int Height, uint[] Data, float Opacity);

    private sealed record Tileset(int FirstGid, int Columns, int TileCount, int TileWidth, int TileHeight, Texture2D Texture);

    private readonly List<Tileset> _tilesets;

    private TiledVisual(IReadOnlyList<Layer> layers, List<Tileset> tilesets)
    {
        Layers = layers;
        _tilesets = tilesets;
    }

    /// <summary>Camadas de tiles visíveis, na ordem do arquivo (de baixo para cima), grupos achatados.</summary>
    public IReadOnlyList<Layer> Layers { get; }

    public static TiledVisual Load(string tmjPath)
    {
        var root = JsonNode.Parse(Godot.FileAccess.GetFileAsString(tmjPath))
            ?? throw new InvalidOperationException($"{tmjPath} vazio ou ilegível");
        var directory = tmjPath[..tmjPath.LastIndexOf('/')];
        var layers = new List<Layer>();
        CollectLayers(root["layers"]!.AsArray(), layers);
        var tilesets = new List<Tileset>();
        foreach (var node in root["tilesets"]!.AsArray())
        {
            var source = (string?)node!["source"]
                ?? throw new InvalidOperationException("tileset embutido no mapa: use .tsx externo (map-conventions §1)");
            if (LoadTileset((int)node["firstgid"]!, $"{directory}/{source}") is { } tileset)
            {
                tilesets.Add(tileset);
            }
        }

        if (tilesets.Count == 0)
        {
            throw new InvalidOperationException("nenhum tileset com imagem");
        }

        tilesets.Sort((a, b) => b.FirstGid.CompareTo(a.FirstGid));
        return new TiledVisual(layers, tilesets);
    }

    /// <summary>Desenha uma camada em <paramref name="target"/>; <paramref name="alpha"/> dá a opacidade de cada célula.</summary>
    public void DrawLayer(CanvasItem target, Layer layer, Func<Vector2I, float> alpha)
    {
        const float s = Units.PixelsPerUnit;
        for (var y = 0; y < layer.Height; y++)
        {
            for (var x = 0; x < layer.Width; x++)
            {
                var raw = layer.Data[y * layer.Width + x];
                var gid = (int)(raw & ~FlagMask);
                if (gid == 0 || Find(gid) is not { } ts)
                {
                    continue;
                }

                var local = gid - ts.FirstGid;
                var source = new Rect2(local % ts.Columns * ts.TileWidth, local / ts.Columns * ts.TileHeight, ts.TileWidth, ts.TileHeight);
                // Tiles maiores que a grade (árvores de 1 tile de largura e 2 de altura, por ex.) ficam presos pelo canto de baixo.
                var dest = new Rect2(x * s, (y + 1) * s - ts.TileHeight, ts.TileWidth, ts.TileHeight);
                var color = new Color(1, 1, 1, layer.Opacity * alpha(new Vector2I(x, y)));
                var flipH = (raw & FlipH) != 0;
                var flipV = (raw & FlipV) != 0;
                if (!flipH && !flipV)
                {
                    target.DrawTextureRectRegion(ts.Texture, dest, source, color);
                    continue;
                }

                var center = dest.GetCenter();
                target.DrawSetTransform(center, 0, new Godot.Vector2(flipH ? -1 : 1, flipV ? -1 : 1));
                target.DrawTextureRectRegion(ts.Texture, new Rect2(-dest.Size / 2, dest.Size), source, color);
                target.DrawSetTransform(Godot.Vector2.Zero, 0, Godot.Vector2.One);
            }
        }
    }

    private Tileset? Find(int gid)
    {
        foreach (var ts in _tilesets)
        {
            if (gid >= ts.FirstGid)
            {
                return gid - ts.FirstGid < ts.TileCount ? ts : null;
            }
        }

        return null;
    }

    private static void CollectLayers(JsonArray nodes, List<Layer> into)
    {
        foreach (var node in nodes)
        {
            if (node is null || (bool?)node["visible"] == false)
            {
                continue;
            }

            switch ((string?)node["type"])
            {
                case "group":
                    CollectLayers(node["layers"]!.AsArray(), into);
                    break;
                case "tilelayer":
                    var data = node["data"]!.AsArray().Select(v => (uint)(long)v!).ToArray();
                    into.Add(new Layer((string)node["name"]!, (int)node["width"]!, (int)node["height"]!, data,
                        (float?)node["opacity"] ?? 1f));
                    break;
            }
        }
    }

    private static Tileset? LoadTileset(int firstGid, string tsxPath)
    {
        if (!Godot.FileAccess.FileExists(tsxPath))
        {
            GD.PushWarning($"Tileset não encontrado: {tsxPath}");
            return null;
        }

        var xml = new XmlDocument();
        xml.LoadXml(Godot.FileAccess.GetFileAsString(tsxPath));
        var tileset = xml.DocumentElement!;
        var image = tileset["image"]?.GetAttribute("source");
        if (string.IsNullOrEmpty(image))
        {
            return null;
        }

        var texturePath = $"{TilesDirectory}/{image[(image.LastIndexOf('/') + 1)..]}";
        var texture = SpriteCatalog.LoadTexture(texturePath);
        if (texture is null)
        {
            return null;
        }

        int Attr(string name) => int.Parse(tileset.GetAttribute(name), System.Globalization.CultureInfo.InvariantCulture);
        return new Tileset(firstGid, Attr("columns"), Attr("tilecount"), Attr("tilewidth"), Attr("tileheight"), texture);
    }
}
