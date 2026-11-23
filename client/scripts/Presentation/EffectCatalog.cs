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

    /// <summary>T-444, Orbes Arcanos: bolinhas girando em volta do corpo, uma por disparo do status.</summary>
    Orbs,

    /// <summary>T-444, Queimadura: três chamas pequenas no corpo e brasas subindo.</summary>
    Burning,

    /// <summary>T-444, Marca Arcana: losango com o número de pilhas ao lado da barra de vida.</summary>
    Mark,

    /// <summary>T-444, Sifão Arcano: anel nos pés e gotas subindo (a Mana sobe mais rápido).</summary>
    Mana,
}

/// <summary>
/// Sinal de um status no personagem enquanto ele dura (T-412). <paramref name="Count"/> é quantas peças desenhar
/// (T-444: orbes, pelo <c>count</c> do <c>current_target</c> do status; pilhas da marca).
/// </summary>
public sealed record StatusAura(AuraKind Kind, Color Color, int Count = 1);

/// <summary>
/// Projétil desenhado pelo cliente (T-444): bola brilhante com rastro, de quem lançou até o alvo. Com
/// <paramref name="Explode"/>, explode no primeiro alvo no raio da explosão da habilidade (Bola de Fogo).
/// </summary>
public sealed record SpellBolt(Color Color, float SizePx, bool Explode, bool Reverse = false);

/// <summary>
/// Raio entre quem canaliza e o alvo enquanto a canalização dura (T-444: Raio Arcano, Feixe do Vazio). Com
/// <paramref name="Grow"/>, fica mais largo e mais claro a cada pulso que já saiu.
/// </summary>
public sealed record ChannelBeam(Color Color, float WidthPx, bool Grow);

/// <summary>
/// Onda que sai do lançador ao usar uma habilidade em volta dele (T-412: Bastião, Juramento), até o raio. Com
/// <paramref name="Blast"/>, uma explosão (T-444: Bola de Fogo, Combustão): clarão cheio no lugar de um anel fino.
/// </summary>
public sealed record CastBurst(Color Color, float Radius, bool Blast = false);

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
    private readonly Dictionary<string, SpellBolt> _bolts = new(StringComparer.Ordinal);
    private readonly List<(string Prefix, SpellBolt Bolt)> _shots = new();
    private readonly List<(string Prefix, CastBurst Burst)> _hitBursts = new();
    private readonly Dictionary<string, Color> _cones = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChannelBeam> _beams = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CastBurst> _meteors = new(StringComparer.Ordinal);

    public EffectAnimation? Shield { get; private set; }

    /// <summary>Sinal do status no personagem (prefixo do id: <c>status.fury</c> vale para <c>status.fury_24</c>).</summary>
    public StatusAura? Aura(string statusId) => Prefixed(_auras, statusId);

    /// <summary>Onda em volta do lançador ao usar a habilidade, ou null.</summary>
    public CastBurst? Burst(string abilityId) => _bursts.GetValueOrDefault(abilityId);

    /// <summary>Impacto de um dano: pela fonte (habilidade/área) se houver, senão pela escola.</summary>
    public EffectAnimation? Impact(string sourceId, DamageSchool school) =>
        Prefixed(_sources, sourceId) ?? _schools.GetValueOrDefault(school);

    public EffectAnimation? Projectile(string abilityId) => _projectiles.GetValueOrDefault(abilityId);

    /// <summary>Projétil desenhado ao usar a habilidade (T-444), ou null.</summary>
    public SpellBolt? Bolt(string abilityId) => _bolts.GetValueOrDefault(abilityId);

    /// <summary>Projétil desenhado a cada dano desta fonte (T-444: disparos dos orbes, pelo prefixo do id do status), ou null.</summary>
    public SpellBolt? Shot(string sourceId) => Prefixed(_shots, sourceId);

    /// <summary>Explosão em cada alvo atingido por esta fonte (T-444: Combustão, Sifão), pelo prefixo do id, ou null.</summary>
    public CastBurst? HitBurst(string sourceId) => Prefixed(_hitBursts, sourceId);

    /// <summary>Cone de fogo desenhado ao usar a habilidade (T-444, Rajada de Chamas): a cor, ou null.</summary>
    public Color? Cone(string abilityId) => _cones.TryGetValue(abilityId, out var c) ? c : null;

    /// <summary>Raio da canalização (T-444), ou null.</summary>
    public ChannelBeam? Beam(string abilityId) => _beams.GetValueOrDefault(abilityId);

    /// <summary>
    /// Meteoro do telegráfo desta habilidade (T-444): uma pedra em chamas caindo durante o aviso e, no fim, a explosão do
    /// tamanho do aviso. A cor; o raio vem do telegráfo.
    /// </summary>
    public CastBurst? Meteor(string abilityId) => _meteors.GetValueOrDefault(abilityId);

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
                catalog._auras.Add((prefix, new StatusAura(kind, Color.FromHtml((string?)node["color"] ?? "#ffffff"))));
            }
            else
            {
                GD.PushWarning($"Efeitos: estilo de aura desconhecido em {prefix}.");
            }
        }

        foreach (var (ability, node) in root["bursts"]?.AsObject() ?? [])
        {
            catalog._bursts[ability] = new CastBurst(Color.FromHtml((string?)node!["color"] ?? "#ffffff"),
                (float?)node["radius"] ?? 5f);
        }

        foreach (var (ability, node) in root["bolts"]?.AsObject() ?? [])
        {
            catalog._bolts[ability] = ReadBolt(node!);
        }

        foreach (var (prefix, node) in root["shots"]?.AsObject() ?? [])
        {
            catalog._shots.Add((prefix, ReadBolt(node!)));
        }

        foreach (var (prefix, node) in root["hitBursts"]?.AsObject() ?? [])
        {
            catalog._hitBursts.Add((prefix, new CastBurst(Color.FromHtml((string?)node!["color"] ?? "#ffffff"),
                (float?)node["radius"] ?? 1f, (bool?)node["blast"] ?? true)));
        }

        foreach (var (ability, node) in root["cones"]?.AsObject() ?? [])
        {
            catalog._cones[ability] = Color.FromHtml((string?)node!["color"] ?? "#ffffff");
        }

        foreach (var (ability, node) in root["beams"]?.AsObject() ?? [])
        {
            catalog._beams[ability] = new ChannelBeam(Color.FromHtml((string?)node!["color"] ?? "#ffffff"),
                (float?)node["width"] ?? 4f, (bool?)node["grow"] ?? false);
        }

        foreach (var (ability, node) in root["meteors"]?.AsObject() ?? [])
        {
            catalog._meteors[ability] = new CastBurst(Color.FromHtml((string?)node!["color"] ?? "#ffffff"), 0f, Blast: true);
        }

        return catalog;

        static SpellBolt ReadBolt(JsonNode node) =>
            new(Color.FromHtml((string?)node["color"] ?? "#ffffff"), (float?)node["size"] ?? 8f, (bool?)node["explode"] ?? false,
                (bool?)node["reverse"] ?? false);

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
