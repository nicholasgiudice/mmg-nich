using System.Numerics;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Bots;

public enum MovementMode
{
    /// <summary>Fica parado.</summary>
    Stand,

    /// <summary>Persegue o alvo até o alcance e para.</summary>
    Chase,

    /// <summary>Mantém distância: recua quando o inimigo chega perto, aproxima quando longe, usa obstáculos.</summary>
    Kite,

    /// <summary>Aproxima-se do aliado mais ferido (curandeiro).</summary>
    SupportFollow,
}

/// <summary>Quais habilidades (além do ataque básico) o bot pode usar e se ignora as condições de uso.</summary>
public sealed record BotPlan(
    string Name,
    MovementMode Movement,
    Func<BotContext, IReadOnlyList<AbilityInfo>> Abilities,
    bool UseBasicAttack,
    bool ForceAbilities);

/// <summary>
/// Bot genérico guiado por dados: a estratégia é um <see cref="BotPlan"/> (modo de movimento + lista de habilidades permitidas).
/// As condições de uso de cada habilidade são deduzidas do papel (<see cref="AbilityRole"/>), não de ids.
/// </summary>
public sealed class ConfigurableBot(BotPlan plan) : IBotController
{
    private readonly Navigator _nav = new();
    private EntityId _focus = EntityId.None;
    private Vector2 _fleeDir;
    private bool _cornered;
    private readonly Reactor _reactor = new();
    private readonly Dictionary<EntityId, float> _allyHp = [];
    private long _allyHitTick = -1_000_000;

    private IReadOnlyList<AbilityInfo>? _allowed;

    public string Name => plan.Name;

    private IReadOnlyList<AbilityInfo> Allowed(BotContext ctx) => _allowed ??= plan.Abilities(ctx);

    public void Think(BotContext ctx, List<Intent> output)
    {
        TrackAllies(ctx);
        var abilities = ctx.Abilities;
        if (abilities.Cast is { } cast)
        {
            // Andar cancelaria a conjuração; conjurar andando (castWhileMoving) segue sem interferência.
            if (ctx.World.Content.Abilities.TryGetValue(cast.AbilityId, out var def) && !def.CastWhileMoving)
            {
                output.Add(new StopIntent(ctx.Self.Id));
            }

            return;
        }

        var enemies = ctx.Enemies();
        var focus = ChooseFocus(ctx, enemies);
        var allowed = Allowed(ctx);
        var fleeing = false;
        if (plan.Movement == MovementMode.Kite && enemies.Count > 0)
        {
            fleeing = ctx.EdgeDistanceTo(enemies[0]) < RetreatDistance(ctx);
        }

        // T-018: ainda não "viu" o novo alvo (tempo de reação): fica parado, a menos que esteja fugindo.
        if (focus is null && enemies.Count > 0 && !fleeing)
        {
            output.Add(new StopIntent(ctx.Self.Id));
            return;
        }

        var dashed = false;
        var used = TryPotion(ctx, output) || TryUseAbilities(ctx, allowed, enemies, focus, fleeing, output, out dashed);
        if (!used && plan.UseBasicAttack)
        {
            dashed = TryBasic(ctx, enemies, focus, fleeing, output);
        }

        if (dashed)
        {
            return;
        }

        Move(ctx, enemies, focus, fleeing, output);
    }

    // ---- foco ----

    private Entity? ChooseFocus(BotContext ctx, List<Entity> enemies)
    {
        if (enemies.Count == 0)
        {
            _focus = EntityId.None;
            return null;
        }

        var sticky = enemies.FirstOrDefault(e => e.Id == _focus);
        if (sticky is not null)
        {
            return sticky;
        }

        // Alvo novo (morte do anterior ou início do combate): só troca depois do tempo de reação.
        if (!_reactor.Ready(ctx, "focus", true))
        {
            return null;
        }

        _reactor.Reset("focus");
        _focus = enemies[0].Id;
        return enemies[0];
    }

    // ---- habilidades ----

