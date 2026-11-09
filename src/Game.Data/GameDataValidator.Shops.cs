using Game.Core.Definitions;

namespace Game.Data;

/// <summary>Validação das lojas (T-321): ofertas apontam para itens que existem e regras de compra e venda.</summary>
internal static partial class GameDataValidator
{
    private static void ValidateShops(RawContent c, List<LoadError> errors)
    {
        UniqueIds(c.Shops, x => x.Id, "loja", errors);
        var items = c.Items.GroupBy(i => i.Value.Id).ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
        foreach (var e in c.Shops)
        {
            var ctx = new Ctx(e.File, e.Path, errors);
            if (string.IsNullOrWhiteSpace(e.Value.Name)) { ctx.Error("name", "Nome vazio."); }
            var seen = new HashSet<(string, int)>();
            for (var i = 0; i < e.Value.Offers.Count; i++)
            {
                var o = e.Value.Offers[i];
                var field = $"offers[{i}]";
                if (!items.TryGetValue(o.Item, out var item))
                {
                    ctx.Error(field + ".item", $"Referência inexistente: item '{o.Item}'.");
                    continue;
                }

                if (o.Level < 1) { ctx.Error(field + ".level", "Nível do item deve ser pelo menos 1."); }
                if (item.IsConsumable && o.Level != 1) { ctx.Error(field + ".level", "Consumível não tem nível (use 1 ou omita)."); }
                if (o.Price is < 0) { ctx.Error(field + ".price", "Não pode ser negativo."); }
                if (!seen.Add((o.Item, o.Level))) { ctx.Error(field, $"Oferta repetida: '{o.Item}' nível {o.Level}."); }
            }
        }

        var r = c.Rules.Shops;
        var rc = new Ctx("rules", "rules.shops", errors);
        rc.Positive("interactRange", r.InteractRange);
        if (r.SellRatio is < 0 or > 1) { rc.Error("sellRatio", "Deve estar em 0..1."); }
        rc.NonNegative("equipmentValueBase", r.EquipmentValueBase);
        rc.NonNegative("equipmentValuePerLevel", r.EquipmentValuePerLevel);
        foreach (var rarity in Enum.GetValues<ItemRarity>())
        {
            rc.NonNegative("equipmentRarityMultiplier." + rarity.ToString().ToLowerInvariant(), r.EquipmentRarityMultiplier.For(rarity));
        }

        if (r.MaxQuantity < 1) { rc.Error("maxQuantity", "Deve ser pelo menos 1."); }
    }
}
