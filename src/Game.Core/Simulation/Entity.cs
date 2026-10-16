using Game.Core.Simulation.Components;

namespace Game.Core.Simulation;

/// <summary>Entidade = id + conjunto de componentes (no máximo um por tipo concreto).</summary>
public sealed class Entity
{
    private readonly Dictionary<Type, IComponent> _components = new();

    internal Entity(EntityId id)
    {
        Id = id;
    }

    public EntityId Id { get; }

    /// <summary>True depois de <see cref="World.Remove"/>. Sistemas que guardam referências devem checar.</summary>
    public bool IsRemoved { get; internal set; }

    /// <summary>Viva = não removida e (sem Health, ou Health vivo).</summary>
    public bool IsAlive => !IsRemoved && (TryGet<Health>()?.IsAlive ?? true);

    public Entity Add<T>(T component)
        where T : class, IComponent
    {
        _components[typeof(T)] = component;
        return this;
    }

    internal void AddByRuntimeType(IComponent component) => _components[component.GetType()] = component;

    public bool Has<T>()
        where T : class, IComponent => _components.ContainsKey(typeof(T));

    public T? TryGet<T>()
        where T : class, IComponent => _components.TryGetValue(typeof(T), out var c) ? (T)c : null;

    public T Get<T>()
        where T : class, IComponent =>
        TryGet<T>() ?? throw new InvalidOperationException($"Entity {Id} has no {typeof(T).Name}");

    public bool Remove<T>()
        where T : class, IComponent => _components.Remove(typeof(T));

    /// <summary>Componentes em ordem determinística (nome do tipo).</summary>
    internal List<KeyValuePair<Type, IComponent>> OrderedComponents() =>
        _components.OrderBy(kv => kv.Key.FullName, StringComparer.Ordinal).ToList();
}
