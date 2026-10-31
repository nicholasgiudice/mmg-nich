using System.Globalization;
using Game.Core.Combat;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Texto de uma habilidade num NH (T-064), gerado da definição já resolvida (<c>ContentCatalog.AbilityAt</c>):
/// custo, recarga, conjuração, acertos e efeitos. Usado no tooltip da barra e no menu de NH. Com as estatísticas do
/// personagem (T-221), dano, cura e escudo aparecem já escalados (base + escalas, sem crítico nem bônus de alvo).
/// </summary>
public static class AbilityText
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Linhas de custo/tempo e efeitos; <paramref name="stats"/> null = valores base dos dados.</summary>
    public static string Describe(AbilityDefinition a, ContentCatalog content, CharacterStats? stats = null)
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

        var effects = Effects(a.Effects, content, stats);
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

    /// <summary>
    /// Valor principal da habilidade com as estatísticas (o primeiro dano, cura ou escudo, inclusive dentro de saltos e
    /// alvos extras), arredondado como no Core; null se ela não tem nenhum. Usado na prévia "+1 FOR: Golpe Pesado 51 → 53".
    /// </summary>
    public static double? MainValue(AbilityDefinition a, CharacterStats stats)
    {
        static double? Find(IEnumerable<EffectDefinition> effects, CharacterStats s)
        {
            foreach (var e in effects)
            {
                var v = e switch
                {
                    DamageEffect d => Scaled(d.Base, d.Scaling, s),
                    HealEffect h => Scaled(h.Base, h.Scaling, s),
                    ShieldEffect sh => Scaled(sh.Amount, sh.Scaling, s),
                    ChainEffect c => Find(c.Effects, s),
                    ExtraTargetsEffect x => Find(x.Effects, s),
                    _ => null,
                };
                if (v is not null)
                {
                    return v;
                }
            }

            return null;
        }

        return Find(a.Effects, stats);
    }

    /// <summary>Mesma conta de <c>StatMath.Scaled</c> (base + Σ coeficiente × estatística), a partir das estatísticas replicadas.</summary>
    public static double Scaled(double baseValue, IReadOnlyDictionary<string, double> scaling, CharacterStats? stats)
    {
        var value = baseValue;
        if (stats is not null)
        {
            foreach (var (key, coef) in scaling)
            {
                value += coef * StatMath.Stat(stats, key);
            }
        }

        return Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static string Effects(IEnumerable<EffectDefinition> effects, ContentCatalog content, CharacterStats? stats) =>
        string.Join("; ", effects.Select(e => Effect(e, content, stats)).Where(t => t.Length > 0));

    private static string Effect(EffectDefinition e, ContentCatalog content, CharacterStats? stats)
    {
        var self = e.Target == EffectTarget.Self ? " em si" : "";
        var onHit = e.OnHit is { } h ? $" (só no {h}º acerto)" : "";
        var text = e switch
        {
            DamageEffect d => $"dano {Value(d.Base, d.Scaling, stats)}",
            HealEffect h2 => $"cura {Value(h2.Base, h2.Scaling, stats)}",
            ShieldEffect s => $"escudo {Value(s.Amount, s.Scaling, stats)} por {Seconds(s.DurationTicks)}"
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
            ChainEffect c => $"salta para {c.Jumps} inimigo(s) a até {Number(c.Radius)} tiles ({Percent(c.Multiplier)}): {Effects(c.Effects, content, stats)}",
            ExtraTargetsEffect x => $"também {ExtraWho(x)}{(x.Multiplier != 1 ? $" ({Percent(x.Multiplier)})" : "")}: {Effects(x.Effects, content, stats)}",
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

    /// <summary>Com estatísticas: o valor escalado; sem: a base dos dados.</summary>
    private static string Value(double baseValue, IReadOnlyDictionary<string, double> scaling, CharacterStats? stats) =>
        stats is null ? Number(baseValue) : Number(Scaled(baseValue, scaling, stats));

    private static string StatusName(string id, ContentCatalog content) =>
        content.Statuses.TryGetValue(id, out var s) ? s.Name : id;

    private static string Seconds(int ticks) => (ticks / (double)World.TickRate).ToString("0.##", Pt) + " s";

    private static string Number(double v) => v.ToString("0.##", Pt);

    private static string Percent(double v) => (v * 100).ToString("0", Pt) + "%";

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Pt) + s[1..];
}
