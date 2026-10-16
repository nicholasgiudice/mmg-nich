using Game.Core.Definitions;

namespace Game.Data;

/// <summary>Conteúdo do jogo já validado, indexado por id (somente leitura).</summary>
public sealed class GameDatabase
{
    public IReadOnlyDictionary<string, ClassDefinition> Classes { get; }
    public IReadOnlyDictionary<string, AbilityDefinition> Abilities { get; }
    public IReadOnlyDictionary<string, StatusDefinition> Statuses { get; }
    public IReadOnlyDictionary<string, AreaDefinition> Areas { get; }
    public IReadOnlyDictionary<string, MonsterDefinition> Monsters { get; }
    public IReadOnlyDictionary<string, ArenaDefinition> Arenas { get; }
    public IReadOnlyDictionary<string, EncounterDefinition> Encounters { get; }

    /// <summary>Regras globais (seção "rules"); seções ausentes = padrões neutros.</summary>
    public RulesDefinition Rules { get; init; } = new();

    public GameDatabase(
        IEnumerable<ClassDefinition> classes,
        IEnumerable<AbilityDefinition> abilities,
        IEnumerable<StatusDefinition> statuses,
        IEnumerable<AreaDefinition> areas,
        IEnumerable<MonsterDefinition> monsters,
        IEnumerable<ArenaDefinition> arenas,
        IEnumerable<EncounterDefinition> encounters)
    {
        Classes = Index(classes, x => x.Id);
        Abilities = Index(abilities, x => x.Id);
        Statuses = Index(statuses, x => x.Id);
        Areas = Index(areas, x => x.Id);
        Monsters = Index(monsters, x => x.Id);
        Arenas = Index(arenas, x => x.Id);
        Encounters = Index(encounters, x => x.Id);
    }

    private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> items, Func<T, string> id) =>
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, T>(
            items.ToDictionary(id, StringComparer.Ordinal));
}
