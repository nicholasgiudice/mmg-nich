using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat.Items;

/// <summary>
/// Item concreto (T-204): base, nível do item, raridade e ids dos afixos sorteados. Os valores saem das definições
/// (<see cref="ItemMath"/>), então isto é tudo que se salva e se manda pela rede. Imutável: componentes compartilham.
/// T-320: <see cref="Count"/> = unidades da pilha (consumíveis, até <c>ItemBaseDefinition.MaxStack</c>); equipamento = 1.
/// </summary>
public sealed record ItemInstance(string BaseId, int Level, ItemRarity Rarity, IReadOnlyList<string> Affixes, int Count = 1)
{
    public static ItemInstance Common(string baseId, int level) => new(baseId, level, ItemRarity.Common, []);

    /// <summary>Pilha de consumível (T-320): nível 1, comum, sem afixos.</summary>
    public static ItemInstance Stack(string baseId, int count) => new(baseId, 1, ItemRarity.Common, [], count);

    /// <summary>Mesmo item com outra quantidade.</summary>
    public ItemInstance WithCount(int count) => this with { Count = count };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(BaseId);
        hasher.Add(Level);
        hasher.Add((int)Rarity);
        hasher.Add(Affixes.Count);
        foreach (var a in Affixes)
        {
            hasher.Add(a);
        }

        hasher.Add(Count);
    }
}

/// <summary>
/// Espaços de itens com cópia sob demanda: <see cref="IComponent.Clone"/> (snapshot a cada tick) compartilha o vetor e quem
/// escrever depois copia. Assim o snapshot não aloca um vetor por personagem.
/// </summary>
public abstract class ItemSlots
{
    private ItemInstance?[] _slots;
    private bool _shared;

    protected ItemSlots(int count)
    {
        _slots = new ItemInstance?[count];
    }

    protected ItemSlots(ItemSlots from)
    {
        from._shared = true;
        _slots = from._slots;
        _shared = true;
    }

    protected ItemInstance? GetAt(int index) => _slots[index];

    protected void SetAt(int index, ItemInstance? item)
    {
        if (_shared)
        {
            _slots = (ItemInstance?[])_slots.Clone();
            _shared = false;
        }

        _slots[index] = item;
    }

    protected IReadOnlyList<ItemInstance?> All => _slots;

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(_slots.Length);
        foreach (var item in _slots)
        {
            hasher.Add(item is not null);
            item?.WriteHash(hasher);
        }
    }
}

/// <summary>Inventário de capacidade fixa (T-204): espaços numerados, vazios = null. Replicável, mas só o dono recebe.</summary>
public sealed class Inventory : ItemSlots, IComponent, IReplicated
{
    public Inventory(int capacity)
        : base(capacity)
    {
    }

    private Inventory(Inventory from)
        : base(from)
    {
    }

    public int Capacity => All.Count;

    public ItemInstance? this[int index]
    {
        get => GetAt(index);
        set => SetAt(index, value);
    }

    public IReadOnlyList<ItemInstance?> Slots => All;

    /// <summary>Primeiro espaço vazio; -1 se cheio.</summary>
    public int FirstFree()
    {
        for (var i = 0; i < All.Count; i++)
        {
            if (All[i] is null)
            {
                return i;
            }
        }

        return -1;
    }

    public IComponent Clone() => new Inventory(this);
}

/// <summary>Itens equipados por espaço (T-204). Visível para todos.</summary>
public sealed class Equipment : ItemSlots, IComponent, IReplicated
{
    private static readonly EquipSlot[] AllSlots = Enum.GetValues<EquipSlot>();

    public Equipment()
        : base(AllSlots.Length)
    {
    }

    private Equipment(Equipment from)
        : base(from)
    {
    }

    public ItemInstance? this[EquipSlot slot]
    {
        get => GetAt((int)slot);
        set => SetAt((int)slot, value);
    }

    /// <summary>Espaços ocupados, na ordem de <see cref="EquipSlot"/>.</summary>
    public IEnumerable<(EquipSlot Slot, ItemInstance Item)> Items
    {
        get
        {
            foreach (var slot in AllSlots)
            {
                if (GetAt((int)slot) is { } item)
                {
                    yield return (slot, item);
                }
            }
        }
    }

    public IComponent Clone() => new Equipment(this);
}

/// <summary>Ouro do personagem (T-320). Replicável, mas só o dono recebe (como o <see cref="Inventory"/>).</summary>
public sealed class Wallet : IComponent, IReplicated
{
    public long Gold { get; set; }

    public IComponent Clone() => new Wallet { Gold = Gold };

    public void WriteHash(StateHasher hasher) => hasher.Add(Gold);
}

/// <summary>
/// Item no chão (T-204): entidade com <see cref="Simulation.Components.Transform"/> e este componente; some em
/// <see cref="ExpiresAtTick"/>. Quem pode pegar (T-394):
/// <list type="bullet">
/// <item>antes de <see cref="FreeAtTick"/>, só o <see cref="Owner"/> (o dono escolhido entre os participantes);</item>
/// <item>a partir de <see cref="FreeAtTick"/>, qualquer um, tenha participado da morte ou não;</item>
/// <item><see cref="Owners"/> vazio (item largado por um jogador): qualquer um, a qualquer momento.</item>
/// </list>
/// <see cref="Owners"/> guarda os participantes da morte (ids de entidade, em ordem), entre os quais o dono foi escolhido.
/// </summary>
public sealed class GroundItem : IComponent, IReplicated
{
    public required ItemInstance Item { get; init; }
    public required IReadOnlyList<int> Owners { get; init; }

    /// <summary>Id da entidade com prioridade até <see cref="FreeAtTick"/>; 0 = ninguém.</summary>
    public int Owner { get; init; }

    public long FreeAtTick { get; init; }
    public long ExpiresAtTick { get; init; }

    /// <summary>Item sem dono: qualquer um pega.</summary>
    public bool IsPublic => Owners.Count == 0;

    /// <summary>Se <paramref name="id"/> pode pegar o item no tick <paramref name="tick"/> (sem contar distância e espaço).</summary>
    public bool CanPickUp(EntityId id, long tick) =>
        IsPublic || tick >= FreeAtTick || Owner == id.Value;

    public IComponent Clone() => new GroundItem { Item = Item, Owners = Owners, Owner = Owner, FreeAtTick = FreeAtTick, ExpiresAtTick = ExpiresAtTick };

    public void WriteHash(StateHasher hasher)
    {
        Item.WriteHash(hasher);
        hasher.Add(Owners.Count);
        foreach (var o in Owners)
        {
            hasher.Add(o);
        }

        hasher.Add(Owner);
        hasher.Add(FreeAtTick);
        hasher.Add(ExpiresAtTick);
    }
}
