using Game.Core.Combat.Items;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using static Game.Site.Fmt;

namespace Game.Site;

internal sealed partial class SiteBuilder
{
    private void BuildClasses()
    {
        var index = NewPage(Url.Classes, "Classes", "As classes de Valdran: função, atributos, equipamento e habilidades.");
        index.Add("<h1>Classes</h1>");
        index.Add("<div class=\"cards\">");
        foreach (var c in ClassesInOrder)
        {
            var text = _content.Classes.GetValueOrDefault(c.Id);
            index.Add($"<a class=\"card\" href=\"{Esc(index.Href(Url.Class(c.Id)))}\">{Sprite(index, c.Id, c.Name)}<strong>{Esc(c.Name)}</strong><span>{Esc(text?.Role ?? "")}</span></a>");
        }

        index.Add("</div>");
        index.Add(ProgressionSummary(index));

        foreach (var c in ClassesInOrder)
        {
            BuildClass(c);
        }
    }

    private string ProgressionSummary(Page p)
    {
        var prog = _db.Rules.Progression;
        var stats = _db.Rules.Stats;
        var rows = new List<(string, string)>
        {
            ("Nível máximo", Num(prog.MaxLevel)),
            ("Pontos de atributo por nível", Num(prog.AttributePointsPerLevel)),
            ("Pontos de habilidade por nível", Num(prog.SkillPointsPerLevel)),
            ("Nível exigido por NH", string.Join(", ", prog.SkillRankLevels.Select((l, i) => $"NH{i + 1}: {l}"))),
            ("Nível exigido por NH (assinatura)", string.Join(", ", prog.SignatureRankLevels.Select((l, i) => $"NH{i + 1}: {l}"))),
            ("Vida por ponto de VIT", Num(stats.HealthPerVit)),
            ("Mana máxima", $"{Num(stats.ManaBase)} + {Num(stats.ManaPerInt)} × INT"),
            ("Mana por segundo a mais por INT", Num(stats.ManaRegenPerInt)),
            ("Chance de crítico", $"{Pct(stats.CritBase)} + {Pct(stats.CritPerAgi)} por AGI (máximo {Pct(stats.CritMax)}); crítico {Mult(stats.CritMultiplier)}"),
            ("Velocidade do ataque básico", $"recarga ÷ (1 + {Num(stats.AttackSpeedPerAgi)} × AGI), bônus até {Pct(stats.AttackSpeedMax)}"),
            ("Defesa", $"redução = D ÷ (D + {Num(stats.DefenseBase)} + {Num(stats.DefensePerAttackerLevel)} × nível do atacante), até {Pct(stats.DefenseMax)}"),
        };
        return "<h2>Regras de todas as classes</h2>" + Dl(rows)
            + "<p class=\"muted\">FOR, DES, AGI, INT e VIT são os atributos; ARMA é o dano da arma equipada. D é a armadura (dano físico) ou a resistência (outras escolas).</p>";
    }