    private bool TryUseAbilities(
        BotContext ctx, IReadOnlyList<AbilityInfo> allowed, List<Entity> enemies, Entity? focus, bool fleeing, List<Intent> output, out bool dashed)
    {
        dashed = false;
        foreach (var info in allowed)
        {
            // Fugindo: só instantâneas (conjurar parado faria o bot ser alcançado).
            if (fleeing && !_cornered && info.NeedsStanding)
            {
                continue;
            }

            if (!Plan(ctx, info, enemies, focus, fleeing, out var target, out var aim))
            {
                continue;
            }

            if (ctx.TryUse(info, target, aim, output))
            {
                _reactor.Reset(info.Id);
                dashed = info.Role == AbilityRole.Dash || info.NeedsStanding;
                return true;
            }
        }

        return false;
    }

    /// <summary>Tenta o ataque básico; retorna true se o bot deve ficar parado neste tick (conjuração).</summary>
    private bool TryBasic(BotContext ctx, List<Entity> enemies, Entity? focus, bool fleeing, List<Intent> output)
    {
        var id = ctx.Abilities.BasicAttackId;
        if (id is null || !ctx.World.Content.Abilities.TryGetValue(id, out var def))
        {
            return false;
        }

        var info = AbilityInfo.For(def);
        if (info.TargetsAllies || info.Role is AbilityRole.Heal or AbilityRole.Defensive || (fleeing && !_cornered && info.NeedsStanding))
        {
            return false;
        }

        foreach (var e in Prioritize(focus, enemies))
        {
            var aim = info.Targeting is CircleAtPointTargeting ? BotContext.PositionOf(e) : (Vector2?)null;
            if (ctx.TryUse(info, e.Id, aim, output))
            {
                return info.NeedsStanding;
            }

            if (!info.NeedsUnitTarget && info.Targeting is not CircleAtPointTargeting)
            {
                break; // habilidades de área/arco não dependem de qual alvo: uma tentativa basta
            }
        }

        return false;
    }

    private static IEnumerable<Entity> Prioritize(Entity? focus, List<Entity> enemies)
    {
        if (focus is not null)
        {
            yield return focus;
        }

        foreach (var e in enemies)
        {
            if (e != focus)
            {
                yield return e;
            }
        }
    }

    /// <summary>Decide se a habilidade deve ser usada agora e contra quem/onde. Condições simples por papel; <c>ForceAbilities</c> ignora-as.</summary>
    private bool Plan(
        BotContext ctx, AbilityInfo info, List<Entity> enemies, Entity? focus, bool fleeing, out EntityId? target, out Vector2? aim)
    {
        target = null;
        aim = null;
        var force = plan.ForceAbilities;
        var cls = ctx.ClassId;
        var nearest = enemies.Count > 0 ? enemies[0] : null;

        switch (info.Role)
        {
            case AbilityRole.Heal:
                return PlanHeal(ctx, info, cls, out target);

            case AbilityRole.Cleanse:
                return PlanCleanse(ctx, info, out target);

            case AbilityRole.Taunt:
                {
                    if (nearest is null)
                    {
                        return false;
                    }

                    // Aliado sob ataque (perdeu vida há pouco) ou, sozinho, vários inimigos. Reage depois do tempo de reação.
                    var allyHit = ctx.World.Tick - _allyHitTick <= 2 * World.TickRate;
                    var group = allyHit || (enemies.Count >= 2 && ctx.Allies().Count <= 1);
                    if (!_reactor.Ready(ctx, info.Id, group || force))
                    {
                        return false;
                    }

                    target = Prioritize(focus, enemies).First().Id;
                    return true;
                }

            case AbilityRole.Defensive:
                {
                    if (nearest is null || (!force && ctx.EdgeDistanceTo(nearest) > 6))
                    {
                        return false;
                    }

                    if (info.NeedsUnitTarget)
                    {
                        target = ctx.Self.Id;
                    }

                    return true;
                }

            case AbilityRole.Interrupt:
                {
                    var casting = enemies.FirstOrDefault(e => e.TryGet<Game.Core.Combat.AbilityState>() is { IsCasting: true });
                    if (casting is null)
                    {
                        return false;
                    }

                    target = casting.Id;
                    return true;
                }

            case AbilityRole.Dash:
                return PlanDash(ctx, info, nearest, focus, fleeing, force, out aim);

            case AbilityRole.Damage:
                return PlanDamage(ctx, info, enemies, focus, cls, out target, out aim);

            default:
                {
                    if (nearest is null || (!force && ctx.EdgeDistanceTo(nearest) > Math.Max(info.Reach, 3)))
                    {
                        return false;
                    }

                    if (info.NeedsUnitTarget)
                    {
                        target = (info.TargetsAllies ? ctx.Self : nearest).Id;
                    }
                    else if (info.Targeting is CircleAtPointTargeting)
                    {
                        aim = BotContext.PositionOf(nearest);
                    }

                    return true;
                }
        }
    }

