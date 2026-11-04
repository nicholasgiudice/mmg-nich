using System.Text.Json;

namespace Game.Site;

/// <summary>
/// Ícones de habilidades, itens e atributos (T-080), a partir de <c>client/assets/art/icons/icons.json</c>. Como os
/// sprites, a folha é copiada e a página mostra só a região do ícone com <c>background-position</c>. Id sem entrada
/// própria não ganha ícone no site (o padrão <c>ui.unknown</c> do jogo não é usado aqui).
/// </summary>
public static class Icons
{
    public const string PublishedDir = "assets/icons";

    /// <summary>Lê o índice e devolve a região por id. Pasta ausente ou arquivo inválido = vazio.</summary>
    public static IReadOnlyDictionary<string, SpriteFrame> Load(string? artDir, List<string> warnings)
    {
        var result = new Dictionary<string, SpriteFrame>(StringComparer.Ordinal);
        var file = artDir is null ? null : Path.Combine(artDir, "icons", "icons.json");
        if (file is null || !File.Exists(file))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            var sheetName = root.GetProperty("sheet").GetString()!;
            var size = root.GetProperty("size").GetInt32();
            var sheet = Path.Combine(artDir!, "icons", sheetName);
            if (!File.Exists(sheet) || Sprites.ReadPngSize(sheet) is not { } sheetSize)
            {
                warnings.Add($"ícones: folha {sheetName} não encontrada ou não é PNG");
                return result;
            }

            foreach (var icon in root.GetProperty("icons").EnumerateObject())
            {
                int x = icon.Value.GetProperty("x").GetInt32(), y = icon.Value.GetProperty("y").GetInt32();
                if (x < 0 || y < 0 || x + size > sheetSize.W || y + size > sheetSize.H)
                {
                    warnings.Add($"ícone de {icon.Name}: região fora da folha");
                    continue;
                }

                result[icon.Name] = new SpriteFrame($"{PublishedDir}/{sheetName}", x, y, size, size, sheetSize.W, sheetSize.H);
            }
        }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or JsonException)
        {
            warnings.Add($"ícones: {e.Message}");
        }

        return result;
    }

    /// <summary>Copia a folha de ícones para <c>out/assets/icons</c>.</summary>
    public static void Copy(string artDir, string outDir, IEnumerable<SpriteFrame> icons)
    {
        foreach (var texture in icons.Select(f => f.Texture).Distinct())
        {
            var source = Path.Combine(artDir, "icons", texture[(PublishedDir.Length + 1)..]);
            var target = Path.Combine(outDir, texture);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, overwrite: true);
        }
    }
}
