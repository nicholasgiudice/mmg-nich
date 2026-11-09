using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-204: seção <c>items</c>, regras <c>rules.items</c> e equipamento inicial das classes.</summary>
public class ItemLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private const string Hit = """{ "id": "hero.hit", "name": "Hit", "targeting": { "type": "self" } }""";

    private static string Class(string startingEquipment) => $$"""
        { "id": "class.hero", "name": "Hero", "speed": 4, "radius": 0.4, "attributes": { "str": 5 }, "baseHealth": 100,
          "startingEquipment": {{startingEquipment}}, "resource": { "type": "mana", "max": 100 }, "basicAttack": "hero.hit" }
        """;

    private static string Data(string items, string classes = "[]", string rules = "{}") => $$"""
        { "classes": {{classes}}, "abilities": [ {{Hit}} ], "items": {{items}}, "rules": { "items": {{rules}} } }
        """;

    private const string Blade = """{ "id": "weapon.blade", "name": "Lâmina", "slot": "weapon", "classes": ["class.hero"], "stats": [ { "stat": "weapon_damage", "base": 4, "perLevel": 1 } ] }""";
    private const string Plate = """{ "id": "armor.plate", "name": "Placas", "slot": "chest", "armorType": "heavy", "stats": [ { "stat": "armor", "base": 6, "perLevel": 2 } ] }""";

    [Fact]
    public void Items_and_rules_load()
    {
        var rules = """
            { "inventorySize": 20, "pickupRange": 3, "groundDuration": 60,
              "armorTypes": { "heavy": { "classes": ["class.hero"], "armorMultiplier": 1.5 } },
              "affixCount": { "uncommon": 1, "rare": 2 },
              "affixes": [ { "id": "affix.crit", "name": "Crítico", "value": { "stat": "crit", "base": 0.005, "perLevel": 0.001 } } ],
              "drops": { "boss": { "chance": 1, "count": 2, "rarity": { "uncommon": 60, "rare": 40 }, "guaranteedRarity": "rare", "items": ["armor.plate"] } } }
            """;
        var r = Load(Data($"[{Blade}, {Plate}]", $"[{Class("""[{ "item": "weapon.blade", "level": 2 }]""")}]", rules));

        Assert.True(r.Success, Errors(r));
        var db = r.Database!;
        var plate = db.Items["armor.plate"];
        Assert.Equal((EquipSlot.Chest, ArmorType.Heavy), (plate.Slot, plate.ArmorType));
        Assert.Equal(26, Assert.Single(plate.Stats).At(10));
        var ir = db.Rules.Items;
        Assert.Equal((20, 3.0, 1200), (ir.InventorySize, ir.PickupRange, ir.GroundDurationTicks));
        Assert.Equal(new[] { "class.hero" }, ir.ArmorTypes.Heavy.Classes);
        Assert.Equal(0.6, ir.ArmorTypes.Light.ArmorMultiplier); // padrão do design
        Assert.Equal(ItemStat.Crit, Assert.Single(ir.Affixes).Value.Stat);
        Assert.Equal((2, ItemRarity.Rare, 40.0), (ir.Drops.Boss.Count, ir.Drops.Boss.GuaranteedRarity, ir.Drops.Boss.Rarity.Rare));
        Assert.Equal(0, ir.Drops.Normal.Chance); // sem tabela, nada cai
        Assert.Equal("weapon.blade", Assert.Single(db.Classes["class.hero"].StartingEquipment).Item);
    }

    [Theory]
    [InlineData("""{ "id": "weapon.x", "name": "X", "slot": "weapon", "armorType": "light" }""", "armorType")]
    [InlineData("""{ "id": "armor.x", "name": "X", "slot": "head" }""", "armorType")]
    [InlineData("""{ "id": "weapon.x", "name": "X", "slot": "weapon", "classes": ["class.nobody"] }""", "classes[0]")]
    [InlineData("""{ "id": "weapon.x", "name": "X", "slot": "weapon", "stats": [ { "stat": "str", "base": -1 } ] }""", "stats[0].base")]
    [InlineData("""{ "id": "weapon.x", "name": "X", "slot": "weapon", "stats": [ { "stat": "str", "base": 1 }, { "stat": "str", "base": 2 } ] }""", "stats[1].stat")]
    [InlineData("""{ "id": "Weapon X", "name": "X", "slot": "weapon" }""", "id")]
    public void Invalid_items_are_errors(string item, string field)
    {
        var r = Load(Data($"[{item}]"));

        Assert.False(r.Success);
        Assert.Contains("items[0]." + field, Errors(r));
    }

    [Theory]
    [InlineData("""{ "inventorySize": 0 }""", "inventorySize")]
    [InlineData("""{ "pickupRange": 0 }""", "pickupRange")]
    [InlineData("""{ "affixCount": { "rare": 1.5 } }""", "affixCount.rare")]
    [InlineData("""{ "affixes": [ { "id": "affix.a", "name": "A", "value": { "stat": "str" } }, { "id": "affix.a", "name": "A", "value": { "stat": "dex" } } ] }""", "affixes[1].id")]
    [InlineData("""{ "drops": { "normal": { "chance": 1.5, "items": ["weapon.blade"] } } }""", "drops.normal.chance")]
    [InlineData("""{ "drops": { "elite": { "chance": 0.5, "items": ["weapon.nothing"] } } }""", "drops.elite.items[0]")]
    [InlineData("""{ "drops": { "elite": { "chance": 0.5, "items": [] } } }""", "drops.elite.items")]
    [InlineData("""{ "drops": { "boss": { "chance": 1, "rarity": { "common": 0 }, "items": ["weapon.blade"] } } }""", "drops.boss.rarity")]
    [InlineData("""{ "armorTypes": { "light": { "classes": ["class.nobody"] } } }""", "armorTypes.light.classes[0]")]
    public void Invalid_item_rules_are_errors(string rules, string field)
    {
        var r = Load(Data($"[{Blade}]", rules: rules));

        Assert.False(r.Success);
        Assert.Contains("rules.items." + field, Errors(r));
    }

    [Theory]
    [InlineData("""[{ "item": "weapon.nothing" }]""", "startingEquipment[0].item")]
    [InlineData("""[{ "item": "weapon.blade", "level": 0 }]""", "startingEquipment[0].level")]
    [InlineData("""[{ "item": "weapon.blade" }, { "item": "weapon.blade" }]""", "startingEquipment[1]")]
    [InlineData("""[{ "item": "weapon.staff" }]""", "não usa")]
    public void Invalid_starting_equipment_is_an_error(string startingEquipment, string expected)
    {
        const string Staff = """{ "id": "weapon.staff", "name": "Cajado", "slot": "weapon", "classes": ["class.other"] }""";
        const string Other = """{ "id": "class.other", "name": "O", "speed": 4, "radius": 0.4, "health": 10, "resource": { "type": "mana", "max": 1 }, "basicAttack": "hero.hit" }""";
        var r = Load(Data($"[{Blade}, {Staff}]", $"[{Class(startingEquipment)}, {Other}]"));

        Assert.False(r.Success);
        Assert.Contains(expected, Errors(r));
    }

    [Fact]
    public void Real_data_gives_each_class_its_starting_weapon_and_drop_tables_reference_items()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        var db = GameDataLoader.LoadDirectory(Path.Combine(dir.FullName, "data")).GetDatabaseOrThrow();

        Assert.All(db.Classes.Values, c => Assert.Equal(EquipSlot.Weapon, db.Items[Assert.Single(c.StartingEquipment).Item].Slot));
        var drops = db.Rules.Items.Drops;
        Assert.All(new[] { drops.Normal, drops.Elite, drops.Boss }.SelectMany(d => d.Items), id => Assert.True(db.Items.ContainsKey(id), id));
        Assert.Equal((0.04, 0.25, 1.0), (drops.Normal.Chance, drops.Elite.Chance, drops.Boss.Chance));
    }

    // ---------- T-320: consumíveis e inventário inicial ----------

    private const string Drink = """{ "id": "item.drink", "name": "Gole", "cooldown": 20, "targeting": { "type": "self" }, "effects": [ { "type": "heal", "base": 60 } ] }""";
    private const string Potion = """{ "id": "consumable.drink", "name": "Poção", "type": "consumable", "stack": 20, "useAbility": "item.drink", "price": 10 }""";

    private static string ConsumableData(string items, string inventory = "[]", string abilities = Drink) => $$"""
        { "classes": [ { "id": "class.hero", "name": "Hero", "speed": 4, "radius": 0.4, "attributes": { "str": 5 }, "baseHealth": 100,
            "startingInventory": {{inventory}}, "resource": { "type": "mana", "max": 100 }, "basicAttack": "hero.hit" } ],
          "abilities": [ {{Hit}}, {{abilities}} ], "items": {{items}}, "rules": { "items": { "inventorySize": 3 } } }
        """;

    [Fact]
    public void Consumables_and_starting_inventory_load()
    {
        var r = Load(ConsumableData($"[{Potion}]", """[{ "item": "consumable.drink", "count": 5 }]"""));

        Assert.True(r.Success, Errors(r));
        var item = r.Database!.Items["consumable.drink"];
        Assert.Equal((ItemKind.Consumable, (EquipSlot?)null, 20, "item.drink", 10), (item.Type, item.Slot, item.MaxStack, item.UseAbility, item.Price));
        var start = Assert.Single(r.Database.Classes["class.hero"].StartingInventory);
        Assert.Equal(("consumable.drink", 5), (start.Item, start.Count));
    }

    [Theory]
    [InlineData("""{ "id": "consumable.x", "name": "X", "type": "consumable" }""", "useAbility")]
    [InlineData("""{ "id": "consumable.x", "name": "X", "type": "consumable", "useAbility": "item.nope" }""", "item.nope")]
    [InlineData("""{ "id": "consumable.x", "name": "X", "type": "consumable", "useAbility": "item.drink", "slot": "weapon" }""", "slot")]
    [InlineData("""{ "id": "consumable.x", "name": "X", "type": "consumable", "useAbility": "item.drink", "stack": 0 }""", "stack")]
    [InlineData("""{ "id": "consumable.x", "name": "X", "type": "consumable", "useAbility": "hero.slow" }""", "instantânea")]
    [InlineData("""{ "id": "consumable.x", "name": "X", "type": "consumable", "useAbility": "hero.bolt" }""", "alvo em si")]
    [InlineData("""{ "id": "weapon.x", "name": "X", "stack": 5, "slot": "weapon" }""", "stack")]
    [InlineData("""{ "id": "weapon.x", "name": "X" }""", "slot")]
    [InlineData("""{ "id": "weapon.x", "name": "X", "slot": "weapon", "useAbility": "item.drink" }""", "useAbility")]
    [InlineData("""{ "id": "weapon.x", "name": "X", "slot": "weapon", "price": -1 }""", "price")]
    public void Invalid_consumables_are_errors(string item, string expected)
    {
        var abilities = Drink + """
            , { "id": "hero.slow", "name": "Lenta", "castTime": 1, "targeting": { "type": "self" } },
              { "id": "hero.bolt", "name": "Raio", "targeting": { "type": "unit", "range": 5 } }
            """;
        var r = Load(ConsumableData($"[{item}]", abilities: abilities));
        Assert.False(r.Success);
        Assert.Contains(expected, Errors(r));
    }

    [Theory]
    [InlineData("""[{ "item": "consumable.nope" }]""", "consumable.nope")]
    [InlineData("""[{ "item": "consumable.drink", "count": 0 }]""", "count")]
    [InlineData("""[{ "item": "consumable.drink", "count": 61 }]""", "startingInventory")] // 4 pilhas de 20 num inventário de 3
    public void Invalid_starting_inventory_is_an_error(string inventory, string expected)
    {
        var r = Load(ConsumableData($"[{Potion}]", inventory));
        Assert.False(r.Success);
        Assert.Contains(expected, Errors(r));
    }

    [Fact]
    public void Consumables_cannot_be_starting_equipment_nor_drop()
    {
        var asEquipment = Load(Data($"[{Blade}, {Potion}]", $"[{Class("""[{ "item": "consumable.drink" }]""")}]").Replace("\"abilities\": [", "\"abilities\": [" + Drink + ","));
        Assert.False(asEquipment.Success);
        Assert.Contains("não é equipamento", Errors(asEquipment));

        var drops = """{ "drops": { "normal": { "chance": 0.5, "items": ["consumable.drink"] } } }""";
        var dropped = Load(Data($"[{Potion}]", rules: drops).Replace("\"abilities\": [", "\"abilities\": [" + Drink + ","));
        Assert.False(dropped.Success);
        Assert.Contains("consumível", Errors(dropped));
    }
}
