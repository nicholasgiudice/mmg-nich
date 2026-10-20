using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.Combat;

/// <summary>Motivo de rejeição de uma intenção de habilidade (a UI traduz: "Em recarga", "Sem Mana"...).</summary>
public enum AbilityRejectReason
{
    Dead,
    AlreadyCasting,

    /// <summary>Genérico para gates (ex.: stun). Gates podem preferir <see cref="Stunned"/>/<see cref="Silenced"/>.</summary>
    CannotAct,
    Stunned,
    Silenced,
    UnknownAbility,
    OnCooldown,
    NotEnoughResource,
    InvalidTarget,
    OutOfRange,
    NoLineOfSight,

    /// <summary>Habilidade com <c>charges</c> sem usos restantes neste encontro (T-017).</summary>
    NoCharges,
}

public enum CastInterruptReason
{
    /// <summary>Entidade moveu-se numa habilidade sem castWhileMoving.</summary>
    Moved,

    /// <summary>Efeito <c>interrupt</c>.</summary>
    Interrupted,
    Died,

    /// <summary>Impedida por status (stun/silence): quem cancela usa <see cref="CastControl.Cancel"/>.</summary>
    Disabled,

    /// <summary>Cancelamento voluntário.</summary>
    Cancelled,
}

public enum ShieldEndReason { Expired, Depleted }

/// <summary>Conjuração (ou uso instantâneo) começou. Instantâneas têm CastTicks = 0 e AbilityExecuted no mesmo tick.</summary>
public sealed record AbilityStarted(
    EntityId Source, string AbilityId, int CastTicks, long EndTick, EntityId? Target, Vector2? AimPoint) : GameEvent;

/// <summary>
/// Habilidade resolvida. Custo e recarga já aplicados. Targets = alvos resolvidos (em ordem de id).
/// ChargesRemaining (T-017) = usos restantes depois deste, ou null se a habilidade tem usos ilimitados.
/// </summary>
public sealed record AbilityExecuted(
    EntityId Source, string AbilityId, IReadOnlyList<EntityId> Targets, string? CostResource, float CostAmount,
    bool IsBasicAttack, long CooldownReadyTick, int? ChargesRemaining = null) : GameEvent;

public sealed record AbilityRejected(EntityId Source, string AbilityId, AbilityRejectReason Reason) : GameEvent;

/// <summary>
/// Acerto extra de uma habilidade com vários acertos (T-060): <paramref name="HitIndex"/> vai de 2 a
/// <paramref name="HitCount"/>. O primeiro acerto é o próprio <see cref="AbilityExecuted"/>.
/// </summary>
public sealed record AbilityHit(EntityId Source, string AbilityId, int HitIndex, int HitCount, IReadOnlyList<EntityId> Targets) : GameEvent;

public sealed record CastInterrupted(EntityId Entity, string AbilityId, CastInterruptReason Reason, EntityId? By) : GameEvent;

/// <summary>
/// Dano resolvido. Amount = dano final após multiplicadores/resistência/arredondamento;
/// Absorbed = parte absorvida por escudos; Applied = vida realmente removida; Overkill = Amount - Absorbed - Applied.
/// Source é None para dano sem fonte. SourceId = id da habilidade/status/área de origem.
/// </summary>
public sealed record DamageDealt(
    EntityId Source, EntityId Target, string SourceId, DamageSchool School, float Amount, float Absorbed,
    float Applied, float Overkill, bool Killed) : GameEvent;

/// <summary>Cura. Amount = valor após arredondamento; Effective = vida realmente recuperada; Overheal = excedente.</summary>
public sealed record HealApplied(
    EntityId Source, EntityId Target, string SourceId, float Amount, float Effective, float Overheal) : GameEvent;

public sealed record ShieldApplied(
    EntityId Source, EntityId Target, string SourceId, int ShieldId, float Amount, long ExpiresAtTick) : GameEvent;

public sealed record ShieldExpired(EntityId Entity, int ShieldId, float Remaining, ShieldEndReason Reason) : GameEvent;

/// <summary>Deslocamento forçado (dash/knockback). Distance = percorrida de fato (pode ser menor que a pedida).</summary>
public sealed record Displaced(
    EntityId Source, EntityId Entity, string SourceId, DisplacementKind Kind, Vector2 From, Vector2 To, float Distance) : GameEvent;
