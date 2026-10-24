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

    /// <summary>T-204: <see cref="NetIntent.Slot"/> = espaço do inventário.</summary>
    EquipItem,

    /// <summary>T-204: <see cref="NetIntent.Slot"/> = espaço de equipamento (<see cref="EquipSlot"/>).</summary>
    UnequipItem,

    /// <summary>T-204: <see cref="NetIntent.Target"/> = item no chão.</summary>
    PickUpItem,

    /// <summary>T-204: <see cref="NetIntent.Slot"/> = espaço do inventário.</summary>
    DropItem,
}

/// <summary>
/// Intenção como vai pela rede. Não leva a entidade: o servidor usa sempre a entidade controlada pela conexão
/// (um cliente não pode mandar intenção por outra entidade).
/// </summary>
[MemoryPackable]
public sealed partial record NetIntent(
    NetIntentKind Kind,
    Vector2 Direction,
    string? AbilityId,
    EntityId? Target,
    Vector2? AimPoint,
    AttributeKind Attribute = AttributeKind.Str,
    int Slot = 0)
{
    public static NetIntent From(Intent intent) => intent switch
    {
        MoveIntent m => new NetIntent(NetIntentKind.Move, m.Direction, null, null, null),
        StopIntent => new NetIntent(NetIntentKind.Stop, default, null, null, null),
        UseAbilityIntent u => new NetIntent(NetIntentKind.UseAbility, default, u.AbilityId, u.Target, u.AimPoint),
        SpendAttributePointIntent a => new NetIntent(NetIntentKind.SpendAttributePoint, default, null, null, null, a.Attribute),
        RaiseSkillRankIntent r => new NetIntent(NetIntentKind.RaiseSkillRank, default, r.AbilityId, null, null),
        ResetPointsIntent => new NetIntent(NetIntentKind.ResetPoints, default, null, null, null),
        EquipItemIntent q => new NetIntent(NetIntentKind.EquipItem, default, null, null, null, Slot: q.InventorySlot),
        UnequipItemIntent u => new NetIntent(NetIntentKind.UnequipItem, default, null, null, null, Slot: (int)u.Slot),
        PickUpItemIntent p => new NetIntent(NetIntentKind.PickUpItem, default, null, p.Item, null),
        DropItemIntent d => new NetIntent(NetIntentKind.DropItem, default, null, null, null, Slot: d.InventorySlot),
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
        NetIntentKind.EquipItem => new EquipItemIntent(entity, Slot),
        NetIntentKind.UnequipItem when Enum.IsDefined((EquipSlot)Slot) => new UnequipItemIntent(entity, (EquipSlot)Slot),
        NetIntentKind.PickUpItem when Target is { } item => new PickUpItemIntent(entity, item),
        NetIntentKind.DropItem => new DropItemIntent(entity, Slot),
        _ => null,
    };
}
