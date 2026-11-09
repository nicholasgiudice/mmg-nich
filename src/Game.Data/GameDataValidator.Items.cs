using Game.Core.Definitions;

namespace Game.Data;

/// <summary>Validação dos itens (T-204): bases, regras de afixos/raridade/queda e equipamento inicial das classes.</summary>
internal static partial class GameDataValidator
{
    private static void ValidateItems(RawContent c, HashSet<string> classes, List<LoadError> errors)
    {
        var items = UniqueIds(c.Items, x => x.Id, "item", errors);
        var abilities = c.Abilities.GroupBy(a => a.Value.Id).ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
        foreach (var e in c.Items)
        {
            ValidateItem(e, classes, abilities, new Ctx(e.File, e.Path, errors));
        }

        ValidateItemRules(c.Rules.Items, classes, items, new Ctx("rules", "rules.items", errors));

        var defs = c.Items.GroupBy(i => i.Value.Id).ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
        foreach (var rank in Enum.GetValues<MonsterRank>())
        {
            var table = c.Rules.Items.Drops.For(rank);
            for (var i = 0; i < table.Items.Count; i++)
            {
                if (defs.TryGetValue(table.Items[i], out var dropped) && dropped.IsConsumable)
                {
                    new Ctx("rules", "rules.items", errors).Error(
                        $"drops.{rank.ToString().ToLowerInvariant()}.items[{i}]", $"'{dropped.Id}' é consumível; a queda é só de equipamento.");
                }
            }
        }
        foreach (var e in c.Classes)
        {
            var ctx = new Ctx(e.File, e.Path, errors);
            var slots = new HashSet<EquipSlot>();
            for (var i = 0; i < e.Value.StartingEquipment.Count; i++)
            {
                var start = e.Value.StartingEquipment[i];
                var field = $"startingEquipment[{i}]";
                if (start.Level < 1) { ctx.Error(field + ".level", "Nível do item deve ser pelo menos 1."); }
                if (!defs.TryGetValue(start.Item, out var item))
                {
                    ctx.Exists(field + ".item", start.Item, items, "item");
                    continue;
                }

                if (item.Slot is not { } slot)
                {
                    ctx.Error(field + ".item", $"'{item.Id}' não é equipamento.");
                    continue;
                }

                if (start.Count != 1) { ctx.Error(field + ".count", "Equipamento inicial tem 1 unidade."); }
                if (!slots.Add(slot)) { ctx.Error(field, $"Dois itens iniciais no espaço {slot}."); }
                if (!Game.Core.Combat.Items.ItemMath.CanUse(c.Rules.Items, item, e.Value.Id))
                {
                    ctx.Error(field + ".item", $"A classe não usa '{item.Id}'.");
                }
            }

            // T-320: inventário inicial (consumíveis empilham; o total tem de caber no inventário).
            var used = 0;
            for (var i = 0; i < e.Value.StartingInventory.Count; i++)
            {
                var start = e.Value.StartingInventory[i];
                var field = $"startingInventory[{i}]";
                if (start.Level < 1) { ctx.Error(field + ".level", "Nível do item deve ser pelo menos 1."); }
                if (start.Count < 1) { ctx.Error(field + ".count", "Deve ser pelo menos 1."); }
                if (!defs.TryGetValue(start.Item, out var item))
                {
                    ctx.Exists(field + ".item", start.Item, items, "item");
                    continue;
                }

                used += (int)Math.Ceiling(Math.Max(1, start.Count) / (double)item.MaxStack);
            }

            if (used > c.Rules.Items.InventorySize)
            {
                ctx.Error("startingInventory", $"Ocupa {used} espaços; o inventário tem {c.Rules.Items.InventorySize}.");
            }
        }
    }

    private static void ValidateItem(Entry<ItemBaseDefinition> e, HashSet<string> classes, Dictionary<string, AbilityDefinition> abilities, Ctx ctx)
    {
        var x = e.Value;
        if (string.IsNullOrWhiteSpace(x.Name)) { ctx.Error("name", "Nome vazio."); }
        if (x.Price < 0) { ctx.Error("price", "Não pode ser negativo."); }
        if (x.Stack < 1) { ctx.Error("stack", "Deve ser pelo menos 1."); }
        if (x.IsConsumable)
        {
            ValidateConsumable(x, abilities, ctx);
            return;
        }

        if (x.Slot is null) { ctx.Error("slot", "Obrigatório em equipamento."); }
        if (x.Stack != 1) { ctx.Error("stack", "Só consumível empilha."); }
        if (x.UseAbility is not null) { ctx.Error("useAbility", "Só em consumível."); }
        for (var i = 0; i < x.Classes.Count; i++)
        {
            ctx.Exists($"classes[{i}]", x.Classes[i], classes, "classe");
        }

        if (ItemBaseDefinition.IsArmorSlot(x.Slot) != x.ArmorType.HasValue)
        {
            ctx.Error("armorType", "Obrigatório em cabeça, peito, mãos e pés; proibido em arma e acessório.");
        }

        var seen = new HashSet<ItemStat>();
        for (var i = 0; i < x.Stats.Count; i++)
        {
            var s = x.Stats[i];
            if (!seen.Add(s.Stat)) { ctx.Error($"stats[{i}].stat", $"Estatística {s.Stat} repetida."); }
            ctx.NonNegative($"stats[{i}].base", s.Base);
            ctx.NonNegative($"stats[{i}].perLevel", s.PerLevel);
        }
    }

