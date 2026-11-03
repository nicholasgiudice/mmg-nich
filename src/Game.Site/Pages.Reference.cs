using Game.Core.Definitions;
using static Game.Site.Fmt;

namespace Game.Site;

internal sealed partial class SiteBuilder
{
    private string Sources(Page p, IEnumerable<UseSource> sources)
    {
        var parts = sources.Select(s => s.Kind switch
        {
            "ability" => _d.AbilityLink(p, s.Id) + (s.Ranks.Count > 0 ? " " + Ranks(s.Ranks) : ""),
            "area" => _d.AreaLink(p, s.Id),
            "phase" => $"{_d.MonsterLink(p, s.Id)}, fase {Esc(_content.Phases.GetValueOrDefault(s.Detail ?? "") ?? s.Detail ?? "")}",
            _ => Code(s.Id),
        }).ToList();
        return parts.Count == 0 ? "<em>nada usado no jogo atual</em>" : string.Join("; ", parts);
    }

    private void BuildStatuses()
    {
        var p = NewPage(Url.Statuses, "Status", "Referência dos status de Valdran: efeitos, duração, acúmulo e quem aplica.");
        p.Add("<h1>Status</h1>");
        p.Add($"<p>Veja também as {p.A(Url.Areas, "áreas")}. \"Renova\": aplicar de novo reinicia a duração; \"acumula\": soma acúmulos até o máximo. Multiplicadores: × 0,5 de velocidade = metade da velocidade.</p>");
        if (_db.Rules.Control is { } control)
        {
            var steps = string.Join(" / ", control.Diminishing.Steps.Select(s => Mult(s)));
            p.Add($"<p>Status com tag {Code("control")} duram {ControlByRank(MonsterRank.Elite)} em elites e {ControlByRank(MonsterRank.Boss)} em chefes, "
                + $"e sofrem retorno decrescente: dentro de {Seconds(control.Diminishing.WindowTicks)}, as aplicações seguidas duram {steps}.</p>");
        }

        foreach (var s in _db.Statuses.Values)
        {
            var unused = !_usage.IsStatusUsed(s.Id);
            p.Add($"<article class=\"ref\" id=\"{Esc(s.Id)}\"><h2>{Esc(_d.StatusLabel(s))}</h2><p>{Code(s.Id)}{(unused ? " <span class=\"badge warn\">em aberto</span>" : "")}</p>");
            p.Add(Dl([
                ("Tags", s.Tags.Count == 0 ? "—" : string.Join(" ", s.Tags.Select(t => Code(t)))),
                ("Duração padrão", $"<span data-k=\"duration\">{Seconds(s.DefaultDurationTicks)}</span>"),
                ("Acúmulo", s.Stacking == StackingMode.Stack ? $"acumula até {s.MaxStacks}" : "renova"),
                ("Efeito", _d.StatusEffects(p, s)),
                ("Aplicado por", Sources(p, _usage.StatusSources.GetValueOrDefault(s.Id) ?? [])),
            ]));
            p.Add("</article>");
        }
    }

    private void BuildAreas()
    {
        var p = NewPage(Url.Areas, "Áreas", "Referência das áreas criadas por habilidades em Valdran: raio, duração e efeitos.");
        p.Add("<h1>Áreas</h1>");
        p.Add($"<p>Áreas que habilidades deixam no chão. Os dados não dão nome às áreas: o nome vem da habilidade que a cria. Veja também os {p.A(Url.Statuses, "status")}.</p>");
        foreach (var a in _db.Areas.Values)
        {
            var unused = !_usage.IsAreaUsed(a.Id);
            var trigger = a.Mode == AreaMode.OnEnterOnce ? "uma vez, quando alguém entra" : $"a cada {Seconds(a.IntervalTicks)}";
            p.Add($"<article class=\"ref\" id=\"{Esc(a.Id)}\"><h2>{Esc(_d.AreaLabel(a.Id))}</h2><p>{Code(a.Id)}{(unused ? " <span class=\"badge warn\">em aberto</span>" : "")}</p>");
            p.Add(Dl([
                ("Raio", Num(a.Radius)),
                ("Duração", Seconds(a.DurationTicks) + (a.ArmTimeTicks > 0 ? $" (arma em {Seconds(a.ArmTimeTicks)})" : "")),
                ("Dispara", trigger),
                ("Afeta", Faction(a.Affects, plural: true) + " de quem criou"),
                ("Efeitos", _d.Effects(p, a.Effects)),
                ("Criada por", Sources(p, _usage.AreaSources.GetValueOrDefault(a.Id) ?? [])),
            ]));
            p.Add("</article>");
        }
    }
}
