using System.Text;
using Game.Core.Combat;
using Game.Core.Definitions;
using static Game.Site.Fmt;

namespace Game.Site;

internal sealed partial class SiteBuilder
{
    private void BuildAbilities()
    {
        var index = NewPage(Url.Abilities, "Habilidades", "Habilidades das classes e dos monstros de Valdran, com os números de cada nível de habilidade.");
        index.Add("<h1>Habilidades</h1>");
        index.Add("<p>Valor = base + coeficiente × atributo (FOR, DES, INT, VIT; ARMA = dano da arma equipada). Tempos em segundos, distâncias em tiles. "
            + "Cada nível de habilidade (NH) herda o que não muda do anterior; as tabelas já mostram o valor efetivo de cada NH.</p>");

        foreach (var c in ClassesInOrder)
        {
            index.Add($"<h2 id=\"{Esc(c.Id)}\">{index.A(Url.Class(c.Id), c.Name)}</h2>");
            index.Add(AbilityList(index, new[] { c.BasicAttack }.Concat(c.Abilities), c.BasicAttack));
        }

        var consumables = _db.Items.Values.Where(i => i.UseAbility is not null).Select(i => i.UseAbility!).Distinct().ToList();
        if (consumables.Count > 0)
        {
            index.Add("<h2 id=\"consumables\">Consumíveis</h2>");
            index.Add(AbilityList(index, consumables, null));
        }

        index.Add("<h2 id=\"monsters\">Monstros</h2>");
        index.Add("<p class=\"muted\">Valores do nível 1; dano, cura e escudo crescem com o nível do monstro.</p>");
        foreach (var m in _db.Monsters.Values)
        {
            index.Add($"<h3>{index.A(Url.Monster(m.Id), m.Name)}</h3>");
            index.Add(AbilityList(index, _usage.AbilityMonsters.Where(kv => kv.Value.Contains(m.Id)).Select(kv => kv.Key), null));
        }

        foreach (var a in _db.Abilities.Values)
        {
            BuildAbility(a);
        }
    }

    private string AbilityList(Page p, IEnumerable<string> ids, string? basic)
    {
        var rows = ids.Where(_db.Abilities.ContainsKey).Select(id => _db.Abilities[id]).Select(a => new[]
        {
            Icon(p, a.Id) + p.A(Url.Ability(a.Id), a.Name) + (a.Id == basic ? " <span class=\"badge\">ataque básico</span>" : "") + (a.Signature ? " <span class=\"badge\">assinatura</span>" : ""),
            Describe.Cost(a),
            Describe.Cooldown(a),
            Esc(Describe.Targeting(a.Targeting)),
            a.HighestRank > 1 ? $"1–{a.HighestRank}" : "—",
        });
        return Table(["Habilidade", "Custo", "Recarga", "Alvo", "NH"], rows);
    }

    /// <summary>Uma coluna da tabela por NH: chave (para testes), título e valor no NH resolvido.</summary>
    private sealed record Column(string Key, string Title, Func<AbilityDefinition, int, string> Value, bool AlwaysInTable = false);

    private void BuildAbility(AbilityDefinition a)
    {
        var used = _usage.IsUsed(a.Id);
        var p = NewPage(Url.Ability(a.Id), a.Name, $"{a.Name}: custo, recarga, alvo e efeitos por nível de habilidade.");
        p.Add($"<div class=\"head\">{Icon(p, a.Id, 2)}<div><h1>{Esc(a.Name)}</h1><p>{Code(a.Id)}{(a.Signature ? " <span class=\"badge\">assinatura</span>" : "")}</p></div></div>");
        if (!used)
        {
            p.Add($"<p class=\"note\">Esta habilidade existe nos dados, mas <strong>nenhuma classe nem monstro a usa hoje</strong>. Não faz parte do jogo atual ({p.A(Url.Open, "em aberto")}).</p>");
        }

        var users = new List<string>();
        if (_usage.AbilityClasses.TryGetValue(a.Id, out var classes))
        {
            users.AddRange(classes.Select(id => _d.ClassLink(p, id) + (_db.Classes[id].BasicAttack == a.Id ? " (ataque básico)" : "")));
        }

