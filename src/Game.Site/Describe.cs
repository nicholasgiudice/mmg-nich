using Game.Core.Definitions;
using Game.Data;
using static Game.Site.Fmt;

namespace Game.Site;

/// <summary>Texto em português (HTML) de alvos, avisos, efeitos e status, com links para as páginas de referência.</summary>
public sealed class Describe
{
    private readonly GameDatabase _db;
    private readonly Usage _usage;
    private readonly IReadOnlyDictionary<string, string> _tags;

    public Describe(GameDatabase db, Usage usage, IReadOnlyDictionary<string, string>? tags = null)
    {
        _db = db;
        _usage = usage;
        _tags = tags ?? new Dictionary<string, string>();
    }

    /// <summary>Rótulo em português de uma tag (de <c>site.json</c>); tag sem rótulo = null.</summary>
    public string? TagLabel(string tag) => _tags.TryGetValue(tag, out var label) ? label : null;

    /// <summary>Rótulos das tags que têm rótulo, sem repetir, separados por vírgula; vazio se nenhuma tem.</summary>
    public string TagList(IEnumerable<string> tags, string separator = ", ") =>
        string.Join(separator, tags.Select(TagLabel).OfType<string>().Distinct().Select(Esc));

    public string StatusLink(Page p, string id) =>
        _db.Statuses.TryGetValue(id, out var s) ? p.A(Url.Status(id), StatusLabel(s)) : "status desconhecido";

    /// <summary>
    /// Nome do status. Variantes com o mesmo nome exibido ganham, entre parênteses, quem as aplica (habilidade e NH), para
    /// não se confundirem; se ainda assim empatam, um número de variante.
    /// </summary>
    public string StatusLabel(StatusDefinition s)
    {
        var same = _db.Statuses.Values.Where(x => x.Name == s.Name).ToList();
        if (same.Count <= 1)
        {
            return s.Name;
        }

        // T-432: o status que é trocado pelo do NH de outra habilidade (Veneno do Caçador).
        if (s.ByRank is { } byRank && _db.Abilities.TryGetValue(byRank.Ability, out var by))
        {
            return $"{s.Name} (pelo NH de {by.Name})";
        }

        // T-441: variantes de um grupo exclusivo (Queimadura) se distinguem pela força.
        if (s.Exclusive is { } ex && same.All(x => x.Exclusive?.Group == ex.Group || x.ByRank is not null)
            && same.Count(x => x.Exclusive?.Tier == ex.Tier) == 1)
        {
            return $"{s.Name} (força {ex.Tier})";
        }

        var labels = same.ToDictionary(x => x.Id, SourceLabel, StringComparer.Ordinal);
        var mine = labels[s.Id];
        return mine.Length > 0 && labels.Values.Count(l => l == mine) == 1
            ? $"{s.Name} ({mine})"
            : $"{s.Name} (variante {same.FindIndex(x => x.Id == s.Id) + 1})";
    }

    /// <summary>Quem aplica o status: nomes das habilidades com os NH, ou das fases de chefe.</summary>
    private string SourceLabel(StatusDefinition s) =>
        string.Join(", ", (_usage.StatusSources.GetValueOrDefault(s.Id) ?? [])
            .Select(src => src.Kind switch
            {
                "ability" when _db.Abilities.TryGetValue(src.Id, out var a) => a.Name + (src.Ranks.Count > 0 ? " " + Ranks(src.Ranks) : ""),
                "phase" when _db.Monsters.TryGetValue(src.Id, out var m) => m.Name,
                "area" => AreaLabel(src.Id).Replace("Área: ", "", StringComparison.Ordinal),
                _ => "",
            })
            .Where(x => x.Length > 0)
            .Distinct());

    /// <summary>
    /// Áreas não têm nome nos dados: usa o nome da habilidade que a cria. Duas áreas da mesma habilidade (Consagração: uma
    /// fere, outra cura) se distinguem por quem afetam.
    /// </summary>
    public string AreaLabel(string id)
    {
        var label = RawAreaLabel(id);
        if (!_db.Areas.TryGetValue(id, out var area))
        {
            return label;
        }

        var twins = _db.Areas.Values.Where(a => a.Id != id && _usage.IsAreaUsed(a.Id) && RawAreaLabel(a.Id) == label).ToList();
        return twins.Count > 0 && twins.All(t => t.Affects != area.Affects)
            ? $"{label} ({Faction(area.Affects, plural: true)})"
            : label;
    }

