using System.Numerics;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Items;

/// <summary>
/// Inventário, equipamento e queda (T-204, <c>design/progression-v0.1.md</c> §6). As intenções passam pelo
/// <see cref="ItemSystem"/>; os métodos aqui servem também a servidor e testes. Toda validação acontece aqui: o cliente
/// só pede.
/// </summary>
public static class Gear
{
    /// <summary>Equipa o item do espaço do inventário; o que estava equipado volta para esse mesmo espaço. Null = feito.</summary>
    public static ItemRejectReason? Equip(World world, Entity e, int inventorySlot)
    {
        if (e.TryGet<Inventory>() is not { } inv || e.TryGet<Equipment>() is not { } eq)
        {
            return ItemRejectReason.NoInventory;
        }

        if (inventorySlot < 0 || inventorySlot >= inv.Capacity || inv[inventorySlot] is not { } item)
        {
            return ItemRejectReason.EmptySlot;
        }

        if (!world.Content.Items.TryGetValue(item.BaseId, out var def))
        {
            return ItemRejectReason.UnknownItem;
        }

        if (!ItemMath.CanUse(world.Content.Rules.Items, def, e.TryGet<UnitInfo>()?.DefinitionId ?? ""))
        {
            return ItemRejectReason.ClassCannotUse;
        }

        var previous = eq[def.Slot];
        eq[def.Slot] = item;
        inv[inventorySlot] = previous;
        Refresh(world, e);
        world.Events.Publish(new ItemEquipped(e.Id, def.Slot, item.BaseId, previous?.BaseId ?? ""));
        return null;
    }

    /// <summary>Tira o item do espaço e põe no primeiro espaço livre do inventário. Null = feito.</summary>
    public static ItemRejectReason? Unequip(World world, Entity e, EquipSlot slot)
    {
        if (e.TryGet<Inventory>() is not { } inv || e.TryGet<Equipment>() is not { } eq)
        {
            return ItemRejectReason.NoInventory;
        }

        if (eq[slot] is not { } item)
        {
            return ItemRejectReason.EmptySlot;
        }

        var free = inv.FirstFree();
        if (free < 0)
        {
            return ItemRejectReason.InventoryFull;
        }

        eq[slot] = null;
        inv[free] = item;
        Refresh(world, e);
        world.Events.Publish(new ItemUnequipped(e.Id, slot, item.BaseId, free));
        return null;
    }

    /// <summary>Pega o item do chão (precisa ter direito, estar no alcance e ter espaço). Null = feito.</summary>
    public static ItemRejectReason? PickUp(World world, Entity e, EntityId itemId)
    {
        if (e.TryGet<Inventory>() is not { } inv)
        {
            return ItemRejectReason.NoInventory;
        }

        if (world.Find(itemId) is not { IsRemoved: false } ground || ground.TryGet<GroundItem>() is not { } g)
        {
            return ItemRejectReason.NotFound;
        }

        if (!g.IsOwner(e.Id))
        {
            return ItemRejectReason.NotOwner;
        }

        var range = (float)world.Content.Rules.Items.PickupRange;
        if (Vector2.DistanceSquared(Position(e), Position(ground)) > range * range)
        {
            return ItemRejectReason.TooFar;
        }

        var free = inv.FirstFree();
        if (free < 0)
        {
            return ItemRejectReason.InventoryFull;
        }

        inv[free] = g.Item;
        world.Remove(ground.Id);
        world.Events.Publish(new ItemPickedUp(e.Id, itemId, g.Item.BaseId, free));
        return null;
    }

    /// <summary>Larga o item do espaço do inventário no chão, aos pés do personagem (só ele pode pegar de volta). Null = feito.</summary>
    public static ItemRejectReason? Drop(World world, Entity e, int inventorySlot)
    {
        if (e.TryGet<Inventory>() is not { } inv)
        {
            return ItemRejectReason.NoInventory;
        }

        if (inventorySlot < 0 || inventorySlot >= inv.Capacity || inv[inventorySlot] is not { } item)
        {
            return ItemRejectReason.EmptySlot;
        }

        inv[inventorySlot] = null;
        SpawnGround(world, Position(e), item, [e.Id.Value], e.Id);
        return null;
    }

