using Game.Core.Combat.Items;
using Game.Core.Definitions;
using static Game.Site.Fmt;

namespace Game.Site;

internal sealed partial class SiteBuilder
{
    /// <summary>Níveis de item mostrados nas tabelas: 1, a cada 5 e o nível máximo do personagem.</summary>
    private List<int> ItemLevels()
    {
        var max = Math.Max(1, _db.Rules.Progression.MaxLevel);
        return new[] { 1 }.Concat(Enumerable.Range(1, max / 5).Select(i => i * 5)).Append(max).Distinct().Where(l => l <= max).ToList();
    }

    private void BuildItems()
    {
        var rules = _db.Rules.Items;
        var p = NewPage(Url.Items, "Itens", "Armas, armaduras, acessórios, consumíveis, raridades, afixos e quedas de Valdran.");
        p.Add("<h1>Itens</h1>");
        p.Add("<p>Valor de um atributo = base + por nível × nível do item. O item que cai de um monstro tem o nível do monstro. "
            + "Valores inteiros são arredondados (meio para cima); chance de crítico e Mana por segundo ficam com fração.</p>");
        p.Add(Dl([
            ("Espaços no inventário", Num(rules.InventorySize)),
            ("Distância para pegar do chão", $"{Num(rules.PickupRange)} tiles"),
            ("Tempo do item no chão", Seconds(rules.GroundDurationTicks)),
        ]));

        var levels = ItemLevels();
        p.Add("<h2 id=\"weapons\">Armas</h2>");
        p.Add(ItemTable(p, _db.Items.Values.Where(i => i.Slot == EquipSlot.Weapon), levels, showType: false));

        p.Add("<h2 id=\"armor-types\">Tipos de armadura</h2>");
        p.Add("<p>A armadura do item é (base + por nível × nível) × multiplicador do tipo, arredondada. Um tipo com resistência pela armadura também dá essa fração da armadura como resistência.</p>");
        p.Add(Table(["Tipo", "Quem usa", "Multiplicador de armadura", "Resistência pela armadura"],
            Enum.GetValues<ArmorType>().Select(t =>
            {
                var def = rules.ArmorTypes.For(t);
                return new[]
                {
                    ArmorType(t),
                    def.Classes.Count == 0 ? "todas as classes" : string.Join(", ", def.Classes.Select(c => _d.ClassLink(p, c))),
                    Mult(def.ArmorMultiplier),
                    def.ResistanceFromArmor == 0 ? "—" : Pct(def.ResistanceFromArmor),
                };
            })));

        p.Add("<h2 id=\"armor\">Armaduras</h2>");
        p.Add(ItemTable(p, _db.Items.Values.Where(i => ItemBaseDefinition.IsArmorSlot(i.Slot)), levels, showType: true));
        p.Add("<h2 id=\"accessories\">Acessórios</h2>");
        p.Add(ItemTable(p, _db.Items.Values.Where(i => i.Slot == EquipSlot.Accessory), levels, showType: false));

        p.Add("<h2 id=\"consumables\">Consumíveis</h2>");
        p.Add("<p>Consumíveis empilham no inventário. Usar gasta 1 unidade e executa a habilidade do item; pilhas do mesmo item dividem a recarga.</p>");
        var consumables = _db.Items.Values.Where(i => i.IsConsumable && i.UseAbility is not null && _db.Abilities.ContainsKey(i.UseAbility)).ToList();
        p.Add(Table(["Item", "Efeito", "Recarga", "Pilha", "Preço base", "Inventário inicial"],
            consumables.Select(i =>
            {
                var a = _db.Abilities[i.UseAbility!];
                var start = _db.Classes.Values.Where(c => c.StartingInventory.Any(s => s.Item == i.Id)).ToList();
                return new[]
                {
                    $"<span id=\"{Esc(i.Id)}\">{Icon(p, i.Id)}{p.A(Url.Ability(a.Id), i.Name)}</span>",
                    _d.Effects(p, a.Effects),
                    Describe.Cooldown(a),
                    Num(i.MaxStack),
                    Num(i.Price),
                    start.Count == 0 ? "—" : string.Join(", ", start.Select(c => $"{_d.ClassLink(p, c.Id)} × {c.StartingInventory.First(s => s.Item == i.Id).Count}")),
                };
            })));

        p.Add("<h2 id=\"rarity\">Raridade e afixos</h2>");
        p.Add(Table(["Raridade", "Afixos"], Enum.GetValues<ItemRarity>().Select(r => new[] { Fmt.Rarity(r), Num(rules.AffixCount.For(r)) })));
        p.Add("<p>Os afixos são sorteados sem repetir no mesmo item. Valor = base + por nível × nível do item.</p>");
        p.Add(Table(["Afixo", "Id", "Atributo", "Base", "Por nível"],
            rules.Affixes.Select(a => new[]
            {
                $"<span id=\"{Esc(a.Id)}\">{Esc(a.Name)}</span>",
                Code(a.Id),
                Stat(a.Value.Stat),
                StatValue(a.Value.Stat, a.Value.Base),
                StatValue(a.Value.Stat, a.Value.PerLevel),
            })));

        p.Add("<h2 id=\"obtain\">Como se obtém</h2>");
        p.Add("<h3>Equipamento inicial</h3>");
        p.Add(Table(["Classe", "Item", "Nível", "Atributos"],
            _db.Classes.Values.SelectMany(c => c.StartingEquipment.Select(s => new[]
            {
                _d.ClassLink(p, c.Id),
                _db.Items.TryGetValue(s.Item, out var def) ? Icon(p, s.Item) + p.A(Url.Item(s.Item), def.Name) : Code(s.Item),
                Num(s.Level),
                ItemStatsAt(s.Item, s.Level),
            }))));

        p.Add($"<h3 id=\"drops\">{Icon(p, "ui.ground_item")}Quedas de monstros</h3>");
        p.Add("<p>A tabela de queda é escolhida pela categoria do monstro. Cada item sorteia a raridade pelos pesos e a base entre os itens da tabela, com o mesmo peso. Os itens caem para quem participou da morte.</p>");
        p.Add(Table(["Categoria", "Monstros", "Chance", "Itens", "Peso de raridade (comum / incomum / raro / épico)", "Garantia", "Itens base"],
            Enum.GetValues<MonsterRank>().Select(r =>
            {
                var t = rules.Drops.For(r);
                return new[]
                {
                    Fmt.Rank(r),
                    string.Join(", ", _db.Monsters.Values.Where(m => m.Rank == r).Select(m => _d.MonsterLink(p, m.Id))),
                    Pct(t.Chance),
                    Num(t.Count),
                    string.Join(" / ", Enum.GetValues<ItemRarity>().Select(x => Num(t.Rarity.For(x)))),
                    t.GuaranteedRarity == ItemRarity.Common ? "—" : $"o primeiro pelo menos {Fmt.Rarity(t.GuaranteedRarity)}",
                    Num(t.Items.Count),
                };
            })));
    }

