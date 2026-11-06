using Game.Core.Definitions;
using Game.Data;
using static Game.Site.Fmt;

namespace Game.Site;

/// <summary>Texto em português (HTML) de alvos, avisos, efeitos e status, com links para as páginas de referência.</summary>
public sealed class Describe
{
    private readonly GameDatabase _db;
    private readonly Usage _usage;

    public Describe(GameDatabase db, Usage usage)
    {
        _db = db;
        _usage = usage;
    }

    public string StatusLink(Page p, string id) =>
        _db.Statuses.TryGetValue(id, out var s) ? p.A(Url.Status(id), StatusLabel(s)) : Code(id);

    /// <summary>Nome do status; variantes com o mesmo nome exibido ganham o sufixo do id para não se confundirem.</summary>
    public string StatusLabel(StatusDefinition s)
    {
        var same = _db.Statuses.Values.Count(x => x.Name == s.Name);
        return same > 1 ? $"{s.Name} ({s.Id[(s.Id.IndexOf('.') + 1)..]})" : s.Name;
    }

    /// <summary>Áreas não têm nome nos dados: usa o nome da habilidade que a cria (ou o id).</summary>
    public string AreaLabel(string id)
    {
        if (_usage.AreaSources.TryGetValue(id, out var sources))
        {
            var names = sources
                .Where(s => s.Kind == "ability" && _db.Abilities.ContainsKey(s.Id))
                .Select(s => _db.Abilities[s.Id].Name + (s.Ranks.Count > 0 && _db.Abilities[s.Id].HighestRank > 1 ? " " + Ranks(s.Ranks) : ""))
                .Distinct()
                .ToList();
            if (names.Count > 0)
            {
                return "Área: " + string.Join(" / ", names);
            }
        }

        return _db.Abilities.Values.FirstOrDefault(a => a.Effects.OfType<SpawnAreaEffect>().Any(e => e.Area == id)) is { } creator
            ? "Área: " + creator.Name
            : id;
    }

    public string AreaLink(Page p, string id) =>
        _db.Areas.ContainsKey(id) ? p.A(Url.Area(id), AreaLabel(id)) : Code(id);

    public string MonsterLink(Page p, string id) =>
        _db.Monsters.TryGetValue(id, out var m) ? p.A(Url.Monster(id), m.Name) : Code(id);

    public string AbilityLink(Page p, string id) =>
        _db.Abilities.TryGetValue(id, out var a) ? p.A(Url.Ability(id), a.Name) : Code(id);

    public string ClassLink(Page p, string id) =>
        _db.Classes.TryGetValue(id, out var c) ? p.A(Url.Class(id), c.Name) : Code(id);

    public static string Targeting(TargetingDefinition t) => t switch
    {
        SelfTargeting => "em si mesmo",
        UnitTargeting u => $"alvo único {Faction(u.Faction)}, alcance {Num(u.Range)}",
        MeleeArcTargeting m => $"arco corpo a corpo, alcance {Num(m.Range)}, {Num(m.Angle)}°",
        LineTargeting l => $"linha {Num(l.Length)} × {Num(l.Width)}",
        CircleAtPointTargeting c => $"círculo no ponto, alcance {Num(c.Range)}, raio {Num(c.Radius)}",
        CircleSelfTargeting c => $"círculo em volta, raio {Num(c.Radius)}",
        _ => t.GetType().Name,
    };

    public static string Telegraph(TelegraphDefinition? t)
    {
        if (t is null)
        {
            return "—";
        }

        var shape = t.Shape switch
        {
            TelegraphShape.Circle => $"círculo, raio {Num(t.Radius)}",
            TelegraphShape.Cone => $"cone {Num(t.Length)} × {Num(t.Angle)}°",
            _ => $"linha {Num(t.Length)} × {Num(t.Width)}",
        };
        var charge = t.Charge ? "; avança pela linha" : "";
        return $"aviso de {Seconds(t.DelayTicks)} ({shape}){charge}";
    }

    public static string Hits(AbilityDefinition a) =>
        a.HitCount <= 1 ? "1" : $"{a.HitCount} ({Seconds(a.Hits!.IntervalTicks)} entre eles)";

    public static string Cost(AbilityDefinition a) =>
        a.Cost is { } c ? $"{Num(c.Amount)} {Resource(c.Resource)}" : "—";

