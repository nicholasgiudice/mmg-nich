using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-204: itens, equipamento inicial, equipar/desequipar, restrições, inventário, queda e saque.</summary>
public class ItemTests
{
    private static readonly ContentCatalog Content = RealContent.Value;

    /// <summary>Conteúdo real com outras regras de item (ex.: queda garantida).</summary>
    private static ContentCatalog With(Func<ItemRules, ItemRules> change) =>
        new(Content.Classes.Values, Content.Abilities.Values, Content.Statuses.Values, Content.Areas.Values,
            Content.Monsters.Values, Content.Arenas.Values, Content.Encounters.Values, Content.Items.Values)
        {
            Rules = Content.Rules with { Items = change(Content.Rules.Items) },
            Threat = Content.Threat,
        };

    private static ContentCatalog AlwaysDrops() =>
        With(r => r with { Drops = r.Drops with { Normal = r.Drops.Normal with { Chance = 1 } } });

    private static World NewWorld(ContentCatalog? content = null, ulong seed = 11) => WorldBuilder.CreateDefault(seed, null, content ?? Content);

    private static Entity Hero(World w, string cls = "class.warrior", float x = 1) =>
        Leveling.SpawnCharacter(w, w.Content.Classes[cls], new Vector2(x, 1));

    private static Entity Monster(World w, string id, float x = 2)
    {
        var m = EntityFactory.SpawnMonster(w, w.Content.Monsters[id], new Vector2(x, 1));
        m.Get<Health>().Current = 1;
        m.Get<Movement>().Speed = 0;
        return m;
    }

    private static List<GameEvent> Step(World w, params Intent[] intents)
    {
        foreach (var i in intents)
        {
            w.Enqueue(i);
        }

        w.Step();
        return w.LastTickEvents.ToList();
    }

    private static List<GameEvent> Kill(World w, Entity attacker, Entity target)
    {
        var events = new List<GameEvent>();
        for (var t = 0; t < 100 && target.IsAlive; t++)
        {
            events.AddRange(Step(w, new UseAbilityIntent(attacker.Id, attacker.Get<AbilityState>().BasicAttackId!, target.Id)));
        }

        Assert.False(target.IsAlive);
        return events;
    }

    private static int Give(Entity e, string baseId, int level, ItemRarity rarity = ItemRarity.Common, params string[] affixes)
    {
        var slot = Gear.AddToInventory(e, new ItemInstance(baseId, level, rarity, affixes));
        Assert.True(slot >= 0);
        return slot;
    }

    private static List<Entity> Ground(World w) => w.Entities.Where(e => e.Has<GroundItem>()).ToList();

    // ---------- equipamento inicial ----------

    [Theory]
    [InlineData("class.warrior", "weapon.sword", 6, 4)]
    [InlineData("class.archer", "weapon.bow", 6, 5)]
    [InlineData("class.mage", "weapon.staff", 3, 13)]
    [InlineData("class.cleric", "weapon.mace", 5, 12)]
    public void Starting_equipment_reproduces_the_prototype_numbers(string cls, string weapon, float damage, int intel)
    {
        var w = NewWorld();
        foreach (var e in new[] { EntityFactory.SpawnPlayer(w, Content.Classes[cls], Vector2.Zero), Hero(w, cls) })
        {
            var s = e.Get<CharacterStats>();
            Assert.Equal(damage, s.WeaponDamage);
            Assert.Equal(intel, s.Int);
            Assert.Equal(0, s.Armor);
            Assert.Equal(0, s.Resistance);
            Assert.Equal(0, s.HealthBonus);
            Assert.Equal(0, s.CritBonus);

            var item = Assert.Single(e.Get<Equipment>().Items);
            Assert.Equal((EquipSlot.Weapon, ItemInstance.Common(weapon, 2)), (item.Slot, item.Item));
            Assert.Equal(30, e.Get<Inventory>().Capacity);
            Assert.All(e.Get<Inventory>().Slots, Assert.Null);

            // Mesmos números da T-201: vida pela VIT e Mana pela INT (o cajado dá +2 INT ao Mago).
            Assert.Equal(StatMath.MaxHealth(Content.Classes[cls], s, Content.Rules.Stats), e.Get<Health>().Max, 3);
            Assert.Equal(80 + (2 * intel), e.Get<ResourcePool>().Max, 3);
        }
    }

