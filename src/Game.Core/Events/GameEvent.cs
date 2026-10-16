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

public sealed record EntityRemoved(EntityId Entity) : GameEvent;
