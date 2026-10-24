namespace Game.Core.Definitions;

/// <summary>Espaço de equipamento (T-204, <c>design/progression-v0.1.md</c> §6). JSON: <c>weapon</c>, <c>head</c>, ...</summary>
public enum EquipSlot { Weapon, Head, Chest, Hands, Feet, Accessory }

/// <summary>Tipo de armadura (T-204): define quem usa, o multiplicador da armadura e a resistência extra.</summary>
public enum ArmorType { Heavy, Medium, Light }

/// <summary>Raridade (T-204): quantos afixos o item sorteia. Ordem = do pior para o melhor.</summary>
public enum ItemRarity { Common, Uncommon, Rare, Epic }

/// <summary>
/// Estatística que um item (base ou afixo) dá. JSON: <c>weapon_damage</c>, <c>armor</c>, <c>resistance</c>, <c>str</c>,
/// <c>dex</c>, <c>agi</c>, <c>int</c>, <c>vit</c>, <c>health</c>, <c>crit</c> (fração, 0,01 = 1%), <c>mana_regen</c> (por segundo).
/// </summary>
public enum ItemStat { WeaponDamage, Armor, Resistance, Str, Dex, Agi, Int, Vit, Health, Crit, ManaRegen }

/// <summary>Valor que cresce com o nível do item: <see cref="Base"/> + <see cref="PerLevel"/> × nível.</summary>
public sealed record ItemStatValue
{
    public required ItemStat Stat { get; init; }
    public double Base { get; init; }
    public double PerLevel { get; init; }

    public double At(int itemLevel) => Base + (PerLevel * itemLevel);
}

/// <summary>
/// Item base (T-204, <c>data/items.json</c>, seção <c>items</c>): espaço, quem usa e as estatísticas base por nível do
/// item. Armaduras (cabeça, peito, mãos, pés) têm <see cref="ArmorType"/>; a armadura base é multiplicada pelo tipo.
/// </summary>
public sealed record ItemBaseDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required EquipSlot Slot { get; init; }

    /// <summary>Classes que usam. Vazio = as do <see cref="ArmorType"/> (armaduras) ou todas.</summary>
    public IReadOnlyList<string> Classes { get; init; } = [];

    /// <summary>Obrigatório em cabeça, peito, mãos e pés; proibido em arma e acessório.</summary>
    public ArmorType? ArmorType { get; init; }

    public IReadOnlyList<ItemStatValue> Stats { get; init; } = [];

    public static bool IsArmorSlot(EquipSlot slot) => slot is EquipSlot.Head or EquipSlot.Chest or EquipSlot.Hands or EquipSlot.Feet;
}

/// <summary>Item que a classe começa usando (T-204; substitui a arma inicial da T-201). Raridade comum, sem afixos.</summary>
public sealed record StartingItemDefinition
{
    public required string Item { get; init; }
    public int Level { get; init; } = 1;
}

/// <summary>Afixo (T-204): uma estatística a mais, com valor pelo nível do item.</summary>
public sealed record AffixDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ItemStatValue Value { get; init; }
}

/// <summary>Tipo de armadura (T-204): quem usa, armadura × multiplicador e resistência = fração da armadura (leve).</summary>
public sealed record ArmorTypeDefinition
{
    public IReadOnlyList<string> Classes { get; init; } = [];
    public double ArmorMultiplier { get; init; } = 1;
    public double ResistanceFromArmor { get; init; }
}

public sealed record ArmorTypes
{
    public ArmorTypeDefinition Heavy { get; init; } = new() { ArmorMultiplier = 1.5 };
    public ArmorTypeDefinition Medium { get; init; } = new();
    public ArmorTypeDefinition Light { get; init; } = new() { ArmorMultiplier = 0.6, ResistanceFromArmor = 0.5 };

    public ArmorTypeDefinition For(ArmorType type) => type switch
    {
        ArmorType.Heavy => Heavy,
        ArmorType.Medium => Medium,
        _ => Light,
    };
}

/// <summary>Um valor por raridade (afixos por raridade, pesos de sorteio).</summary>
public sealed record RarityValues
{
    public double Common { get; init; }
    public double Uncommon { get; init; }
    public double Rare { get; init; }
    public double Epic { get; init; }

    public double For(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => Common,
        ItemRarity.Uncommon => Uncommon,
        ItemRarity.Rare => Rare,
        _ => Epic,
    };
}

/// <summary>
/// Tabela de queda de uma categoria de monstro (T-204). Com chance <see cref="Chance"/> o monstro deixa
/// <see cref="Count"/> itens; cada um sorteia a raridade pelos pesos e a base entre <see cref="Items"/> (mesmo peso).
/// O primeiro item sai com pelo menos <see cref="GuaranteedRarity"/>.
/// </summary>
public sealed record DropTableDefinition
{
    public double Chance { get; init; }
    public int Count { get; init; } = 1;
    public RarityValues Rarity { get; init; } = new() { Common = 1 };
    public ItemRarity GuaranteedRarity { get; init; } = ItemRarity.Common;
    public IReadOnlyList<string> Items { get; init; } = [];
}

public sealed record DropTables
{
    public DropTableDefinition Normal { get; init; } = new();
    public DropTableDefinition Elite { get; init; } = new();
    public DropTableDefinition Boss { get; init; } = new();

    public DropTableDefinition For(MonsterRank rank) => rank switch
    {
        MonsterRank.Elite => Elite,
        MonsterRank.Boss => Boss,
        _ => Normal,
    };
}

/// <summary>
/// Regras de itens (T-204, <c>data/items.json</c>, seção <c>rules.items</c>; §6 do design de progressão).
/// Sem a seção não há afixos nem quedas.
/// </summary>
public sealed record ItemRules
{
    /// <summary>Espaços do inventário.</summary>
    public int InventorySize { get; init; } = 30;

    /// <summary>Distância máxima (tiles) entre o personagem e o item no chão para pegar.</summary>
    public double PickupRange { get; init; } = 2;

    /// <summary>Quanto tempo o item fica no chão antes de sumir. JSON: "groundDuration" (segundos).</summary>
    public int GroundDurationTicks { get; init; } = 120 * 20;

    public ArmorTypes ArmorTypes { get; init; } = new();

    /// <summary>Quantos afixos cada raridade sorteia (inteiros).</summary>
    public RarityValues AffixCount { get; init; } = new();

    public IReadOnlyList<AffixDefinition> Affixes { get; init; } = [];

    public DropTables Drops { get; init; } = new();
}
