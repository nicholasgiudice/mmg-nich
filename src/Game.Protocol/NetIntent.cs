using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using MemoryPack;

namespace Game.Protocol;

public enum NetIntentKind : byte
{
    Move,
    Stop,
    UseAbility,

    /// <summary>T-203: usa <see cref="NetIntent.Attribute"/>.</summary>
    SpendAttributePoint,

    /// <summary>T-203: usa <see cref="NetIntent.AbilityId"/>.</summary>
    RaiseSkillRank,
    ResetPoints,
}

/// <summary>
/// Intenção como vai pela rede. Não leva a entidade: o servidor usa sempre a entidade controlada pela conexão
/// (um cliente não pode mandar intenção por outra entidade).
/// </summary>
[MemoryPackable]
public sealed partial record NetIntent(
    NetIntentKind Kind, Vector2 Direction, string? AbilityId, EntityId? Target, Vector2? AimPoint, AttributeKind Attribute = AttributeKind.Str)
{
    public static NetIntent From(Intent intent) => intent switch
    {
        MoveIntent m => new NetIntent(NetIntentKind.Move, m.Direction, null, null, null),
        StopIntent => new NetIntent(NetIntentKind.Stop, default, null, null, null),
        UseAbilityIntent u => new NetIntent(NetIntentKind.UseAbility, default, u.AbilityId, u.Target, u.AimPoint),
        SpendAttributePointIntent a => new NetIntent(NetIntentKind.SpendAttributePoint, default, null, null, null, a.Attribute),
        RaiseSkillRankIntent r => new NetIntent(NetIntentKind.RaiseSkillRank, default, r.AbilityId, null, null),
        ResetPointsIntent => new NetIntent(NetIntentKind.ResetPoints, default, null, null, null),
        _ => throw new NotSupportedException($"Intenção sem formato de rede: {intent.GetType().Name}"),
    };

    /// <summary>A intenção do Core para a entidade da conexão; null se a mensagem é inválida.</summary>
    public Intent? ToIntent(EntityId entity) => Kind switch
    {
        NetIntentKind.Move => float.IsFinite(Direction.X) && float.IsFinite(Direction.Y) ? new MoveIntent(entity, Direction) : null,
        NetIntentKind.Stop => new StopIntent(entity),
        NetIntentKind.UseAbility when !string.IsNullOrEmpty(AbilityId) => new UseAbilityIntent(entity, AbilityId, Target, AimPoint),
        NetIntentKind.SpendAttributePoint when Enum.IsDefined(Attribute) => new SpendAttributePointIntent(entity, Attribute),
        NetIntentKind.RaiseSkillRank when !string.IsNullOrEmpty(AbilityId) => new RaiseSkillRankIntent(entity, AbilityId),
        NetIntentKind.ResetPoints => new ResetPointsIntent(entity),
        _ => null,
    };
}
