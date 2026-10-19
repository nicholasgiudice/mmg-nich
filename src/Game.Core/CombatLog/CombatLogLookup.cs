using Game.Core.Combat;
using Game.Core.Simulation;

namespace Game.Core.CombatLog;

/// <summary>
/// O que o log de combate precisa saber além dos eventos (T-102): nomes das unidades, definições de conteúdo e o
/// tick. Permite formatar o log tanto com o <see cref="World"/> (local/servidor) quanto só com o que o cliente de
/// rede recebe (snapshot + conteúdo).
/// </summary>
public interface ICombatLogLookup
{
    ContentCatalog Content { get; }

    /// <summary>Tick do estado consultado (o último tick concluído).</summary>
    long Tick { get; }

    /// <summary>Nome de exibição de uma unidade presente agora; null se não existe ou não tem nome.</summary>
    string? UnitName(EntityId id);

    /// <summary>Unidades com nome presentes agora (o gravador guarda os nomes para entidades removidas depois).</summary>
    IEnumerable<(EntityId Id, string Name)> NamedUnits();
}

/// <summary>Consulta sobre o mundo da simulação (cliente local e servidor).</summary>
public sealed class WorldLogLookup(World world) : ICombatLogLookup
{
    public ContentCatalog Content => world.Content;

    public long Tick => world.Tick;

    public string? UnitName(EntityId id) => world.Find(id)?.TryGet<UnitInfo>()?.Name;

    public IEnumerable<(EntityId Id, string Name)> NamedUnits()
    {
        foreach (var e in world.Entities)
        {
            if (e.TryGet<UnitInfo>() is { } unit)
            {
                yield return (e.Id, unit.Name);
            }
        }
    }
}

/// <summary>Consulta sobre um snapshot recebido + o conteúdo carregado pelo cliente (cliente de rede).</summary>
public sealed class SnapshotLogLookup : ICombatLogLookup
{
    private readonly Dictionary<EntityId, string> _names = new();

    public SnapshotLogLookup(WorldSnapshot snapshot, ContentCatalog content)
    {
        Content = content;
        Tick = snapshot.Tick;
        foreach (var e in snapshot.Entities)
        {
            if (e.TryGet<UnitInfo>() is { } unit)
            {
                _names[e.Id] = unit.Name;
            }
        }
    }

    public ContentCatalog Content { get; }

    public long Tick { get; }

    public string? UnitName(EntityId id) => _names.TryGetValue(id, out var name) ? name : null;

    public IEnumerable<(EntityId Id, string Name)> NamedUnits() => _names.Select(kv => (kv.Key, kv.Value));
}
