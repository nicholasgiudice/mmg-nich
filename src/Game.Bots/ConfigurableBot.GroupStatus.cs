using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Bots;

/// <summary>
/// T-423: quando usar habilidades que só aplicam estados das mecânicas da segunda classe do Clérigo (T-421, T-422). O tipo
/// sai dos estados que a habilidade aplica, não do id:
/// <list type="bullet">
/// <item><b>Emergência</b> (estado com vida mínima de 1, Santuário): um aliado no raio (ou o próprio) com até
/// <c>emergencyBelow</c> (0,3) de vida.</item>
/// <item><b>Ataque básico trocado</b> (Ira Divina): <c>minAoe</c> (2) inimigos ou mais no raio do golpe em volta do alvo.</item>
/// <item><b>Cura por segundo</b> (Oração Restauradora): um aliado no raio (ou o próprio) com até <c>healBelow</c> (0,75).</item>
/// <item><b>Reforço do grupo</b> (Bênção, Inspiração, Graça Veloz: atributos, Mana por segundo, velocidade em aliados): com um
/// inimigo a até <c>buffRange</c> (10) tiles, alguém que a habilidade alcança ainda sem o estado. Usa no começo da luta e
/// de novo quando o estado acaba.</item>
/// <item><b>Marca</b> (Selo da Luz: vida pelo dano para quem bate): no alvo do bot (o do grupo), se ainda não tem o estado.</item>
/// </list>
/// As outras habilidades seguem o papel de <see cref="AbilityInfo"/>.
/// </summary>
public sealed partial class ConfigurableBot
{
    private enum StatusPlanKind
    {
        None,
        Emergency,
        BasicAttackSwap,
        HealOverTime,
        GroupBuff,
        Mark,
    }

    /// <summary>O que a habilidade aplica: estados em quem usa, estados nos aliados em volta (raio) e o tipo.</summary>
    private sealed record StatusPlan(StatusPlanKind Kind, IReadOnlyList<string> OnSelf, IReadOnlyList<string> OnAllies, double Radius, double SwapRadius)
    {
        public IEnumerable<string> All => OnSelf.Concat(OnAllies);
    }

    private readonly Dictionary<AbilityDefinition, StatusPlan> _statusPlans = new(ReferenceEqualityComparer.Instance);

    private StatusPlan StatusPlanOf(BotContext ctx, AbilityInfo info)
    {
        if (!_statusPlans.TryGetValue(info.Definition, out var plan))
        {
            plan = BuildStatusPlan(ctx.World.Content, info);
            _statusPlans[info.Definition] = plan;
        }

        return plan;
    }

    private static StatusPlan BuildStatusPlan(ContentCatalog content, AbilityInfo info)
    {
        var none = new StatusPlan(StatusPlanKind.None, [], [], 0, 0);
        var def = info.Definition;
        if (info.DamageTotal > 0)
        {
            return none;
        }

        StatusDefinition? Status(string id) => content.Statuses.TryGetValue(id, out var s) ? s : null;

        if (def.Targeting is UnitTargeting { Faction: FactionFilter.Enemy })
        {
            var marks = def.Effects.OfType<ApplyStatusEffect>().Where(e => e.Target == EffectTarget.Targets).Select(e => e.Status).ToList();
            return marks.Any(id => Status(id)?.AttackerLifesteal is not null)
                ? new StatusPlan(StatusPlanKind.Mark, [], marks, 0, 0)
                : none;
        }

        if (def.Targeting is not SelfTargeting)
        {
            return none;
        }

        var onSelf = def.Effects.OfType<ApplyStatusEffect>().Select(e => e.Status).ToList();
        var extras = def.Effects.OfType<ExtraTargetsEffect>().Where(x => x.Affects == FactionFilter.Ally).ToList();
        var onAllies = extras.SelectMany(x => x.Effects.OfType<ApplyStatusEffect>()).Select(e => e.Status).Distinct().ToList();
        var radius = extras.Select(x => x.Radius).DefaultIfEmpty(0).Max();
        var statuses = onSelf.Concat(onAllies).Select(Status).OfType<StatusDefinition>().ToList();

        if (statuses.Any(s => s.Flags.Undying))
        {
            return new StatusPlan(StatusPlanKind.Emergency, onSelf, onAllies, radius, 0);
        }

        if (statuses.FirstOrDefault(s => s.BasicAttack is not null) is { } swap)
        {
            var strike = content.Abilities.TryGetValue(swap.BasicAttack!, out var a) ? a : null;
            var swapRadius = strike?.Effects.OfType<ExtraTargetsEffect>().Select(x => x.Radius).DefaultIfEmpty(0).Max() ?? 0;
            return new StatusPlan(StatusPlanKind.BasicAttackSwap, onSelf, onAllies, radius, swapRadius);
        }

        if (statuses.Any(s => s.Periodic.Any(p => p.Effects.OfType<HealEffect>().Any())))
        {
            return new StatusPlan(StatusPlanKind.HealOverTime, onSelf, onAllies, radius, 0);
        }

        var buff = statuses.Any(s => s.Stats is not null)
            || (onAllies.Count > 0 && statuses.Any(s => s.Modifiers.SpeedMultiplier > 1));
        return buff ? new StatusPlan(StatusPlanKind.GroupBuff, onSelf, onAllies, radius, 0) : none;
    }

