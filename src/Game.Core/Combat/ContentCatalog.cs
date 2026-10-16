using Game.Core.Definitions;

namespace Game.Core.Combat;

/// <summary>
/// Definições de conteúdo já carregadas, entregues ao Core (que não referencia Game.Data).
/// Quem carrega os dados (servidor, cliente, testes) monta o catálogo a partir do <c>GameDatabase</c>.
/// </summary>
public sealed class ContentCatalog
{
    public static readonly ContentCatalog Empty = new();

    public ContentCatalog(
        IEnumerable<ClassDefinition>? classes = null,
        IEnumerable<AbilityDefinition>? abilities = null,
        IEnumerable<StatusDefinition>? statuses = null,
        IEnumerable<AreaDefinition>? areas = null,
        IEnumerable<MonsterDefinition>? monsters = null,
        IEnumerable<ArenaDefinition>? arenas = null,
        IEnumerable<EncounterDefinition>? encounters = null)
    {
        Classes = Index(classes, x => x.Id);
        Abilities = Index(abilities, x => x.Id);
        Statuses = Index(statuses, x => x.Id);
        Areas = Index(areas, x => x.Id);
        Monsters = Index(monsters, x => x.Id);
        Arenas = Index(arenas, x => x.Id);
        Encounters = Index(encounters, x => x.Id);
    }

    public IReadOnlyDictionary<string, ClassDefinition> Classes { get; }
    public IReadOnlyDictionary<string, AbilityDefinition> Abilities { get; }
    public IReadOnlyDictionary<string, StatusDefinition> Statuses { get; }
    public IReadOnlyDictionary<string, AreaDefinition> Areas { get; }
    public IReadOnlyDictionary<string, MonsterDefinition> Monsters { get; }
    public IReadOnlyDictionary<string, ArenaDefinition> Arenas { get; }
    public IReadOnlyDictionary<string, EncounterDefinition> Encounters { get; }

    private static Dictionary<string, T> Index<T>(IEnumerable<T>? items, Func<T, string> id) =>
        (items ?? []).ToDictionary(id, StringComparer.Ordinal);
}