    [Fact]
    public void Item_stats_follow_the_design_formulas()
    {
        double[] Stats(string id, int level, params string[] affixes) =>
            ItemMath.StatsOf(Content, new ItemInstance(id, level, ItemRarity.Rare, affixes));

        Assert.Equal(9, Stats("weapon.axe", 5)[(int)ItemStat.WeaponDamage]);
        Assert.Equal(8, Stats("weapon.mace", 5)[(int)ItemStat.WeaponDamage]); // 3 + 4,5 = 7,5 → 8
        Assert.Equal(5, Stats("weapon.scepter", 5)[(int)ItemStat.WeaponDamage]); // 2 + 3
        Assert.Equal(4, Stats("weapon.scepter", 5)[(int)ItemStat.Int]); // 1 + 2,5 = 3,5 → 4

        Assert.Equal(24, Stats("armor.heavy_chest", 5)[(int)ItemStat.Armor]); // 16 × 1,5
        Assert.Equal(16, Stats("armor.medium_chest", 5)[(int)ItemStat.Armor]);
        var light = Stats("armor.light_chest", 5);
        Assert.Equal(10, light[(int)ItemStat.Armor]); // 16 × 0,6 = 9,6 → 10
        Assert.Equal(5, light[(int)ItemStat.Resistance]); // 50% da armadura
        Assert.Equal(8, Stats("armor.medium_feet", 5)[(int)ItemStat.Armor]);
        Assert.Equal(12, Stats("accessory.ring", 5)[(int)ItemStat.Resistance]); // 4 + 7,5 = 11,5 → 12

        var affixed = Stats("accessory.ring", 10, "affix.str", "affix.crit", "affix.health", "affix.mana_regen");
        Assert.Equal(5, affixed[(int)ItemStat.Str]); // 1 + 4
        Assert.Equal(0.015, affixed[(int)ItemStat.Crit], 6);
        Assert.Equal(50, affixed[(int)ItemStat.Health]);
        Assert.Equal(0.5, affixed[(int)ItemStat.ManaRegen], 6);
    }

    // ---------- equipar e desequipar ----------

    [Fact]
    public void Equipping_and_unequipping_changes_the_stats()
    {
        var w = NewWorld();
        var hero = Hero(w);
        var s = hero.Get<CharacterStats>();
        var health = hero.Get<Health>();
        var baseMax = health.Max;
        var baseCrit = StatMath.CritChance(s, Content.Rules.Stats);
        var chest = Give(hero, "armor.heavy_chest", 5);
        var ring = Give(hero, "accessory.ring", 4, ItemRarity.Rare, "affix.health", "affix.crit");

        var events = Step(w, new EquipItemIntent(hero.Id, chest), new EquipItemIntent(hero.Id, ring));

        Assert.Equal(2, events.OfType<ItemEquipped>().Count());
        Assert.Equal(24, s.Armor);
        Assert.Equal(10, s.Resistance); // anel: 4 + 6
        Assert.Equal(baseMax + 20, health.Max, 3);
        Assert.Equal(health.Max, health.Current, 3); // a vida nova vem junto
        Assert.Equal(baseCrit + 0.009, StatMath.CritChance(s, Content.Rules.Stats), 5);
        Assert.Null(hero.Get<Inventory>()[chest]);

        events = Step(w, new UnequipItemIntent(hero.Id, EquipSlot.Chest), new UnequipItemIntent(hero.Id, EquipSlot.Accessory));

        Assert.Equal(2, events.OfType<ItemUnequipped>().Count());
        Assert.Equal(0, s.Armor);
        Assert.Equal(0, s.Resistance);
        Assert.Equal(baseMax, health.Max, 3);
        Assert.Equal(baseCrit, StatMath.CritChance(s, Content.Rules.Stats), 6);
        Assert.Equal(2, hero.Get<Inventory>().Slots.Count(i => i is not null));
    }

    [Fact]
    public void Equipping_a_weapon_swaps_it_with_the_equipped_one_and_keeps_allocated_points()
    {
        var w = NewWorld();
        var hero = Hero(w);
        hero.Get<CharacterProgress>().AttributePoints = 2;
        Step(w, new SpendAttributePointIntent(hero.Id, AttributeKind.Str), new SpendAttributePointIntent(hero.Id, AttributeKind.Str));
        var s = hero.Get<CharacterStats>();
        Assert.Equal(14, s.Str);
        var slot = Give(hero, "weapon.axe", 5, ItemRarity.Uncommon, "affix.str");

        var equipped = Assert.Single(Step(w, new EquipItemIntent(hero.Id, slot)).OfType<ItemEquipped>());

        Assert.Equal(("weapon.axe", "weapon.sword"), (equipped.BaseId, equipped.PreviousId));
        Assert.Equal(9, s.WeaponDamage);
        Assert.Equal(17, s.Str); // 12 + 2 pontos + 3 do afixo
        Assert.Equal(ItemInstance.Common("weapon.sword", 2), hero.Get<Inventory>()[slot]);

        Step(w, new EquipItemIntent(hero.Id, slot));
        Assert.Equal(6, s.WeaponDamage);
        Assert.Equal(14, s.Str);
        Assert.Equal(2, hero.Get<CharacterProgress>().AllocatedStr);
    }

