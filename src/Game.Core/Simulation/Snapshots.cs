namespace Game.Core.Simulation;

/// <summary>Cópia imutável de uma entidade (componentes clonados; alterá-los não afeta o mundo).</summary>
public sealed class EntitySnapshot
{
    private readonly Dictionary<Type, IComponent> _components;

    internal EntitySnapshot(EntityId id, Dictionary<Type, IComponent> components)
    {
        Id = id;
        _components = components;
    }

    public EntityId Id { get; }

    public T? TryGet<T>()
        where T : class, IComponent => _components.TryGetValue(typeof(T), out var c) ? (T)c : null;

    public T Get<T>()
        where T : class, IComponent =>
        TryGet<T>() ?? throw new InvalidOperationException($"Snapshot {Id} has no {typeof(T).Name}");
}

public sealed record WorldSnapshot(long Tick, IReadOnlyList<EntitySnapshot> Entities);
