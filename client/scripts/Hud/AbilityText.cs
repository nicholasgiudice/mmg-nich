using System.Globalization;
using System.Text;
using Game.Core.Combat;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Texto de uma habilidade num NH (T-064), gerado da definição já resolvida (<c>ContentCatalog.AbilityAt</c>):
/// custo, recarga, conjuração, acertos e efeitos, um efeito por linha, em frases ("Causa 51 de dano físico"). Os estados
/// aplicados vêm explicados ("Atordoado por 1 s: não age"). Usado no tooltip da barra e na tela de habilidades. Com as
/// estatísticas do personagem (T-221), dano, cura e escudo aparecem já escalados (base + escalas, sem crítico nem bônus
/// de alvo); sem elas, a base e as escalas ("60 + 2 × VIT"). A frase do que a habilidade faz vem de
/// <see cref="Summary"/> (campo <c>description</c> dos dados). Sem Godot, para poder ser testado.
/// </summary>
public static class AbilityText
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Largura, em caracteres, da quebra de linha dos tooltips (o tooltip do Godot não quebra sozinho).</summary>
    public const int TooltipWidth = 54;

    /// <summary>O que a habilidade faz, escrito para o jogador (campo <c>description</c>); vazio se não houver.</summary>
    public static string Summary(AbilityDefinition a) => a.Description?.Trim() ?? "";

    /// <summary><see cref="Summary"/> quebrado em linhas de até <paramref name="width"/> caracteres, para tooltip.</summary>
    public static string WrappedSummary(AbilityDefinition a, int width = TooltipWidth) => Wrap(Summary(a), width);

    /// <summary>
    /// Linhas de custo/tempo e efeitos; <paramref name="stats"/> null = valores base dos dados. <paramref name="rankOf"/> dá o
    /// NH do personagem em outras habilidades, para os estados cuja força vem do NH de outra (Veneno do Caçador pela Flecha
    /// Envenenada, Marca Arcana do Sifão); null = NH1. O NH da própria <paramref name="a"/> sai da definição resolvida.
    /// </summary>
    public static string Describe(AbilityDefinition a, ContentCatalog content, CharacterStats? stats = null, Func<string, int>? rankOf = null)
    {
        var ctx = new Ctx(content, stats, a, rankOf);
        var lines = new List<string>();
        var timing = new List<string>();
        if (a.Passive)
        {
            timing.Add("passiva: sempre ativa, não vai para a barra");
        }

        if (a.Cost is { } cost && cost.Amount > 0)
        {
            timing.Add($"custo {cost.Amount.ToString("0", Pt)} de {ResourceName(cost.Resource)}");
        }

        if (a.CooldownTicks > 0)
        {
            timing.Add($"recarga {Seconds(a.CooldownTicks)}" + (a.CooldownCharges > 1 ? $" ({a.CooldownCharges} usos guardados)" : ""));
        }

        if (a.CastTimeTicks > 0)
        {
            timing.Add($"conjuração {Seconds(a.CastTimeTicks)}" + (a.CastWhileMoving ? " (dá para andar)" : " (parado)"));
        }
        else if (!a.Passive && a.Channel is null)
        {
            timing.Add("instantânea");
        }

        if (timing.Count > 0)
        {
            lines.Add(Capitalize(string.Join(", ", timing)));
        }

        if (a.Channel is { } ch)
        {
            var parts = new List<string> { $"canalizada: {ch.Pulses} pulsos, um a cada {Seconds(ch.IntervalTicks)}" };
            if (ch.PulseGrowth > 0)
            {
                parts.Add($"cada pulso causa {Percent(ch.PulseGrowth)} a mais que o anterior");
            }

            parts.Add(ch.MoveSpeedMultiplier is { } mv ? $"dá para andar a {Percent(mv)} da velocidade" : "andar cancela");
            lines.Add(Capitalize(string.Join(", ", parts)));
        }

        if (a.Telegraph is { } tel)
        {
            lines.Add($"Aviso no chão: o efeito cai {Seconds(tel.DelayTicks)} depois");
        }

        var onSelf = a.Targeting is SelfTargeting;
        if (a.HitCount > 1)
        {
            var interval = a.Hits!.IntervalTicks > 0 ? $", um a cada {Seconds(a.Hits.IntervalTicks)}" : "";
            lines.Add($"{a.HitCount} acertos{interval}; cada acerto:");
        }

        lines.AddRange(EffectLines(a.Effects, ctx, onSelf));
        if (a.Passive && a.PassiveStatus is { } passive && content.Statuses.TryGetValue(passive, out var ps))
        {
            var (status, note) = ByRank(ps, ctx);
            var what = StatusTraits(status, ctx) + note;
            if (what.Length > 0)
            {
                lines.Add(Capitalize(what));
            }
        }

        if (a.Channel is { OnComplete.Count: > 0 } done)
        {
            lines.Add("Se canalizar até o fim: " + Join(done.OnComplete, ctx, onSelf: false));
        }

        return string.Join("\n", lines);
    }

    /// <summary>Alcance e área em tiles, para a dica da tela de habilidades (T-081): "alcance 6 · raio 2,5"; vazio se em si mesmo.</summary>
    public static string Reach(AbilityDefinition a) => a.Targeting switch
    {
        UnitTargeting u => $"alcance {u.Range.ToString("0.#", Pt)}",
        MeleeArcTargeting m => $"corpo a corpo ({m.Range.ToString("0.#", Pt)}, arco de {m.Angle.ToString("0", Pt)}°)",
        LineTargeting { ExplodeRadius: { } boom } l =>
            $"linha de {l.Length.ToString("0.#", Pt)} × {l.Width.ToString("0.#", Pt)}, explode no primeiro inimigo (raio {boom.ToString("0.#", Pt)})",
        LineTargeting l => $"linha de {l.Length.ToString("0.#", Pt)} × {l.Width.ToString("0.#", Pt)}",
        CircleAtPointTargeting c => $"alcance {c.Range.ToString("0.#", Pt)} · raio {c.Radius.ToString("0.#", Pt)}",
        CircleSelfTargeting c => $"em volta de si · raio {c.Radius.ToString("0.#", Pt)}",
        _ => "",
    };

    /// <summary>
    /// <see cref="Reach"/> (com maiúscula) seguido de <see cref="Describe"/>: o texto inteiro de um NH, como na barra e na
    /// tela de habilidades.
    /// </summary>
    public static string Body(AbilityDefinition a, ContentCatalog content, CharacterStats? stats = null, Func<string, int>? rankOf = null) =>
        (Reach(a) is { Length: > 0 } reach ? Capitalize(reach) + "\n" : "") + Describe(a, content, stats, rankOf);

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

    /// <summary>Nome do recurso para o jogador ("Mana"); aceita o id com ou sem o prefixo <c>resource.</c>.</summary>
    public static string ResourceName(string type) =>
        (type.StartsWith("resource.", StringComparison.Ordinal) ? type["resource.".Length..] : type) switch
        {
            "vigor" => "Vigor",
            "focus" => "Foco",
            "mana" => "Mana",
            "faith" => "Fé",
            var other => other,
        };

    /// <summary>Quebra <paramref name="text"/> em linhas de até <paramref name="width"/> caracteres, nas palavras.</summary>
    public static string Wrap(string text, int width = TooltipWidth)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            var line = new StringBuilder();
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > width)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                }

                line.Append(line.Length > 0 ? " " : "").Append(word);
            }

            lines.Add(line.ToString());
        }

        return string.Join("\n", lines);
    }

    /// <summary>O que o texto precisa saber além do efeito: conteúdo, estatísticas, a habilidade descrita e os NH do personagem.</summary>
    private sealed record Ctx(ContentCatalog Content, CharacterStats? Stats, AbilityDefinition Ability, Func<string, int>? RankOf)
    {
        /// <summary>
        /// NH do personagem em <paramref name="abilityId"/>, e se ele é conhecido. Para a habilidade descrita, o NH que está
        /// sendo mostrado (a definição resolvida é a mesma instância do catálogo); para as outras, <see cref="RankOf"/>.
        /// </summary>
        public (int Rank, bool Known) RankFor(string abilityId)
        {
            if (abilityId == Ability.Id && Content.Abilities.TryGetValue(abilityId, out var baseDef))
            {
                for (var r = 1; r <= baseDef.HighestRank; r++)
                {
                    if (ReferenceEquals(Content.AbilityAt(abilityId, r), Ability))
                    {
                        return (r, true);
                    }
                }
            }

            return RankOf is null ? (1, false) : (Math.Max(1, RankOf(abilityId)), true);
        }
    }

    /// <summary>Um efeito por linha, começando com maiúscula.</summary>
    private static IEnumerable<string> EffectLines(IEnumerable<EffectDefinition> effects, Ctx ctx, bool onSelf) =>
        effects.Select(e => Effect(e, ctx, onSelf)).Where(t => t.Length > 0).Select(Capitalize);

    /// <summary>Efeitos aninhados (saltos, alvos extras, áreas) numa linha só.</summary>
    private static string Join(IEnumerable<EffectDefinition> effects, Ctx ctx, bool onSelf) =>
        string.Join("; ", effects.Select(e => Effect(e, ctx, onSelf)).Where(t => t.Length > 0));

    /// <summary>
    /// Frase de um efeito, em minúscula. <paramref name="onSelf"/>: a habilidade é em si mesmo, então o alvo do efeito é
    /// quem usa ("você ganha Fúria" em vez de "aplica Fúria").
    /// </summary>
    private static string Effect(EffectDefinition e, Ctx ctx, bool onSelf)
    {
        var self = onSelf || e.Target == EffectTarget.Self;
        var inYou = e.Target == EffectTarget.Self && !onSelf ? " em você" : "";
        var onHit = e.OnHit is { } h ? $" (só no {h}º acerto)" : "";
        var stats = ctx.Stats;
        var text = e switch
        {
            DamageEffect d => Damage(d, stats),
            HealEffect h2 => Heal(h2, stats) + inYou,
            ShieldEffect s => $"escudo de {Value(s.Amount, s.Scaling, stats)} por {Seconds(s.DurationTicks)}{inYou} (absorve dano antes da vida)"
                + (s.Refund is { } r ? $"; o que sobrar volta {Percent(r.Fraction)} como {ResourceName(r.Resource)}" : ""),
            ApplyStatusEffect st => ApplyStatus(st, ctx, self),
            RemoveStatusEffect rm => (rm.Count == 0 ? "tira todos os efeitos de " : rm.Count == 1 ? "tira um efeito de " : $"tira {rm.Count} efeitos de ")
                + TagList(rm.Tags.Count > 0 ? rm.Tags : [rm.Tag], rm.Count == 0 ? "e" : "ou") + (e.Target == EffectTarget.Self || onSelf ? " de você" : " do alvo"),
            DisplacementEffect m => Displacement(m),
            InterruptEffect => "interrompe a conjuração do alvo",
            ThreatEffect t => Threat(t),
            SpawnAreaEffect sa => ctx.Content.Areas.TryGetValue(sa.Area, out var area) ? Area(area, ctx) : "deixa uma área no chão",
            ChainEffect c => $"salta para {c.Jumps} {Side(c.Affects, c.Jumps)} a até {Number(c.Radius)} tiles, com {Percent(c.Multiplier)} do efeito: {Join(c.Effects, ctx, false)}",
            ExtraTargetsEffect x => $"também {ExtraWho(x)}{(x.Multiplier != 1 ? $" ({Percent(x.Multiplier)})" : "")}: {Join(x.Effects, ctx, false)}",
            ReviveEffect rv => $"levanta {rv.Count} aliado caído a até {Number(rv.Radius)} tiles com {Percent(rv.HealthFraction)} da vida",
            ResourceEffect res => $"recupera {Number(res.Amount)} de {ResourceName(res.Resource)}",
            SummonEffect su => $"invoca {su.Count} {(ctx.Content.Monsters.TryGetValue(su.Monster, out var mon) ? mon.Name : su.Monster)}"
                + (su.MaxAlive > 0 ? $" (até {su.MaxAlive} ao mesmo tempo)" : ""),
            CopyStatusEffect cp => $"copia {TagList(cp.Tags, "e")} do alvo para "
                + (cp.Count == 0 ? $"todos os {Side(cp.Affects, 2)}" : cp.Count == 1 ? $"1 {Side(cp.Affects, 1)}" : $"até {cp.Count} {Side(cp.Affects, 2)}")
                + $" a até {Number(cp.Radius)} tiles",
            IfStatusEffect ifs => $"em cada inimigo com {TagList(ifs.Tags, "ou")}: {Join(ifs.Effects, ctx, false)}"
                + (ifs.Consume ? $" (e tira {TagList(ifs.Tags, "e")})" : ""),
            CurrentTargetEffect ct => (ct.Count == 1 ? "1 disparo" : $"{ct.Count} disparos")
                + $" no seu alvo atual a até {Number(ct.Range)} tiles, cada um: {Join(ct.Effects, ctx, false)}",
            _ => "",
        };
        return text.Length == 0 ? "" : text + onHit;
    }

    private static string Heal(HealEffect h, CharacterStats? stats)
    {
        var fixedPart = h.Base != 0 || h.Scaling.Count > 0 || h.MaxHealthFraction == 0 ? Value(h.Base, h.Scaling, stats) : "";
        var fraction = h.MaxHealthFraction > 0 ? $"{Percent(h.MaxHealthFraction)} da vida máxima" : "";
        return fixedPart.Length > 0 && fraction.Length > 0 ? $"cura {fixedPart} de vida + {fraction}"
            : fraction.Length > 0 ? $"cura {fraction}"
            : $"cura {fixedPart} de vida";
    }

    private static string Damage(DamageEffect d, CharacterStats? stats)
    {
        var parts = new List<string> { $"causa {Value(d.Base, d.Scaling, stats)} de dano {SchoolName(d.School)}" };
        foreach (var (tag, bonus) in d.BonusVsTags.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            parts.Add($"+{Percent(bonus)} contra {TagName(tag)}");
        }

        if (d.LowHealthBonus is { } low)
        {
            parts.Add($"+{Percent(low.Bonus)} em alvo com menos de {Percent(low.BelowHealth)} da vida");
        }

        if (d.MovingBonus > 0)
        {
            parts.Add($"+{Percent(d.MovingBonus)} se o alvo estiver andando");
        }

        if (d.IgnoreArmor > 0)
        {
            parts.Add($"ignora {Percent(d.IgnoreArmor)} da armadura");
        }

        if (d.BehindFraction is { } behind)
        {
            parts.Add($"quem está atrás na linha recebe {Percent(behind)}");
        }

        if (d.ThreatMultiplier is { } tm && tm != 1)
        {
            parts.Add($"gera {Number(tm)}× a ameaça normal");
        }

        if (d.SelfHeal > 0)
        {
            parts.Add($"você cura {Percent(d.SelfHeal)} do dano");
        }

        if (d.AllyHeal is { } ally)
        {
            parts.Add($"{(ally.IncludeSelf ? "você e os aliados" : "os aliados")} a até {Number(ally.Radius)} tiles curam {Percent(ally.Fraction)} do dano");
        }

        if (d.ResetCooldownOnKill)
        {
            parts.Add("se matar, a recarga zera");
        }

        return string.Join(", ", parts);
    }

    private static string ApplyStatus(ApplyStatusEffect st, Ctx ctx, bool self)
    {
        if (!ctx.Content.Statuses.TryGetValue(st.Status, out var declared))
        {
            return st.Status;
        }

        var (s, note) = ByRank(declared, ctx);
        var duration = st.DurationTicks ?? s.DefaultDurationTicks;
        var stacks = st.Stacks > 1 ? $"{st.Stacks} pilhas de " : "";
        var head = (self ? "você ganha " : "aplica ") + stacks + s.Name + $" por {Seconds(duration)}";
        var traits = StatusTraits(s, ctx) + note;
        return traits.Length > 0 ? $"{head} ({traits})" : head;
    }

    /// <summary>
    /// O status que vale de fato quando a força vem do NH de outra habilidade (<c>byRank</c>), e a nota que diz qual NH foi
    /// usado ("; pelo NH II de Flecha Envenenada"); sem <c>byRank</c>, o próprio status e nota vazia.
    /// </summary>
    private static (StatusDefinition Status, string Note) ByRank(StatusDefinition s, Ctx ctx)
    {
        if (s.ByRank is not { Statuses.Count: > 0 } br)
        {
            return (s, "");
        }

        var (rank, known) = ctx.RankFor(br.Ability);
        var id = br.Statuses[Math.Clamp(rank, 1, br.Statuses.Count) - 1];
        if (!ctx.Content.Statuses.TryGetValue(id, out var resolved))
        {
            return (s, "");
        }

        if (br.Ability == ctx.Ability.Id)
        {
            return (resolved, "");
        }

        var name = ctx.Content.Abilities.TryGetValue(br.Ability, out var source) ? source.Name : br.Ability;
        return (resolved, known ? $"; pelo seu NH {Roman(rank)} de {name}" : $"; com {name} no NH I, mais forte nos NH maiores");
    }

    /// <summary>
    /// O que um estado faz, em poucas palavras ("não age", "recebe 25% menos dano"). Cobre as peças comuns; mecânicas
    /// muito próprias de uma habilidade ficam para a descrição dela.
    /// </summary>
    private static string StatusTraits(StatusDefinition s, Ctx ctx)
    {
        var stats = ctx.Stats;
        var t = new List<string>();
        var f = s.Flags;
        if (f.Stunned) { t.Add("não age"); }
        if (f.Feared) { t.Add("foge com medo"); }
        if (f.Rooted) { t.Add("não consegue andar"); }
        if (f.Silenced) { t.Add("não usa habilidades"); }
        if (f.Untargetable) { t.Add("não pode ser alvo"); }
        if (f.KnockbackImmune) { t.Add("não é empurrado"); }
        if (f.Undying) { t.Add("não cai abaixo de 1 de vida"); }

        var m = s.Modifiers;
        if (s.Frontal is { } fr)
        {
            t.Add($"recebe {Percent(1 - fr.DamageTakenMultiplier)} menos dano de frente");
            if (fr.ReflectMeleeFraction > 0)
            {
                t.Add($"quem bate de frente corpo a corpo leva de volta {Percent(fr.ReflectMeleeFraction)} do dano que a guarda segurou");
            }
        }

        if (Differs(m.DamageDealtMultiplier)) { t.Add($"causa {MoreLess(m.DamageDealtMultiplier)} dano"); }
        if (Differs(m.SpeedMultiplier)) { t.Add($"anda {Percent(Math.Abs(1 - m.SpeedMultiplier))} mais {(m.SpeedMultiplier < 1 ? "devagar" : "rápido")}"); }
        if (Differs(m.DamageTakenMultiplier)) { t.Add($"recebe {MoreLess(m.DamageTakenMultiplier)} dano"); }
        foreach (var (school, mult) in m.DamageTakenBySchool.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (Differs(mult))
            {
                t.Add($"recebe {MoreLess(mult)} dano {SchoolName(school)}");
            }
        }

        if (Differs(m.HealingTakenMultiplier)) { t.Add($"recebe {MoreLess(m.HealingTakenMultiplier)} cura"); }
        if (Differs(m.ControlDurationMultiplier)) { t.Add($"controle dura {Percent(Math.Abs(1 - m.ControlDurationMultiplier))} {(m.ControlDurationMultiplier < 1 ? "menos" : "mais")}"); }

        foreach (var p in s.Periodic)
        {
            foreach (var e in p.Effects)
            {
                var every = p.IntervalTicks == World.TickRate ? "por segundo" : $"a cada {Seconds(p.IntervalTicks)}";
                switch (e)
                {
                    case DamageEffect d:
                        t.Add($"{Value(d.Base, d.Scaling, stats)} de dano {SchoolName(d.School)} {every}" + (d.MovingBonus > 0 ? $" (+{Percent(d.MovingBonus)} se o alvo andar)" : ""));
                        break;
                    case HealEffect h:
                        t.Add($"cura {Value(h.Base, h.Scaling, stats)} {every}");
                        break;
                    default:
                        if (Effect(e, ctx, onSelf: false) is { Length: > 0 } other)
                        {
                            t.Add($"{(p.IntervalTicks == World.TickRate ? "a cada segundo" : every)}, {other}");
                        }

                        break;
                }
            }
        }

        if (s.Stacking == StackingMode.Stack && s.MaxStacks > 1) { t.Add($"acumula até {s.MaxStacks}"); }
        if (s.Exclusive is not null) { t.Add($"não soma com outra {s.Name}: fica a mais forte"); }
        if (s.Lifesteal is { } ls)
        {
            t.Add($"recupera {Percent(ls.Fraction)} do dano causado como vida" + LifestealExtra(ls));
            if (ls.KillHeal > 0) { t.Add($"ao matar um inimigo, cura {Percent(ls.KillHeal)} da vida máxima"); }
        }

        if (s.AttackerLifesteal is { } al)
        {
            t.Add($"você e os aliados que baterem nele recuperam {Percent(al.Fraction)} do dano como vida" + LifestealExtra(al));
        }

        if (s.Redirect is { } rd)
        {
            t.Add($"{Percent(rd.Fraction)} do dano recebido vai para quem protege, se estiver a até {Number(rd.MaxDistance)} tiles"
                + (rd.Reduction > 0 ? $", que recebe {Percent(rd.Reduction)} menos" : ""));
        }

        if (s.Reflect is { } rf) { t.Add($"devolve {Percent(rf.Fraction)} do dano a quem bate"); }
        if (s.Breaks is { } br)
        {
            if (br.DamageFraction > 0) { t.Add($"quebra ao receber {Percent(br.DamageFraction)} da vida máxima em dano"); }
            if (br.Interrupt) { t.Add("quebra ao ser interrompido"); }
        }

        if (s.Crit is { } cr)
        {
            if (cr.Guaranteed) { t.Add("todo ataque é crítico"); }
            else if (cr.Bonus > 0) { t.Add($"+{Percent(cr.Bonus)} de chance de crítico" + (cr.MinDistance > 0 ? $" contra alvos a {Number(cr.MinDistance)} tiles ou mais" : "")); }
            if (cr.Multiplier is { } mult) { t.Add($"crítico causa {Percent(mult)}"); }
        }

        if (s.RangeBonus > 0) { t.Add($"+{Number(s.RangeBonus)} de alcance"); }
        if (s.Stats is { } ss)
        {
            foreach (var (key, v) in ss.Attributes.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                t.Add($"+{Value(v.Base, v.Scaling, stats)} {(key == "all" ? "em todos os atributos" : "de " + key.ToUpperInvariant())}");
            }

            if (ss.ManaRegen is { } mr) { t.Add($"+{Value(mr.Base, mr.Scaling, stats)} de Mana por segundo"); }
        }

        if (s.Empower is { } em)
        {
            var names = em.Abilities.Select(id => ctx.Content.Abilities.TryGetValue(id, out var a) ? a.Name : id).Distinct();
            t.Add($"o próximo uso de {string.Join(" ou ", names)} causa {Percent(em.Multiplier - 1)} a mais");
        }

        if (s.HitEffects.Count > 0) { t.Add($"todo acerto seu: {Join(s.HitEffects, ctx, onSelf: false)}"); }
        if (s.BasicAttack is { } basicId && ctx.Content.AbilityAt(basicId, 1) is { } basic && basic.Id != ctx.Ability.Id)
        {
            var reach = Reach(basic);
            t.Add($"seu ataque básico vira {basic.Name}{(reach.Length > 0 ? $", {reach}" : "")}: {Join(basic.Effects, ctx with { Ability = basic }, onSelf: false)}");
        }

        if (s.JumpOnExpire is { } jump)
        {
            t.Add($"quando {TagList(jump.Tags, "ou")} que você pôs acaba num inimigo, pula para o inimigo mais próximo a até {Number(jump.Radius)} tiles que não o tenha");
        }

        if (s.OnExpire.Count > 0) { t.Add($"ao acabar o tempo: {Join(s.OnExpire, ctx, onSelf: false)}"); }
        if (s.ImmuneToTags.Count > 0) { t.Add($"imune a {TagList(s.ImmuneToTags, "e")}"); }
        return string.Join(", ", t);

        static bool Differs(double multiplier) => Math.Abs(multiplier - 1) > 1e-9;
    }

    private static string LifestealExtra(LifestealDefinition ls) =>
        ls.BelowHealth > 0 ? $" ({Number(ls.BelowHealthMultiplier)}× com menos de {Percent(ls.BelowHealth)} da vida)" : "";

    private static string MoreLess(double multiplier) =>
        $"{Percent(Math.Abs(multiplier - 1))} {(multiplier < 1 ? "menos" : "mais")}";

    private static string Displacement(DisplacementEffect m) => (m.Kind, m.Direction) switch
    {
        (DisplacementKind.Dash, DisplacementDirection.TowardTarget) => $"avança até o alvo (até {Number(m.Distance)} tiles)",
        (DisplacementKind.Dash, DisplacementDirection.Backward) => $"salta {Number(m.Distance)} tiles para trás",
        (DisplacementKind.Dash, _) => $"salta {Number(m.Distance)} tiles na direção da mira",
        _ => $"empurra {Number(m.Distance)} tiles",
    };

    private static string Threat(ThreatEffect t)
    {
        var parts = new List<string>();
        if (t.TauntDurationTicks is { } taunt)
        {
            parts.Add($"obriga os monstros a atacar você por {Seconds(taunt)}");
        }

        if (t.Amount > 0)
        {
            parts.Add($"+{Number(t.Amount)} de ameaça");
        }

        return string.Join(", ", parts);
    }

    private static string Area(AreaDefinition area, Ctx ctx)
    {
        var where = area.FollowOwner ? $"cria em volta de você uma área de raio {Number(area.Radius)}" : $"deixa no chão uma área de raio {Number(area.Radius)}";
        var who = area.Affects == FactionFilter.Ally ? "nos aliados dentro" : area.Affects == FactionFilter.Any ? "em todos dentro" : "nos inimigos dentro";
        var effects = Join(area.Effects, ctx, false);
        var head = $"{where} por {Seconds(area.DurationTicks)}";
        if (area.ArmTimeTicks > 0)
        {
            head += $" (arma em {Seconds(area.ArmTimeTicks)})";
        }

        return area.Mode == AreaMode.OnEnterOnce
            ? $"{head}; no primeiro inimigo que pisar: {effects}"
            : $"{head}; {(area.IntervalTicks == World.TickRate ? "a cada segundo" : $"a cada {Seconds(Math.Max(1, area.IntervalTicks))}")}, {who}: {effects}";
    }

    private static string ExtraWho(ExtraTargetsEffect x)
    {
        var side = x.Affects == FactionFilter.Ally ? "aliado" : "inimigo";
        if (x.Count == 0)
        {
            return $"os {side}s a até {Number(x.Radius)} tiles";
        }

        var which = x.Select == TargetSelect.MostInjured ? "mais ferido" : "mais próximo";
        return x.Count == 1 ? $"o {side} {which}" : $"{x.Count} {side}s ({which})";
    }

    /// <summary>"inimigo(s)"/"aliado(s)" pela facção, no singular quando <paramref name="count"/> é 1.</summary>
    private static string Side(FactionFilter affects, int count) =>
        (affects == FactionFilter.Ally ? "aliado" : affects == FactionFilter.Any ? "alvo" : "inimigo") + (count == 1 ? "" : "s");

    /// <summary>Com estatísticas: o valor escalado; sem: a base e as escalas dos dados ("60 + 2 × VIT").</summary>
    private static string Value(double baseValue, IReadOnlyDictionary<string, double> scaling, CharacterStats? stats)
    {
        if (stats is not null || scaling.Count == 0)
        {
            return Number(Scaled(baseValue, scaling, stats));
        }

        var terms = scaling.Where(kv => kv.Value != 0).Select(kv => $"{Number(kv.Value)} × {StatName(kv.Key)}").ToList();
        if (baseValue != 0)
        {
            terms.Insert(0, Number(baseValue));
        }

        return terms.Count == 0 ? "0" : string.Join(" + ", terms);
    }

    private static string StatName(string key) => key switch
    {
        "weapon" => "ARMA",
        "str" => "FOR",
        "dex" => "DES",
        "agi" => "AGI",
        "int" => "INT",
        "vit" => "VIT",
        var other => other.ToUpperInvariant(),
    };

    private static string SchoolName(DamageSchool school) => school switch
    {
        DamageSchool.Physical => "físico",
        DamageSchool.Arcane => "arcano",
        DamageSchool.Fire => "de fogo",
        DamageSchool.Frost => "de gelo",
        DamageSchool.Holy => "sagrado",
        DamageSchool.Nature => "de natureza",
        _ => school.ToString().ToLowerInvariant(),
    };

    /// <summary>Escola pelo nome dos dados (snake_case, ex.: <c>holy</c>).</summary>
    private static string SchoolName(string key) =>
        Enum.TryParse<DamageSchool>(key.Replace("_", "", StringComparison.Ordinal), ignoreCase: true, out var school) ? SchoolName(school) : key;

    private static string TagList(IEnumerable<string> tags, string conjunction)
    {
        var names = tags.Where(t => t.Length > 0).Select(TagName).Distinct().ToList();
        return names.Count <= 1
            ? string.Concat(names)
            : string.Join(", ", names.Take(names.Count - 1)) + $" {conjunction} " + names[^1];
    }

    private static string TagName(string tag) => tag switch
    {
        "poison" => "veneno",
        "hunter_venom" => "Veneno do Caçador",
        "curse" => "maldição",
        "bleed" => "sangramento",
        "burn" => "queimadura",
        "stun" => "atordoamento",
        "root" => "prisão",
        "freeze" => "congelamento",
        "slow" => "lentidão",
        "control" => "controle",
        "debuff" => "efeitos negativos",
        "undead" => "mortos-vivos",
        "corrupted" => "corrompidos",
        "beast" => "feras",
        var other => other.Replace('_', ' '),
    };

    private static string Roman(int n) => n switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        _ => n.ToString(Pt),
    };

    private static string Seconds(int ticks) => (ticks / (double)World.TickRate).ToString("0.##", Pt) + " s";

    private static string Number(double v) => v.ToString("0.##", Pt);

    private static string Percent(double v) => (v * 100).ToString("0", Pt) + "%";

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0], Pt) + s[1..];
}
