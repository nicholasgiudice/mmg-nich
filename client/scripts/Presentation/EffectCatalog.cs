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

/// <summary>Como um status aparece no personagem (T-412), desenhado sem arte do pacote.</summary>
public enum AuraKind
{
    /// <summary>Anel dourado pulsando nos pés (proteção: Bastião, Juramento, Inabalável).</summary>
    Ring,

    /// <summary>Labaredas em volta do corpo (fúria).</summary>
    Flames,

    /// <summary>Linha do protegido até quem o protege (Interceptação: o dano desviado), mais um anel fino nos pés.</summary>
    Link,

    /// <summary>
    /// T-424: sinais pequenos subindo dos lados do corpo, no formato de <see cref="StatusAura.Glyph"/> (os estados de grupo
    /// do Sacerdote: cura, atributos, velocidade, Mana).
    /// </summary>
    Motes,

    /// <summary>T-424: arcos de vento girando rápido em volta dos pés (velocidade: Graça Veloz).</summary>
    Wind,

    /// <summary>T-424: cúpula de luz translúcida sobre o corpo (Santuário: não cai abaixo de 1 de vida).</summary>
    Dome,

    /// <summary>T-424: selo girando no chão, nos pés (marca num inimigo: Selo da Luz).</summary>
    Sigil,
}

/// <summary>Formato dos sinais de <see cref="AuraKind.Motes"/> (T-424).</summary>
public enum AuraGlyph
{
    /// <summary>Faísca (losango de quatro pontas).</summary>
    Spark,

    /// <summary>Sinal de mais (cura).</summary>
    Plus,

    /// <summary>Gota (Mana).</summary>
    Drop,

    /// <summary>Seta para cima (atributos).</summary>
    Chevron,
}

/// <summary>Sinal de um status no personagem enquanto ele dura (T-412; <paramref name="Glyph"/> só em <see cref="AuraKind.Motes"/>, T-424).</summary>
public sealed record StatusAura(AuraKind Kind, Color Color, AuraGlyph Glyph = AuraGlyph.Spark);

/// <summary>
/// Onda que sai do lançador ao usar uma habilidade em volta dele (T-412: Bastião, Juramento), até o raio. Com
/// <paramref name="AtTarget"/> (T-424: Golpe Sagrado da Ira Divina), sai do primeiro alvo, até o raio dos alvos extras.
/// </summary>
public sealed record CastBurst(Color Color, float Radius, bool AtTarget = false);

/// <summary>Coluna de luz caindo sobre cada alvo da habilidade (T-424: Julgamento).</summary>
public sealed record LightPillar(Color Color);

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
    private readonly List<(string Prefix, StatusAura Aura)> _auras = new();
    private readonly Dictionary<string, CastBurst> _bursts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LightPillar> _pillars = new(StringComparer.Ordinal);

    public EffectAnimation? Shield { get; private set; }

    /// <summary>Sinal do status no personagem (prefixo do id: <c>status.fury</c> vale para <c>status.fury_24</c>).</summary>
    public StatusAura? Aura(string statusId) => Prefixed(_auras, statusId);

    /// <summary>Onda em volta do lançador ao usar a habilidade, ou null.</summary>
    public CastBurst? Burst(string abilityId) => _bursts.GetValueOrDefault(abilityId);

    /// <summary>Coluna de luz nos alvos da habilidade (T-424), ou null.</summary>
    public LightPillar? Pillar(string abilityId) => _pillars.GetValueOrDefault(abilityId);

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

        foreach (var (prefix, node) in root["auras"]?.AsObject() ?? [])
        {
            if (Enum.TryParse<AuraKind>((string?)node!["style"], ignoreCase: true, out var kind))
            {
                var glyph = Enum.TryParse<AuraGlyph>((string?)node["glyph"], ignoreCase: true, out var g) ? g : AuraGlyph.Spark;
                catalog._auras.Add((prefix, new StatusAura(kind, Color.FromHtml((string?)node["color"] ?? "#ffffff"), glyph)));
            }
            else
            {
                GD.PushWarning($"Efeitos: estilo de aura desconhecido em {prefix}.");
            }
        }

        foreach (var (ability, node) in root["bursts"]?.AsObject() ?? [])
        {
            catalog._bursts[ability] = new CastBurst(Color.FromHtml((string?)node!["color"] ?? "#ffffff"),
                (float?)node["radius"] ?? 5f, (string?)node["at"] == "target");
        }

        foreach (var (ability, node) in root["pillars"]?.AsObject() ?? [])
        {
            catalog._pillars[ability] = new LightPillar(Color.FromHtml((string?)node!["color"] ?? "#ffffff"));
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
