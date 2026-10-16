using System.Numerics;
using Game.Core.Simulation;

namespace Game.Core.Intents;

/// <summary>Pedido de uma entidade (jogador, IA ou rede). Nunca aplica efeito por si só.</summary>
public abstract record Intent(EntityId Entity);

public sealed record MoveIntent(EntityId Entity, Vector2 Direction) : Intent(Entity);

public sealed record StopIntent(EntityId Entity) : Intent(Entity);

/// <summary>Tratada pelo <c>AbilitySystem</c> (Combat), que lê <c>world.TickIntents</c>.</summary>
public sealed record UseAbilityIntent(EntityId Entity, string AbilityId, EntityId? Target = null, Vector2? AimPoint = null)
    : Intent(Entity);
