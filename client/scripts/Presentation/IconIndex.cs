using System.Text.Json.Nodes;
using Game.Core.Combat;
using Game.Core.Definitions;

namespace MmoRpgClient.Presentation;

/// <summary>Região de um ícone na folha (pixels, quadrado de <see cref="IconIndex.Size"/>).</summary>
public readonly record struct IconRegion(int X, int Y);

/// <summary>
/// Índice de <c>assets/art/icons/icons.json</c> (T-080), sem Godot: id → região na folha. O <see cref="IconCatalog"/>
/// transforma as regiões em texturas; os testes usam este índice direto para conferir a cobertura.
/// </summary>
public sealed class IconIndex
{
    public required string Sheet { get; init; }
    public required int Size { get; init; }
    public required string Fallback { get; init; }
    public required IReadOnlyDictionary<string, IconRegion> Icons { get; init; }

    public static IconIndex Parse(string json)
    {
        var root = JsonNode.Parse(json)!;
        return new IconIndex
        {
            Sheet = (string)root["sheet"]!,
            Size = (int)root["size"]!,
            Fallback = (string)root["fallback"]!,
            Icons = root["icons"]!.AsObject().ToDictionary(
                kv => kv.Key, kv => new IconRegion((int)kv.Value!["x"]!, (int)kv.Value["y"]!), StringComparer.Ordinal),
        };
    }

    /// <summary>Chave do ícone de um atributo (<c>attribute.str</c> ...).</summary>
    public static string AttributeId(AttributeKind attribute) => $"attribute.{attribute.ToString().ToLowerInvariant()}";

    public const string GroundItemId = "ui.ground_item";

    /// <summary>
    /// Ids que as telas mostram com ícone: ataque básico e habilidades de cada classe, itens base (T-320: inclusive os
    /// consumíveis), atributos e o item no chão.
    /// </summary>
    public static IEnumerable<string> RequiredIds(ContentCatalog content) =>
        content.Classes.Values
            .SelectMany(c => c.Abilities.Prepend(c.BasicAttack))
            .Concat(content.Items.Keys)
            .Concat(Enum.GetValues<AttributeKind>().Select(AttributeId))
            .Append(GroundItemId)
            .Distinct(StringComparer.Ordinal);

    /// <summary>Ids exigidos que não têm ícone próprio (cairiam no <see cref="Fallback"/>).</summary>
    public IReadOnlyList<string> Missing(ContentCatalog content) =>
        RequiredIds(content).Where(id => !Icons.ContainsKey(id)).Order(StringComparer.Ordinal).ToList();
}
