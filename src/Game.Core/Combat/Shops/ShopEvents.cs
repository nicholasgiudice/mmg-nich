using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.Combat.Shops;

public enum ShopAction { Buy, Sell }

/// <summary>Motivo de recusa de compra ou venda (T-321; a UI traduz).</summary>
public enum ShopRejectReason
{
    /// <summary>O NPC não existe, não é NPC ou não tem loja.</summary>
    NoShop,
    TooFar,

    /// <summary>Índice de oferta fora da loja, ou oferta que aponta para item que não existe mais.</summary>
    InvalidOffer,

    /// <summary>Quantidade menor que 1, acima do máximo por pedido ou acima do que há no espaço.</summary>
    InvalidQuantity,
    NotEnoughGold,
    InventoryFull,

    /// <summary>Sem inventário ou carteira.</summary>
    NoInventory,

    /// <summary>Espaço do inventário fora do intervalo ou vazio (item equipado não está no inventário: não se vende).</summary>
    EmptySlot,

    /// <summary>Item que o catálogo não conhece: a loja não sabe o valor.</summary>
    CannotSell,
}

/// <summary>Compra feita (T-321): <paramref name="Quantity"/> unidades de <paramref name="BaseId"/> por <paramref name="Gold"/> no total.</summary>
public sealed record ItemBought(EntityId Entity, EntityId Npc, string BaseId, int Level, int Quantity, int Gold) : GameEvent;

/// <summary>Venda feita (T-321): saíram <paramref name="Quantity"/> unidades do espaço <paramref name="InventorySlot"/> por <paramref name="Gold"/>.</summary>
public sealed record ItemSold(EntityId Entity, EntityId Npc, string BaseId, int InventorySlot, int Quantity, int Gold) : GameEvent;

public sealed record ShopRejected(EntityId Entity, ShopAction Action, ShopRejectReason Reason) : GameEvent;