    private void BuildClass(ClassDefinition c)
    {
        var text = _content.Classes.GetValueOrDefault(c.Id) ?? new ClassText();
        var p = NewPage(Url.Class(c.Id), c.Name, $"{c.Name} em Valdran: {text.Role}".TrimEnd(' ', ':'));
        var rules = _db.Rules;
        p.Add($"<div class=\"head\">{Sprite(p, c.Id, c.Name, 3)}<div><h1>{Esc(c.Name)}</h1><p>{Code(c.Id)}</p><p class=\"lead\">{Esc(text.Summary)}</p></div></div>");
        p.Add(Dl([
            ("Função", Esc(text.Role)),
            ("Forças", Esc(text.Strengths)),
            ("Fraquezas", Esc(text.Weaknesses)),
        ]));

        p.Add("<h2>Atributos e recursos</h2>");
        var s = StatMath.FromClass(c);
        var rows = new List<(string, string)>();
        if (s is not null)
        {
            var a = c.Attributes!;
            rows.Add(("Atributos iniciais", $"FOR {a.Str} · DES {a.Dex} · AGI {a.Agi} · INT {a.Int} · VIT {a.Vit}"));
            rows.Add(("Vida base", Num(c.BaseHealth)));
            rows.Add(("Crescimento da vida", $"+{Num(c.HealthGrowth)} por nível"));
            rows.Add(("Vida no nível 1", $"<span data-k=\"health\">{Num(StatMath.MaxHealth(c, s, rules.Stats))}</span> <span class=\"muted\">(base + VIT × {Num(rules.Stats.HealthPerVit)} + (nível − 1) × {Num(c.HealthGrowth)})</span>"));
        }
        else
        {
            rows.Add(("Vida", $"<span data-k=\"health\">{Num(c.Health)}</span>"));
        }

        rows.Add(("Velocidade", $"{Num(c.Speed)} tiles por segundo"));
        rows.Add(("Raio do corpo", $"{Num(c.Radius)} tiles"));
        if (s is not null && c.Resource.Type == "mana")
        {
            var regen = c.Resource.RegenPerSecond + StatMath.ManaRegenBonus(s, rules.Stats);
            rows.Add(("Mana no nível 1", $"{Num(StatMath.MaxMana(s, rules.Stats))}; regenera {Num(regen)} por segundo <span class=\"muted\">({Num(c.Resource.RegenPerSecond)} da classe + INT × {Num(rules.Stats.ManaRegenPerInt)})</span>"));
            rows.Add(("Chance de crítico no nível 1", Pct(StatMath.CritChance(s, rules.Stats))));
        }
        else
        {
            rows.Add((Resource(c.Resource.Type), $"{Num(c.Resource.Max)}; regenera {Num(c.Resource.RegenPerSecond)} por segundo"));
        }

        p.Add(Dl(rows));

        p.Add("<h2>Equipamento</h2>");
        var equip = new List<(string, string)>();
        foreach (var item in c.StartingEquipment)
        {
            var name = _db.Items.TryGetValue(item.Item, out var def) ? def.Name : item.Item;
            equip.Add(("Equipamento inicial", $"{p.A(Url.Item(item.Item), name)} nível {item.Level}, comum: {ItemStatsAt(item.Item, item.Level)}"));
        }

        var weapons = _db.Items.Values.Where(i => i.Slot == EquipSlot.Weapon && ItemMath.CanUse(rules.Items, i, c.Id)).ToList();
        equip.Add(("Armas que pode usar", weapons.Count == 0 ? "—" : string.Join(", ", weapons.Select(w => p.A(Url.Item(w.Id), w.Name)))));
        var types = Enum.GetValues<ArmorType>()
            .Where(t => rules.Items.ArmorTypes.For(t).Classes is var cl && (cl.Count == 0 || cl.Contains(c.Id)))
            .Select(t => p.A($"{Url.Items}#armor-types", ArmorType(t)));
        equip.Add(("Armaduras que pode usar", string.Join(", ", types)));
        if (c.Consumables.Count > 0)
        {
            equip.Add(("Consumíveis", string.Join(", ", c.Consumables.Select(id => _d.AbilityLink(p, id)))));
        }

        p.Add(Dl(equip));

        p.Add("<h2>Ataque básico</h2>");
        p.Add(AbilityCards(p, [c.BasicAttack]));
        p.Add("<h2>Habilidades</h2>");
        p.Add(AbilityCards(p, c.Abilities));
    }

    /// <summary>Estatísticas de um item comum no nível dado, pelas contas do Core (arredondamento e tipo de armadura).</summary>
    private string ItemStatsAt(string itemId, int level)
    {
        var totals = ItemMath.StatsOf(_catalog, ItemInstance.Common(itemId, level));
        var parts = Enum.GetValues<ItemStat>().Where(st => totals[(int)st] != 0).Select(st => $"{Stat(st)} {StatValue(st, totals[(int)st])}");
        return string.Join(", ", parts);
    }

    /// <summary>Cartões com o resumo do NH1 de cada habilidade.</summary>
    private string AbilityCards(Page p, IEnumerable<string> ids)
    {
        var sb = new System.Text.StringBuilder("<div class=\"abilities\">");
        foreach (var id in ids)
        {
            if (!_db.Abilities.TryGetValue(id, out var a))
            {
                continue;
            }

            var badge = a.Signature ? " <span class=\"badge\">assinatura</span>" : "";
            var ranks = a.HighestRank > 1 ? $" · {a.HighestRank} NH" : "";
            sb.Append($"<article class=\"ability\"><h3>{p.A(Url.Ability(a.Id), a.Name)}{badge}</h3>")
                .Append($"<p class=\"muted\">Custo {Describe.Cost(a)} · recarga {Describe.Cooldown(a)} · {Describe.Targeting(a.Targeting)}{ranks}</p>")
                .Append(_d.Effects(p, a.Effects))
                .Append("</article>");
        }

        return sb.Append("</div>").ToString();
    }
}
