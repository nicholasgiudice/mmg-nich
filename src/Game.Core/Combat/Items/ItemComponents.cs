using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat.Items;

/// <summary>
/// Item concreto (T-204): base, nível do item, raridade e ids dos afixos sorteados. Os valores saem das definições
/// (<see cref="ItemMath"/>), então isto é tudo que se salva e se manda pela rede. Imutável: componentes compartilham.
/// </summary>
public sealed record ItemInstance(string BaseId, int Level, ItemRarity Rarity, IReadOnlyList<string> Affixes)
{
    public static ItemInstance Common(string baseId, int level) => new(baseId, level, ItemRarity.Common, []);

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

/// <summary>
/// Item no chão (T-204): entidade com <see cref="Simulation.Components.Transform"/> e este componente. Só quem está em
/// <see cref="Owners"/> (ids de entidade, em ordem) pode pegar; some em <see cref="ExpiresAtTick"/>.
/// </summary>
public sealed class GroundItem : IComponent, IReplicated
{
    public required ItemInstance Item { get; init; }
    public required IReadOnlyList<int> Owners { get; init; }
    public long ExpiresAtTick { get; init; }

    public bool IsOwner(EntityId id) => Owners.Contains(id.Value);

    public IComponent Clone() => new GroundItem { Item = Item, Owners = Owners, ExpiresAtTick = ExpiresAtTick };

    public void WriteHash(StateHasher hasher)
    {
        Item.WriteHash(hasher);
        hasher.Add(Owners.Count);
        foreach (var o in Owners)
        {
            hasher.Add(o);
        }

        hasher.Add(ExpiresAtTick);
    }
}
