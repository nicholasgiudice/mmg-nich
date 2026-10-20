using Game.Core.Simulation;

namespace Game.Core.Events;

/// <summary>
/// Base de todo evento. O tick é carimbado pelo <see cref="EventBus"/> ao publicar.
/// Para criar um evento: <c>public sealed record Foo(EntityId X) : GameEvent;</c> e
/// <c>world.Events.Publish(new Foo(...))</c>.
/// </summary>
public abstract record GameEvent
{
    public long Tick { get; internal set; }
}

public sealed record EntitySpawned(EntityId Entity) : GameEvent;

public sealed record EntityDied(EntityId Entity) : GameEvent;

/// <summary>Entidade caída voltou à vida (T-061, Milagre) com <paramref name="Health"/> de vida. By = quem levantou.</summary>
public sealed record EntityRevived(EntityId Entity, EntityId By, string SourceId, float Health) : GameEvent;

public sealed record EntityRemoved(EntityId Entity) : GameEvent;
