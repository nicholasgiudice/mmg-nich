using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-321: seção <c>shops</c>, regras <c>rules.shops</c> e as lojas reais.</summary>
public class ShopLoaderTests
{
    private static LoadResult Load(string shops, string rules = "{}") => GameDataLoader.LoadSources([("t.json", $$"""
        { "abilities": [ { "id": "item.drink", "name": "Gole", "targeting": { "type": "self" } } ],
          "items": [
            { "id": "consumable.drink", "name": "Poção", "type": "consumable", "stack": 20, "useAbility": "item.drink", "price": 10 },
            { "id": "weapon.blade", "name": "Lâmina", "slot": "weapon" } ],
          "shops": {{shops}}, "rules": { "shops": {{rules}} } }
        """)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private static GameDatabase Real()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "data", "classes.json")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return GameDataLoader.LoadDirectory(Path.Combine(dir!, "data")).GetDatabaseOrThrow();
    }

    [Fact]
    public void Shops_and_rules_load()
    {
        var r = Load(
            """[ { "id": "shop.x", "name": "X", "offers": [ { "item": "consumable.drink" }, { "item": "weapon.blade", "level": 3, "price": 7 } ] } ]""",
            """{ "interactRange": 2, "sellRatio": 0.5, "equipmentValueBase": 1, "equipmentValuePerLevel": 2, "maxQuantity": 9 }""");

        Assert.True(r.Success, Errors(r));
        var shop = r.Database!.Shops["shop.x"];
        Assert.Equal(("X", 2), (shop.Name, shop.Offers.Count));
        Assert.Equal(new ShopOfferDefinition { Item = "consumable.drink" }, shop.Offers[0]);
        Assert.Equal(new ShopOfferDefinition { Item = "weapon.blade", Level = 3, Price = 7 }, shop.Offers[1]);
        var rules = r.Database.Rules.Shops;
        Assert.Equal((2.0, 0.5, 1.0, 2.0, 9), (rules.InteractRange, rules.SellRatio, rules.EquipmentValueBase, rules.EquipmentValuePerLevel, rules.MaxQuantity));
    }

    [Theory]
    [InlineData("""[ { "id": "shop.x", "name": "X", "offers": [ { "item": "weapon.nope" } ] } ]""", "weapon.nope")]
    [InlineData("""[ { "id": "shop.x", "name": "X", "offers": [ { "item": "weapon.blade", "level": 0 } ] } ]""", "level")]
    [InlineData("""[ { "id": "shop.x", "name": "X", "offers": [ { "item": "consumable.drink", "level": 2 } ] } ]""", "Consumível não tem nível")]
    [InlineData("""[ { "id": "shop.x", "name": "X", "offers": [ { "item": "weapon.blade", "price": -1 } ] } ]""", "price")]
    [InlineData("""[ { "id": "shop.x", "name": "X", "offers": [ { "item": "weapon.blade" }, { "item": "weapon.blade" } ] } ]""", "repetida")]
    [InlineData("""[ { "id": "shop.x", "name": "" } ]""", "Nome vazio")]
    [InlineData("""[ { "id": "shop.x", "name": "X" }, { "id": "shop.x", "name": "Y" } ]""", "shop.x")]
    [InlineData("""[ { "id": "Shop X", "name": "X" } ]""", "inválido")]
    public void Invalid_shops_are_errors(string shops, string expected)
    {
        var r = Load(shops);
        Assert.False(r.Success);
        Assert.Contains(expected, Errors(r));
    }

    [Theory]
    [InlineData("""{ "interactRange": 0 }""", "interactRange")]
    [InlineData("""{ "sellRatio": 1.5 }""", "sellRatio")]
    [InlineData("""{ "equipmentValueBase": -1 }""", "equipmentValueBase")]
    [InlineData("""{ "equipmentRarityMultiplier": { "rare": -1 } }""", "equipmentRarityMultiplier.rare")]
    [InlineData("""{ "maxQuantity": 0 }""", "maxQuantity")]
    public void Invalid_shop_rules_are_errors(string rules, string expected)
    {
        var r = Load("[]", rules);
        Assert.False(r.Success);
        Assert.Contains(expected, Errors(r));
    }

    [Fact]
    public void Real_shops_point_to_existing_items_and_every_npc_with_shop_is_in_a_safe_zone()
    {
        var db = Real();
        Assert.Equal(["shop.smith", "shop.temple"], db.Shops.Keys.Order(StringComparer.Ordinal));
        Assert.All(db.Shops.Values.SelectMany(s => s.Offers), o => Assert.True(db.Items.ContainsKey(o.Item), o.Item));

        var npcs = db.Zones.Values.SelectMany(z => z.Npcs.Select(n => (Zone: z, Npc: n))).ToList();
        Assert.All(npcs.Where(x => x.Npc.Shop is not null), x =>
        {
            Assert.Equal(ZoneKind.Safe, x.Zone.Kind);
            Assert.True(db.Shops.ContainsKey(x.Npc.Shop!), x.Npc.Shop);
        });
        Assert.Equal(["shop.smith", "shop.temple"], npcs.Select(x => x.Npc.Shop).OfType<string>().Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Temple_sells_both_potions_and_the_smith_common_weapons_and_armor_from_level_2_to_6()
    {
        var db = Real();
        Assert.Equal(
            ["consumable.minor_healing_potion", "consumable.minor_mana_potion"],
            db.Shops["shop.temple"].Offers.Select(o => o.Item).Order(StringComparer.Ordinal));

        var smith = db.Shops["shop.smith"].Offers;
        var gear = db.Items.Values.Where(i => i.Slot is EquipSlot.Weapon || ItemBaseDefinition.IsArmorSlot(i.Slot)).Select(i => i.Id).ToHashSet();
        Assert.Equal(gear, smith.Select(o => o.Item).ToHashSet());
        Assert.All(gear, id => Assert.Equal([2, 3, 4, 5, 6], smith.Where(o => o.Item == id).Select(o => o.Level).Order()));
    }
}
