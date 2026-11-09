using System.Numerics;
using Game.Core.Combat.Items;
using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Shops;

/// <summary>
/// Lojas (T-321, ordem <see cref="SystemOrder.Shops"/>, depois dos itens): trata <see cref="BuyIntent"/> e
/// <see cref="SellIntent"/>. Cada pedido é atômico: valida tudo, depois mexe no ouro e no inventário no mesmo tick e publica
/// <see cref="ItemBought"/>/<see cref="ItemSold"/>, ou nada muda e sai <see cref="ShopRejected"/>.
/// </summary>
public sealed class ShopSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var intent in world.TickIntents)
        {
            switch (intent)
            {
                case BuyIntent buy when world.Find(buy.Entity) is { } e:
                    Publish(world, e, ShopAction.Buy, Shop.Buy(world, e, buy.Npc, buy.Offer, buy.Quantity));
                    break;
                case SellIntent sell when world.Find(sell.Entity) is { } e:
                    Publish(world, e, ShopAction.Sell, Shop.Sell(world, e, sell.Npc, sell.InventorySlot, sell.Quantity));
                    break;
            }
        }
    }

    private static void Publish(World world, Entity e, ShopAction action, ShopRejectReason? reason)
    {
        if (reason is { } r)
        {
            world.Events.Publish(new ShopRejected(e.Id, action, r));
        }
    }
}

/// <summary>Regras de compra e venda (T-321). Os métodos servem também a servidor e testes; null = feito.</summary>
public static class Shop
{
    /// <summary>Cria o NPC do mapa na zona (T-321): só posição e <see cref="NpcInfo"/>. O nome vem da propriedade <c>name</c>.</summary>
    public static Entity SpawnNpc(World world, NpcPlacementDefinition placement) =>
        world.Spawn(
            new Transform { Position = new Vector2((float)placement.Position.X, (float)placement.Position.Y) },
            new NpcInfo
            {
                DefinitionId = placement.Npc,
                Name = placement.Properties.GetValueOrDefault("name") ?? placement.Name,
                Shop = placement.Shop,
            });

    /// <summary>Valor do item para a loja: <c>price</c> do consumível ou a fórmula de equipamento (<see cref="ShopRules"/>).</summary>
    public static int ValueOf(ContentCatalog content, ItemInstance item) =>
        content.Items.TryGetValue(item.BaseId, out var def) ? ValueOf(content.Rules.Shops, def, item.Level, item.Rarity) : 0;

    public static int ValueOf(ShopRules rules, ItemBaseDefinition def, int level, ItemRarity rarity) =>
        def.IsConsumable
            ? def.Price
            : (int)Math.Round((rules.EquipmentValueBase + (rules.EquipmentValuePerLevel * level)) * rules.EquipmentRarityMultiplier.For(rarity), MidpointRounding.AwayFromZero);

    /// <summary>Preço de compra por unidade de uma oferta (o declarado ou o valor do item comum no nível da oferta).</summary>
    public static int PriceOf(ContentCatalog content, ShopOfferDefinition offer) =>
        offer.Price ?? (content.Items.TryGetValue(offer.Item, out var def) ? ValueOf(content.Rules.Shops, def, offer.Level, ItemRarity.Common) : 0);

    /// <summary>Quanto a loja paga por unidade: <see cref="ShopRules.SellRatio"/> × valor, para baixo.</summary>
    public static int SellPriceOf(ContentCatalog content, ItemInstance item) =>
        (int)Math.Floor(content.Rules.Shops.SellRatio * ValueOf(content, item));

    /// <summary>Compra <paramref name="quantity"/> unidades da oferta <paramref name="offerIndex"/> da loja do NPC.</summary>
    public static ShopRejectReason? Buy(World world, Entity e, EntityId npcId, int offerIndex, int quantity)
    {
        if (Reach(world, e, npcId, out var shop, out var wallet, out var inv) is { } reason)
        {
            return reason;
        }

        if (offerIndex < 0 || offerIndex >= shop!.Offers.Count || !world.Content.Items.TryGetValue(shop.Offers[offerIndex].Item, out var def))
        {
            return ShopRejectReason.InvalidOffer;
        }

        if (quantity < 1 || quantity > world.Content.Rules.Shops.MaxQuantity)
        {
            return ShopRejectReason.InvalidQuantity;
        }

        var offer = shop.Offers[offerIndex];
        var total = (long)PriceOf(world.Content, offer) * quantity;
        if (wallet!.Gold < total)
        {
            return ShopRejectReason.NotEnoughGold;
        }

        var item = def.IsConsumable ? ItemInstance.Stack(def.Id, quantity) : ItemInstance.Common(def.Id, offer.Level);
        var fits = def.IsConsumable ? Gear.Room(world, inv!, item) >= quantity : inv!.Slots.Count(s => s is null) >= quantity;
        if (!fits)
        {
            return ShopRejectReason.InventoryFull;
        }

        for (var n = def.IsConsumable ? 1 : quantity; n > 0; n--)
        {
            Gear.AddToInventory(world, e, item);
        }

        wallet.Gold -= total;
        world.Events.Publish(new ItemBought(e.Id, npcId, def.Id, item.Level, quantity, (int)total));
        return null;
    }

