using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Shops;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-321: NPCs com loja, compra e venda.</summary>
public class ShopTests
{
    private static readonly ContentCatalog Content = RealContent.Value;
    private const string Potion = "consumable.minor_healing_potion";

    private static NpcPlacementDefinition Placement(string shop) =>
        Content.Zones["zone.city"].Npcs.Single(n => n.Shop == shop);

    private static int OfferOf(string shop, string item, int level = 1) =>
        Content.Shops[shop].Offers.Select((o, i) => (o, i)).Single(x => x.o.Item == item && x.o.Level == level).i;

    /// <summary>Mundo com o NPC da loja e um personagem (inventário vazio) a <paramref name="distance"/> tiles dele.</summary>
    private static (World World, Entity Hero, Entity Npc) Setup(string shop = "shop.temple", float distance = 1, long gold = 0)
    {
        var w = WorldBuilder.CreateDefault(3, null, Content);
        var npc = Shop.SpawnNpc(w, Placement(shop));
        var at = npc.Get<Transform>().Position + new Vector2(distance, 0);
        var hero = Leveling.SpawnCharacter(w, Content.Classes["class.warrior"], at);
        var inv = hero.Get<Inventory>();
        for (var i = 0; i < inv.Capacity; i++)
        {
            inv[i] = null;
        }

        hero.Get<Wallet>().Gold = gold;
        return (w, hero, npc);
    }

    private static List<GameEvent> Step(World w, Intent intent)
    {
        w.Enqueue(intent);
        w.Step();
        return w.LastTickEvents.ToList();
    }

    private static ShopRejectReason Rejection(List<GameEvent> events) => Assert.Single(events.OfType<ShopRejected>()).Reason;

    // ---------- NPC ----------

    [Fact]
    public void Npc_has_no_health_nor_faction_and_cannot_be_targeted()
    {
        var (w, hero, npc) = Setup();
        var info = npc.Get<NpcInfo>();
        Assert.Equal(("npc.priest", "Sacerdote", "shop.temple"), (info.DefinitionId, info.Name, info.Shop));
        Assert.Null(npc.TryGet<Health>());
        Assert.Null(npc.TryGet<FactionTag>());

        // Alvo único (tiro do Arqueiro) recusa o NPC; o golpe em arco do Guerreiro, em cima dele, não o atinge.
        var archer = Leveling.SpawnCharacter(w, Content.Classes["class.archer"], hero.Get<Transform>().Position);
        var shot = Step(w, new UseAbilityIntent(archer.Id, "archer.shot", npc.Id));
        Assert.Equal(AbilityRejectReason.InvalidTarget, Assert.Single(shot.OfType<AbilityRejected>()).Reason);
        var swing = Step(w, new UseAbilityIntent(hero.Id, hero.Get<AbilityState>().BasicAttackId!, npc.Id, npc.Get<Transform>().Position));
        Assert.DoesNotContain(swing.OfType<DamageDealt>(), d => d.Target == npc.Id);
        Assert.DoesNotContain(npc.Id, swing.OfType<AbilityExecuted>().SelectMany(x => x.Targets));
    }

    // ---------- compra ----------

    [Fact]
    public void Buying_potions_takes_the_gold_and_stacks_them()
    {
        var price = Shop.PriceOf(Content, Content.Shops["shop.temple"].Offers[OfferOf("shop.temple", Potion)]);
        Assert.Equal(Content.Items[Potion].Price, price);
        var (w, hero, npc) = Setup(gold: (price * 25) + 3);

        var events = Step(w, new BuyIntent(hero.Id, npc.Id, OfferOf("shop.temple", Potion), 25));

        var bought = Assert.Single(events.OfType<ItemBought>());
        Assert.Equal((Potion, 25, price * 25, npc.Id), (bought.BaseId, bought.Quantity, bought.Gold, bought.Npc));
        Assert.Equal(3, hero.Get<Wallet>().Gold);
        Assert.Equal([20, 5], hero.Get<Inventory>().Slots.Where(i => i is not null).Select(i => i!.Count));
    }