    public static string Cooldown(AbilityDefinition a)
    {
        var text = a.CooldownTicks > 0 ? Seconds(a.CooldownTicks) : "—";
        if (a.CooldownCharges > 1)
        {
            text += $", {a.CooldownCharges} cargas que recarregam uma a uma";
        }

        return text;
    }

    public static string Cast(AbilityDefinition a) =>
        a.CastTimeTicks > 0 ? Seconds(a.CastTimeTicks) + (a.CastWhileMoving ? " (andando)" : "") : "instantânea";

    /// <summary>Lista de efeitos como itens de lista HTML.</summary>
    public string Effects(Page p, IEnumerable<EffectDefinition> effects)
    {
        var items = effects.Select(e => $"<li>{Effect(p, e)}</li>").ToList();
        return items.Count == 0 ? "—" : $"<ul class=\"fx\">{string.Concat(items)}</ul>";
    }

    public string Effect(Page p, EffectDefinition e)
    {
        var text = e switch
        {
            DamageEffect d => $"dano {School(d.School)} <b>{Scaled(d.Base, d.Scaling)}</b>{Bonus(d.BonusVsTags)}",
            HealEffect h => $"cura <b>{Scaled(h.Base, h.Scaling)}</b>",
            ShieldEffect s => $"escudo <b>{Scaled(s.Amount, s.Scaling)}</b> por {Seconds(s.DurationTicks)}"
                + (s.Refund is { } r ? $"; ao expirar, devolve {Pct(r.Fraction)} da sobra em {Resource(r.Resource)}" : ""),
            ApplyStatusEffect s => $"aplica {StatusLink(p, s.Status)} por {StatusDuration(s)}",
            RemoveStatusEffect r => RemoveText(r),
            DisplacementEffect d => d.Kind == DisplacementKind.Dash
                ? $"salta {Num(d.Distance)} {Direction(d.Direction)}"
                : $"empurra {Num(d.Distance)} {Direction(d.Direction)}",
            InterruptEffect => "interrompe a conjuração",
            ThreatEffect t => ThreatText(t),
            ResourceEffect r => $"{(r.Amount >= 0 ? "+" : "")}{Num(r.Amount)} de {Resource(r.Resource)}",
            SpawnAreaEffect a => $"cria {AreaLink(p, a.Area)}{AreaSummary(a.Area)}",
            SummonEffect m => $"invoca {m.Count} × {MonsterLink(p, m.Monster)}"
                + (m.Radius > 0 ? $" em raio {Num(m.Radius)}" : "")
                + (m.MaxAlive > 0 ? $" (no máximo {m.MaxAlive} {(m.MaxAlive == 1 ? "viva" : "vivas")})" : ""),
            ChainEffect c => $"salta até {c.Jumps} {(c.Jumps == 1 ? "vez" : "vezes")} para o {Faction(c.Affects)} mais próximo a até {Num(c.Radius)}, com {Pct(c.Multiplier)} da potência:"
                + Effects(p, c.Effects),
            ExtraTargetsEffect x => ExtraText(p, x),
            ReviveEffect r => $"levanta até {r.Count} {(r.Count == 1 ? "aliado caído" : "aliados caídos")} a até {Num(r.Radius)} com {Pct(r.HealthFraction)} da vida máxima",
            _ => Esc(e.GetType().Name),
        };

        if (e.Target == EffectTarget.Self && e is not ChainEffect and not ExtraTargetsEffect)
        {
            text += " <span class=\"muted\">(em quem usa)</span>";
        }

        return e.OnHit is { } hit ? $"no {hit}º acerto: {text}" : text;
    }

    private string StatusDuration(ApplyStatusEffect s)
    {
        if (s.DurationTicks is { } d)
        {
            return Seconds(d);
        }

        return _db.Statuses.TryGetValue(s.Status, out var st) ? $"{Seconds(st.DefaultDurationTicks)} (duração padrão)" : "duração padrão";
    }

    private string AreaSummary(string id) =>
        _db.Areas.TryGetValue(id, out var a) ? $" (raio {Num(a.Radius)}, {Seconds(a.DurationTicks)})" : "";

