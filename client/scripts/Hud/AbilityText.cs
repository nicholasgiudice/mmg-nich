using System.Globalization;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Texto de uma habilidade num NH (T-064), gerado da definição já resolvida (<c>ContentCatalog.AbilityAt</c>):
/// custo, recarga, conjuração, acertos e efeitos. Usado no tooltip da barra e no menu de NH.
/// </summary>
public static class AbilityText
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Linhas de custo/tempo e efeitos.</summary>
    public static string Describe(AbilityDefinition a, ContentCatalog content)
    {
        var lines = new List<string>();
        var timing = new List<string>();
        if (a.Cost is { } cost)
        {
            timing.Add($"custo {cost.Amount.ToString("0", Pt)} {HudTheme.ResourceName(cost.Resource)}");
        }

        if (a.CooldownTicks > 0)
        {
            timing.Add($"recarga {Seconds(a.CooldownTicks)}" + (a.CooldownCharges > 1 ? $" ({a.CooldownCharges} cargas)" : ""));
        }

        if (a.CastTimeTicks > 0)
        {
            timing.Add($"conjuração {Seconds(a.CastTimeTicks)}");
        }

        if (timing.Count > 0)
        {
            lines.Add(Capitalize(string.Join(", ", timing)));
        }

        var effects = Effects(a.Effects, content);
        if (a.HitCount > 1)
        {
            var interval = a.Hits!.IntervalTicks > 0 ? $", a cada {Seconds(a.Hits.IntervalTicks)}" : "";
            lines.Add($"{a.HitCount} acertos{interval}; cada um: {effects}");
        }
        else if (effects.Length > 0)
        {
            lines.Add(Capitalize(effects));
        }

        return string.Join("\n", lines);
    }

    private static string Effects(IEnumerable<EffectDefinition> effects, ContentCatalog content) =>
        string.Join("; ", effects.Select(e => Effect(e, content)).Where(t => t.Length > 0));

    private static string Effect(EffectDefinition e, ContentCatalog content)
    {
        var self = e.Target == EffectTarget.Self ? " em si" : "";
        var onHit = e.OnHit is { } h ? $" (só no {h}º acerto)" : "";
        var text = e switch
        {
            DamageEffect d => $"dano {Number(d.Base)}",
            HealEffect h2 => $"cura {Number(h2.Base)}",
            ShieldEffect s => $"escudo {Number(s.Amount)} por {Seconds(s.DurationTicks)}"
                + (s.Refund is { } r ? $", devolve {Percent(r.Fraction)} da sobra em {HudTheme.ResourceName(r.Resource)}" : ""),
            ApplyStatusEffect st => StatusName(st.Status, content)
                + (st.DurationTicks is { } dur ? $" {Seconds(dur)}" : ""),
            RemoveStatusEffect rm => $"remove {(rm.Count == 0 ? "todos os" : rm.Count.ToString(Pt))} "
                + $"({string.Join(", ", rm.Tags.Count > 0 ? rm.Tags : [rm.Tag])})",
            DisplacementEffect m => $"{(m.Kind == DisplacementKind.Dash ? "salto" : "empurra")} de {Number(m.Distance)} tiles",
            InterruptEffect => "interrompe",
            ThreatEffect t => t.TauntDurationTicks is { } taunt ? $"provoca por {Seconds(taunt)}" : "",
            SpawnAreaEffect sa => content.Areas.TryGetValue(sa.Area, out var area)
                ? $"deixa área de raio {Number(area.Radius)} por {Seconds(area.DurationTicks)}"
                : "deixa área",
            ChainEffect c => $"salta para {c.Jumps} inimigo(s) a até {Number(c.Radius)} tiles ({Percent(c.Multiplier)}): {Effects(c.Effects, content)}",
            ExtraTargetsEffect x => $"também {ExtraWho(x)}{(x.Multiplier != 1 ? $" ({Percent(x.Multiplier)})" : "")}: {Effects(x.Effects, content)}",
            ReviveEffect rv => $"levanta {rv.Count} aliado caído a até {Number(rv.Radius)} tiles com {Percent(rv.HealthFraction)} da vida",
            ResourceEffect res => $"{Number(res.Amount)} de {HudTheme.ResourceName(res.Resource)}",
            SummonEffect su => $"invoca {su.Count}",
            _ => "",
        };
        return text.Length == 0 ? "" : text + self + onHit;
    }

    private static string ExtraWho(ExtraTargetsEffect x)
    {
        var side = x.Affects == FactionFilter.Ally ? "aliado" : "inimigo";
        if (x.Count == 0)
        {
            return $"os {side}s a até {Number(x.Radius)} tiles do alvo";
        }

        var which = x.Select == TargetSelect.MostInjured ? "mais ferido" : "mais próximo";
        return x.Count == 1 ? $"o {side} {which}" : $"{x.Count} {side}s ({which})";
    }

    private static string StatusName(string id, ContentCatalog content) =>
        content.Statuses.TryGetValue(id, out var s) ? s.Name : id;

    private static string Seconds(int ticks) => (ticks / (double)World.TickRate).ToString("0.##", Pt) + " s";

    private static string Number(double v) => v.ToString("0.##", Pt);

    private static string Percent(double v) => (v * 100).ToString("0", Pt) + "%";

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Pt) + s[1..];
}
