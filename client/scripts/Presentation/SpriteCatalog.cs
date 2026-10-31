using System.Text.Json.Nodes;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>Direção de uma folha de sprites (as folhas LPC têm 4).</summary>
public enum SpriteDirection { Up, Left, Down, Right }

/// <summary>Quadros seguidos de uma animação numa direção: retângulo do primeiro, quantidade e pivô (pés).</summary>
public sealed record SpriteStrip(int X, int Y, int Width, int Height, int Count, Godot.Vector2 Pivot)
{
    public Rect2 Frame(int index) => new(X + Width * Math.Clamp(index, 0, Count - 1), Y, Width, Height);
}

/// <summary>Uma animação nas 4 direções.</summary>
public sealed record SpriteAnimation(IReadOnlyDictionary<SpriteDirection, SpriteStrip> Strips, float Fps, bool Loop)
{
    public SpriteStrip For(SpriteDirection direction) =>
        Strips.TryGetValue(direction, out var strip) ? strip : Strips.Values.First();

    public float DurationSeconds(SpriteDirection direction) => For(direction).Count / Fps;
}

/// <summary>Sprite de uma classe ou monstro (T-053): textura, escala inteira e animações por papel.</summary>
public sealed class SpriteDefinition
{
    public required Texture2D Texture { get; init; }
    public required int Scale { get; init; }

    /// <summary>Distância (pixels, já com escala) do pivô ao topo da cabeça: onde ficam barra de vida e textos.</summary>
    public required float HeadPx { get; init; }

    public required IReadOnlyDictionary<string, SpriteAnimation> Animations { get; init; }

    /// <summary>Papel (idle, walk, attack, cast, death) → animação da folha; sem entrada, o papel é o próprio nome.</summary>
    public required IReadOnlyDictionary<string, string> Use { get; init; }

    public SpriteAnimation? For(string role) =>
        Animations.TryGetValue(Use.GetValueOrDefault(role, role), out var anim) ? anim : null;
}

/// <summary>
/// Lê <c>res://assets/art/sprites.json</c> (mapeamento definição → sprite, só do cliente). Sprite cuja textura
/// ainda não existe fica de fora, e a unidade continua desenhada como círculo.
/// </summary>
public sealed class SpriteCatalog
{
    public const string ArtDirectory = "res://assets/art";

    public static readonly SpriteCatalog Empty = new(new Dictionary<string, SpriteDefinition>());

    private readonly IReadOnlyDictionary<string, SpriteDefinition> _sprites;

    private SpriteCatalog(IReadOnlyDictionary<string, SpriteDefinition> sprites) => _sprites = sprites;

    public SpriteDefinition? For(string definitionId) => _sprites.GetValueOrDefault(definitionId);

    public static SpriteCatalog Load(string directory = ArtDirectory)
    {
        var path = $"{directory}/sprites.json";
        if (!Godot.FileAccess.FileExists(path))
        {
            GD.PushWarning($"Sem {path}: unidades desenhadas como círculos.");
            return Empty;
        }

        var root = JsonNode.Parse(Godot.FileAccess.GetFileAsString(path))!;
        var layouts = root["layouts"]!.AsObject();
        var sprites = new Dictionary<string, SpriteDefinition>(StringComparer.Ordinal);
        foreach (var (id, node) in root["sprites"]!.AsObject())
        {
            var texturePath = $"{directory}/{(string)node!["texture"]!}";
            var texture = LoadTexture(texturePath);
            if (texture is null)
            {
                GD.Print($"sprite {id}: {texturePath} não encontrado; usando círculo.");
                continue;
            }

            var layout = layouts[(string)node["layout"]!]!;
            var scale = (int?)node["scale"] ?? 1;
            var frame = Pair(layout["frame"]!, Godot.Vector2.Zero);
            var pivot = Pair(layout["pivot"]!, frame / 2);
            var order = layout["dirOrder"]?.AsArray().Select(d => ParseDirection((string)d!)).ToList()
                ?? [SpriteDirection.Up, SpriteDirection.Left, SpriteDirection.Down, SpriteDirection.Right];
            var animations = layout["animations"]!.AsObject().ToDictionary(
                kv => kv.Key, kv => ParseAnimation(kv.Value!, frame, pivot, order), StringComparer.Ordinal);
            sprites[id] = new SpriteDefinition
            {
                Texture = texture,
                Scale = scale,
                HeadPx = ((float?)layout["headPx"] ?? frame.Y) * scale,
                Animations = animations,
                Use = node["use"]?.AsObject().ToDictionary(kv => kv.Key, kv => (string)kv.Value!, StringComparer.Ordinal)
                    ?? new Dictionary<string, string>(),
            };
        }

        return new SpriteCatalog(sprites);
    }