    private bool PlanHeal(BotContext ctx, AbilityInfo info, string cls, out EntityId? target)
    {
        target = null;
        var threshold = (float)ctx.Options.GetDouble(cls, "healBelow", 0.75);
        var injured = ctx.Allies()
            .Where(a => BotContext.HealthOf(a) <= threshold)
            .OrderBy(BotContext.HealthOf)
            .ThenBy(a => a.Id.Value)
            .ToList();
        if (!_reactor.Ready(ctx, info.Id, injured.Count > 0))
        {
            return false;
        }

        if (info.NeedsUnitTarget)
        {
            // O mais ferido ao alcance (a validação de alcance faz o resto).
            target = injured[0].Id;
            return true;
        }

        // Cura própria/em área centrada em si: vale se alguém (ou o próprio) perto está ferido.
        return injured.Any(a => a.Id == ctx.Self.Id || ctx.EdgeDistanceTo(a) <= Math.Max(info.AreaRadius, 0.5));
    }

    /// <summary>Purificação: aliado (ou o próprio) com status de uma das tags removíveis; o mais ferido primeiro.</summary>
    private bool PlanCleanse(BotContext ctx, AbilityInfo info, out EntityId? target)
    {
        target = null;
        var afflicted = ctx.Allies()
            .Where(a => info.CleanseTags.Any(t => StatusManager.HasTag(ctx.World, a, t)))
            .OrderBy(BotContext.HealthOf)
            .ThenBy(a => a.Id.Value)
            .FirstOrDefault();
        if (!_reactor.Ready(ctx, info.Id, afflicted is not null))
        {
            return false;
        }

        target = afflicted!.Id;
        return info.NeedsUnitTarget;
    }

    /// <summary>Consumível de cura própria (poção) quando a vida cai abaixo de <c>potionBelow</c> (padrão 0,4; 0 desliga).</summary>
    private bool TryPotion(BotContext ctx, List<Intent> output)
    {
        var below = (float)ctx.Options.GetDouble(ctx.ClassId, "potionBelow", 0.4);
        var consumables = ctx.Abilities.Consumables;
        if (consumables.Count == 0 || below <= 0)
        {
            return false;
        }

        var low = ctx.HealthFraction <= below;
        if (!_reactor.Ready(ctx, "potion", low))
        {
            return false;
        }

        foreach (var id in consumables)
        {
            if (!ctx.World.Content.Abilities.TryGetValue(id, out var def))
            {
                continue;
            }

            var info = AbilityInfo.For(def);
            if (!info.HasHeal || !(info.IsSelfCentered || info.NeedsUnitTarget))
            {
                continue;
            }

            if (ctx.TryUse(info, info.NeedsUnitTarget ? ctx.Self.Id : null, null, output))
            {
                _reactor.Reset("potion");
                return true;
            }
        }

        return false;
    }