        var isMonster = _usage.AbilityMonsters.TryGetValue(a.Id, out var monsters);
        if (isMonster)
        {
            users.AddRange(monsters!.Select(id => _d.MonsterLink(p, id)));
        }

        var head = new List<(string, string)>();
        if (users.Count > 0)
        {
            head.Add(("Usada por", string.Join(", ", users)));
        }

        if (a.Tags.Count > 0)
        {
            head.Add(("Tags", string.Join(" ", a.Tags.Select(t => Code(t)))));
        }

        var items = _db.Items.Values.Where(i => i.UseAbility == a.Id).ToList();
        if (items.Count > 0)
        {
            head.Add(("Usada pelo item", string.Join(", ", items.Select(i => Icon(p, i.Id) + p.A($"{Url.Items}#{i.Id}", i.Name)))));
        }

        p.Add(Dl(head));
        if (isMonster)
        {
            p.Add("<p class=\"muted\">Valores do nível 1 do monstro; dano, cura e escudo crescem com o nível (veja a página do monstro).</p>");
        }

        var resolved = Enumerable.Range(1, a.HighestRank).Select(r => AbilityRanks.Resolve(a, r)).ToList();
        var prog = _db.Rules.Progression;
        var columns = new List<Column>
        {
            new("cost", "Custo", (x, _) => Describe.Cost(x), AlwaysInTable: true),
            new("cooldown", "Recarga", (x, _) => Describe.Cooldown(x), AlwaysInTable: true),
            new("cast", "Conjuração", (x, _) => Describe.Cast(x)),
            new("targeting", "Alvo", (x, _) => Esc(Describe.Targeting(x.Targeting))),
            new("telegraph", "Aviso", (x, _) => Esc(Describe.Telegraph(x.Telegraph))),
            new("hits", "Acertos", (x, _) => Describe.Hits(x)),
            new("effects", "Efeitos", (x, _) => _d.Effects(p, x.Effects), AlwaysInTable: true),
        };
        if (a.HighestRank > 1 && classes is not null)
        {
            columns.Insert(0, new("level", "Nível exigido", (_, r) => Num(prog.RequiredLevel(a.Signature, r)), AlwaysInTable: true));
        }

        if (a.HighestRank == 1)
        {
            p.Add(Dl(columns.Select(c => (c.Title, $"<span data-k=\"{c.Key}\">{c.Value(resolved[0], 1)}</span>"))));
            return;
        }

        var values = columns.ToDictionary(c => c.Key, c => resolved.Select((x, i) => c.Value(x, i + 1)).ToList());
        var constant = columns.Where(c => !c.AlwaysInTable && values[c.Key].Distinct().Count() == 1).ToList();
        var varying = columns.Except(constant).ToList();
        if (constant.Count > 0)
        {
            p.Add("<h2>Em todos os NH</h2>");
            p.Add(Dl(constant.Select(c => (c.Title, $"<span data-k=\"{c.Key}\">{values[c.Key][0]}</span>"))));
        }

        p.Add("<h2>Por nível de habilidade</h2>");
        p.Add("<p class=\"muted\">Destacado: o que mudou em relação ao NH anterior.</p>");
        var sb = new StringBuilder("<div class=\"table-wrap\"><table class=\"ranks\"><thead><tr><th>NH</th>");
        sb.Append(string.Concat(varying.Select(c => $"<th>{c.Title}</th>"))).Append("</tr></thead><tbody>");
        for (var i = 0; i < resolved.Count; i++)
        {
            sb.Append($"<tr data-rank=\"{i + 1}\"><th>{i + 1}</th>");
            foreach (var c in varying)
            {
                var changed = i > 0 && values[c.Key][i] != values[c.Key][i - 1] && c.Key != "level";
                sb.Append($"<td data-k=\"{c.Key}\"{(changed ? " class=\"chg\"" : "")}>{values[c.Key][i]}</td>");
            }

            sb.Append("</tr>");
        }

        p.Add(sb.Append("</tbody></table></div>").ToString());
    }
}
