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

    /// <summary>T-434: corrente pesada nos pés (lento e enfraquecido: Tiro Debilitante).</summary>
    Hobble,

    /// <summary>T-434: gotas de sangue caindo do corpo (sangramento: Ferida Aberta).</summary>
    Drip,

    /// <summary>T-434: olho dourado ao lado da cabeça (Olho de Falcão).</summary>
    Eye,

    /// <summary>T-434: gota com o número de acúmulos ao lado do corpo (Veneno do Caçador).</summary>
    Stacks,
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

/// <summary>
/// Sinal de um status no personagem enquanto ele dura (T-412). <paramref name="Glyph"/>: formato em <see cref="AuraKind.Motes"/>
/// (T-424); <paramref name="Stacks"/>: acúmulos, para <see cref="AuraKind.Stacks"/> (T-434).
/// </summary>
public sealed record StatusAura(AuraKind Kind, Color Color, AuraGlyph Glyph = AuraGlyph.Spark, int Stacks = 0);

/// <summary>
/// Tamanho e brilho de um projétil ou impacto (T-434: o Tiro Certeiro é um tiro mais forte que o Disparo). <c>Scale</c>
/// multiplica o tamanho padrão; <c>Glow</c> = rastro dourado do projétil ou anel que se abre no impacto (null = nada).
/// </summary>
/// <c>OnArrive</c>: impacto tocado quando o projétil chega, para habilidade sem dano na hora (Ferida Aberta).
public sealed record FxStyle(float Scale, Color? Glow, EffectAnimation? OnArrive = null)
{
    public static readonly FxStyle Default = new(1f, null);
}

/// <summary>
/// Onda que sai do lançador ao usar uma habilidade em volta dele (T-412: Bastião, Juramento), até o raio. Com
/// <paramref name="AtTarget"/> (T-424: Golpe Sagrado da Ira Divina), sai do primeiro alvo, até o raio dos alvos extras;
/// com <paramref name="AtAim"/> (T-434, Praga), sai do ponto mirado, até o raio da área da habilidade.
/// </summary>
public sealed record CastBurst(Color Color, float Radius, bool AtTarget = false, bool AtAim = false);

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
    private readonly Dictionary<string, FxStyle> _projectileStyles = new(StringComparer.Ordinal);
    private readonly List<(string Prefix, FxStyle Style)> _impactStyles = new();
    private readonly Dictionary<string, (EffectAnimation Animation, FxStyle Style)> _pierces = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Color> _spreads = new(StringComparer.Ordinal);
    private readonly List<(string Prefix, Color Color)> _jumps = new();
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

    /// <summary>Tamanho e rastro do projétil da habilidade (T-434); sem entrada, o padrão.</summary>
    public FxStyle ProjectileStyle(string abilityId) => _projectileStyles.GetValueOrDefault(abilityId) ?? FxStyle.Default;

    /// <summary>
    /// Flecha que atravessa a linha inteira (T-434, Flecha Perfurante): animação e estilo, ou null. Vai de quem usou até o
    /// fim da linha, na direção mirada, sem parar nos alvos.
    /// </summary>
    public (EffectAnimation Animation, FxStyle Style)? Pierce(string abilityId) =>
        _pierces.TryGetValue(abilityId, out var p) ? p : null;

    /// <summary>Cor das linhas do alvo até quem recebeu a cópia dos status (T-434, Flecha Dispersa), ou null.</summary>
    public Color? Spread(string abilityId) => _spreads.TryGetValue(abilityId, out var c) ? c : null;

    /// <summary>Cor da linha do pulo de um status que acabou para outro alvo (T-434, Praga), por prefixo do id; ou null.</summary>
    public Color? Jump(string statusId) =>
        _jumps.Where(j => statusId.StartsWith(j.Prefix, StringComparison.Ordinal)).OrderByDescending(j => j.Prefix.Length)
            .Select(j => (Color?)j.Color).FirstOrDefault();

    /// <summary>Tamanho e anel do impacto pela fonte do dano (prefixo, T-434); sem entrada, o padrão.</summary>
    public FxStyle ImpactStyle(string sourceId) => Prefixed(_impactStyles, sourceId) ?? FxStyle.Default;

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

        // Fonte e projétil: o nome da animação ou, com tamanho e brilho (T-434), { "animation", "scale", "glow" }.
        foreach (var (prefix, node) in root["impacts"]?["sources"]?.AsObject() ?? [])
        {
            if (Find(AnimationName(node)) is { } a)
            {
                catalog._sources.Add((prefix, a));
                if (Style(node) is { } style)
                {
                    catalog._impactStyles.Add((prefix, style));
                }
            }
        }

        foreach (var (ability, node) in root["projectiles"]?.AsObject() ?? [])
        {
            if (Find(AnimationName(node)) is { } a)
            {
                catalog._projectiles[ability] = a;
                if (Style(node) is { } style)
                {
                    catalog._projectileStyles[ability] = style;
                }
            }
        }

        foreach (var (ability, node) in root["pierces"]?.AsObject() ?? [])
        {
            if (Find(AnimationName(node)) is { } a)
            {
                catalog._pierces[ability] = (a, Style(node) ?? FxStyle.Default);
            }
        }

        foreach (var (ability, node) in root["spreads"]?.AsObject() ?? [])
        {
            catalog._spreads[ability] = Color.FromHtml((string?)node ?? "#ffffff");
        }

        foreach (var (prefix, node) in root["jumps"]?.AsObject() ?? [])
        {
            catalog._jumps.Add((prefix, Color.FromHtml((string?)node ?? "#ffffff")));
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
                (float?)node["radius"] ?? 5f, (string?)node["at"] == "target", (string?)node["at"] == "aim");
        }

        foreach (var (ability, node) in root["pillars"]?.AsObject() ?? [])
        {
            catalog._pillars[ability] = new LightPillar(Color.FromHtml((string?)node!["color"] ?? "#ffffff"));
        }

        return catalog;

        static string? AnimationName(JsonNode? node) => node is JsonObject o ? (string?)o["animation"] : (string?)node;

        FxStyle? Style(JsonNode? node) => node is JsonObject o
            ? new FxStyle((float?)o["scale"] ?? 1f, (string?)o["glow"] is { } glow ? Color.FromHtml(glow) : null,
                Find((string?)o["impact"]))
            : null;

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