    /// <summary>Guarda quando algum aliado (não o próprio bot) perdeu vida, para a Provocação reagir a "aliado sob ataque".</summary>
    private void TrackAllies(BotContext ctx)
    {
        foreach (var a in ctx.Allies())
        {
            if (a.Id == ctx.Self.Id)
            {
                continue;
            }

            var hp = a.TryGet<Health>()?.Current ?? 0f;
            if (_allyHp.TryGetValue(a.Id, out var last) && hp < last)
            {
                _allyHitTick = ctx.World.Tick;
            }

            _allyHp[a.Id] = hp;
        }
    }

    private static bool PlanDash(
        BotContext ctx, AbilityInfo info, Entity? nearest, Entity? focus, bool fleeing, bool force, out Vector2? aim)
    {
        aim = null;
        if (nearest is null || info.Dash is null)
        {
            return false;
        }

        var pos = ctx.Position;
        if (info.IsBackwardDash)
        {
            var close = ctx.EdgeDistanceTo(nearest) < 2.5;
            if (!(fleeing || force || (close && ctx.HealthFraction < 0.5f)))
            {
                return false;
            }

            var away = pos - BotContext.PositionOf(nearest);
            away = away.LengthSquared() < 1e-6f ? new Vector2(0, 1) : Vector2.Normalize(away);
            aim = pos + (away * (float)info.Dash.Distance);
            return true;
        }

        // Dash para frente: fecha distância quando o alvo está longe.
        var target = focus ?? nearest;
        var gap = ctx.EdgeDistanceTo(target);
        if (!force && (gap < 3.5 || gap > 3.5 + info.Dash.Distance + 2))
        {
            return false;
        }

        aim = BotContext.PositionOf(target);
        return true;
    }

    private bool PlanDamage(
        BotContext ctx, AbilityInfo info, List<Entity> enemies, Entity? focus, string cls, out EntityId? target, out Vector2? aim)
    {
        target = null;
        aim = null;
        if (enemies.Count == 0)
        {
            return false;
        }

        var first = Prioritize(focus, enemies).First();
        target = first.Id;
        if (!info.IsAoe)
        {
            return true;
        }

        var minAoe = (int)Math.Min(ctx.Options.GetDouble(cls, "minAoe", 2), enemies.Count);
        if (plan.ForceAbilities)
        {
            minAoe = 1;
        }

        switch (info.Targeting)
        {
            case CircleSelfTargeting c:
                {
                    var n = ctx.World.Spatial.InRadius(ctx.Position, (float)c.Radius, e => enemies.Contains(e)).Count;
                    return n >= minAoe;
                }

            case CircleAtPointTargeting c:
                {
                    // Centro no inimigo com mais vizinhos dentro do raio, desde que ao alcance.
                    Entity? best = null;
                    var bestCount = 0;
                    foreach (var e in enemies)
                    {
                        if (ctx.EdgeDistanceTo(e) > c.Range)
                        {
                            continue;
                        }

                        var p = BotContext.PositionOf(e);
                        var n = enemies.Count(o => Vector2.Distance(p, BotContext.PositionOf(o)) <= c.Radius + BotContext.RadiusOf(o));
                        if (n > bestCount)
                        {
                            best = e;
                            bestCount = n;
                        }
                    }

                    if (best is null || bestCount < minAoe)
                    {
                        return false;
                    }

                    target = best.Id;
                    aim = BotContext.PositionOf(best);
                    return true;
                }

            default: // linha
                {
                    var dir = BotContext.PositionOf(first) - ctx.Position;
                    if (dir.LengthSquared() < 1e-6f)
                    {
                        return true;
                    }

                    var l = (LineTargeting)info.Targeting;
                    var n = ctx.World.Spatial.InOrientedRect(ctx.Position, Vector2.Normalize(dir), (float)l.Length, (float)l.Width, e => enemies.Contains(e)).Count;
                    return n >= minAoe;
                }
        }
    }

    // ---- movimento ----

    private double ReachOfBasic(BotContext ctx)
    {
        var id = ctx.Abilities.BasicAttackId;
        return id is not null && ctx.World.Content.Abilities.TryGetValue(id, out var def) ? AbilityInfo.For(def).Reach : 1.0;
    }

