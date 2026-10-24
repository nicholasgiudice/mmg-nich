using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;

namespace Game.Core.Combat.Items;

/// <summary>
/// Contas dos itens (T-204, <c>design/progression-v0.1.md</c> §6). Estatísticas inteiras (dano de arma, armadura,
/// resistência, atributos, vida) são arredondadas por item; crítico e Mana/s ficam fracionários.
/// </summary>
public static class ItemMath
{
    private static readonly int StatCount = Enum.GetValues<ItemStat>().Length;

    public static bool IsInteger(ItemStat stat) => stat is not (ItemStat.Crit or ItemStat.ManaRegen);

    /// <summary>Valor de uma estatística no nível do item, já arredondado se for inteira.</summary>
    public static double ValueAt(ItemStatValue v, int itemLevel) => Round(v.Stat, v.At(itemLevel));

    /// <summary>Classes que usam a base: as dela; senão as do tipo de armadura; senão todas (lista vazia).</summary>
    public static IReadOnlyList<string> ClassesOf(ItemRules rules, ItemBaseDefinition item) =>
        item.Classes.Count > 0 ? item.Classes : item.ArmorType is { } t ? rules.ArmorTypes.For(t).Classes : [];

    public static bool CanUse(ItemRules rules, ItemBaseDefinition item, string classId)
    {
        var classes = ClassesOf(rules, item);
        return classes.Count == 0 || classes.Contains(classId);
    }

    /// <summary>
    /// Estatísticas do item, indexadas por <see cref="ItemStat"/>: base (armadura × tipo; leve ganha resistência pela
    /// fração da armadura) + afixos. Base ou afixo desconhecido conta 0.
    /// </summary>
    public static double[] StatsOf(ContentCatalog content, ItemInstance item)
    {
        var totals = new double[StatCount];
        if (!content.Items.TryGetValue(item.BaseId, out var def))
        {
            return totals;
        }

        var rules = content.Rules.Items;
        var type = def.ArmorType is { } t ? rules.ArmorTypes.For(t) : null;
        foreach (var v in def.Stats)
        {
            if (v.Stat == ItemStat.Armor && type is not null)
            {
                var armor = Round(ItemStat.Armor, v.At(item.Level) * type.ArmorMultiplier);
                totals[(int)ItemStat.Armor] += armor;
                totals[(int)ItemStat.Resistance] += Round(ItemStat.Resistance, armor * type.ResistanceFromArmor);
            }
            else
            {
                totals[(int)v.Stat] += ValueAt(v, item.Level);
            }
        }

        foreach (var id in item.Affixes)
        {
            if (rules.Affixes.FirstOrDefault(a => a.Id == id) is { } affix)
            {
                totals[(int)affix.Value.Stat] += ValueAt(affix.Value, item.Level);
            }
        }

        return totals;
    }

    /// <summary>Soma das estatísticas dos itens equipados.</summary>
    public static double[] StatsOf(ContentCatalog content, Equipment? equipment)
    {
        var totals = new double[StatCount];
        foreach (var (_, item) in equipment?.Items ?? [])
        {
            var s = StatsOf(content, item);
            for (var i = 0; i < totals.Length; i++)
            {
                totals[i] += s[i];
            }
        }

        return totals;
    }

    /// <summary>
    /// Refaz as estatísticas (menos o nível): perfil da classe + pontos distribuídos + equipamento. Não mexe em vida e Mana
    /// (quem chama usa <see cref="Leveling.RefreshDerived"/>).
    /// </summary>
    public static void Recompute(ContentCatalog content, ClassDefinition cls, CharacterStats s, CharacterProgress? progress, Equipment? equipment)
    {
        var a = cls.Attributes ?? new AttributeSet();
        var g = StatsOf(content, equipment);
        s.Str = a.Str + (progress?.AllocatedStr ?? 0) + (int)g[(int)ItemStat.Str];
        s.Dex = a.Dex + (progress?.AllocatedDex ?? 0) + (int)g[(int)ItemStat.Dex];
        s.Agi = a.Agi + (progress?.AllocatedAgi ?? 0) + (int)g[(int)ItemStat.Agi];
        s.Int = a.Int + (progress?.AllocatedInt ?? 0) + (int)g[(int)ItemStat.Int];
        s.Vit = a.Vit + (progress?.AllocatedVit ?? 0) + (int)g[(int)ItemStat.Vit];
        s.WeaponDamage = (float)g[(int)ItemStat.WeaponDamage];
        s.Armor = (float)g[(int)ItemStat.Armor];
        s.Resistance = (float)g[(int)ItemStat.Resistance];
        s.HealthBonus = (float)g[(int)ItemStat.Health];
        s.CritBonus = (float)g[(int)ItemStat.Crit];
        s.ManaRegen = (float)g[(int)ItemStat.ManaRegen];
    }

    /// <summary>Equipamento inicial da classe (comum, sem afixos). Lança se a classe aponta para item inexistente.</summary>
    public static Equipment StartingEquipment(ContentCatalog content, ClassDefinition cls)
    {
        var equipment = new Equipment();
        foreach (var start in cls.StartingEquipment)
        {
            if (!content.Items.TryGetValue(start.Item, out var def))
            {
                throw new InvalidOperationException($"Item inicial '{start.Item}' de {cls.Id} não está no catálogo (faltou passar os itens?)");
            }

            equipment[def.Slot] = ItemInstance.Common(def.Id, start.Level);
        }

        return equipment;
    }

    private static double Round(ItemStat stat, double value) =>
        IsInteger(stat) ? Math.Round(value, MidpointRounding.AwayFromZero) : value;
}
