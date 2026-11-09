using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-320: consumíveis como itens (pilhas, uso pela intenção, recarga por habilidade) e ouro dos monstros.</summary>
public class ConsumableTests
{
    private static readonly ContentCatalog Content = RealContent.Value;
    private const string Potion = "consumable.minor_healing_potion";
    private const string ManaPotion = "consumable.minor_mana_potion";
    private const string PotionAbility = "item.minor_healing_potion";

    private static World NewWorld(ulong seed = 11) => WorldBuilder.CreateDefault(seed, null, Content);

    /// <summary>Personagem com o inventário vazio (sem as poções iniciais).</summary>
    private static Entity Hero(World w, float x = 1, string cls = "class.warrior", int party = 0)
    {
        var e = Leveling.SpawnCharacter(w, w.Content.Classes[cls], new Vector2(x, 1), new CharacterBuild { Level = 2 });
        var inv = e.Get<Inventory>();
        for (var i = 0; i < inv.Capacity; i++)
        {
            inv[i] = null;
        }

        if (party != 0)
        {
            e.Add(new PartyMembership { PartyId = party });
        }

        return e;
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

    private static Entity Wolf(World w, int? level = null)
    {
        var m = EntityFactory.SpawnMonster(w, w.Content.Monsters["monster.grey_wolf"], new Vector2(2, 1), level);
        m.Get<Health>().Current = 1;
        m.Get<Movement>().Speed = 0;
        return m;
    }

    private static List<GameEvent> Kill(World w, Entity attacker, Entity target)
    {
        var events = new List<GameEvent>();
        for (var t = 0; t < 200 && target.IsAlive; t++)
        {
            events.AddRange(Step(w, new UseAbilityIntent(attacker.Id, attacker.Get<AbilityState>().BasicAttackId!, target.Id)));
        }

        Assert.False(target.IsAlive);
        return events;
    }

    private static int?[] Counts(Entity e, int n) => e.Get<Inventory>().Slots.Take(n).Select(i => i?.Count).ToArray();

    // ---------- pilhas ----------

    [Fact]
    public void Potions_stack_up_to_the_maximum_and_then_open_a_new_slot()
    {
        var w = NewWorld();
        var hero = Hero(w);
        Assert.Equal(20, Content.Items[Potion].MaxStack);

        Assert.Equal(0, Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 15)));
        Assert.Equal(0, Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 10)));
        Assert.Equal([20, 5, null], Counts(hero, 3));

        // Outro consumível não entra na pilha da poção de vida.
        Assert.Equal(2, Gear.AddToInventory(w, hero, ItemInstance.Stack(ManaPotion, 3)));
        Assert.Equal(1, Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 4)));
        Assert.Equal([20, 9, 3], Counts(hero, 3));
        Assert.Equal(29, Consumables.CountOf(hero, Potion));
    }

    [Fact]
    public void A_stack_that_does_not_fit_whole_is_not_added()
    {
        var w = NewWorld();
        var hero = Hero(w);
        var inv = hero.Get<Inventory>();
        for (var i = 0; i < inv.Capacity - 1; i++)
        {
            inv[i] = ItemInstance.Common("armor.light_hands", 1);
        }

        inv[inv.Capacity - 1] = ItemInstance.Stack(Potion, 18);
        Assert.Equal(-1, Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 3)));
        Assert.Equal(18, inv[inv.Capacity - 1]!.Count);
        Assert.Equal(inv.Capacity - 1, Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 2)));
        Assert.Equal(20, inv[inv.Capacity - 1]!.Count);
    }

    [Fact]
    public void Dropping_drops_the_whole_stack_and_picking_up_stacks_again()
    {
        var w = NewWorld();
        var hero = Hero(w);
        Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 20));
        Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 7));

        Step(w, new DropItemIntent(hero.Id, 1));
        Assert.Equal([20, null], Counts(hero, 2));
        var ground = Assert.Single(w.Entities, e => e.Has<GroundItem>());
        Assert.Equal(7, ground.Get<GroundItem>().Item.Count);

        Step(w, new UseItemIntent(hero.Id, 0)); // abre lugar na primeira pilha
        var picked = Step(w, new PickUpItemIntent(hero.Id, ground.Id));
        Assert.Equal(0, Assert.Single(picked.OfType<ItemPickedUp>()).InventorySlot);
        Assert.Equal([20, 6], Counts(hero, 2));
    }

    [Fact]
    public void A_consumable_cannot_be_equipped()
    {
        var w = NewWorld();
        var hero = Hero(w);
        Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 5));

        var rejected = Assert.Single(Step(w, new EquipItemIntent(hero.Id, 0)).OfType<ItemRejected>());
        Assert.Equal((ItemAction.Equip, ItemRejectReason.NotEquippable), (rejected.Action, rejected.Reason));
    }

    // ---------- uso ----------

    [Fact]
    public void Using_a_potion_heals_spends_one_unit_and_respects_the_cooldown()
    {
        var w = NewWorld();
        var hero = Hero(w);
        Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 3));
        var hp = hero.Get<Health>();
        hp.Current = 10;

        var used = Step(w, new UseItemIntent(hero.Id, 0));
        Assert.Equal(PotionAbility, Assert.Single(used.OfType<AbilityExecuted>()).AbilityId);
        var ev = Assert.Single(used.OfType<ItemUsed>());
        Assert.Equal((0, Potion, PotionAbility, 2), (ev.InventorySlot, ev.BaseId, ev.AbilityId, ev.Remaining));
        Assert.Equal(70, hp.Current);
        Assert.Equal(2, hero.Get<Inventory>()[0]!.Count);
        Assert.DoesNotContain(PotionAbility, hero.Get<AbilityState>().Known);

        var again = Step(w, new UseItemIntent(hero.Id, 0));
        var rejected = Assert.Single(again.OfType<ItemRejected>());
        Assert.Equal((ItemAction.Use, ItemRejectReason.OnCooldown), (rejected.Action, rejected.Reason));
        Assert.Equal(2, hero.Get<Inventory>()[0]!.Count);

        var cooldown = Content.Abilities[PotionAbility].CooldownTicks;
        for (var t = 0; t < cooldown; t++)
        {
            w.Step();
        }

        Assert.Single(Step(w, new UseItemIntent(hero.Id, 0)).OfType<ItemUsed>());
        Assert.Equal(1, hero.Get<Inventory>()[0]!.Count);
    }

    [Fact]
    public void Two_stacks_of_the_same_potion_share_the_cooldown()
    {
        var w = NewWorld();
        var hero = Hero(w);
        Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 25)); // 20 + 5
        Gear.AddToInventory(w, hero, ItemInstance.Stack(ManaPotion, 2));
        hero.Get<Health>().Current = 10;
        hero.Get<ResourcePool>().Current = 0;

        Assert.Single(Step(w, new UseItemIntent(hero.Id, 1)).OfType<ItemUsed>());
        var rejected = Assert.Single(Step(w, new UseItemIntent(hero.Id, 0)).OfType<ItemRejected>());
        Assert.Equal(ItemRejectReason.OnCooldown, rejected.Reason);
        Assert.Equal([20, 4, 2], Counts(hero, 3));

        // A poção de Mana tem habilidade própria: não divide a recarga com a de vida.
        Assert.Single(Step(w, new UseItemIntent(hero.Id, 2)).OfType<ItemUsed>());
        Assert.Equal(40, hero.Get<ResourcePool>().Current);
    }

    [Fact]
    public void The_last_unit_empties_the_slot()
    {
        var w = NewWorld();
        var hero = Hero(w);
        Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 1));

        var ev = Assert.Single(Step(w, new UseItemIntent(hero.Id, 0)).OfType<ItemUsed>());
        Assert.Equal(0, ev.Remaining);
        Assert.Null(hero.Get<Inventory>()[0]);
    }

    [Fact]
    public void Use_is_rejected_with_a_reason()
    {
        var w = NewWorld();
        var hero = Hero(w);
        Gear.AddToInventory(w, hero, ItemInstance.Common("weapon.axe", 1));
        Gear.AddToInventory(w, hero, ItemInstance.Stack(Potion, 2));

        ItemRejectReason Reject(int slot) =>
            Assert.Single(Step(w, new UseItemIntent(hero.Id, slot)).OfType<ItemRejected>(), r => r.Action == ItemAction.Use).Reason;

        Assert.Equal(ItemRejectReason.EmptySlot, Reject(5));
        Assert.Equal(ItemRejectReason.EmptySlot, Reject(-1));
        Assert.Equal(ItemRejectReason.NotConsumable, Reject(0));

        // Intenção de morto o mundo já descarta; a regra vale também para quem chama direto (servidor, testes).
        w.Kill(hero);
        Assert.Empty(Step(w, new UseItemIntent(hero.Id, 1)).OfType<ItemRejected>());
        Assert.Equal(ItemRejectReason.Dead, Consumables.Use(w, hero, 1));
        Assert.Equal(2, hero.Get<Inventory>()[1]!.Count);
    }

    [Fact]
    public void New_characters_start_with_five_healing_potions_and_no_gold()
    {
        var w = NewWorld();
        foreach (var cls in Content.Classes.Values)
        {
            var e = Leveling.SpawnCharacter(w, cls, Vector2.Zero);
            Assert.Equal(5, Consumables.CountOf(e, Potion));
            Assert.Equal(0, e.Get<Wallet>().Gold);
            Assert.DoesNotContain(PotionAbility, e.Get<AbilityState>().Known);
        }
    }

    // ---------- ouro ----------

    [Fact]
    public void A_kill_gives_gold_within_the_variance()
    {
        var rules = Content.Rules.Progression;
        Assert.True(rules.GoldPerMonsterLevel > 0);
        var w = NewWorld();
        var hero = Hero(w);
        var wolf = Wolf(w, level: 3);
        var expected = rules.GoldPerMonsterLevel * 3 * rules.GoldByRank.Normal;

        var events = Kill(w, hero, wolf);

        var gold = Assert.Single(events.OfType<GoldGained>());
        Assert.Equal((hero.Id, wolf.Id), (gold.Entity, gold.Monster));
        Assert.InRange(gold.Amount, Math.Round(expected * (1 - rules.GoldVariance)), Math.Round(expected * (1 + rules.GoldVariance)));
        Assert.Equal(gold.Amount, hero.Get<Wallet>().Gold);
    }

    [Fact]
    public void Gold_is_split_equally_with_the_party_and_rounded_down()
    {
        var rules = Content.Rules.Progression;
        var w = NewWorld();
        var killer = Hero(w, 1, party: 1);
        var friend = Hero(w, 12, party: 1);
        var stranger = Hero(w, 3);
        var wolf = Wolf(w, level: 5);
        var expected = rules.GoldPerMonsterLevel * 5 * rules.GoldByRank.Normal;

        var events = Kill(w, killer, wolf);

        var gains = events.OfType<GoldGained>().ToDictionary(g => g.Entity, g => g.Amount);
        Assert.Equal([killer.Id, friend.Id], gains.Keys.Order());
        Assert.Equal(gains[killer.Id], gains[friend.Id]);
        var total = gains.Values.Sum();
        // Cada um recebe a metade arredondada para baixo: o total dado perde no máximo 1 (o resto).
        Assert.InRange(total, Math.Round(expected * (1 - rules.GoldVariance)) - 1, Math.Round(expected * (1 + rules.GoldVariance)));
        Assert.Equal(gains[friend.Id], friend.Get<Wallet>().Gold);
        Assert.Equal(0, stranger.Get<Wallet>().Gold);
    }

    [Fact]
    public void Gold_is_deterministic_with_the_seed()
    {
        static long[] Run(ulong seed)
        {
            var w = NewWorld(seed);
            var hero = Hero(w);
            var gold = new List<long>();
            for (var i = 0; i < 8; i++)
            {
                Kill(w, hero, Wolf(w, level: 10));
                gold.Add(hero.Get<Wallet>().Gold);
            }

            return gold.ToArray();
        }

        var a = Run(7);
        Assert.Equal(a, Run(7));
        var amounts = a.Zip(a.Skip(1), (x, y) => y - x).Prepend(a[0]).ToList();
        Assert.True(amounts.Distinct().Count() > 1, "a variação deveria mudar o ouro entre mortes");
    }

    [Fact]
    public void Without_gold_rules_nothing_is_dropped()
    {
        var content = new ContentCatalog(Content.Classes.Values, Content.Abilities.Values, Content.Statuses.Values, Content.Areas.Values,
            Content.Monsters.Values, Content.Arenas.Values, Content.Encounters.Values, Content.Items.Values)
        {
            Rules = Content.Rules with { Progression = Content.Rules.Progression with { GoldPerMonsterLevel = 0 } },
            Threat = Content.Threat,
        };
        var w = WorldBuilder.CreateDefault(11, null, content);
        var hero = Hero(w);

        Assert.Empty(Kill(w, hero, Wolf(w)).OfType<GoldGained>());
        Assert.Equal(0, hero.Get<Wallet>().Gold);
    }
}
