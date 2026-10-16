using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.Combat.Status;

/// <summary>Status novo aplicado. Source = None se sem fonte. Duration = duração efetiva em ticks (após regras de controle).</summary>
public sealed record StatusApplied(
    EntityId Source, EntityId Target, string StatusId, int InstanceId, int Stacks, long ExpiresAtTick, int DurationTicks) : GameEvent;

/// <summary>Status já existente teve a duração renovada (modo Refresh, ou Stack já no máximo).</summary>
public sealed record StatusRefreshed(
    EntityId Source, EntityId Target, string StatusId, int InstanceId, int Stacks, long ExpiresAtTick) : GameEvent;

/// <summary>Um acúmulo a mais (modo Stack, abaixo do máximo). Stacks = total agora.</summary>
public sealed record StatusStacked(
    EntityId Source, EntityId Target, string StatusId, int InstanceId, int Stacks, long ExpiresAtTick) : GameEvent;

/// <summary>Acabou o tempo.</summary>
public sealed record StatusExpired(EntityId Entity, string StatusId, int InstanceId) : GameEvent;

/// <summary>Removido antes do fim (purificação, dissipar...). By = quem removeu (None se sem fonte).</summary>
public sealed record StatusRemoved(EntityId Entity, string StatusId, int InstanceId, EntityId By) : GameEvent;

/// <summary>Aplicação de controle ignorada (imunidade por retorno decrescente ou multiplicador de rank 0).</summary>
public sealed record StatusResisted(EntityId Source, EntityId Target, string StatusId) : GameEvent;
