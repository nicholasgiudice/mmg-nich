namespace Game.Core.Simulation;

/// <summary>
/// Cópia imutável de uma entidade (componentes clonados; alterá-los não afeta o mundo).
/// O cliente de rede monta snapshots recebidos com <see cref="Create"/>.
/// </summary>
public sealed class EntitySnapshot
{
    // Poucos componentes por entidade: busca linear num array é mais barata que um dicionário por snapshot.
    private readonly IComponent[] _components;

    private EntitySnapshot(EntityId id, IComponent[] components)
    {
        Id = id;
        _components = components;
    }

    public EntityId Id { get; }

    /// <summary>Componentes em ordem determinística (nome do tipo), para serialização.</summary>
    public IReadOnlyList<IComponent> Components => _components;

    /// <summary>
    /// Monta um snapshot a partir de componentes já prontos (ex.: recebidos pela rede). Os componentes são
    /// usados como estão (sem clonar) e ordenados pelo nome do tipo; tipo repetido é erro.
    /// </summary>
    public static EntitySnapshot Create(EntityId id, IEnumerable<IComponent> components)
    {
        var array = components.OrderBy(c => c.GetType().FullName, StringComparer.Ordinal).ToArray();
        for (var i = 1; i < array.Length; i++)
        {
            if (array[i].GetType() == array[i - 1].GetType())
            {
                throw new ArgumentException($"Componente repetido em {id}: {array[i].GetType().Name}", nameof(components));
            }
        }

        return new EntitySnapshot(id, array);
    }

    /// <summary>Usado pelo <see cref="World"/>: componentes já clonados e em ordem.</summary>
    internal static EntitySnapshot FromOrdered(EntityId id, IComponent[] components) => new(id, components);

    public T? TryGet<T>()
        where T : class, IComponent
    {
        foreach (var c in _components)
        {
            if (c.GetType() == typeof(T))
            {
                return (T)c;
            }
        }

        return null;
    }

    public T Get<T>()
        where T : class, IComponent =>
        TryGet<T>() ?? throw new InvalidOperationException($"Snapshot {Id} has no {typeof(T).Name}");
}

public sealed record WorldSnapshot(long Tick, IReadOnlyList<EntitySnapshot> Entities);