    /// <summary>Distância de parada na perseguição: 90% do menor alcance entre o básico e as habilidades permitidas ofensivas "principais".</summary>
    private double StopDistance(BotContext ctx)
    {
        var reach = plan.UseBasicAttack ? ReachOfBasic(ctx) : double.MaxValue;
        foreach (var a in Allowed(ctx))
        {
            if (a.Role == AbilityRole.Damage && a.Reach > 0 && plan.ForceAbilities)
            {
                reach = Math.Min(reach, a.Reach);
            }
        }

        if (reach == double.MaxValue)
        {
            reach = 1.0;
        }

        return Math.Max(0.3, reach * 0.9);
    }

    private double PreferredDistance(BotContext ctx) =>
        ctx.Options.GetDouble(ctx.ClassId, "kiteDistance", Math.Min(ReachOfBasic(ctx) * 0.85, 7.0));

    private double RetreatDistance(BotContext ctx) =>
        ctx.Options.GetDouble(ctx.ClassId, "retreatDistance", PreferredDistance(ctx) * 0.55);

    private void Move(BotContext ctx, List<Entity> enemies, Entity? focus, bool fleeing, List<Intent> output)
    {
        switch (plan.Movement)
        {
            case MovementMode.Stand:
                output.Add(new StopIntent(ctx.Self.Id));
                return;

            case MovementMode.Chase:
                if (focus is null)
                {
                    output.Add(new StopIntent(ctx.Self.Id));
                    return;
                }

                ChaseTo(ctx, focus, StopDistance(ctx), output);
                return;

            case MovementMode.SupportFollow:
                {
                    var healReach = Allowed(ctx).Where(a => a.Role == AbilityRole.Heal).Select(a => a.Reach).DefaultIfEmpty(5).Max();
                    var needy = ctx.Allies()
                        .Where(a => a.Id != ctx.Self.Id)
                        .OrderBy(BotContext.HealthOf)
                        .ThenBy(a => a.Id.Value)
                        .FirstOrDefault();
                    if (needy is null)
                    {
                        output.Add(new StopIntent(ctx.Self.Id));
                        return;
                    }

                    ChaseTo(ctx, needy, Math.Max(1.0, healReach * 0.8), output);
                    return;
                }

            case MovementMode.Kite:
                MoveKite(ctx, enemies, focus, fleeing, output);
                return;
        }
    }

    private void ChaseTo(BotContext ctx, Entity target, double stopEdge, List<Intent> output)
    {
        if (ctx.EdgeDistanceTo(target) <= stopEdge)
        {
            output.Add(new StopIntent(ctx.Self.Id));
            return;
        }

        _nav.MoveToward(ctx, BotContext.PositionOf(target), output);
    }

    private void MoveKite(BotContext ctx, List<Entity> enemies, Entity? focus, bool fleeing, List<Intent> output)
    {
        if (enemies.Count == 0 || focus is null)
        {
            output.Add(new StopIntent(ctx.Self.Id));
            return;
        }

        if (fleeing)
        {
            var (dir, clear) = KiteMath.BestFlee(ctx, enemies, _fleeDir);
            _cornered = clear < 1f;
            if (!_cornered)
            {
                _fleeDir = dir;
                output.Add(new MoveIntent(ctx.Self.Id, dir));
                return;
            }

            // Encurralado: para e luta.
            output.Add(new StopIntent(ctx.Self.Id));
            return;
        }

        _cornered = false;
        _fleeDir = Vector2.Zero;
        var preferred = PreferredDistance(ctx);
        var dist = ctx.EdgeDistanceTo(focus);
        var los = ctx.World.Map?.HasLineOfSight(ctx.Position, BotContext.PositionOf(focus)) ?? true;
        if (dist > preferred + 1.5 || !los)
        {
            _nav.MoveToward(ctx, BotContext.PositionOf(focus), output);
        }
        else
        {
            output.Add(new StopIntent(ctx.Self.Id));
        }
    }
}