    /// <summary>Vende <paramref name="quantity"/> unidades do espaço do inventário a qualquer NPC com loja (sem recompra).</summary>
    public static ShopRejectReason? Sell(World world, Entity e, EntityId npcId, int inventorySlot, int quantity)
    {
        if (Reach(world, e, npcId, out _, out var wallet, out var inv) is { } reason)
        {
            return reason;
        }

        if (inventorySlot < 0 || inventorySlot >= inv!.Capacity || inv[inventorySlot] is not { } item)
        {
            return ShopRejectReason.EmptySlot;
        }

        if (!world.Content.Items.ContainsKey(item.BaseId))
        {
            return ShopRejectReason.CannotSell;
        }

        if (quantity < 1 || quantity > item.Count)
        {
            return ShopRejectReason.InvalidQuantity;
        }

        var total = (long)SellPriceOf(world.Content, item) * quantity;
        inv[inventorySlot] = quantity == item.Count ? null : item.WithCount(item.Count - quantity);
        wallet!.Gold += total;
        world.Events.Publish(new ItemSold(e.Id, npcId, item.BaseId, inventorySlot, quantity, (int)total));
        return null;
    }

    /// <summary>NPC com loja ao alcance, e quem pede tem inventário e carteira.</summary>
    private static ShopRejectReason? Reach(World world, Entity e, EntityId npcId, out ShopDefinition? shop, out Wallet? wallet, out Inventory? inv)
    {
        shop = null;
        wallet = e.TryGet<Wallet>();
        inv = e.TryGet<Inventory>();
        if (wallet is null || inv is null)
        {
            return ShopRejectReason.NoInventory;
        }

        if (world.Find(npcId) is not { IsRemoved: false } npc || npc.TryGet<NpcInfo>()?.Shop is not { } shopId
            || !world.Content.Shops.TryGetValue(shopId, out shop))
        {
            return ShopRejectReason.NoShop;
        }

        var range = (float)world.Content.Rules.Shops.InteractRange;
        var from = e.TryGet<Transform>()?.Position ?? Vector2.Zero;
        var to = npc.TryGet<Transform>()?.Position ?? Vector2.Zero;
        return Vector2.DistanceSquared(from, to) > range * range ? ShopRejectReason.TooFar : null;
    }
}

/// <summary>Formatadores de log das lojas (T-321, categoria Itens).</summary>
public static class ShopLog
{
    public static CombatLogFormatters Register(CombatLogFormatters f)
    {
        f.Register<ItemBought>((c, e) => c.Entry(
            e, LogCategory.Itens, $"{c.Name(e.Entity)} comprou {Units(e.Quantity)}{c.SourceLabel(e.BaseId)} por {e.Gold} de ouro", e.Entity));
        f.Register<ItemSold>((c, e) => c.Entry(
            e, LogCategory.Itens, $"{c.Name(e.Entity)} vendeu {Units(e.Quantity)}{c.SourceLabel(e.BaseId)} por {e.Gold} de ouro", e.Entity));
        f.Register<ShopRejected>((c, e) => c.Entry(e, LogCategory.Itens, $"{c.Name(e.Entity)}: {RejectText(e.Reason)}", e.Entity));
        return f;
    }

    private static string Units(int n) => n == 1 ? "" : $"{n}× ";

    public static string RejectText(ShopRejectReason r) => r switch
    {
        ShopRejectReason.NoShop => "ninguém para negociar",
        ShopRejectReason.TooFar => "longe demais da loja",
        ShopRejectReason.InvalidOffer => "oferta inválida",
        ShopRejectReason.InvalidQuantity => "quantidade inválida",
        ShopRejectReason.NotEnoughGold => "ouro insuficiente",
        ShopRejectReason.InventoryFull => "inventário cheio",
        ShopRejectReason.NoInventory => "sem inventário",
        ShopRejectReason.EmptySlot => "espaço vazio",
        _ => "a loja não compra este item",
    };
}