    [Theory]
    [InlineData("class.mage", "weapon.sword", false)]
    [InlineData("class.warrior", "weapon.staff", false)]
    [InlineData("class.cleric", "weapon.scepter", true)]
    [InlineData("class.archer", "armor.heavy_chest", false)]
    [InlineData("class.archer", "armor.medium_chest", true)]
    [InlineData("class.mage", "armor.medium_head", false)]
    [InlineData("class.mage", "armor.light_head", true)]
    [InlineData("class.warrior", "armor.light_feet", true)]
    [InlineData("class.mage", "accessory.ring", true)]
    public void Classes_only_equip_what_they_can_use(string cls, string item, bool allowed)
    {
        var w = NewWorld();
        var hero = Hero(w, cls);
        var slot = Give(hero, item, 3);

        var events = Step(w, new EquipItemIntent(hero.Id, slot));

        if (allowed)
        {
            Assert.Single(events.OfType<ItemEquipped>());
            Assert.Equal(item, hero.Get<Equipment>()[Content.Items[item].Slot]!.BaseId);
        }
        else
        {
            var rejected = Assert.Single(events.OfType<ItemRejected>());
            Assert.Equal((ItemAction.Equip, ItemRejectReason.ClassCannotUse), (rejected.Action, rejected.Reason));
            Assert.Equal(item, hero.Get<Inventory>()[slot]!.BaseId);
        }
    }

    [Fact]
    public void Invalid_requests_are_rejected_with_a_reason()
    {
        var w = NewWorld();
        var hero = Hero(w);

        var events = Step(
            w,
            new EquipItemIntent(hero.Id, 0),
            new EquipItemIntent(hero.Id, 99),
            new UnequipItemIntent(hero.Id, EquipSlot.Head),
            new DropItemIntent(hero.Id, -1),
            new PickUpItemIntent(hero.Id, new EntityId(999)));

        Assert.Equal(
            new[] { ItemRejectReason.EmptySlot, ItemRejectReason.EmptySlot, ItemRejectReason.EmptySlot, ItemRejectReason.EmptySlot, ItemRejectReason.NotFound },
            events.OfType<ItemRejected>().Select(r => r.Reason));
    }

    // ---------- inventário cheio ----------

    [Fact]
    public void Full_inventory_blocks_unequip_and_pickup_but_not_swapping()
    {
        var w = NewWorld(AlwaysDrops());
        var hero = Hero(w);
        for (var i = 0; i < 30; i++)
        {
            Give(hero, "armor.light_hands", 1);
        }

        Assert.Equal(-1, Gear.AddToInventory(hero, ItemInstance.Common("accessory.ring", 1)));
        var rejected = Assert.Single(Step(w, new UnequipItemIntent(hero.Id, EquipSlot.Weapon)).OfType<ItemRejected>());
        Assert.Equal(ItemRejectReason.InventoryFull, rejected.Reason);
        Assert.NotNull(hero.Get<Equipment>()[EquipSlot.Weapon]);

        Kill(w, hero, Monster(w, "monster.field_rat"));
        var loot = Assert.Single(Ground(w));
        rejected = Assert.Single(Step(w, new PickUpItemIntent(hero.Id, loot.Id)).OfType<ItemRejected>());
        Assert.Equal(ItemRejectReason.InventoryFull, rejected.Reason);
        Assert.False(loot.IsRemoved);

        Assert.Single(Step(w, new EquipItemIntent(hero.Id, 0)).OfType<ItemEquipped>()); // troca: o espaço fica com nada
        Assert.Single(Step(w, new PickUpItemIntent(hero.Id, loot.Id)).OfType<ItemPickedUp>());
        Assert.True(loot.IsRemoved);
    }

    // ---------- queda ----------

