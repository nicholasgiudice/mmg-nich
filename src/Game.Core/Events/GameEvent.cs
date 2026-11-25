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

/// <summary>
/// T-503: um monstro de campo de um ponto com <c>announce</c> nasceu (<paramref name="Spawned"/> = true) ou morreu (false).
/// Vai para todos os jogadores da zona, perto ou longe: por isso não cita a entidade (o filtro de interesse só deixa passar
/// evento sem entidade para todos). <paramref name="Area"/> é o nome da subárea do ponto (null se o ponto não está em nenhuma).
/// </summary>
public sealed record FieldBossAnnounced(string MonsterId, string? Area, bool Spawned) : GameEvent;
