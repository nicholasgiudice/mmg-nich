using System.Text.Json.Nodes;
using Game.Core.Definitions;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>Animação de efeito (T-054): quadros de uma folha, em ordem.</summary>
public sealed record EffectAnimation(Texture2D Texture, IReadOnlyList<Rect2> Frames, float Fps, bool Loop, bool Rotate)
{
    /// <summary>Duração de uma passada (s).</summary>
    public double Duration => Frames.Count / Math.Max(Fps, 0.01f);

    /// <summary>Quadro no tempo <paramref name="t"/> (s); sem laço, fica no último.</summary>
    public Rect2 FrameAt(double t)
    {
        var i = (int)(t * Fps);
        return Frames[Loop ? i % Frames.Count : Math.Clamp(i, 0, Frames.Count - 1)];
    }

    /// <summary>Desenha o quadro centrado em <paramref name="center"/>, com <paramref name="size"/> pixels de lado.</summary>
    public void Draw(CanvasItem canvas, Vector2 center, float size, double t, Color modulate)
    {
        var frame = FrameAt(t);
        var scale = size / Math.Max(frame.Size.X, frame.Size.Y);
        var drawn = frame.Size * scale;
        canvas.DrawTextureRectRegion(Texture, new Rect2(center - drawn / 2, drawn), frame, modulate);
    }
}

/// <summary>Arte de uma área no chão: um quadro do tamanho da área (<c>fill</c>) ou vários espalhados (<c>scatter</c>).</summary>
public sealed record AreaEffect(EffectAnimation Animation, bool Scatter);

/// <summary>
/// Efeitos visuais por cima da apresentação (T-054), lidos de <c>assets/art/effects/effects.json</c>: impacto por
/// escola de dano ou pela fonte, projétil por habilidade, arte das áreas e a bolha de escudo. Só visual: o Core não sabe
/// que isso existe. Sem o arquivo, tudo fica como antes (formas desenhadas).
/// </summary>
public sealed class EffectCatalog
{
    public static readonly EffectCatalog Empty = new();

    private readonly Dictionary<DamageSchool, EffectAnimation> _schools = new();
    private readonly List<(string Prefix, EffectAnimation Animation)> _sources = new();
    private readonly Dictionary<string, EffectAnimation> _projectiles = new(StringComparer.Ordinal);
    private readonly List<(string Prefix, AreaEffect Effect)> _areas = new();

    public EffectAnimation? Shield { get; private set; }

    /// <summary>Impacto de um dano: pela fonte (habilidade/área) se houver, senão pela escola.</summary>
    public EffectAnimation? Impact(string sourceId, DamageSchool school) =>
        Prefixed(_sources, sourceId) ?? _schools.GetValueOrDefault(school);

    public EffectAnimation? Projectile(string abilityId) => _projectiles.GetValueOrDefault(abilityId);

    public AreaEffect? Area(string areaId) => Prefixed(_areas, areaId);

    private static T? Prefixed<T>(List<(string Prefix, T Value)> list, string id)
        where T : class =>
        list.Where(p => id.StartsWith(p.Prefix, StringComparison.Ordinal)).OrderByDescending(p => p.Prefix.Length)
            .Select(p => p.Value).FirstOrDefault();

    public static EffectCatalog Load(string directory = SpriteCatalog.ArtDirectory)
    {
        var path = $"{directory}/effects/effects.json";
        if (!Godot.FileAccess.FileExists(path))
        {
            GD.PushWarning($"Sem {path}: efeitos só com formas desenhadas.");
            return Empty;
        }

        var root = JsonNode.Parse(Godot.FileAccess.GetFileAsString(path))!;
        var sheets = new Dictionary<string, (Texture2D Texture, Vector2 Cell)>(StringComparer.Ordinal);
        foreach (var (id, node) in root["sheets"]!.AsObject())
        {
            var texturePath = $"{directory}/{(string)node!["texture"]!}";
            if (SpriteCatalog.LoadTexture(texturePath) is not { } texture)
            {
                GD.PushWarning($"Efeitos: {texturePath} não encontrado.");
                continue;
            }

            var cell = node["cell"]!.AsArray();
            sheets[id] = (texture, new Vector2((float)cell[0]!, (float)cell[1]!));
        }

        var animations = new Dictionary<string, EffectAnimation>(StringComparer.Ordinal);
        foreach (var (id, node) in root["animations"]!.AsObject())
        {
            if (!sheets.TryGetValue((string)node!["sheet"]!, out var sheet))
            {
                continue;
            }

            var frames = node["frames"]!.AsArray()
                .Select(f => new Rect2((int)f![0]! * sheet.Cell.X, (int)f[1]! * sheet.Cell.Y, sheet.Cell.X, sheet.Cell.Y))
                .ToList();
            animations[id] = new EffectAnimation(sheet.Texture, frames, (float?)node["fps"] ?? 12f,
                (bool?)node["loop"] ?? false, (bool?)node["rotate"] ?? false);
        }

        var catalog = new EffectCatalog { Shield = Find((string?)root["shield"]) };
        foreach (var (school, anim) in root["impacts"]?["schools"]?.AsObject() ?? [])
        {
            if (Enum.TryParse<DamageSchool>(school, ignoreCase: true, out var s) && Find((string?)anim) is { } a)
            {
                catalog._schools[s] = a;
            }
        }

        foreach (var (prefix, anim) in root["impacts"]?["sources"]?.AsObject() ?? [])
        {
            if (Find((string?)anim) is { } a)
            {
                catalog._sources.Add((prefix, a));
            }
        }

        foreach (var (ability, anim) in root["projectiles"]?.AsObject() ?? [])
        {
            if (Find((string?)anim) is { } a)
            {
                catalog._projectiles[ability] = a;
            }
        }

        foreach (var (prefix, node) in root["areas"]?.AsObject() ?? [])
        {
            if (Find((string?)node!["animation"]) is { } a)
            {
                catalog._areas.Add((prefix, new AreaEffect(a, (string?)node["mode"] != "fill")));
            }
        }

        return catalog;

        EffectAnimation? Find(string? id)
        {
            if (id is null)
            {
                return null;
            }

            if (!animations.TryGetValue(id, out var found))
            {
                GD.PushWarning($"Efeitos: animação {id} não existe.");
            }

            return found;
        }
    }
}