    [Fact]
    public void Killing_a_monster_drops_an_item_of_its_level_for_the_participants()
    {
        var w = NewWorld(AlwaysDrops());
        var hero = Hero(w);
        var wolf = Monster(w, "monster.grey_wolf");

        var events = Kill(w, hero, wolf);

        var dropped = Assert.Single(events.OfType<ItemDropped>());
        var loot = Assert.Single(Ground(w));
        var g = loot.Get<GroundItem>();
        Assert.Equal((loot.Id, wolf.Id, 2), (dropped.Item, dropped.Source, dropped.Level));
        Assert.Equal(2, g.Item.Level);
        Assert.Contains(g.Item.BaseId, Content.Rules.Items.Drops.Normal.Items);
        Assert.Equal((int)Content.Rules.Items.AffixCount.For(g.Item.Rarity), g.Item.Affixes.Count);
        Assert.Equal(new[] { hero.Id.Value }, g.Owners);
        Assert.Equal(wolf.Get<Transform>().Position, loot.Get<Transform>().Position);
        Assert.Equal(w.Tick + Content.Rules.Items.GroundDurationTicks, g.ExpiresAtTick);
    }

    [Fact]
    public void Drops_are_deterministic_per_seed()
    {
        List<(string, int, ItemRarity, string)> Run(ulong seed)
        {
            var w = NewWorld(seed: seed);
            var hero = Hero(w);
            var boss = EntityFactory.SpawnMonster(w, Content.Monsters["monster.forest_matriarch"], new Vector2(2, 1));
            var elite = EntityFactory.SpawnMonster(w, Content.Monsters["monster.spider_big"], new Vector2(2, 1));
            var result = new List<(string, int, ItemRarity, string)>();
            for (var i = 0; i < 40; i++)
            {
                result.AddRange(Gear.DropLoot(w, i % 2 == 0 ? boss : elite, [hero])
                    .Select(e => e.Get<GroundItem>().Item)
                    .Select(it => (it.BaseId, it.Level, it.Rarity, string.Join(",", it.Affixes))));
            }

            return result;
        }

        Assert.Equal(Run(5), Run(5));
        Assert.NotEqual(Run(5), Run(6));
    }

    [Fact]
    public void Boss_drops_two_items_and_the_first_is_at_least_uncommon()
    {
        var w = NewWorld();
        var hero = Hero(w);
        var boss = EntityFactory.SpawnMonster(w, Content.Monsters["monster.forest_matriarch"], new Vector2(2, 1));
        var rarities = new List<ItemRarity>();
        for (var i = 0; i < 200; i++)
        {
            var loot = Gear.DropLoot(w, boss, [hero]);
            Assert.Equal(2, loot.Count);
            Assert.True(loot[0].Get<GroundItem>().Item.Rarity >= ItemRarity.Uncommon);
            Assert.All(loot, l => Assert.Equal(8, l.Get<GroundItem>().Item.Level));
            rarities.AddRange(loot.Select(l => l.Get<GroundItem>().Item.Rarity));
        }

        Assert.DoesNotContain(ItemRarity.Common, rarities); // pesos 0 / 60 / 35 / 5
        Assert.Contains(ItemRarity.Epic, rarities);
        Assert.All(Ground(w), e => Assert.Equal(e.Get<GroundItem>().Item.Affixes.Count, e.Get<GroundItem>().Item.Affixes.Distinct().Count()));
    }

    [Fact]
    public void Normal_monsters_drop_about_eight_percent_of_the_time()
    {
        var w = NewWorld();
        var hero = Hero(w);
        var rat = EntityFactory.SpawnMonster(w, Content.Monsters["monster.field_rat"], new Vector2(2, 1));
        var drops = Enumerable.Range(0, 5000).Sum(_ => Gear.DropLoot(w, rat, [hero]).Count);

        Assert.InRange(drops, 300, 500); // 8% de 5000 = 400
    }

    [Fact]
    public void Nothing_drops_without_participants_with_progression()
    {
        var w = NewWorld(AlwaysDrops());
        var player = EntityFactory.SpawnPlayer(w, Content.Classes["class.warrior"], new Vector2(1, 1)); // encontro de teste
        var rat = Monster(w, "monster.field_rat");

        var events = Kill(w, player, rat);

        Assert.Empty(events.OfType<ItemDropped>());
        Assert.Empty(Ground(w));
        Assert.Empty(Gear.DropLoot(w, rat, []));
    }

    // ---------- saque ----------