    [Fact]
    public void Buying_equipment_gives_a_common_item_at_the_offer_level()
    {
        var offer = OfferOf("shop.smith", "weapon.sword", 4);
        var price = Shop.PriceOf(Content, Content.Shops["shop.smith"].Offers[offer]);
        var (w, hero, npc) = Setup("shop.smith", gold: price * 2);

        Assert.Single(Step(w, new BuyIntent(hero.Id, npc.Id, offer, 2)).OfType<ItemBought>());

        Assert.Equal(0, hero.Get<Wallet>().Gold);
        var items = hero.Get<Inventory>().Slots.Where(i => i is not null).ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(ItemInstance.Common("weapon.sword", 4), i));
    }

    [Fact]
    public void Buying_without_gold_is_refused_and_changes_nothing()
    {
        var (w, hero, npc) = Setup(gold: 9);
        Assert.Equal(ShopRejectReason.NotEnoughGold, Rejection(Step(w, new BuyIntent(hero.Id, npc.Id, OfferOf("shop.temple", Potion), 1))));
        Assert.Equal(9, hero.Get<Wallet>().Gold);
        Assert.All(hero.Get<Inventory>().Slots, Assert.Null);
    }

    [Fact]
    public void Buying_without_room_is_refused_but_partial_stacks_count_as_room()
    {
        var (w, hero, npc) = Setup(gold: 10_000);
        var inv = hero.Get<Inventory>();
        for (var i = 0; i < inv.Capacity; i++)
        {
            inv[i] = ItemInstance.Common("armor.light_hands", 1);
        }

        inv[0] = ItemInstance.Stack(Potion, 18);
        var offer = OfferOf("shop.temple", Potion);

        Assert.Equal(ShopRejectReason.InventoryFull, Rejection(Step(w, new BuyIntent(hero.Id, npc.Id, offer, 3))));
        Assert.Equal(10_000, hero.Get<Wallet>().Gold);
        Assert.Single(Step(w, new BuyIntent(hero.Id, npc.Id, offer, 2)).OfType<ItemBought>());
        Assert.Equal(20, inv[0]!.Count);
    }

    [Fact]
    public void Buying_from_far_away_or_with_a_bad_offer_is_refused()
    {
        var (w, hero, npc) = Setup(distance: (float)Content.Rules.Shops.InteractRange + 0.5f, gold: 100);
        Assert.Equal(ShopRejectReason.TooFar, Rejection(Step(w, new BuyIntent(hero.Id, npc.Id, 0, 1))));

        hero.Get<Transform>().Position = npc.Get<Transform>().Position;
        Assert.Equal(ShopRejectReason.InvalidOffer, Rejection(Step(w, new BuyIntent(hero.Id, npc.Id, 99, 1))));
        Assert.Equal(ShopRejectReason.InvalidOffer, Rejection(Step(w, new BuyIntent(hero.Id, npc.Id, -1, 1))));
        Assert.Equal(ShopRejectReason.InvalidQuantity, Rejection(Step(w, new BuyIntent(hero.Id, npc.Id, 0, 0))));
        Assert.Equal(ShopRejectReason.NoShop, Rejection(Step(w, new BuyIntent(hero.Id, hero.Id, 0, 1))));
        Assert.Equal(100, hero.Get<Wallet>().Gold);
    }

    [Fact]
    public void A_npc_without_shop_does_not_trade()
    {
        var w = WorldBuilder.CreateDefault(3, null, Content);
        var inn = Shop.SpawnNpc(w, Content.Zones["zone.city"].Npcs.Single(n => n.Npc == "npc.innkeeper"));
        var hero = Leveling.SpawnCharacter(w, Content.Classes["class.warrior"], inn.Get<Transform>().Position);
        Assert.Equal(ShopRejectReason.NoShop, Rejection(Step(w, new SellIntent(hero.Id, inn.Id, 0, 1))));
    }

    // ---------- venda ----------

    [Fact]
    public void Selling_part_of_a_stack_pays_the_sell_ratio_of_the_price()
    {
        var (w, hero, npc) = Setup("shop.smith"); // vende a qualquer NPC com loja
        hero.Get<Inventory>()[2] = ItemInstance.Stack(Potion, 7);
        var each = (int)Math.Floor(Content.Rules.Shops.SellRatio * Content.Items[Potion].Price);

        var sold = Assert.Single(Step(w, new SellIntent(hero.Id, npc.Id, 2, 4)).OfType<ItemSold>());

        Assert.Equal((Potion, 2, 4, each * 4), (sold.BaseId, sold.InventorySlot, sold.Quantity, sold.Gold));
        Assert.Equal(each * 4, hero.Get<Wallet>().Gold);
        Assert.Equal(3, hero.Get<Inventory>()[2]!.Count);

        Assert.Equal(ShopRejectReason.InvalidQuantity, Rejection(Step(w, new SellIntent(hero.Id, npc.Id, 2, 4))));
        Assert.Single(Step(w, new SellIntent(hero.Id, npc.Id, 2, 3)).OfType<ItemSold>());
        Assert.Null(hero.Get<Inventory>()[2]);
    }

    [Fact]
    public void Selling_equipment_uses_the_level_and_rarity_formula()
    {
        var rules = Content.Rules.Shops;
        var (w, hero, npc) = Setup();
        var ring = new ItemInstance("accessory.ring", 5, ItemRarity.Rare, ["affix.dex", "affix.str"]);
        hero.Get<Inventory>()[0] = ring;
        var value = Math.Round((rules.EquipmentValueBase + (rules.EquipmentValuePerLevel * 5)) * rules.EquipmentRarityMultiplier.Rare, MidpointRounding.AwayFromZero);

        var sold = Assert.Single(Step(w, new SellIntent(hero.Id, npc.Id, 0, 1)).OfType<ItemSold>());

        Assert.Equal((int)Math.Floor(rules.SellRatio * value), sold.Gold);
        Assert.Equal(sold.Gold, Shop.SellPriceOf(Content, ring));
        Assert.Null(hero.Get<Inventory>()[0]);
    }

    [Fact]
    public void Equipped_items_cannot_be_sold()
    {
        var (w, hero, npc) = Setup();
        Assert.NotNull(hero.Get<Equipment>()[EquipSlot.Weapon]);

        // O equipado não está no inventário: nenhum espaço do inventário aponta para ele.
        Assert.Equal(ShopRejectReason.EmptySlot, Rejection(Step(w, new SellIntent(hero.Id, npc.Id, 0, 1))));
        Assert.Equal(ShopRejectReason.EmptySlot, Rejection(Step(w, new SellIntent(hero.Id, npc.Id, 99, 1))));
        Assert.NotNull(hero.Get<Equipment>()[EquipSlot.Weapon]);
        Assert.Equal(0, hero.Get<Wallet>().Gold);
    }

    [Fact]
    public void Prices_of_the_smith_grow_with_the_item_level()
    {
        var smith = Content.Shops["shop.smith"];
        var swords = smith.Offers.Where(o => o.Item == "weapon.sword").OrderBy(o => o.Level).ToList();
        Assert.Equal([2, 3, 4, 5, 6], swords.Select(o => o.Level));
        var prices = swords.Select(o => Shop.PriceOf(Content, o)).ToList();
        Assert.Equal(prices.Order(), prices);
        Assert.True(prices[0] > 0);
        Assert.All(smith.Offers, o => Assert.False(Content.Items[o.Item].IsConsumable));
    }
}
