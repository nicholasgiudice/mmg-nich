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

        if (def.Slot is not { } slot)
        {
            return ItemRejectReason.NotEquippable;
        }

        if (!ItemMath.CanUse(world.Content.Rules.Items, def, e.TryGet<UnitInfo>()?.DefinitionId ?? ""))
        {
            return ItemRejectReason.ClassCannotUse;
        }

        var previous = eq[slot];
        eq[slot] = item;
        inv[inventorySlot] = previous;
        Refresh(world, e);
        world.Events.Publish(new ItemEquipped(e.Id, slot, item.BaseId, previous?.BaseId ?? ""));
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

    /// <summary>
    /// Pega o item do chão (precisa ter direito, estar no alcance e ter espaço). Pilhas (T-320) completam as pilhas do mesmo
    /// item antes de abrir espaço novo; se a pilha inteira não couber, nada é pego. Null = feito.
    /// </summary>
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

        var free = AddToInventory(world, e, g.Item);
        if (free < 0)
        {
            return ItemRejectReason.InventoryFull;
        }

        world.Remove(ground.Id);
        world.Events.Publish(new ItemPickedUp(e.Id, itemId, g.Item.BaseId, free));
        return null;
    }

    /// <summary>
    /// Larga o item do espaço do inventário no chão, aos pés do personagem (só ele pode pegar de volta). Pilha (T-320): larga
    /// a pilha inteira. Null = feito.
    /// </summary>
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

    /// <summary>
    /// Põe o item no inventário; retorna o primeiro espaço usado ou -1 (sem inventário ou sem lugar, e então nada muda).
    /// Consumível (T-320): completa as pilhas do mesmo item, em ordem de espaço, até <see cref="ItemBaseDefinition.MaxStack"/>,
    /// e o que sobrar abre espaços novos. Tudo ou nada: se não couber inteiro, não entra nada.
    /// </summary>
    public static int AddToInventory(World world, Entity e, ItemInstance item)
    {
        if (e.TryGet<Inventory>() is not { } inv || item.Count < 1)
        {
            return -1;
        }

        var max = MaxStack(world, item);
        if (max <= 1)
        {
            var free = inv.FirstFree();
            if (free >= 0)
            {
                inv[free] = item;
            }

            return free;
        }

        if (Room(inv, item, max) < item.Count)
        {
            return -1;
        }

        var left = item.Count;
        var first = -1;
        for (var i = 0; i < inv.Capacity && left > 0; i++)
        {
            if (inv[i] is { } other && SameStack(other, item) && other.Count < max)
            {
                var add = Math.Min(left, max - other.Count);
                inv[i] = other.WithCount(other.Count + add);
                left -= add;
                first = first < 0 ? i : first;
            }
        }

        for (var i = 0; i < inv.Capacity && left > 0; i++)
        {
            if (inv[i] is null)
            {
                var add = Math.Min(left, max);
                inv[i] = item.WithCount(add);
                left -= add;
                first = first < 0 ? i : first;
            }
        }

        return first;
    }

    /// <summary>Quantas unidades do item ainda cabem no inventário (pilhas do mesmo item + espaços livres).</summary>
    public static int Room(World world, Inventory inv, ItemInstance item) => Room(inv, item, MaxStack(world, item));

    /// <summary>Unidades por espaço do item (1 para equipamento e itens que o catálogo não conhece).</summary>
    public static int MaxStack(World world, ItemInstance item) =>
        world.Content.Items.TryGetValue(item.BaseId, out var def) ? def.MaxStack : 1;

    private static int Room(Inventory inv, ItemInstance item, int max)
    {
        var room = 0;
        foreach (var slot in inv.Slots)
        {
            room += slot is null ? max : SameStack(slot, item) ? Math.Max(0, max - slot.Count) : 0;
        }

        return room;
    }

    private static bool SameStack(ItemInstance a, ItemInstance b) =>
        a.BaseId == b.BaseId && a.Level == b.Level && a.Rarity == b.Rarity && a.Affixes.SequenceEqual(b.Affixes);

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
