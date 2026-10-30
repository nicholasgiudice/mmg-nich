using System.Globalization;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Definitions;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Nomes, cores e tooltips de itens (T-222). Os valores saem do Core (<see cref="ItemMath.StatsOf(ContentCatalog, ItemInstance)"/>),
/// então o que a tela mostra é o que o servidor soma nas estatísticas. Só apresentação.
/// </summary>
public static class ItemText
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly ItemStat[] Stats = Enum.GetValues<ItemStat>();

    public static Color RarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Uncommon => new Color(0.35f, 0.9f, 0.35f),
        ItemRarity.Rare => new Color(0.35f, 0.6f, 1f),
        ItemRarity.Epic => new Color(0.75f, 0.4f, 1f),
        _ => new Color(0.85f, 0.85f, 0.85f),
    };

    public static string RarityName(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Uncommon => "Incomum",
        ItemRarity.Rare => "Raro",
        ItemRarity.Epic => "Épico",
        _ => "Comum",
    };

    public static string SlotName(EquipSlot slot) => slot switch
    {
        EquipSlot.Weapon => "Arma",
        EquipSlot.Head => "Cabeça",
        EquipSlot.Chest => "Peito",
        EquipSlot.Hands => "Mãos",
        EquipSlot.Feet => "Pés",
        _ => "Acessório",
    };

    public static string ArmorName(ArmorType type) => type switch
    {
        ArmorType.Heavy => "pesada",
        ArmorType.Medium => "média",
        _ => "leve",
    };

    public static string StatName(ItemStat stat) => stat switch
    {
        ItemStat.WeaponDamage => "Dano da arma",
        ItemStat.Armor => "Armadura",
        ItemStat.Resistance => "Resistência",
        ItemStat.Str => "FOR",
        ItemStat.Dex => "DES",
        ItemStat.Agi => "AGI",
        ItemStat.Int => "INT",
        ItemStat.Vit => "VIT",
        ItemStat.Health => "Vida",
        ItemStat.Crit => "Crítico",
        _ => "Mana/s",
    };

    /// <summary>Valor formatado de uma estatística (crítico em %, Mana/s com decimais, o resto inteiro).</summary>
    public static string Value(ItemStat stat, double v, bool signed = true)
    {
        var sign = signed && v > 0 ? "+" : "";
        return stat switch
        {
            ItemStat.Crit => sign + (v * 100).ToString("0.##", Pt) + "%",
            ItemStat.ManaRegen => sign + v.ToString("0.##", Pt),
            _ => sign + v.ToString("0", Pt),
        };
    }

    public static string Name(ContentCatalog content, ItemInstance item) =>
        content.Items.TryGetValue(item.BaseId, out var def) ? def.Name : item.BaseId;

    /// <summary>Nome curto para o quadrado do inventário (até 3 letras).</summary>
    public static string Abbreviation(ContentCatalog content, ItemInstance item)
    {
        var words = Name(content, item).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length > 2).ToList();
        return words.Count switch
        {
            0 => "?",
            1 => words[0][..Math.Min(3, words[0].Length)],
            _ => string.Concat(words.Take(2).Select(w => char.ToUpperInvariant(w[0]))),
        };
    }

    public static bool CanUse(ContentCatalog content, ItemInstance item, string? classId) =>
        classId is null || !content.Items.TryGetValue(item.BaseId, out var def) || ItemMath.CanUse(content.Rules.Items, def, classId);

    /// <summary>
    /// Tooltip em BBCode: nome na cor da raridade, tipo e nível, estatísticas (base + afixos), quem usa e, se
    /// <paramref name="equipped"/> for dado, a diferença para o que está equipado no mesmo espaço.
    /// </summary>
    public static string Tooltip(ContentCatalog content, ItemInstance item, string? classId, ItemInstance? equipped, bool isEquipped)
    {
        content.Items.TryGetValue(item.BaseId, out var def);
        var lines = new List<string>
        {
            $"[b][color=#{RarityColor(item.Rarity).ToHtml(false)}]{Hud.Escape(Name(content, item))}[/color][/b]",
            $"[color=#aaaaaa]{RarityName(item.Rarity)} · {(def is null ? "?" : SlotName(def.Slot))}"
                + (def?.ArmorType is { } t ? $" ({ArmorName(t)})" : "") + $" · nível do item {item.Level}[/color]",
        };

        var stats = ItemMath.StatsOf(content, item);
        foreach (var stat in Stats.Where(s => stats[(int)s] != 0))
        {
            lines.Add($"{StatName(stat)} {Value(stat, stats[(int)stat])}");
        }

        foreach (var id in item.Affixes)
        {
            if (content.Rules.Items.Affixes.FirstOrDefault(a => a.Id == id) is { } affix)
            {
                lines.Add($"[color=#{RarityColor(item.Rarity).ToHtml(false)}]◆ {Hud.Escape(affix.Name)}[/color] "
                    + $"[color=#aaaaaa]({StatName(affix.Value.Stat)} {Value(affix.Value.Stat, ItemMath.ValueAt(affix.Value, item.Level))})[/color]");
            }
        }

        if (!CanUse(content, item, classId) && def is not null)
        {
            var who = ItemMath.ClassesOf(content.Rules.Items, def)
                .Select(c => content.Classes.TryGetValue(c, out var cls) ? cls.Name : c);
            lines.Add($"[color=#ff8866]Sua classe não usa (só {string.Join(", ", who)})[/color]");
        }

        if (!isEquipped && def is not null)
        {
            lines.Add("");
            lines.Add(equipped is null ? $"[color=#aaaaaa]Nada equipado em {SlotName(def.Slot)}.[/color]" : "[color=#aaaaaa]Comparado com o equipado:[/color]");
            if (equipped is not null)
            {
                var current = ItemMath.StatsOf(content, equipped);
                var any = false;
                foreach (var stat in Stats)
                {
                    var diff = stats[(int)stat] - current[(int)stat];
                    if (Math.Abs(diff) < 1e-6)
                    {
                        continue;
                    }

                    any = true;
                    var color = diff > 0 ? "77ee77" : "ff7766";
                    lines.Add($"[color=#{color}]{StatName(stat)} {Value(stat, diff)}[/color]");
                }

                if (!any)
                {
                    lines.Add("[color=#aaaaaa]Mesmas estatísticas.[/color]");
                }
            }

            lines.Add("[color=#aaaaaa]Clique: equipar · botão direito: largar no chão[/color]");
        }
        else if (isEquipped)
        {
            lines.Add("");
            lines.Add("[color=#aaaaaa]Clique: desequipar (vai para o inventário)[/color]");
        }

        return string.Join("\n", lines);
    }

    public static string Rejection(ItemRejectReason reason) => reason switch
    {
        ItemRejectReason.NoInventory => "Sem inventário",
        ItemRejectReason.EmptySlot => "Espaço vazio",
        ItemRejectReason.UnknownItem => "Item desconhecido",
        ItemRejectReason.ClassCannotUse => "Sua classe não usa esse item",
        ItemRejectReason.InventoryFull => "Inventário cheio",
        ItemRejectReason.NotFound => "O item não está mais lá",
        ItemRejectReason.NotOwner => "Esse item não é seu",
        ItemRejectReason.TooFar => "Longe demais para pegar",
        _ => reason.ToString(),
    };
}