    private string RawAreaLabel(string id)
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
            : "Área";
    }

    public string AreaLink(Page p, string id) =>
        _db.Areas.ContainsKey(id) ? p.A(Url.Area(id), AreaLabel(id)) : "área desconhecida";

    public string MonsterLink(Page p, string id) =>
        _db.Monsters.TryGetValue(id, out var m) ? p.A(Url.Monster(id), m.Name) : "monstro desconhecido";

    public string AbilityLink(Page p, string id) =>
        _db.Abilities.TryGetValue(id, out var a) ? p.A(Url.Ability(id), a.Name) : "habilidade desconhecida";

    public string ClassLink(Page p, string id) =>
        _db.Classes.TryGetValue(id, out var c) ? p.A(Url.Class(id), c.Name)
        : _usage.SecondClasses.TryGetValue(id, out var sc) ? p.A(Url.Class(id), sc.Def.Name) // T-075
        : "classe desconhecida";

    public static string Targeting(TargetingDefinition t) => t switch
    {
        SelfTargeting => "em si mesmo",
        UnitTargeting u => $"alvo único {Faction(u.Faction)}, alcance {Num(u.Range)}",
        MeleeArcTargeting m => $"arco corpo a corpo, alcance {Num(m.Range)}, {Num(m.Angle)}°",
        LineTargeting l => $"linha {Num(l.Length)} × {Num(l.Width)}"
            + (l.ExplodeRadius is { } r ? $", explode no primeiro atingido (raio {Num(r)})" : ""),
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
        if (a.CooldownOnInterrupt && a.CooldownTicks > 0)
        {
            text += " (também quando interrompida)";
        }

        if (a.CooldownCharges > 1)
        {
            text += $", {a.CooldownCharges} cargas que recarregam uma a uma";
        }

        return text;
    }

    public static string Cast(AbilityDefinition a) =>
        a.Channel is not null ? "canalizada"
        : a.CastTimeTicks > 0 ? Seconds(a.CastTimeTicks) + (a.CastWhileMoving ? " (andando)" : "") : "instantânea";

    /// <summary>
    /// Canalização (T-442): pulsos, intervalo, crescimento por pulso, andar canalizando e o que sai ao ir até o fim.
    /// Os efeitos da habilidade saem a cada pulso.
    /// </summary>
    public string Channel(Page p, ChannelDefinition? c)
    {
        if (c is null)
        {
            return "—";
        }

        var text = $"{c.Pulses} {(c.Pulses == 1 ? "pulso" : "pulsos")}, um a cada {Seconds(c.IntervalTicks)}";
        if (c.PulseGrowth != 0)
        {
            text += $"; cada pulso causa {Pct(c.PulseGrowth)} a mais que o anterior (acumulando)";
        }

        text += c.MoveSpeedMultiplier is { } move
            ? $"; pode andar canalizando, com velocidade {Mult(move)}"
            : "; andar corta";
        if (c.OnComplete.Count > 0)
        {
            text += "; se for até o fim, depois do último pulso:" + Effects(p, c.OnComplete);
        }

        return text;
    }

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
            DamageEffect d => $"dano {School(d.School)} <b>{Scaled(d.Base, d.Scaling)}</b>{Bonus(d.BonusVsTags)}{DamageExtras(d)}",
            HealEffect h => $"cura <b>{HealValue(h)}</b>",
            ShieldEffect s => $"escudo <b>{Scaled(s.Amount, s.Scaling)}</b> por {Seconds(s.DurationTicks)}"
                + (s.Refund is { } r ? $"; ao expirar, devolve {Pct(r.Fraction)} da sobra em {Resource(r.Resource)}" : ""),
            ApplyStatusEffect s => $"aplica {(s.Stacks > 1 ? $"{s.Stacks} acúmulos de " : "")}{StatusLink(p, s.Status)} {StatusDuration(s)}",
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
            CopyStatusEffect c => CopyText(c),
            IfStatusEffect i => $"só nos alvos que têm {TagsOr(i.Tags)} no momento do uso"
                + (i.Consume ? ", que perdem esses status depois dos efeitos" : "") + ":" + Effects(p, i.Effects),
            CurrentTargetEffect c => $"{(c.Count == 1 ? "1 disparo" : $"{c.Count} disparos")} no alvo atual de quem tem o status "
                + $"(o último {Faction(c.Affects)} que ele escolheu como alvo), a até {Num(c.Range)} e com linha de visão; sem alvo atual, nada sai:"
                + Effects(p, c.Effects),
            _ => Esc(e.GetType().Name), // efeito novo sem texto: o teste do site acusa o nome do tipo
        };

        if (e.Target == EffectTarget.Self && e is not ChainEffect and not ExtraTargetsEffect and not IfStatusEffect and not CurrentTargetEffect)
        {
            text += " <span class=\"muted\">(em quem usa)</span>";
        }

        return e.OnHit is { } hit ? $"no {hit}º acerto: {text}" : text;
    }

    /// <summary>"por 4 s", "por 4 s (duração padrão)" ou, no status trocado pelo do NH, "pela duração do status do NH".</summary>
    private string StatusDuration(ApplyStatusEffect s)
    {
        if (s.DurationTicks is { } d)
        {
            return "por " + Seconds(d);
        }

        if (_db.Statuses.TryGetValue(s.Status, out var ranked) && ranked.ByRank is not null)
        {
            return "pela duração padrão do status do NH";
        }

        return _db.Statuses.TryGetValue(s.Status, out var st) ? $"por {Seconds(st.DefaultDurationTicks)} (duração padrão)" : "pela duração padrão";
    }

    private string AreaSummary(string id) =>
        _db.Areas.TryGetValue(id, out var a)
            ? $" (raio {Num(a.Radius)}, {Seconds(a.DurationTicks)}{(a.FollowOwner ? ", acompanha quem a criou" : "")})"
            : "";

    /// <summary>Tags com rótulo, ligadas por "ou"; sem nenhum rótulo, "alguns status".</summary>
    private string TagsOr(IEnumerable<string> tags)
    {
        var list = TagList(tags, " ou ");
        return list.Length == 0 ? "alguns status" : list;
    }

    private static string HealValue(HealEffect h)
    {
        if (h.MaxHealthFraction <= 0)
        {
            return Scaled(h.Base, h.Scaling);
        }

        var max = $"{Pct(h.MaxHealthFraction)} da vida máxima de quem recebe";
        return h.Base == 0 && h.Scaling.Count == 0 ? max : $"{Scaled(h.Base, h.Scaling)} + {max}";
    }

    /// <summary>O que um dano faz além do valor (T-406, T-407, T-422, T-431, T-432).</summary>
    private static string DamageExtras(DamageEffect d)
    {
        var parts = new List<string>();
        if (d.BehindFraction is { } behind)
        {
            parts.Add($"o primeiro alvo leva o dano inteiro, e quem está atrás dele leva {Pct(behind)}");
        }

        if (d.IgnoreArmor > 0)
        {
            parts.Add($"ignora {Pct(d.IgnoreArmor)} da {(d.School == DamageSchool.Physical ? "armadura" : "resistência")} do alvo");
        }

        if (d.LowHealthBonus is { } low)
        {
            parts.Add($"+{Pct(low.Bonus)} em alvo com menos de {Pct(low.BelowHealth)} da vida");
        }

        if (d.ResetCooldownOnKill)
        {
            parts.Add("se matar o alvo, a recarga volta a zero");
        }

        if (d.MovingBonus > 0)
        {
            parts.Add($"+{Pct(d.MovingBonus)} nos pulsos em que o alvo andou");
        }

        if (d.ThreatMultiplier is { } threat)
        {
            parts.Add($"gera {Num(threat)} vezes o dano em ameaça");
        }

        if (d.SelfHeal > 0)
        {
            parts.Add($"quem causou se cura em {Pct(d.SelfHeal)} do dano causado");
        }

        if (d.AllyHeal is { } ally)
        {
            parts.Add($"a cada acerto, os aliados a até {Num(ally.Radius)} de quem causou{(ally.IncludeSelf ? ", inclusive ele," : "")} se curam em {Pct(ally.Fraction)} do dano causado");
        }

        return parts.Count == 0 ? "" : "; " + string.Join("; ", parts);
    }

    private string CopyText(CopyStatusEffect c)
    {
        var who = c.Count == 0
            ? $"todos os {Faction(c.Affects, plural: true)}"
            : c.Count == 1 ? $"o {Faction(c.Affects)} mais próximo" : $"os {c.Count} {Faction(c.Affects, plural: true)} mais próximos";
        return $"copia os status de {TagsOr(c.Tags)} do alvo, com os mesmos acúmulos e o tempo que falta, para {who} a até {Num(c.Radius)} dele";
    }

    private string Bonus(IReadOnlyDictionary<string, double> bonus) =>
        bonus.Count == 0
            ? ""
            : " (" + string.Join(", ", bonus.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"+{Pct(kv.Value)} contra {(TagLabel(kv.Key) is { } t ? "o tipo " + Esc(t) : "alguns monstros")}")) + ")";

    private string RemoveText(RemoveStatusEffect r)
    {
        var tags = TagList((r.Tags.Count > 0 ? r.Tags : [r.Tag]).Where(t => t.Length > 0), " ou ");
        var count = r.Count == 0 ? "todos os status" : r.Count == 1 ? "1 status" : $"{r.Count} status";
        return tags.Length == 0 ? $"remove {count}" : $"remove {count} de {tags}";
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

        if (m.HealingTakenMultiplier != 1)
        {
            parts.Add($"cura recebida {Mult(m.HealingTakenMultiplier)}");
        }

        foreach (var (school, mult) in m.DamageTakenBySchool.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var name = Enum.GetValues<DamageSchool>().FirstOrDefault(d => Game.Core.Combat.UnitInfo.SchoolName(d) == school) is var d0
                && Game.Core.Combat.UnitInfo.SchoolName(d0) == school ? School(d0) : school;
            parts.Add($"dano {name} recebido {Mult(mult)}");
        }

        if (s.Reflect is { } r)
        {
            parts.Add($"devolve {Pct(r.Fraction)} do dano recebido a quem atacou");
        }

        if (s.Breaks is { } b)
        {
            var how = new List<string>();
            if (b.DamageFraction > 0)
            {
                how.Add($"ao receber {Pct(b.DamageFraction)} da vida máxima em dano");
            }

            if (b.Interrupt)
            {
                how.Add("ao ser interrompido");
            }

            if (how.Count > 0)
            {
                parts.Add($"quebra {string.Join(" ou ", how)}");
            }
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

        if (s.Flags.Feared)
        {
            parts.Add("com medo (não age e foge de onde veio o medo)");
        }

        if (s.Flags.KnockbackImmune)
        {
            parts.Add("não é empurrado por inimigos");
        }

        if (s.Flags.Undying)
        {
            parts.Add("não cai: um dano que o mataria o deixa com 1 de vida");
        }

        if (s.Frontal is { } f)
        {
            var reflect = f.ReflectMeleeFraction > 0 ? $"; devolve {Pct(f.ReflectMeleeFraction)} do dano corpo a corpo bloqueado a quem atacou" : "";
            parts.Add($"dano vindo da frente ({Num(f.ArcDegrees)}°) {Mult(f.DamageTakenMultiplier)}{reflect}");
        }

        if (s.ImmuneToTags.Count > 0)
        {
            var immune = TagList(s.ImmuneToTags, " e ");
            parts.Add(immune.Length == 0 ? "imune a alguns status" : $"imune a {immune}");
        }

        if (s.Empower is { } emp)
        {
            var names = string.Join(" ou ", emp.Abilities.Select(a => AbilityLink(p, a)));
            parts.Add($"o próximo uso de {names} sai com potência {Mult(emp.Multiplier)} e consome o status");
        }

        parts.AddRange(SecondClassParts(p, s));
        return parts.Count == 0 ? "—" : string.Join("; ", parts);
    }

    /// <summary>Campos de status das segundas classes (T-406 a T-442).</summary>
    private IEnumerable<string> SecondClassParts(Page p, StatusDefinition s)
    {
        var stacks = s.Stacking == StackingMode.Stack && s.MaxStacks > 1 ? " por acúmulo" : "";
        if (s.ByRank is { } byRank)
        {
            var ranks = string.Join(", ", byRank.Statuses.Select((id, i) => $"NH{i + 1}: {StatusLink(p, id)}"));
            yield return $"não fica em ninguém: no lugar dele vai o status do NH de {AbilityLink(p, byRank.Ability)} de quem aplica ({ranks}); sem NH, o do NH1";
        }

        if (s.Redirect is { } r)
        {
            var reduction = r.Reduction > 0 ? $", que o recebe {Pct(r.Reduction)} menor" : "";
            yield return $"{Pct(r.Fraction)} do dano que receberia vai para quem aplicou o status{reduction}, se ele estiver vivo e a até {Num(r.MaxDistance)}";
        }

        if (s.Lifesteal is { } l)
        {
            yield return $"cura {Pct(l.Fraction)}{stacks} do dano que causa em outros" + LifestealExtras(l);
        }

        if (s.AttackerLifesteal is { } al)
        {
            yield return $"quem bate nele (aliados de quem aplicou, inclusive ele) cura {Pct(al.Fraction)} do dano causado" + LifestealExtras(al);
        }

        if (s.Stats is { } st)
        {
            foreach (var (key, value) in st.Attributes.OrderBy(kv => kv.Key == "all" ? "" : kv.Key, StringComparer.Ordinal))
            {
                var what = key == "all" ? "todos os atributos" : ScalingLabel(key);
                yield return $"+<b>{Scaled(value.Base, value.Scaling)}</b>{stacks} em {what}{FromCaster(value)}";
            }

            if (st.ManaRegen is { } mana)
            {
                yield return $"+<b>{Scaled(mana.Base, mana.Scaling)}</b>{stacks} de Mana por segundo{FromCaster(mana)}";
            }
        }

        if (s.Crit is { } c)
        {
            var range = c.MinDistance > 0 ? $" contra alvos a {Num(c.MinDistance)} tiles ou mais" : "";
            if (c.Guaranteed)
            {
                yield return $"todo dano de habilidade e do ataque básico é crítico{range}";
            }

            if (c.Bonus > 0)
            {
                yield return $"+{Pct(c.Bonus)}{stacks} de chance de crítico nas habilidades e no ataque básico{range}";
            }

            if (c.Multiplier is { } m)
            {
                yield return $"crítico {Mult(m)} em vez de {Mult(_db.Rules.Stats.CritMultiplier)}{range}";
            }
        }

        if (s.RangeBonus != 0)
        {
            yield return $"+{Num(s.RangeBonus)}{stacks} de alcance nas habilidades à distância";
        }

        if (s.BasicAttack is { } basic)
        {
            yield return $"o ataque básico vira {AbilityLink(p, basic)}";
        }

        if (s.HitEffects.Count > 0)
        {
            yield return "a cada acerto de habilidade ou do ataque básico, no alvo: " + string.Join("; ", s.HitEffects.Select(e => Effect(p, e)));
        }

        if (s.OnExpire.Count > 0)
        {
            yield return "ao acabar pelo tempo (não se for tirado nem se quem o tem morrer): " + string.Join("; ", s.OnExpire.Select(e => Effect(p, e)));
        }

        if (s.JumpOnExpire is { } jump)
        {
            yield return $"quando um status de {TagsOr(jump.Tags)} aplicado por quem tem este status acaba pelo tempo num inimigo, ele pula, "
                + $"com todos os acúmulos, para o inimigo mais perto a até {Num(jump.Radius)} que ainda não tem nenhum desses status";
        }

        if (s.Exclusive is not null)
        {
            yield return "não convive com as outras variantes do mesmo grupo: fica a mais forte, com a maior duração";
        }
    }

    private static string FromCaster(ScaledValueDefinition v) =>
        v.Scaling.Count > 0 ? " <span class=\"muted\">(pelos atributos de quem aplicou, calculado ao aplicar)</span>" : "";

    private static string LifestealExtras(LifestealDefinition l)
    {
        var text = "";
        if (l.BelowHealth > 0)
        {
            text += $"; com menos de {Pct(l.BelowHealth)} da vida, essa parte {Mult(l.BelowHealthMultiplier)}";
        }

        if (l.KillHeal > 0)
        {
            text += $"; ao matar um inimigo, cura também {Pct(l.KillHeal)} da vida máxima";
        }

        return text;
    }
}
