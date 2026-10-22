using System.Collections.Concurrent;
using System.Reflection;
using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Server.Interest;

/// <summary>
/// Entidades citadas por um evento (T-106: eventos filtrados por interesse). Lê, por reflexão feita uma vez por tipo,
/// todas as propriedades <see cref="EntityId"/>, <c>EntityId?</c> e listas de <see cref="EntityId"/> — evento novo do
/// Core entra sem código aqui.
/// </summary>
public static class EventEntities
{
    private static readonly ConcurrentDictionary<Type, Func<GameEvent, IEnumerable<EntityId>>> Readers = new();

    public static IEnumerable<EntityId> Of(GameEvent e) => Readers.GetOrAdd(e.GetType(), Build)(e);

    /// <summary>
    /// O evento vai para quem vê (ou acabou de ver) alguma entidade citada; evento sem entidade vai para todos.
    /// <c>EntityId.None</c> (sem fonte) não conta.
    /// </summary>
    public static bool Relevant(GameEvent e, ISet<EntityId> visible, IReadOnlyCollection<EntityId> known)
    {
        var any = false;
        foreach (var id in Of(e))
        {
            if (id.IsNone)
            {
                continue;
            }

            any = true;
            if (visible.Contains(id) || known.Contains(id))
            {
                return true;
            }
        }

        return !any;
    }

    private static Func<GameEvent, IEnumerable<EntityId>> Build(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(EntityId) || p.PropertyType == typeof(EntityId?)
                || typeof(IEnumerable<EntityId>).IsAssignableFrom(p.PropertyType))
            .ToArray();
        return e =>
        {
            var ids = new List<EntityId>();
            foreach (var p in props)
            {
                switch (p.GetValue(e))
                {
                    case EntityId id:
                        ids.Add(id);
                        break;
                    case IEnumerable<EntityId> list:
                        ids.AddRange(list);
                        break;
                }
            }

            return ids;
        };
    }
}
