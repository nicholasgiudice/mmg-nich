using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;

namespace Game.Core.Combat.Items;

/// <summary>
/// Uso de consumíveis (T-320): <see cref="UseItemIntent"/> executa a habilidade do item (<see cref="ItemBaseDefinition.UseAbility"/>)
/// no próprio personagem e gasta 1 unidade da pilha. A recarga é a da habilidade, guardada em <c>AbilityState.ReadyAtTick</c>
/// pelo id dela: pilhas do mesmo item (e itens diferentes com a mesma habilidade) dividem a recarga. A habilidade de item
/// não precisa ser conhecida pelo personagem e é sempre instantânea (o validador de dados exige).
/// </summary>
public static class Consumables
{
    /// <summary>Trata a intenção (chamado pelo <see cref="AbilitySystem"/>); recusa publica <see cref="ItemRejected"/>.</summary>
    public static void Handle(World world, UseItemIntent intent)
    {
        if (world.Find(intent.Entity) is { } e && Use(world, e, intent.InventorySlot) is { } reason)
        {
            world.Events.Publish(new ItemRejected(e.Id, ItemAction.Use, reason));
        }
    }

    /// <summary>Usa o consumível do espaço. Null = feito.</summary>
    public static ItemRejectReason? Use(World world, Entity e, int inventorySlot)
    {
        if (Validate(world, e, inventorySlot, out var check) is { } reason)
        {
            return reason;
        }

        var inv = e.Get<Inventory>();
        var item = inv[inventorySlot]!;
        var remaining = item.Count - 1;
        inv[inventorySlot] = remaining > 0 ? item.WithCount(remaining) : null;
        AbilitySystem.ExecuteItemAbility(world, e, check!);
        world.Events.Publish(new ItemUsed(e.Id, inventorySlot, item.BaseId, check!.Ability!.Id, remaining));
        return null;
    }

    /// <summary>Pode usar o consumível do espaço agora? Null = pode (e <paramref name="check"/> traz a habilidade e os alvos).</summary>
    public static ItemRejectReason? Validate(World world, Entity e, int inventorySlot, out AbilityCheck? check)
    {
        check = null;
        if (e.TryGet<Inventory>() is not { } inv)
        {
            return ItemRejectReason.NoInventory;
        }

        if (!e.IsAlive)
        {
            return ItemRejectReason.Dead;
        }

        if (inventorySlot < 0 || inventorySlot >= inv.Capacity || inv[inventorySlot] is not { } item)
        {
            return ItemRejectReason.EmptySlot;
        }

        if (!world.Content.Items.TryGetValue(item.BaseId, out var def))
        {
            return ItemRejectReason.UnknownItem;
        }

        if (!def.IsConsumable || def.UseAbility is not { } abilityId)
        {
            return ItemRejectReason.NotConsumable;
        }

        check = AbilityValidator.Validate(world, e, abilityId, null, null, itemUse: true);
        return check.Reason switch
        {
            null => null,
            AbilityRejectReason.Dead => ItemRejectReason.Dead,
            AbilityRejectReason.OnCooldown => ItemRejectReason.OnCooldown,
            AbilityRejectReason.UnknownAbility => ItemRejectReason.UnknownItem,
            _ => ItemRejectReason.CannotUseNow,
        };
    }

    /// <summary>Primeiro espaço com o consumível <paramref name="baseId"/>; -1 se não tem.</summary>
    public static int FindSlot(Entity e, string baseId)
    {
        if (e.TryGet<Inventory>() is { } inv)
        {
            for (var i = 0; i < inv.Capacity; i++)
            {
                if (inv[i]?.BaseId == baseId)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>Total de unidades do item no inventário (todas as pilhas).</summary>
    public static int CountOf(Entity e, string baseId) =>
        e.TryGet<Inventory>()?.Slots.Where(i => i?.BaseId == baseId).Sum(i => i!.Count) ?? 0;
}