    /// <summary>Põe o item no primeiro espaço livre; retorna o espaço ou -1 (sem inventário ou cheio).</summary>
    public static int AddToInventory(Entity e, ItemInstance item)
    {
        var free = e.TryGet<Inventory>()?.FirstFree() ?? -1;
        if (free >= 0)
        {
            e.Get<Inventory>()[free] = item;
        }

        return free;
    }

    /// <summary>Cria o item no chão com dono e prazo (<see cref="ItemRules.GroundDurationTicks"/>) e publica <see cref="ItemDropped"/>.</summary>
    public static Entity SpawnGround(World world, Vector2 position, ItemInstance item, IEnumerable<int> owners, EntityId source)
    {
        var ground = world.Spawn(
            new Transform { Position = position },
            new GroundItem
            {
                Item = item,
                Owners = owners.Distinct().Order().ToArray(),
                ExpiresAtTick = world.Tick + world.Content.Rules.Items.GroundDurationTicks,
            });
        world.Events.Publish(new ItemDropped(ground.Id, source, item.BaseId, item.Level, item.Rarity));
        return ground;
    }

    /// <summary>
    /// Queda do monstro morto para <paramref name="owners"/> (quem participou da morte; sem ninguém, nada cai nem consome o
    /// RNG). Tabela pela categoria do monstro, nível do item = nível do monstro. Retorna os itens no chão.
    /// </summary>
    public static IReadOnlyList<Entity> DropLoot(World world, Entity monster, IReadOnlyCollection<Entity> owners)
    {
        if (owners.Count == 0 || monster.TryGet<UnitInfo>() is not { } info)
        {
            return [];
        }

        var table = world.Content.Rules.Items.Drops.For(info.Rank);
        if (table.Items.Count == 0 || table.Count < 1 || table.Chance <= 0 || world.Random.NextFloat() >= table.Chance)
        {
            return [];
        }

        var position = Position(monster);
        var ids = owners.Select(o => o.Id.Value).ToList();
        var dropped = new List<Entity>(table.Count);
        for (var i = 0; i < table.Count; i++)
        {
            var rarity = RollRarity(world.Random, table.Rarity);
            if (i == 0 && rarity < table.GuaranteedRarity)
            {
                rarity = table.GuaranteedRarity;
            }

            var baseId = table.Items[world.Random.NextInt(table.Items.Count)];
            dropped.Add(SpawnGround(world, position, Roll(world, baseId, info.Level, rarity), ids, monster.Id));
        }

        return dropped;
    }

    /// <summary>Cria o item com os afixos da raridade sorteados (distintos) pelo RNG da simulação.</summary>
    public static ItemInstance Roll(World world, string baseId, int level, ItemRarity rarity)
    {
        var rules = world.Content.Rules.Items;
        var count = (int)rules.AffixCount.For(rarity);
        var pool = rules.Affixes.Select(a => a.Id).ToList();
        var affixes = new List<string>(count);
        while (affixes.Count < count && pool.Count > 0)
        {
            var k = world.Random.NextInt(pool.Count);
            affixes.Add(pool[k]);
            pool.RemoveAt(k);
        }

        return new ItemInstance(baseId, level, rarity, affixes);
    }

    /// <summary>Refaz as estatísticas a partir do equipamento e ajusta vida e Mana máximas.</summary>
    public static void Refresh(World world, Entity e)
    {
        if (e.TryGet<CharacterStats>() is not { } s || e.TryGet<UnitInfo>() is not { } info
            || !world.Content.Classes.TryGetValue(info.DefinitionId, out var cls))
        {
            return;
        }

        ItemMath.Recompute(world.Content, cls, s, e.TryGet<CharacterProgress>(), e.TryGet<Equipment>());
        Leveling.RefreshDerived(world, e);
    }

    private static ItemRarity RollRarity(SimRandom random, RarityValues weights)
    {
        var all = Enum.GetValues<ItemRarity>();
        var total = all.Sum(r => Math.Max(0, weights.For(r)));
        if (total <= 0)
        {
            return ItemRarity.Common;
        }

        var roll = random.NextFloat() * total;
        foreach (var r in all)
        {
            roll -= Math.Max(0, weights.For(r));
            if (roll < 0)
            {
                return r;
            }
        }

        return all.Last(r => weights.For(r) > 0);
    }

    private static Vector2 Position(Entity e) => e.TryGet<Transform>()?.Position ?? Vector2.Zero;
}
