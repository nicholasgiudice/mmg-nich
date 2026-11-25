using Game.Core.Combat;
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
        index.Add(SecondClassCards(index, ClassesInOrder, withBase: true));
        index.Add(ProgressionSummary(index));

        foreach (var c in ClassesInOrder)
        {
            BuildClass(c);
            foreach (var sc in c.SecondClasses)
            {
                BuildSecondClass(c, sc);
            }
        }
    }

    /// <summary>Cartões das segundas classes (T-075), com a função de cada uma; vazio se nenhuma classe tem.</summary>
    private string SecondClassCards(Page p, IEnumerable<ClassDefinition> bases, bool withBase)
    {
        var list = bases.SelectMany(c => c.SecondClasses.Select(sc => (Base: c, Def: sc))).ToList();
        if (list.Count == 0)
        {
            return "";
        }

        var prog = _db.Rules.Progression;
        var sb = new System.Text.StringBuilder(withBase
            ? $"<h2 id=\"segundas-classes\">Segundas classes</h2><p>No nível {Num(prog.SecondClassLevel)}, cada personagem escolhe uma segunda classe entre as duas da sua classe base. A escolha é definitiva.</p>"
            : $"<h2 id=\"segundas-classes\">Segundas classes</h2><p>No nível {Num(prog.SecondClassLevel)}, {Esc(list[0].Base.Name)} escolhe uma destas, de forma definitiva.</p>");
        sb.Append("<div class=\"cards\">");
        foreach (var (b, sc) in list)
        {
            var text = _content.Classes.GetValueOrDefault(sc.Id);
            var from = withBase ? $"<span class=\"muted\">{Esc(b.Name)}</span>" : "";
            sb.Append($"<a class=\"card\" href=\"{Esc(p.Href(Url.Class(sc.Id)))}\">{Sprite(p, sc.Id, sc.Name)}<strong>{Esc(sc.Name)}</strong>{from}<span>{Esc(text?.Role ?? "")}</span></a>");
        }

        return sb.Append("</div>").ToString();
    }

    /// <summary>Página de uma segunda classe (T-075): textos, como e quando se escolhe, níveis por NH e habilidades.</summary>
    private void BuildSecondClass(ClassDefinition baseClass, SecondClassDefinition sc)
    {
        var text = _content.Classes.GetValueOrDefault(sc.Id) ?? new ClassText();
        var p = NewPage(Url.Class(sc.Id), sc.Name, $"{sc.Name}, segunda classe de {baseClass.Name} em Valdran: {text.Role}".TrimEnd(' ', ':'));
        var prog = _db.Rules.Progression;
        p.Add($"<div class=\"head\">{Sprite(p, sc.Id, sc.Name, 3)}<div><h1>{Esc(sc.Name)}</h1>"
            + $"<p><span class=\"badge\">segunda classe</span> de {p.A(Url.Class(baseClass.Id), baseClass.Name)}</p>"
            + $"<p class=\"lead\">{Esc(text.Summary)}</p></div></div>");
        p.Add(Dl([
            ("Função", Esc(text.Role)),
            ("Forças", Esc(text.Strengths)),
            ("Fraquezas", Esc(text.Weaknesses)),
        ]));

        var abilities = sc.Abilities.Where(_db.Abilities.ContainsKey).Select(id => _db.Abilities[id]).ToList();
        var normal = abilities.Where(a => !a.Signature).ToList();
        var signature = abilities.Where(a => a.Signature).ToList();
        var passives = abilities.Count(a => a.Passive);
        var others = baseClass.SecondClasses.Where(o => o.Id != sc.Id).Select(o => p.A(Url.Class(o.Id), o.Name)).ToList();
        string Levels(IReadOnlyList<int> levels) => string.Join(", ", levels.Select((l, i) => $"NH{i + 1}: {l}"));
        var rows = new List<(string, string)>
        {
            ("Classe base", p.A(Url.Class(baseClass.Id), baseClass.Name)),
            ("Escolha", $"no nível {Num(prog.SecondClassLevel)}, definitiva (não dá para trocar)"
                + (others.Count > 0 ? $"; a outra opção de {Esc(baseClass.Name)} é {string.Join(" ou ", others)}" : "")),
            ("Pontos de atributo", $"+{Num(prog.SecondClassAttributePoints)} ao escolher"),
            ("Habilidades", $"{normal.Count} {(normal.Count == 1 ? "normal" : "normais")} com até {normal.Select(a => a.HighestRank).DefaultIfEmpty(0).Max()} NH"
                + (signature.Count > 0 ? $" e {signature.Count} de assinatura com até {signature.Max(a => a.HighestRank)} NH" : "")
                + (passives > 0 ? $"; {passives} {(passives == 1 ? "é passiva" : "são passivas")} (não ocupa tecla nem custa Mana)" : "")
                + "; recebem pontos como as da classe base"),
            ("Nível exigido por NH", Levels(prog.SecondClassRankLevels)),
            ("Nível exigido por NH (assinatura)", Levels(prog.SecondClassSignatureRankLevels)),
        };
        p.Add("<h2>Como funciona</h2>");
        p.Add(Dl(rows));

        p.Add("<h2>Habilidades</h2>");
        p.Add(AbilityCards(p, sc.Abilities));
        p.Add($"<p class=\"muted\">Os números de cada nível de habilidade estão na página de cada uma e no {p.A($"{Url.Abilities}#{sc.Id}", "índice de habilidades")}.</p>");
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
            ("Segunda classe", $"escolhida no nível {Num(prog.SecondClassLevel)}, definitiva; dá {Num(prog.SecondClassAttributePoints)} pontos de atributo a mais"),
            ("Nível exigido por NH (segunda classe)", string.Join(", ", prog.SecondClassRankLevels.Select((l, i) => $"NH{i + 1}: {l}"))),
            ("Nível exigido por NH (assinatura da segunda classe)", string.Join(", ", prog.SecondClassSignatureRankLevels.Select((l, i) => $"NH{i + 1}: {l}"))),
            ("Vida por ponto de VIT", Num(stats.HealthPerVit)),
            ("Mana máxima", $"{Num(stats.ManaBase)} + {Num(stats.ManaPerInt)} × INT"),
            ("Mana por segundo a mais por INT", Num(stats.ManaRegenPerInt)),
            ("Chance de crítico", $"{Pct(stats.CritBase)} + {Pct(stats.CritPerAgi)} por AGI (máximo {Pct(stats.CritMax)}); crítico {Mult(stats.CritMultiplier)}"),
            ("Velocidade do ataque básico", $"recarga ÷ (1 + {Num(stats.AttackSpeedPerAgi)} × AGI), bônus até {Pct(stats.AttackSpeedMax)}"),
            ("Defesa", $"redução = D ÷ (D + {Num(stats.DefenseBase)} + {Num(stats.DefensePerAttackerLevel)} × nível do atacante), até {Pct(stats.DefenseMax)}"),
        };
        rows.Insert(0, ("Atributos", Attributes(p)));
        return "<h2>Regras de todas as classes</h2>" + Dl(rows)
            + "<p class=\"muted\">FOR, DES, AGI, INT e VIT são os atributos; ARMA é o dano da arma equipada. D é a armadura (dano físico) ou a resistência (outras escolas).</p>";
    }

    private void BuildClass(ClassDefinition c)
    {
        var text = _content.Classes.GetValueOrDefault(c.Id) ?? new ClassText();
        var p = NewPage(Url.Class(c.Id), c.Name, $"{c.Name} em Valdran: {text.Role}".TrimEnd(' ', ':'));
        var rules = _db.Rules;
        p.Add($"<div class=\"head\">{Sprite(p, c.Id, c.Name, 3)}<div><h1>{Esc(c.Name)}</h1><p class=\"lead\">{Esc(text.Summary)}</p></div></div>");
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
            rows.Add(("Atributos iniciais", Attributes(p, a)));
            rows.Add(("Vida base", Num(c.BaseHealth)));
            rows.Add(("Crescimento da vida", $"+{Num(c.HealthGrowth)} por nível"));
            rows.Add(("Vida no nível 1", $"<span data-k=\"health\">{Num(StatMath.MaxHealth(c, s, rules.Stats))}</span> <span class=\"muted\">(base + VIT × {Num(rules.Stats.HealthPerVit)} + (nível − 1) × {Num(c.HealthGrowth)})</span>"));
        }
        else
        {
            rows.Add(("Vida", $"<span data-k=\"health\">{Num(c.Health)}</span>"));
        }

        rows.Add(("Velocidade", $"{Num(c.Speed)} tiles por segundo"));
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
            equip.Add(("Equipamento inicial", $"{Icon(p, item.Item)}{p.A(Url.Item(item.Item), name)} nível {item.Level}, comum: {ItemStatsAt(item.Item, item.Level)}"));
        }

        var weapons = _db.Items.Values.Where(i => i.Slot == EquipSlot.Weapon && ItemMath.CanUse(rules.Items, i, c.Id)).ToList();
        equip.Add(("Armas que pode usar", weapons.Count == 0 ? "—" : string.Join(", ", weapons.Select(w => Icon(p, w.Id) + p.A(Url.Item(w.Id), w.Name)))));
        var types = Enum.GetValues<ArmorType>()
            .Where(t => rules.Items.ArmorTypes.For(t).Classes is var cl && (cl.Count == 0 || cl.Contains(c.Id)))
            .Select(t => p.A($"{Url.Items}#armor-types", ArmorType(t)));
        equip.Add(("Armaduras que pode usar", string.Join(", ", types)));
        if (c.StartingInventory.Count > 0)
        {
            equip.Add(("Inventário inicial", string.Join(", ", c.StartingInventory.Select(s =>
                Icon(p, s.Item) + p.A($"{Url.Items}#{s.Item}", _db.Items.TryGetValue(s.Item, out var it) ? it.Name : s.Item) + $" × {s.Count}"))));
        }

        p.Add(Dl(equip));

        p.Add("<h2>Ataque básico</h2>");
        p.Add(AbilityCards(p, [c.BasicAttack]));
        p.Add("<h2>Habilidades</h2>");
        p.Add(AbilityCards(p, c.Abilities));
        p.Add(SecondClassCards(p, [c], withBase: false));
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

            var badge = Badges(a, null, _usage.BasicAttackOf.ContainsKey(a.Id));
            var ranks = a.HighestRank > 1 ? $" · {a.HighestRank} NH" : "";
            sb.Append($"<article class=\"ability\"><h3>{Icon(p, a.Id)}{p.A(Url.Ability(a.Id), a.Name)}{badge}</h3>");
            if (a.Passive)
            {
                // T-403: a passiva não tem custo, recarga nem alvo; mostra o que mantém no NH1.
                sb.Append($"<p class=\"muted\">Passiva, sempre ativa{ranks}</p>")
                    .Append($"<p>{PassiveEffect(p, AbilityRanks.Resolve(a, 1))}</p>");
            }
            else
            {
                var channel = a.Channel is { } ch ? $" · canalizada, {ch.Pulses} pulsos" : "";
                sb.Append($"<p class=\"muted\">Custo {Describe.Cost(a)} · recarga {Describe.Cooldown(a)} · {Esc(Describe.Targeting(a.Targeting))}{channel}{ranks}</p>")
                    .Append(_d.Effects(p, a.Effects));
            }

            sb.Append("</article>");
        }

        return sb.Append("</div>").ToString();
    }
}
