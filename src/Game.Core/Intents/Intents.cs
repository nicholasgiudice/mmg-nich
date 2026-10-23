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

/// <summary>Gasta um ponto de atributo livre (T-203). Tratada pelo <c>ProgressionSystem</c>.</summary>
public sealed record SpendAttributePointIntent(EntityId Entity, Definitions.AttributeKind Attribute) : Intent(Entity);

/// <summary>Gasta um ponto de habilidade para subir o NH (NH0 → NH1 aprende a habilidade, T-203).</summary>
public sealed record RaiseSkillRankIntent(EntityId Entity, string AbilityId) : Intent(Entity);

/// <summary>Devolve todos os pontos de atributo e de habilidade (T-203). Só fora de combate.</summary>
public sealed record ResetPointsIntent(EntityId Entity) : Intent(Entity);