    /// <summary>T-320: consumível sem espaço, sem estatísticas, com uma habilidade de item instantânea.</summary>
    private static void ValidateConsumable(ItemBaseDefinition x, Dictionary<string, AbilityDefinition> abilities, Ctx ctx)
    {
        if (x.Slot is not null) { ctx.Error("slot", "Consumível não tem espaço de equipamento."); }
        if (x.ArmorType is not null) { ctx.Error("armorType", "Consumível não tem tipo de armadura."); }
        if (x.Stats.Count > 0) { ctx.Error("stats", "Consumível não dá estatísticas."); }
        if (x.Classes.Count > 0) { ctx.Error("classes", "Consumível serve para todas as classes."); }
        if (x.UseAbility is not { } id)
        {
            ctx.Error("useAbility", "Obrigatório em consumível.");
            return;
        }

        if (!abilities.TryGetValue(id, out var ability))
        {
            ctx.Error("useAbility", $"Habilidade '{id}' não existe.");
            return;
        }

        if (ability.CastTimeTicks > 0 || ability.Ranks.Count > 0 || ability.Telegraph is not null || ability.CooldownCharges > 1)
        {
            ctx.Error("useAbility", $"A habilidade '{id}' de item deve ser instantânea, sem NH, telegráfo nem cargas por recarga.");
        }

        if (ability.Targeting is not SelfTargeting and not CircleSelfTargeting)
        {
            ctx.Error("useAbility", $"A habilidade '{id}' de item deve ter alvo em si (self ou circle_self).");
        }
    }

    private static void ValidateItemRules(ItemRules r, HashSet<string> classes, HashSet<string> items, Ctx c)
    {
        if (r.InventorySize < 1) { c.Error("inventorySize", "Deve ser pelo menos 1."); }
        c.Positive("pickupRange", r.PickupRange);
        c.PositiveTicks("groundDuration", r.GroundDurationTicks);

        foreach (var type in Enum.GetValues<ArmorType>())
        {
            var t = r.ArmorTypes.For(type);
            var name = "armorTypes." + type.ToString().ToLowerInvariant();
            c.NonNegative(name + ".armorMultiplier", t.ArmorMultiplier);
            c.NonNegative(name + ".resistanceFromArmor", t.ResistanceFromArmor);
            for (var i = 0; i < t.Classes.Count; i++)
            {
                c.Exists($"{name}.classes[{i}]", t.Classes[i], classes, "classe");
            }
        }

        foreach (var rarity in Enum.GetValues<ItemRarity>())
        {
            var n = r.AffixCount.For(rarity);
            if (n < 0 || n != Math.Floor(n)) { c.Error("affixCount." + rarity.ToString().ToLowerInvariant(), "Deve ser um inteiro maior ou igual a 0."); }
        }

        var affixIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < r.Affixes.Count; i++)
        {
            var a = r.Affixes[i];
            if (!IdPattern().IsMatch(a.Id)) { c.Error($"affixes[{i}].id", $"Id '{a.Id}' inválido."); }
            if (!affixIds.Add(a.Id)) { c.Error($"affixes[{i}].id", $"Afixo duplicado '{a.Id}'."); }
            c.NonNegative($"affixes[{i}].value.base", a.Value.Base);
            c.NonNegative($"affixes[{i}].value.perLevel", a.Value.PerLevel);
        }

        foreach (var rank in Enum.GetValues<MonsterRank>())
        {
            var d = r.Drops.For(rank);
            var name = "drops." + rank.ToString().ToLowerInvariant();
            if (d.Chance is < 0 or > 1) { c.Error(name + ".chance", "Deve estar em 0..1."); }
            if (d.Count < 1) { c.Error(name + ".count", "Deve ser pelo menos 1."); }
            var total = 0.0;
            foreach (var rarity in Enum.GetValues<ItemRarity>())
            {
                var w = d.Rarity.For(rarity);
                c.NonNegative($"{name}.rarity.{rarity.ToString().ToLowerInvariant()}", w);
                total += Math.Max(0, w);
            }

            if (d.Chance > 0 && total <= 0) { c.Error(name + ".rarity", "Algum peso de raridade precisa ser maior que 0."); }
            if (d.Chance > 0 && d.Items.Count == 0) { c.Error(name + ".items", "Tabela com chance de queda precisa de itens."); }
            for (var i = 0; i < d.Items.Count; i++)
            {
                c.Exists($"{name}.items[{i}]", d.Items[i], items, "item");
            }
        }
    }
}