    [Fact]
    public void Only_participants_in_range_pick_up_and_only_once()
    {
        var w = NewWorld(AlwaysDrops());
        var hero = Hero(w);
        var friend = Hero(w, "class.archer", x: 1.5f);
        var stranger = Hero(w, "class.mage", x: 2.5f);
        Kill(w, hero, Monster(w, "monster.field_rat"));
        var loot = Assert.Single(Ground(w));

        var rejected = Assert.Single(Step(w, new PickUpItemIntent(stranger.Id, loot.Id)).OfType<ItemRejected>());
        Assert.Equal((ItemAction.PickUp, ItemRejectReason.NotOwner), (rejected.Action, rejected.Reason));

        hero.Get<Transform>().Position = new Vector2(10, 1);
        rejected = Assert.Single(Step(w, new PickUpItemIntent(hero.Id, loot.Id)).OfType<ItemRejected>());
        Assert.Equal(ItemRejectReason.TooFar, rejected.Reason);

        hero.Get<Transform>().Position = new Vector2(3, 1);
        var events = Step(w, new PickUpItemIntent(hero.Id, loot.Id), new PickUpItemIntent(friend.Id, loot.Id));
        var picked = Assert.Single(events.OfType<ItemPickedUp>());
        Assert.Equal((hero.Id, loot.Id, 0), (picked.Entity, picked.Item, picked.InventorySlot));
        Assert.Equal(ItemRejectReason.NotFound, Assert.Single(events.OfType<ItemRejected>()).Reason);
        Assert.True(loot.IsRemoved);
        Assert.Equal(loot.Get<GroundItem>().Item, hero.Get<Inventory>()[0]);
    }

    [Fact]
    public void Healers_of_the_killer_share_the_loot_rights()
    {
        var w = NewWorld(AlwaysDrops());
        var hero = Hero(w);
        var cleric = Hero(w, "class.cleric", x: 0);
        cleric.Get<CharacterProgress>().SkillPoints = 1;
        Step(w, new RaiseSkillRankIntent(cleric.Id, "cleric.direct_heal"));
        hero.Get<Health>().Current -= 30;
        for (var t = 0; t < 200 && !w.LastTickEvents.OfType<HealApplied>().Any(h => h.Target == hero.Id); t++)
        {
            Step(w, new UseAbilityIntent(cleric.Id, "cleric.direct_heal", hero.Id));
        }

        Assert.Contains(w.LastTickEvents, e => e is HealApplied h && h.Target == hero.Id);

        Kill(w, hero, Monster(w, "monster.field_rat"));

        Assert.Equal(new[] { hero.Id.Value, cleric.Id.Value }.Order(), Assert.Single(Ground(w)).Get<GroundItem>().Owners);
    }

    [Fact]
    public void Dropped_items_belong_to_who_dropped_them_and_expire()
    {
        var w = NewWorld();
        var hero = Hero(w);
        var other = Hero(w, "class.archer");
        var slot = Give(hero, "accessory.ring", 3, ItemRarity.Uncommon, "affix.vit");

        var dropped = Assert.Single(Step(w, new DropItemIntent(hero.Id, slot)).OfType<ItemDropped>());

        Assert.Null(hero.Get<Inventory>()[slot]);
        var loot = w.Find(dropped.Item)!;
        Assert.Equal((hero.Id, "accessory.ring", ItemRarity.Uncommon), (dropped.Source, dropped.BaseId, dropped.Rarity));
        Assert.Equal(new[] { hero.Id.Value }, loot.Get<GroundItem>().Owners);
        Assert.Equal(ItemRejectReason.NotOwner, Assert.Single(Step(w, new PickUpItemIntent(other.Id, loot.Id)).OfType<ItemRejected>()).Reason);

        var expiry = loot.Get<GroundItem>().ExpiresAtTick;
        var expired = new List<GroundItemExpired>();
        while (w.Tick < expiry)
        {
            expired.AddRange(Step(w).OfType<GroundItemExpired>());
        }

        Assert.Equal(dropped.Item, Assert.Single(expired).Item);
        Assert.True(loot.IsRemoved);
    }

    // ---------- estado ----------

    [Fact]
    public void Snapshots_do_not_see_later_changes_to_inventory_and_equipment()
    {
        var w = NewWorld();
        var hero = Hero(w);
        var slot = Give(hero, "armor.heavy_head", 2);
        var before = w.Snapshot();
        var hash = w.ComputeStateHash();

        Step(w, new EquipItemIntent(hero.Id, slot));

        var old = before.Entities.Single(e => e.Id == hero.Id).Components;
        Assert.Equal("armor.heavy_head", old.OfType<Inventory>().Single()[slot]!.BaseId);
        Assert.Null(old.OfType<Equipment>().Single()[EquipSlot.Head]);
        Assert.Equal("armor.heavy_head", hero.Get<Equipment>()[EquipSlot.Head]!.BaseId);
        Assert.NotEqual(hash, w.ComputeStateHash());
    }
}
