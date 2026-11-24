using Game.Core.Definitions;

namespace Game.Core.Spatial;

/// <summary>
/// Terreno que aplica um estado enquanto a unidade está dentro (T-502: a água funda do Brejo, <c>status.deep_water</c>). Vem
/// das áreas de classe <see cref="SlowTerrainClass"/> do mapa do Tiled (<c>design/map-conventions-v0.1.md</c> §6) e é lido
/// pelo <see cref="Combat.Areas.TerrainSystem"/>. Unidade com uma das <see cref="IgnoreTags"/> (tags do monstro) não sente.
/// </summary>
public sealed record TerrainArea
{
    /// <summary>Classe da área no Tiled (camada <c>areas</c>).</summary>
    public const string SlowTerrainClass = "slow_terrain";

    /// <summary>Propriedade obrigatória: id do estado aplicado enquanto a unidade está dentro.</summary>
    public const string StatusProperty = "status";

    /// <summary>Propriedade opcional: tags de monstro que não sentem o terreno, separadas por vírgula.</summary>
    public const string IgnoreTagsProperty = "ignoreTags";

    public required string Name { get; init; }
    public required RectDefinition Area { get; init; }
    public required string Status { get; init; }
    public IReadOnlyList<string> IgnoreTags { get; init; } = [];

    /// <summary>Contém o ponto (o centro da unidade)? Mesma regra de <see cref="RectDefinition.Contains"/>.</summary>
    public bool Contains(System.Numerics.Vector2 p) => Area.Contains(p.X, p.Y);

    /// <summary>Uma unidade com estas tags sente o terreno?</summary>
    public bool Affects(IReadOnlyList<string> tags) => !IgnoreTags.Any(tags.Contains);

    /// <summary>
    /// As áreas <see cref="SlowTerrainClass"/> de um mapa, na ordem do mapa. A carga (<c>TiledMapReader</c>) já garante a
    /// propriedade <c>status</c>; sem ela aqui é erro de programação.
    /// </summary>
    public static IReadOnlyList<TerrainArea> FromAreas(IEnumerable<MapArea> areas) =>
        areas.Where(a => a.Kind == SlowTerrainClass).Select(a => new TerrainArea
        {
            Name = a.Name,
            Area = a.Area,
            Status = a.Properties.GetValueOrDefault(StatusProperty) is { Length: > 0 } s
                ? s
                : throw new InvalidOperationException($"Area '{a.Name}' ({SlowTerrainClass}) has no '{StatusProperty}' property"),
            IgnoreTags = ParseTags(a.Properties.GetValueOrDefault(IgnoreTagsProperty)),
        }).ToList();

    /// <summary>"amphibious, beast" → ["amphibious", "beast"] (sem espaços nem itens vazios).</summary>
    public static IReadOnlyList<string> ParseTags(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
