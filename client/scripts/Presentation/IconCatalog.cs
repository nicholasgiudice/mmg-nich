using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Ícones de habilidades, itens e atributos (T-080): lê <c>res://assets/art/icons/icons.json</c> e devolve um
/// <see cref="AtlasTexture"/> da folha por id. Id sem ícone usa o ícone padrão (<see cref="IconIndex.Fallback"/>).
/// Os ícones têm <see cref="Size"/> px (34 no pacote atual); desenhe com filtro <c>Nearest</c>.
/// </summary>
public sealed class IconCatalog
{
    public const string IconDirectory = SpriteCatalog.ArtDirectory + "/icons";

    public static readonly IconCatalog Empty = new(null, null);

    private readonly IconIndex? _index;
    private readonly Texture2D? _sheet;
    private readonly Dictionary<string, AtlasTexture> _cache = new(StringComparer.Ordinal);

    private IconCatalog(IconIndex? index, Texture2D? sheet)
    {
        _index = index;
        _sheet = sheet;
    }

    /// <summary>Lado do ícone em pixels (0 sem catálogo).</summary>
    public int Size => _index?.Size ?? 0;

    public IconIndex? Index => _index;

    /// <summary>Ícone do id; sem entrada, o ícone padrão; sem catálogo, null.</summary>
    public Texture2D? For(string id)
    {
        if (_index is null || _sheet is null)
        {
            return null;
        }

        if (!_index.Icons.ContainsKey(id))
        {
            id = _index.Fallback;
        }

        if (_cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        if (!_index.Icons.TryGetValue(id, out var region))
        {
            return null;
        }

        var atlas = new AtlasTexture { Atlas = _sheet, Region = new Rect2(region.X, region.Y, _index.Size, _index.Size) };
        _cache[id] = atlas;
        return atlas;
    }

    public static IconCatalog Load(string directory = IconDirectory)
    {
        var path = $"{directory}/icons.json";
        if (!Godot.FileAccess.FileExists(path))
        {
            GD.PushWarning($"Sem {path}: telas sem ícones.");
            return Empty;
        }

        var index = IconIndex.Parse(Godot.FileAccess.GetFileAsString(path));
        var sheet = SpriteCatalog.LoadTexture($"{directory}/{index.Sheet}");
        if (sheet is null)
        {
            GD.PushWarning($"Folha de ícones {directory}/{index.Sheet} não encontrada: telas sem ícones.");
            return Empty;
        }

        return new IconCatalog(index, sheet);
    }
}
