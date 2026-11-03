using Game.Core.Definitions;
using static Game.Site.Fmt;

namespace Game.Site;

internal sealed partial class SiteBuilder
{
    /// <summary>Um lugar onde o monstro aparece no mundo aberto.</summary>
    private sealed record Spot(ZoneDefinition Zone, MonsterSpawnDefinition Spawn, int Level);

    private IEnumerable<Spot> SpotsOf(string monsterId) =>
        _db.Zones.Values.SelectMany(z => z.MonsterSpawns.Where(s => s.Monster == monsterId)
            .Select(s => new Spot(z, s, s.Level ?? _db.Monsters[monsterId].Level)));

    private void BuildMonsters()
    {
        var p = NewPage(Url.Monsters, "Monstros", "Bestiário de Valdran: nível, vida, habilidades, onde aparece e o que deixa cair.");
        p.Add("<h1>Monstros</h1>");
        p.Add(Table(
            ["Monstro", "Nível", "Categoria", "Vida (nv. 1)", "Velocidade", "Habilidades"],
            _db.Monsters.Values.Select(m => new[]
            {
                $"<span class=\"mini\">{Sprite(p, m.Id, m.Name, 1)}</span>{p.A(Url.Monster(m.Id), m.Name)}",
                Num(m.Level),
                Fmt.Rank(m.Rank),
                Num(m.Health),
                Num(m.Speed),
                string.Join(", ", m.Abilities.Select(id => _d.AbilityLink(p, id))),
            })));
        p.Add(ScalingRules());

        p.Add("<h2>Onde aparecem</h2>");
        foreach (var z in _db.Zones.Values)
        {
            p.Add($"<h3 id=\"{Esc(z.Id)}\">{Esc(z.Name)}</h3>");
            var info = z.Kind == ZoneKind.Safe ? "Zona segura, sem monstros." : $"Corpos ficam {Seconds(z.CorpseTicks)} no chão.";
            var gates = z.Gates.Where(g => _db.Zones.ContainsKey(g.To)).Select(g => p.A(Url.Zone(g.To), _db.Zones[g.To].Name)).ToList();
            p.Add($"<p>{Code(z.Id)} · {info}{(gates.Count > 0 ? " Portões para: " + string.Join(", ", gates) + "." : "")}</p>");
            if (z.MonsterSpawns.Count > 0)
            {
                p.Add(Table(["Subárea", "Monstro", "Quantidade", "Nível", "Renasce em"],
                    z.MonsterSpawns.Select(s => new[]
                    {
                        Esc(s.Area ?? "—"),
                        _d.MonsterLink(p, s.Monster),
                        Num(s.Count),
                        Num(s.Level ?? _db.Monsters[s.Monster].Level),
                        Seconds(s.RespawnTicks),
                    })));
            }
        }

        if (_db.Encounters.Count > 0)
        {
            p.Add("<h2>Encontros de teste</h2>");
            p.Add("<p class=\"muted\">Arenas fechadas do protótipo, usadas em testes e no simulador; não fazem parte do mundo aberto.</p>");
            p.Add(Table(["Encontro", "Monstros", "Jogadores", "Nível dos monstros", "Limite"],
                _db.Encounters.Values.Select(e => new[]
                {
                    $"<span id=\"{Esc(e.Id)}\">{Esc(e.Name)}</span> {Code(e.Id)}",
                    string.Join(", ", e.Enemies.GroupBy(x => x.Monster).Select(g => $"{g.Count()} × {_d.MonsterLink(p, g.Key)}")),
                    string.Join(", ", e.Players.Select(x => _d.ClassLink(p, x.Class))),
                    e.MonsterLevel is { } l ? Num(l) : "padrão",
                    e.Defeat.TimeLimitTicks is { } t ? Seconds(t) : "—",
                })));
        }

        foreach (var m in _db.Monsters.Values)
        {
            BuildMonster(m);
        }
    }