    private string ItemTable(Page p, IEnumerable<ItemBaseDefinition> items, List<int> levels, bool showType)
    {
        var rules = _db.Rules.Items;
        var head = new List<string> { "Item", "Espaço" };
        if (showType)
        {
            head.Add("Tipo");
        }

        head.AddRange(["Quem usa", "Atributos (base + por nível × nível)"]);
        head.AddRange(levels.Select(l => $"Nv. {l}"));
        var sb = new System.Text.StringBuilder("<div class=\"table-wrap\"><table class=\"items\"><thead><tr>");
        sb.Append(string.Concat(head.Select(h => $"<th>{h}</th>"))).Append("</tr></thead><tbody>");
        foreach (var i in items)
        {
            var classes = ItemMath.ClassesOf(rules, i);
            sb.Append($"<tr id=\"{Esc(i.Id)}\" data-item=\"{Esc(i.Id)}\"><td><span class=\"named\">{Icon(p, i.Id)}<span>{Esc(i.Name)}<br>{Code(i.Id)}</span></span></td><td>{(i.Slot is { } slot ? Slot(slot) : "—")}</td>");
            if (showType)
            {
                sb.Append($"<td>{(i.ArmorType is { } t ? p.A($"{Url.Items}#armor-types", ArmorType(t)) : "—")}</td>");
            }

            sb.Append($"<td>{(classes.Count == 0 ? "todas" : string.Join(", ", classes.Select(c => _d.ClassLink(p, c))))}</td>");
            var formulas = i.Stats.Select(v =>
                $"<span data-stat=\"{Esc(v.Stat.ToString())}\">{Stat(v.Stat)} <span data-k=\"base\">{StatValue(v.Stat, v.Base)}</span> + <span data-k=\"perLevel\">{StatValue(v.Stat, v.PerLevel)}</span> × nível</span>");
            sb.Append($"<td>{string.Join("<br>", formulas)}</td>");
            foreach (var l in levels)
            {
                sb.Append($"<td data-level=\"{l}\">{ItemStatsAt(i.Id, l)}</td>");
            }

            sb.Append("</tr>");
        }

        return sb.Append("</tbody></table></div>").ToString();
    }
}
