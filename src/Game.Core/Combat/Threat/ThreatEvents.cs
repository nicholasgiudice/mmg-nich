using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.Combat.Threat;

public enum AggroReason
{
    Proximity,
    Damage,
    Heal,
    Threat,
    Taunt,

    /// <summary>Um membro do mesmo grupo de spawn entrou em combate.</summary>
    Group,
}

/// <summary>O monstro passou a ter <paramref name="Target"/> na sua tabela de ameaça.</summary>
public sealed record AggroGained(EntityId Monster, EntityId Target, AggroReason Reason) : GameEvent;

/// <summary>O alvo atual do monstro mudou (<see cref="EntityId.None"/> = sem alvo).</summary>
public sealed record TargetChanged(EntityId Monster, EntityId Previous, EntityId Current) : GameEvent;

/// <summary>O monstro foi provocado: é forçado a atacar <paramref name="Taunter"/> até <paramref name="UntilTick"/>.</summary>
public sealed record Taunted(EntityId Taunter, EntityId Monster, long UntilTick) : GameEvent;