    /// <summary>Textura importada pelo Godot; sem importação (editor ainda não abriu), lê o PNG direto.</summary>
    internal static Texture2D? LoadTexture(string path)
    {
        if (ResourceLoader.Exists(path))
        {
            return GD.Load<Texture2D>(path);
        }

        var absolute = ProjectSettings.GlobalizePath(path);
        if (!System.IO.File.Exists(absolute))
        {
            return null;
        }

        var image = Image.LoadFromFile(absolute);
        return image is null ? null : ImageTexture.CreateFromImage(image);
    }

    private static SpriteAnimation ParseAnimation(JsonNode node, Godot.Vector2 frame, Godot.Vector2 pivot, IReadOnlyList<SpriteDirection> order)
    {
        // Animação com quadro maior que o do layout (armas grandes do gerador LPC: 128 ou 192 px).
        if (node["frame"] is { } ownFrame)
        {
            frame = Pair(ownFrame, frame);
            pivot = node["pivot"] is { } ownPivot ? Pair(ownPivot, frame / 2) : frame / 2;
        }

        var strips = new Dictionary<SpriteDirection, SpriteStrip>();
        if (node["grid"] is { } grid)
        {
            var row = (int)grid["row"]!;
            var col = (int)grid["col"]!;
            var count = (int)grid["count"]!;
            for (var i = 0; i < order.Count; i++)
            {
                strips[order[i]] = new SpriteStrip(col * (int)frame.X, (row + i) * (int)frame.Y, (int)frame.X, (int)frame.Y, count, pivot);
            }
        }
        else if (node["all"] is { } all)
        {
            var strip = ParseStrip(all, frame, pivot);
            foreach (var d in Enum.GetValues<SpriteDirection>())
            {
                strips[d] = strip;
            }
        }
        else
        {
            foreach (var d in Enum.GetValues<SpriteDirection>())
            {
                if (node[d.ToString().ToLowerInvariant()] is { } strip)
                {
                    strips[d] = ParseStrip(strip, frame, pivot);
                }
            }
        }

        return new SpriteAnimation(strips, (float?)node["fps"] ?? 10f, (bool?)node["loop"] ?? false);
    }

    private static SpriteStrip ParseStrip(JsonNode node, Godot.Vector2 frame, Godot.Vector2 pivot)
    {
        var width = (int?)node["w"] ?? (int)frame.X;
        var height = (int?)node["h"] ?? (int)frame.Y;
        return new SpriteStrip((int)node["x"]!, (int)node["y"]!, width, height, (int?)node["count"] ?? 1,
            node["pivot"] is { } p ? Pair(p, pivot) : pivot);
    }

    private static Godot.Vector2 Pair(JsonNode node, Godot.Vector2 fallback) =>
        node is JsonArray { Count: 2 } a ? new Godot.Vector2((float)a[0]!, (float)a[1]!) : fallback;

    private static SpriteDirection ParseDirection(string name) => name switch
    {
        "up" => SpriteDirection.Up,
        "left" => SpriteDirection.Left,
        "down" => SpriteDirection.Down,
        "right" => SpriteDirection.Right,
        _ => throw new InvalidOperationException($"Direção desconhecida em sprites.json: {name}"),
    };
}