    private string ScalingRules()
    {
        var s = _db.Rules.Monsters;
        string Growth(MonsterRank rank)
        {
            var h = s.HealthFactor(2, rank) - 1;
            var pw = s.PowerFactor(2, rank) - 1;
            return $"vida × (1 + {Num(h)} × (nível − 1)); potência × (1 + {Num(pw)} × (nível − 1))";
        }

        var rows = Enum.GetValues<MonsterRank>().Select(r => new[] { Fmt.Rank(r), Growth(r), ControlByRank(r) });
        return "<h2>Nível e categoria</h2><p>Os números dos monstros valem para o nível 1. No nível em que o monstro nasce, a vida e a potência "
            + "(dano, cura e escudo das habilidades, áreas e status dele) crescem assim:</p>"
            + Table(["Categoria", "Crescimento", "Duração do controle recebido"], rows);
    }

    private string ControlByRank(MonsterRank rank) =>
        _db.Rules.Control is { } c ? Mult(c.DurationByRank.For(rank)) : "× 1";

    private void BuildMonster(MonsterDefinition m)
    {
        var desc = _content.Monsters.GetValueOrDefault(m.Id) ?? "";
        var p = NewPage(Url.Monster(m.Id), m.Name, $"{m.Name}: nível {m.Level}, {Fmt.Rank(m.Rank)}. {desc}".Trim());
        var scaling = _db.Rules.Monsters;
        p.Add($"<div class=\"head\">{Sprite(p, m.Id, m.Name, m.Rank == MonsterRank.Boss ? 3 : 2)}<div><h1>{Esc(m.Name)}</h1><p>{Code(m.Id)}</p><p class=\"lead\">{Esc(desc)}</p></div></div>");
        var rows = new List<(string, string)>
        {
            ("Nível padrão", $"<span data-k=\"level\">{Num(m.Level)}</span>"),
            ("Categoria", Fmt.Rank(m.Rank)),
            ("Vida (nível 1)", $"<span data-k=\"health\">{Num(m.Health)}</span>"),
            ("Velocidade", $"{Num(m.Speed)} tiles por segundo"),
            ("Raio do corpo", $"{Num(m.Radius)} tiles"),
        };
        if (m.Tags.Count > 0)
        {
            rows.Add(("Tags", string.Join(" ", m.Tags.Select(t => Code(t)))));
        }

        if (m.Armor != 0 || m.Resistance != 0)
        {
            rows.Add(("Defesa", $"armadura {Num(m.Armor)}, resistência {Num(m.Resistance)}"));
        }

        if (m.Resistances.Count > 0)
        {
            rows.Add(("Resistências", string.Join(", ", m.Resistances.Select(kv => $"{Esc(kv.Key)} {Pct(kv.Value)}"))));
        }

        rows.Add(("Controle recebido", $"duração {ControlByRank(m.Rank)}"));
        var xp = _db.Rules.Progression.MonsterXp(m.Level, m.Rank, m.Level);
        rows.Add(("XP ao matar", $"{Num(xp)} no nível padrão, para um personagem do mesmo nível, sozinho"));
        p.Add(Dl(rows));

        // IA
        var ai = _content.Ai.GetValueOrDefault(m.Ai.Id);
        p.Add("<h2>Comportamento</h2>");
        p.Add($"<p>{Code(m.Ai.Id)}{(ai is null ? "" : " — " + Esc(ai))}</p>");
        if (m.Ai.Parameters.Count > 0)
        {
            p.Add("<p class=\"muted\">Parâmetros: " + string.Join(", ", m.Ai.Parameters.Select(kv => $"{Code(kv.Key)} {Num(kv.Value)}")) + "</p>");
        }

        // Habilidades
        p.Add("<h2>Habilidades</h2>");
        p.Add("<p class=\"muted\">Valores do nível 1.</p>");
        var abilityIds = m.Abilities.Concat(m.Phases.SelectMany(ph => ph.Abilities ?? [])).Distinct();
        p.Add(Table(["Habilidade", "Recarga", "Alvo", "Efeitos"],
            abilityIds.Where(_db.Abilities.ContainsKey).Select(id => _db.Abilities[id]).Select(a => new[]
            {
                p.A(Url.Ability(a.Id), a.Name),
                Describe.Cooldown(a),
                Esc(Describe.Targeting(a.Targeting)) + (a.Telegraph is null ? "" : "; " + Esc(Describe.Telegraph(a.Telegraph))),
                _d.Effects(p, a.Effects),
            })));

        if (m.Phases.Count > 0)
        {
            p.Add("<h2>Fases</h2>");
            p.Add("<p class=\"muted\">A fase começa quando a vida cai até o percentual.</p>");
            p.Add(Table(["Fase", "Vida", "Ao entrar", "Habilidades"],
                m.Phases.Select(ph => new[]
                {
                    $"{Esc(_content.Phases.GetValueOrDefault(ph.Id) ?? ph.Id)} {Code(ph.Id)}",
                    Num(ph.HealthPercent) + "%",
                    (ph.OnEnter.Count == 0 ? "—" : _d.Effects(p, ph.OnEnter)) + (ph.NoSummons ? "<p class=\"muted\">sem invocações nesta fase</p>" : ""),
                    ph.Abilities is null ? "todas" : string.Join(", ", ph.Abilities.Select(id => _d.AbilityLink(p, id))),
                })));
        }

        // Onde aparece
        p.Add("<h2>Onde aparece</h2>");
        var spots = SpotsOf(m.Id).ToList();
        var summoners = _usage.SummonSources.GetValueOrDefault(m.Id) ?? [];
        if (spots.Count > 0)
        {
            p.Add(Table(["Zona", "Subárea", "Quantidade", "Nível", "Renasce em", "Vida no nível", "Potência"],
                spots.Select(s => new[]
                {
                    p.A(Url.Zone(s.Zone.Id), s.Zone.Name),
                    Esc(s.Spawn.Area ?? "—"),
                    Num(s.Spawn.Count),
                    Num(s.Level),
                    Seconds(s.Spawn.RespawnTicks),
                    Num(m.Health * scaling.HealthFactor(s.Level, m.Rank)),
                    Mult(scaling.PowerFactor(s.Level, m.Rank)),
                })));
        }

        foreach (var src in summoners)
        {
            var who = _db.Monsters.GetValueOrDefault(src.Id);
            var how = src.Kind == "phase"
                ? $"ao entrar na fase {Esc(_content.Phases.GetValueOrDefault(src.Detail ?? "") ?? src.Detail ?? "")}"
                : src.Kind == "ability" ? $"com {_d.AbilityLink(p, src.Id)}" : "";
            var summoner = src.Kind == "ability"
                ? _usage.AbilityMonsters.GetValueOrDefault(src.Id)?.FirstOrDefault() is { } mid ? _db.Monsters[mid] : null
                : who;
            if (summoner is null)
            {
                continue;
            }

            var levels = SpotsOf(summoner.Id).Select(s => s.Level).Distinct().ToList();
            var at = levels.Count == 0 ? "" : " No nível de quem invoca: " + string.Join("; ", levels.Select(l => $"nv. {l}, vida {Num(m.Health * scaling.HealthFactor(l, m.Rank))}")) + ".";
            p.Add($"<p>Invocado por {_d.MonsterLink(p, summoner.Id)} {how}.{at}</p>");
        }

        var encounters = _db.Encounters.Values.Where(e => e.Enemies.Any(x => x.Monster == m.Id)).ToList();
        if (encounters.Count > 0)
        {
            p.Add("<p class=\"muted\">Encontros de teste: " + string.Join(", ", encounters.Select(e => p.A(Url.Encounter(e.Id), e.Name))) + ".</p>");
        }

        if (spots.Count == 0 && summoners.Count == 0)
        {
            p.Add("<p>Não aparece no mundo aberto hoje.</p>");
        }

        // Quedas
        p.Add("<h2>Quedas</h2>");
        p.Add(DropTable(p, m.Rank));
    }

    private string DropTable(Page p, MonsterRank rank)
    {
        var t = _db.Rules.Items.Drops.For(rank);
        var rarity = string.Join(" / ", Enum.GetValues<ItemRarity>().Select(r => $"{Fmt.Rarity(r)} {Num(t.Rarity.For(r))}"));
        var rows = new List<(string, string)>
        {
            ("Tabela", Fmt.Rank(rank)),
            ("Chance de queda", Pct(t.Chance)),
            ("Itens por queda", Num(t.Count)),
            ("Peso de raridade", rarity),
        };
        if (t.GuaranteedRarity != ItemRarity.Common)
        {
            rows.Add(("Garantia", $"o primeiro item sai pelo menos {Fmt.Rarity(t.GuaranteedRarity)}"));
        }

        rows.Add(("Itens possíveis", $"{t.Items.Count} itens base, mesmo peso; o nível do item é o nível do monstro ({p.A($"{Url.Items}#drops", "ver itens")})"));
        return Dl(rows);
    }
}