    private static string Bonus(IReadOnlyDictionary<string, double> bonus) =>
        bonus.Count == 0
            ? ""
            : " (" + string.Join(", ", bonus.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"+{Pct(kv.Value)} contra {Esc(kv.Key)}")) + ")";

    private static string RemoveText(RemoveStatusEffect r)
    {
        var tags = (r.Tags.Count > 0 ? r.Tags : [r.Tag]).Where(t => t.Length > 0).Select(t => Code(t));
        var count = r.Count == 0 ? "todos os status" : r.Count == 1 ? "1 status" : $"{r.Count} status";
        return $"remove {count} com tag {string.Join(" ou ", tags)}";
    }

    private static string ThreatText(ThreatEffect t)
    {
        var parts = new List<string>();
        if (t.Amount != 0)
        {
            parts.Add($"+{Num(t.Amount)} de ameaça");
        }

        if (t.TauntDurationTicks is { } d)
        {
            parts.Add($"provoca por {Seconds(d)} (os monstros atacam quem usou)");
        }

        return parts.Count == 0 ? "ameaça" : string.Join("; ", parts);
    }

    private string ExtraText(Page p, ExtraTargetsEffect x)
    {
        var who = x.Count == 0
            ? $"todos os {Faction(x.Affects, plural: true)}"
            : x.Select == TargetSelect.MostInjured
                ? (x.Count == 1 ? $"o {Faction(x.Affects)} mais ferido" : $"os {x.Count} {Faction(x.Affects, plural: true)} mais feridos")
                : (x.Count == 1 ? $"o {Faction(x.Affects)} mais próximo" : $"os {x.Count} {Faction(x.Affects, plural: true)} mais próximos");
        var power = x.Multiplier != 1 ? $", com {Pct(x.Multiplier)} da potência" : "";
        return $"também {who} a até {Num(x.Radius)} do alvo{power}:" + Effects(p, x.Effects);
    }

    private static string Direction(DisplacementDirection d) => d switch
    {
        DisplacementDirection.Backward => "para trás",
        DisplacementDirection.AwayFromSource => "para longe de quem usou",
        DisplacementDirection.TowardTarget => "em direção ao alvo",
        _ => "na direção da mira",
    };

    /// <summary>O que um status faz (modificadores, dano periódico, travas, guarda frontal, imunidade, fortalecimento).</summary>
    public string StatusEffects(Page p, StatusDefinition s)
    {
        var parts = new List<string>();
        foreach (var per in s.Periodic)
        {
            parts.Add($"a cada {Seconds(per.IntervalTicks)}: {string.Join("; ", per.Effects.Select(e => Effect(p, e)))}");
        }

        var m = s.Modifiers;
        if (m.SpeedMultiplier != 1)
        {
            parts.Add($"velocidade {Mult(m.SpeedMultiplier)}");
        }

        if (m.DamageDealtMultiplier != 1)
        {
            parts.Add($"dano causado {Mult(m.DamageDealtMultiplier)}");
        }

        if (m.DamageTakenMultiplier != 1)
        {
            parts.Add($"dano recebido {Mult(m.DamageTakenMultiplier)}");
        }

        if (m.ControlDurationMultiplier != 1)
        {
            parts.Add($"duração de controle recebido {Mult(m.ControlDurationMultiplier)}");
        }

        if (s.Flags.Stunned)
        {
            parts.Add("atordoado (não age)");
        }

        if (s.Flags.Rooted)
        {
            parts.Add("preso no lugar");
        }

        if (s.Flags.Silenced)
        {
            parts.Add("silenciado");
        }

        if (s.Flags.Untargetable)
        {
            parts.Add("não pode ser alvo");
        }

        if (s.Frontal is { } f)
        {
            var reflect = f.ReflectMeleeFraction > 0 ? $"; devolve {Pct(f.ReflectMeleeFraction)} do dano corpo a corpo bloqueado a quem atacou" : "";
            parts.Add($"dano vindo da frente ({Num(f.ArcDegrees)}°) {Mult(f.DamageTakenMultiplier)}{reflect}");
        }

        if (s.ImmuneToTags.Count > 0)
        {
            parts.Add($"imune a status com tag {string.Join(" ou ", s.ImmuneToTags.Select(t => Code(t)))}");
        }

        if (s.Empower is { } emp)
        {
            var names = string.Join(" ou ", emp.Abilities.Select(a => AbilityLink(p, a)));
            parts.Add($"o próximo uso de {names} sai com potência {Mult(emp.Multiplier)} e consome o status");
        }

        return parts.Count == 0 ? "—" : string.Join("; ", parts);
    }
}