    /// <summary>Null se a habilidade não é de um dos tipos acima (segue o papel); senão, se deve ser usada agora e em quem.</summary>
    private bool? PlanStatusAbility(BotContext ctx, AbilityInfo info, List<Entity> enemies, Entity? focus, out EntityId? target)
    {
        target = null;
        var plan = StatusPlanOf(ctx, info);
        if (plan.Kind == StatusPlanKind.None)
        {
            return null;
        }

        if (enemies.Count == 0)
        {
            return false;
        }

        var cls = ctx.ClassId;
        switch (plan.Kind)
        {
            case StatusPlanKind.Mark:
                {
                    var reach = info.Reach;
                    var mark = Prioritize(focus, enemies).FirstOrDefault(e => ctx.EdgeDistanceTo(e) <= reach
                        && !plan.OnAllies.Any(id => StatusManager.Has(ctx.World, e, id)));
                    target = mark?.Id;
                    return mark is not null;
                }

            case StatusPlanKind.Emergency:
                {
                    var below = (float)ctx.Options.GetDouble(cls, "emergencyBelow", 0.3);
                    var falling = Recipients(ctx, plan).Any(a => BotContext.HealthOf(a) <= below);
                    return _reactor.Ready(ctx, info.Id, falling);
                }

            case StatusPlanKind.HealOverTime:
                {
                    var below = (float)ctx.Options.GetDouble(cls, "healBelow", 0.75);
                    var injured = Recipients(ctx, plan).Any(a => BotContext.HealthOf(a) <= below);
                    return _reactor.Ready(ctx, info.Id, injured);
                }

            case StatusPlanKind.BasicAttackSwap:
                {
                    var center = focus ?? enemies[0];
                    var minAoe = (int)ctx.Options.GetDouble(cls, "minAoe", 2);
                    var p = BotContext.PositionOf(center);
                    var around = enemies.Count(o => Vector2.Distance(p, BotContext.PositionOf(o)) <= plan.SwapRadius + BotContext.RadiusOf(o));
                    return ctx.EdgeDistanceTo(center) <= ReachOfBasic(ctx) && around >= minAoe;
                }

            default: // GroupBuff
                {
                    if (ctx.EdgeDistanceTo(enemies[0]) > ctx.Options.GetDouble(cls, "buffRange", 10))
                    {
                        return false;
                    }

                    var ids = plan.All.ToList();
                    return Recipients(ctx, plan).Any(a => !ids.Any(id => StatusManager.Has(ctx.World, a, id)));
                }
        }
    }

    /// <summary>Quem a habilidade alcança: o próprio (se ela aplica estado nele) e os outros aliados no raio.</summary>
    private static IEnumerable<Entity> Recipients(BotContext ctx, StatusPlan plan)
    {
        foreach (var a in ctx.Allies())
        {
            if (a.Id == ctx.Self.Id)
            {
                if (plan.OnSelf.Count > 0)
                {
                    yield return a;
                }
            }
            else if (plan.OnAllies.Count > 0 && ctx.EdgeDistanceTo(a) <= plan.Radius)
            {
                yield return a;
            }
        }
    }
}
