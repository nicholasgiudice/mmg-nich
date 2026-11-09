using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.Combat.Items;

/// <summary>Ação de item pedida por intenção (T-204).</summary>
public enum ItemAction { Equip, Unequip, PickUp, Drop, Use }

/// <summary>Motivo de rejeição de uma intenção de item (a UI traduz).</summary>
public enum ItemRejectReason
{
    /// <summary>A entidade não tem inventário nem equipamento.</summary>
    NoInventory,

    /// <summary>Espaço do inventário fora do intervalo ou vazio, ou nada equipado no espaço pedido.</summary>
    EmptySlot,

    /// <summary>O item aponta para uma base que não existe no catálogo.</summary>
    UnknownItem,
    ClassCannotUse,
    InventoryFull,

    /// <summary>O item no chão não existe mais (alguém pegou ou sumiu).</summary>
    NotFound,

    /// <summary>Quem pediu não tem direito ao item no chão.</summary>
    NotOwner,
    TooFar,

    /// <summary>T-320: equipar um item que não é equipamento (consumível).</summary>
    NotEquippable,

    /// <summary>T-320: usar um item que não é consumível.</summary>
    NotConsumable,

    /// <summary>T-320: a habilidade do consumível está em recarga (as pilhas do mesmo item dividem a recarga).</summary>
    OnCooldown,

    /// <summary>T-320: morto não usa item.</summary>
    Dead,

    /// <summary>T-320: a habilidade do item não pode ser usada agora (conjurando, atordoado, sem alvo...).</summary>
    CannotUseNow,
}

/// <summary>Item foi para o chão: queda de monstro ou largado por um personagem (<paramref name="Source"/>).</summary>
public sealed record ItemDropped(EntityId Item, EntityId Source, string BaseId, int Level, ItemRarity Rarity) : GameEvent;

/// <summary>
/// Consumível usado (T-320): a habilidade <paramref name="AbilityId"/> foi executada e saiu 1 unidade do espaço
/// <paramref name="InventorySlot"/>; <paramref name="Remaining"/> = o que sobrou nele (0 = espaço vazio).
/// </summary>
public sealed record ItemUsed(EntityId Entity, int InventorySlot, string BaseId, string AbilityId, int Remaining) : GameEvent;

/// <summary>Ouro recebido (T-320): a parte de <paramref name="Entity"/> pela morte de <paramref name="Monster"/>.</summary>
public sealed record GoldGained(EntityId Entity, int Amount, EntityId Monster) : GameEvent;

/// <summary>Item do chão foi para o espaço <paramref name="InventorySlot"/> do inventário.</summary>
public sealed record ItemPickedUp(EntityId Entity, EntityId Item, string BaseId, int InventorySlot) : GameEvent;

/// <summary>Item equipado no espaço; <paramref name="PreviousId"/> = base do que voltou ao inventário ("" se nada).</summary>
public sealed record ItemEquipped(EntityId Entity, EquipSlot Slot, string BaseId, string PreviousId) : GameEvent;

/// <summary>Item desequipado, foi para o espaço <paramref name="InventorySlot"/> do inventário.</summary>
public sealed record ItemUnequipped(EntityId Entity, EquipSlot Slot, string BaseId, int InventorySlot) : GameEvent;

/// <summary>Item no chão sumiu por tempo.</summary>
public sealed record GroundItemExpired(EntityId Item, string BaseId) : GameEvent;

public sealed record ItemRejected(EntityId Entity, ItemAction Action, ItemRejectReason Reason) : GameEvent;
