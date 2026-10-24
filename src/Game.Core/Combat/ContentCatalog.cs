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
        IEnumerable<EncounterDefinition>? encounters = null,
        IEnumerable<ItemBaseDefinition>? items = null)
    {
        Classes = Index(classes, x => x.Id);
        Abilities = Index(abilities, x => x.Id);
        Statuses = Index(statuses, x => x.Id);
        Areas = Index(areas, x => x.Id);
        Monsters = Index(monsters, x => x.Id);
        Arenas = Index(arenas, x => x.Id);
        Encounters = Index(encounters, x => x.Id);
        Items = Index(items, x => x.Id);

        // NH pré-calculados: o catálogo é imutável e compartilhado entre mundos (inclusive em paralelo).
        foreach (var a in Abilities.Values)
        {
            for (var rank = 1; rank <= a.HighestRank; rank++)
            {
                _ranked[(a.Id, rank)] = AbilityRanks.Resolve(a, rank);
            }
        }
    }

    private readonly Dictionary<(string Id, int Rank), AbilityDefinition> _ranked = new();

    /// <summary>Regras globais de ameaça (<c>data/rules.json</c>, seção "threat"). T-011.</summary>
    public Threat.ThreatRules Threat { get; init; } = new();

    public IReadOnlyDictionary<string, ClassDefinition> Classes { get; }
    public IReadOnlyDictionary<string, AbilityDefinition> Abilities { get; }
    public IReadOnlyDictionary<string, StatusDefinition> Statuses { get; }
    public IReadOnlyDictionary<string, AreaDefinition> Areas { get; }
    public IReadOnlyDictionary<string, MonsterDefinition> Monsters { get; }
    public IReadOnlyDictionary<string, ArenaDefinition> Arenas { get; }
    public IReadOnlyDictionary<string, EncounterDefinition> Encounters { get; }

    /// <summary>Itens base (T-204). As regras de afixos e queda estão em <see cref="RulesDefinition.Items"/>.</summary>
    public IReadOnlyDictionary<string, ItemBaseDefinition> Items { get; }

    /// <summary>Regras globais (data/rules.json). Seções ausentes = padrões neutros.</summary>
    public RulesDefinition Rules { get; init; } = new();

    /// <summary>
    /// Impressão digital dos dados de origem (calculada por quem carrega o conteúdo, ex.: <c>Game.Data</c>).
    /// Vazia em conteúdo montado à mão. Cliente e servidor só jogam juntos com o mesmo valor (T-101).
    /// </summary>
    public string Fingerprint { get; init; } = "";

    /// <summary>
    /// Definição efetiva de uma habilidade no NH <paramref name="rank"/> (T-060), limitado ao intervalo que a
    /// habilidade tem. Null se a habilidade não existe.
    /// </summary>
    public AbilityDefinition? AbilityAt(string id, int rank)
    {
        if (!Abilities.TryGetValue(id, out var ability))
        {
            return null;
        }

        return _ranked[(id, Math.Clamp(rank, 1, ability.HighestRank))];
    }

    private static Dictionary<string, T> Index<T>(IEnumerable<T>? items, Func<T, string> id) =>
        (items ?? []).ToDictionary(id, StringComparer.Ordinal);
}
