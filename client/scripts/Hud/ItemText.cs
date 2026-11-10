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

    /// <summary>Consumível cuja habilidade cura vida (a poção da tecla Q, T-323).</summary>
    public static bool IsHealingConsumable(ContentCatalog content, string id) =>
        content.Items.TryGetValue(id, out var item) && item.IsConsumable && item.UseAbility is { } use
        && content.Abilities.TryGetValue(use, out var ability) && ability.Effects.OfType<HealEffect>().Any();

    /// <summary>Ouro com separador de milhar: "1.250 de ouro".</summary>
    public static string Gold(long amount) => amount.ToString("#,0", Pt) + " de ouro";

    /// <summary>Quantas unidades do item há no inventário, somando as pilhas.</summary>
    public static int CountOf(Inventory? inventory, string baseId) =>
        inventory?.Slots.Where(i => i?.BaseId == baseId).Sum(i => i!.Count) ?? 0;

    /// <summary>Efeito e recarga do consumível (a habilidade que ele usa), em linhas de tooltip.</summary>
    public static IEnumerable<string> ConsumableLines(ContentCatalog content, ItemBaseDefinition def)
    {
        if (def.UseAbility is { } use && content.AbilityAt(use, 1) is { } ability)
        {
            yield return Hud.Escape(AbilityText.Describe(ability, content));
            yield return "[color=#aaaaaa]A recarga vale para todas as unidades do mesmo item.[/color]";
        }
    }

    public static bool CanUse(ContentCatalog content, ItemInstance item, string? classId) =>
        classId is null || !content.Items.TryGetValue(item.BaseId, out var def) || ItemMath.CanUse(content.Rules.Items, def, classId);

    /// <summary>
    /// Tooltip em BBCode: nome na cor da raridade, tipo e nível, estatísticas (base + afixos), quem usa e, se
    /// <paramref name="equipped"/> for dado, a diferença para o que está equipado no mesmo espaço.
    /// </summary>
    /// <param name="sellPrice">T-323: na aba Vender da loja, quanto a loja paga por unidade (null = fora da loja).</param>
    public static string Tooltip(ContentCatalog content, ItemInstance item, string? classId, ItemInstance? equipped, bool isEquipped,
        int? sellPrice = null)
    {
        content.Items.TryGetValue(item.BaseId, out var def);
        var lines = new List<string>
        {
            $"[b][color=#{RarityColor(item.Rarity).ToHtml(false)}]{Hud.Escape(Name(content, item))}[/color][/b]",
            def is { IsConsumable: true }
                ? $"[color=#aaaaaa]consumível · {item.Count}/{def.MaxStack}[/color]"
                : $"[color=#aaaaaa]{RarityName(item.Rarity)} · {(def?.Slot is { } s ? SlotName(s) : "?")}"
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

        if (def is { IsConsumable: true })
        {
            lines.AddRange(ConsumableLines(content, def));
        }

        if (sellPrice is { } price)
        {
            lines.Add("");
            lines.Add(item.Count > 1
                ? $"[color=#ffd966]Vende por {Gold(price)} cada ({Gold((long)price * item.Count)} a pilha)[/color]"
                : $"[color=#ffd966]Vende por {Gold(price)}[/color]");
            return string.Join("\n", lines);
        }

        if (def is { IsConsumable: true })
        {
            lines.Add("");
            lines.Add("[color=#aaaaaa]Duplo clique: usar · arraste para a barra (1 a 6): atalho · botão direito ou arraste para fora: largar no chão[/color]");
        }

        if (!CanUse(content, item, classId) && def is not null)
        {
            var who = ItemMath.ClassesOf(content.Rules.Items, def)
                .Select(c => content.Classes.TryGetValue(c, out var cls) ? cls.Name : c);
            lines.Add($"[color=#ff8866]Sua classe não usa (só {string.Join(", ", who)})[/color]");
        }

        if (!isEquipped && def?.Slot is { } slot)
        {
            lines.Add("");
            lines.Add(equipped is null ? $"[color=#aaaaaa]Nada equipado em {SlotName(slot)}.[/color]" : "[color=#aaaaaa]Comparado com o equipado:[/color]");
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

            lines.Add("[color=#aaaaaa]Arraste para o boneco ou duplo clique: equipar · botão direito ou arraste para fora: largar no chão[/color]");
        }
        else if (isEquipped)
        {
            lines.Add("");
            lines.Add("[color=#aaaaaa]Arraste para a mochila ou duplo clique: desequipar[/color]");
        }

        return string.Join("\n", lines);
    }

    /// <summary>Recusa da loja (T-323), texto curto.</summary>
    public static string ShopRejection(Game.Core.Combat.Shops.ShopRejectReason reason) => reason switch
    {
        Game.Core.Combat.Shops.ShopRejectReason.NoShop => "Ninguém para negociar aqui",
        Game.Core.Combat.Shops.ShopRejectReason.TooFar => "Chegue mais perto",
        Game.Core.Combat.Shops.ShopRejectReason.InvalidOffer => "Essa oferta não existe mais",
        Game.Core.Combat.Shops.ShopRejectReason.InvalidQuantity => "Quantidade inválida",
        Game.Core.Combat.Shops.ShopRejectReason.NotEnoughGold => "Ouro insuficiente",
        Game.Core.Combat.Shops.ShopRejectReason.InventoryFull => "Sem espaço no inventário",
        Game.Core.Combat.Shops.ShopRejectReason.NoInventory => "Sem inventário",
        Game.Core.Combat.Shops.ShopRejectReason.EmptySlot => "Espaço vazio",
        Game.Core.Combat.Shops.ShopRejectReason.CannotSell => "A loja não compra esse item",
        _ => reason.ToString(),
    };

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
        ItemRejectReason.NotEquippable => "Esse item não se equipa",
        ItemRejectReason.NotConsumable => "Esse item não se usa",
        ItemRejectReason.OnCooldown => "Em recarga",
        ItemRejectReason.Dead => "Você está morto",
        ItemRejectReason.CannotUseNow => "Não dá para usar agora",
        _ => reason.ToString(),
    };
}
