using System.Buffers.Binary;
using System.Text.Json;

namespace Game.Site;

/// <summary>Um quadro de uma folha de sprites: textura (caminho publicado), retângulo e tamanho da folha, em pixels.</summary>
public sealed record SpriteFrame(string Texture, int X, int Y, int W, int H, int SheetW, int SheetH);

/// <summary>
/// Primeiro quadro parado (de frente, olhando para baixo) de cada classe e monstro, a partir de
/// <c>client/assets/art/sprites.json</c> (T-053). Não recorta a imagem: a folha é copiada e a página mostra só o quadro
/// com <c>background-position</c>, então não precisa de biblioteca de imagem.
/// </summary>
public static class Sprites
{
    public const string PublishedDir = "assets/sprites";

    /// <summary>Lê o layout e devolve o quadro por id (classe ou monstro). Pasta ausente ou arquivo inválido = vazio.</summary>
    public static IReadOnlyDictionary<string, SpriteFrame> Load(string? artDir, List<string> warnings)
    {
        var result = new Dictionary<string, SpriteFrame>(StringComparer.Ordinal);
        var file = artDir is null ? null : Path.Combine(artDir, "sprites.json");
        if (file is null || !File.Exists(file))
        {
            return result;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        var root = doc.RootElement;
        if (!root.TryGetProperty("layouts", out var layouts) || !root.TryGetProperty("sprites", out var sprites))
        {
            return result;
        }

        foreach (var sprite in sprites.EnumerateObject())
        {
            try
            {
                var texture = sprite.Value.GetProperty("texture").GetString()!;
                var sheet = Path.Combine(artDir!, texture);
                if (!File.Exists(sheet) || ReadPngSize(sheet) is not { } size)
                {
                    warnings.Add($"sprite de {sprite.Name}: folha {texture} não encontrada ou não é PNG");
                    continue;
                }

                var layout = layouts.GetProperty(sprite.Value.GetProperty("layout").GetString()!);
                if (IdleFrame(layout) is not { } idle)
                {
                    warnings.Add($"sprite de {sprite.Name}: layout sem animação 'idle' reconhecida");
                    continue;
                }

                result[sprite.Name] = new SpriteFrame($"{PublishedDir}/{texture}", idle.X, idle.Y, idle.W, idle.H, size.W, size.H);
            }
            catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or JsonException)
            {
                warnings.Add($"sprite de {sprite.Name}: {e.Message}");
            }
        }

        return result;
    }

    /// <summary>Quadro parado olhando para baixo: "grid" (linha da direção "down" em dirOrder) ou entrada por direção.</summary>
    private static (int X, int Y, int W, int H)? IdleFrame(JsonElement layout)
    {
        var frame = layout.GetProperty("frame");
        int fw = frame[0].GetInt32(), fh = frame[1].GetInt32();
        if (!layout.GetProperty("animations").TryGetProperty("idle", out var idle))
        {
            return null;
        }

        if (idle.TryGetProperty("grid", out var grid))
        {
            var row = grid.GetProperty("row").GetInt32();
            var col = grid.GetProperty("col").GetInt32();
            if (layout.TryGetProperty("dirOrder", out var order))
            {
                var dirs = order.EnumerateArray().Select(d => d.GetString()).ToList();
                row += Math.Max(0, dirs.IndexOf("down"));
            }

            return (col * fw, row * fh, fw, fh);
        }

        var dir = idle.TryGetProperty("down", out var down) ? down : idle.EnumerateObject().First().Value;
        var w = dir.TryGetProperty("w", out var ww) ? ww.GetInt32() : fw;
        var h = dir.TryGetProperty("h", out var hh) ? hh.GetInt32() : fh;
        return (dir.GetProperty("x").GetInt32(), dir.GetProperty("y").GetInt32(), w, h);
    }

    /// <summary>Largura e altura do cabeçalho IHDR de um PNG.</summary>
    public static (int W, int H)? ReadPngSize(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(path);
        if (stream.Read(header) < 24 || header[1] != 'P' || header[2] != 'N' || header[3] != 'G')
        {
            return null;
        }

        return (BinaryPrimitives.ReadInt32BigEndian(header[16..]), BinaryPrimitives.ReadInt32BigEndian(header[20..]));
    }

    /// <summary>Copia as folhas usadas para <c>out/assets/sprites</c>.</summary>
    public static void Copy(string artDir, string outDir, IEnumerable<SpriteFrame> frames)
    {
        foreach (var texture in frames.Select(f => f.Texture).Distinct())
        {
            var source = Path.Combine(artDir, texture[(PublishedDir.Length + 1)..]);
            var target = Path.Combine(outDir, texture);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, overwrite: true);
        }
    }

    /// <summary>
    /// Elemento que mostra só o quadro, ampliado <paramref name="scale"/> vezes, com pixels nítidos. Sem
    /// <paramref name="alt"/>, a imagem é decorativa (ao lado de um nome que já diz o que é) e fica oculta para leitores de tela.
    /// </summary>
    public static string Html(Page page, SpriteFrame f, string? alt, int scale = 2, string cls = "sprite") =>
        $"<span class=\"{cls}\" {(alt is null ? "aria-hidden=\"true\"" : $"role=\"img\" aria-label=\"{Fmt.Esc(alt)}\"")} style=\"width:{f.W * scale}px;height:{f.H * scale}px;"
        + $"background-image:url('{Fmt.Esc(page.Href(f.Texture))}');background-size:{f.SheetW * scale}px {f.SheetH * scale}px;"
        + $"background-position:-{f.X * scale}px -{f.Y * scale}px\"></span>";
}
