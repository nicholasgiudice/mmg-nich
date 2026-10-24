using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;

namespace Game.Core.Combat.Items;

/// <summary>
/// Itens (T-204, ordem <see cref="SystemOrder.Items"/>, depois da progressão, que sorteia a queda das mortes do tick):
/// trata as intenções de equipar, desequipar, pegar e largar e tira do chão o que passou do prazo.
/// </summary>
public sealed class ItemSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var intent in world.TickIntents)
        {
            switch (intent)
            {
                case EquipItemIntent equip:
                    Handle(world, equip.Entity, ItemAction.Equip, e => Gear.Equip(world, e, equip.InventorySlot));
                    break;
                case UnequipItemIntent unequip:
                    Handle(world, unequip.Entity, ItemAction.Unequip, e => Gear.Unequip(world, e, unequip.Slot));
                    break;
                case PickUpItemIntent pick:
                    Handle(world, pick.Entity, ItemAction.PickUp, e => Gear.PickUp(world, e, pick.Item));
                    break;
                case DropItemIntent drop:
                    Handle(world, drop.Entity, ItemAction.Drop, e => Gear.Drop(world, e, drop.InventorySlot));
                    break;
            }
        }

        foreach (var e in world.Entities)
        {
            if (e.TryGet<GroundItem>() is { } g && world.Tick >= g.ExpiresAtTick)
            {
                world.Remove(e.Id);
                world.Events.Publish(new GroundItemExpired(e.Id, g.Item.BaseId));
            }
        }
    }

    private static void Handle(World world, EntityId id, ItemAction action, Func<Entity, ItemRejectReason?> run)
    {
        if (world.Find(id) is { } e && run(e) is { } reason)
        {
            world.Events.Publish(new ItemRejected(id, action, reason));
        }
    }
}

/// <summary>Formatadores de log dos eventos de itens (T-204).</summary>
public static class ItemLog
{
    public static CombatLogFormatters Register(CombatLogFormatters f)
    {
        f.Register<ItemDropped>((c, e) => c.Entry(
            e, LogCategory.Itens, $"{c.Name(e.Source)} deixou cair {c.SourceLabel(e.BaseId)} nv {e.Level} ({RarityName(e.Rarity)})", e.Source, e.Item));
        f.Register<ItemPickedUp>((c, e) => c.Entry(e, LogCategory.Itens, $"{c.Name(e.Entity)} pegou {c.SourceLabel(e.BaseId)}", e.Entity, e.Item));
        f.Register<ItemEquipped>((c, e) => c.Entry(e, LogCategory.Itens, $"{c.Name(e.Entity)} equipou {c.SourceLabel(e.BaseId)}", e.Entity));
        f.Register<ItemUnequipped>((c, e) => c.Entry(e, LogCategory.Itens, $"{c.Name(e.Entity)} desequipou {c.SourceLabel(e.BaseId)}", e.Entity));
        f.Register<GroundItemExpired>((c, e) => c.Entry(e, LogCategory.Itens, $"{c.SourceLabel(e.BaseId)} sumiu do chão", e.Item));
        f.Register<ItemRejected>((c, e) => c.Entry(e, LogCategory.Itens, $"{c.Name(e.Entity)}: {RejectText(e.Reason)}", e.Entity));
        return f;
    }

    public static string RarityName(ItemRarity r) => r switch
    {
        ItemRarity.Common => "comum",
        ItemRarity.Uncommon => "incomum",
        ItemRarity.Rare => "raro",
        _ => "épico",
    };

    private static string RejectText(ItemRejectReason r) => r switch
    {
        ItemRejectReason.NoInventory => "sem inventário",
        ItemRejectReason.EmptySlot => "espaço vazio",
        ItemRejectReason.UnknownItem => "item desconhecido",
        ItemRejectReason.ClassCannotUse => "a classe não usa este item",
        ItemRejectReason.InventoryFull => "inventário cheio",
        ItemRejectReason.NotFound => "o item não está mais lá",
        ItemRejectReason.NotOwner => "o item não é seu",
        _ => "longe demais para pegar",
    };
}
