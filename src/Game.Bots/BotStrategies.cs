using Game.Core.Definitions;
using Game.Core.Intents;

namespace Game.Bots;

/// <summary>
/// Catálogo de estratégias. O nome aceita o sufixo <c>+dodge</c> (ex.: <c>rotation+dodge</c>) que envolve o controlador
/// numa camada que sai de telegráfos/áreas perigosas (ver <see cref="IHazardSource"/>).
/// </summary>
public static class BotStrategies
{
    public const string DodgeSuffix = "+dodge";

    public static IReadOnlyList<string> Names { get; } =
        ["idle", "basic_only", "rotation", "kite", "taunt_and_basic", "explosion_spam", "heal_only"];

    public static IBotController Create(string spec)
    {
        var dodge = spec.EndsWith(DodgeSuffix, StringComparison.Ordinal);
        var name = dodge ? spec[..^DodgeSuffix.Length] : spec;
        var plan = PlanFor(name) ?? throw new ArgumentException(
            $"Estratégia desconhecida '{name}'. Disponíveis: {string.Join(", ", Names)} (aceitam o sufixo {DodgeSuffix}).");
        IBotController bot = new ConfigurableBot(plan with { Name = spec });
        return dodge ? new DodgingController(bot, spec) : bot;
    }

    private static BotPlan? PlanFor(string name) => name switch
    {
        "idle" => new BotPlan(name, MovementMode.Stand, _ => [], UseBasicAttack: true, ForceAbilities: false),
        "basic_only" => new BotPlan(name, MovementMode.Chase, _ => [], true, false),
        "rotation" => new BotPlan(name, MovementMode.Chase, Rotation, true, false),
        "kite" => new BotPlan(name, MovementMode.Kite, Rotation, true, false),
        "taunt_and_basic" => new BotPlan(name, MovementMode.Chase, Taunts, true, true),
        "explosion_spam" => new BotPlan(name, MovementMode.Chase, Explosion, true, true),
        "heal_only" => new BotPlan(name, MovementMode.SupportFollow, Heals, UseBasicAttack: false, ForceAbilities: true),
        _ => null,
    };

    /// <summary>Habilidades não básicas da classe na ordem da definição (= prioridade), ajustada por <c>order</c> e <c>skip</c>.</summary>
    private static IReadOnlyList<AbilityInfo> Rotation(BotContext ctx)
    {
        var all = ClassAbilities(ctx);
        var skip = ctx.Options.GetList(ctx.ClassId, "skip");
        var order = ctx.Options.GetList(ctx.ClassId, "order");
        var kept = all.Where(a => !a.Matches(skip)).ToList();
        if (order.Count == 0)
        {
            return kept;
        }

        var first = order.SelectMany(sel => kept.Where(a => a.Matches([sel]))).Distinct().ToList();
        return first.Concat(kept.Where(a => !first.Contains(a))).ToList();
    }

    private static IReadOnlyList<AbilityInfo> Taunts(BotContext ctx) =>
        Selected(ctx, "taunt", a => a.Role == AbilityRole.Taunt);

    private static IReadOnlyList<AbilityInfo> Heals(BotContext ctx) =>
        Selected(ctx, "heal", a => a.Role == AbilityRole.Heal);

    /// <summary>Padrão: a habilidade de área ofensiva de maior dano; sem área, a de maior dano. Substituível por <c>explosion=id|tag:x</c>.</summary>
    private static IReadOnlyList<AbilityInfo> Explosion(BotContext ctx)
    {
        var sel = ctx.Options.GetList(ctx.ClassId, "explosion");
        var all = ClassAbilities(ctx);
        if (sel.Count > 0)
        {
            return all.Where(a => a.Matches(sel)).ToList();
        }

        var damage = all.Where(a => a.Role == AbilityRole.Damage).ToList();
        var pool = damage.Where(a => a.IsAoe).ToList();
        if (pool.Count == 0)
        {
            pool = damage;
        }

        var best = pool.OrderByDescending(a => a.DamageTotal).FirstOrDefault();
        return best is null ? [] : [best];
    }

    private static IReadOnlyList<AbilityInfo> Selected(BotContext ctx, string key, Func<AbilityInfo, bool> byRole)
    {
        var sel = ctx.Options.GetList(ctx.ClassId, key);
        var all = ClassAbilities(ctx);
        return sel.Count > 0 ? all.Where(a => a.Matches(sel)).ToList() : all.Where(byRole).ToList();
    }

    private static List<AbilityInfo> ClassAbilities(BotContext ctx)
    {
        var st = ctx.Abilities;
        var list = new List<AbilityInfo>();
        foreach (var id in st.Known)
        {
            if (id != st.BasicAttackId && ctx.AbilityDef(id) is { } def)
            {
                list.Add(AbilityInfo.For(def));
            }
        }

        return list;
    }
}

/// <summary>Envolve outro controlador: se o bot está dentro de um perigo anunciado, sai dele (por cima do movimento do controlador interno).</summary>
public sealed class DodgingController(IBotController inner, string name) : IBotController
{
    private const float Margin = 0.35f;
    private readonly List<Hazard> _hazards = [];
    private readonly List<Hazard> _all = [];
    private readonly Reactor _reactor = new();
    private readonly Dictionary<object, long> _noticeAt = [];
    private readonly HashSet<object> _current = [];

    public string Name => name;

    public void Think(BotContext ctx, List<Intent> output)
    {
        _hazards.Clear();
        _all.Clear();
        ctx.Hazards.Collect(ctx.World, _all);
        Notice(ctx);
        var radius = ctx.Radius + Margin;
        var pos = ctx.Position;
        if (_hazards.Count == 0 || !_hazards.Any(h => h.Contains(pos, radius)))
        {
            inner.Think(ctx, output);
            return;
        }

        var escape = FindEscape(ctx, radius);
        if (escape is null)
        {
            inner.Think(ctx, output); // sem saída: age normalmente
            return;
        }

        // Sair do perigo tem prioridade sobre mover/conjurar: o controlador interno só contribui com instantâneas.
        var temp = new List<Intent>();
        inner.Think(ctx, temp);
        foreach (var intent in temp)
        {
            if (intent is UseAbilityIntent use
                && ctx.AbilityDef(use.AbilityId) is { } def
                && def.CastTimeTicks == 0
                && def.Effects.All(e => e is not DisplacementEffect))
            {
                output.Add(intent);
            }
        }

        var dir = escape.Value - pos;
        output.Add(new MoveIntent(ctx.Self.Id, System.Numerics.Vector2.Normalize(dir)));
    }

    /// <summary>T-018: um perigo só entra na decisão depois do tempo de reação desde que apareceu (identidade ignora o tick de resolução).</summary>
    private void Notice(BotContext ctx)
    {
        _current.Clear();
        foreach (var h in _all)
        {
            var key = KeyOf(h);
            _current.Add(key);
            if (!_noticeAt.TryGetValue(key, out var at))
            {
                at = ctx.World.Tick + _reactor.DrawTicks(ctx);
                _noticeAt[key] = at;
            }

            if (ctx.World.Tick >= at)
            {
                _hazards.Add(h);
            }
        }

        if (_noticeAt.Count > _current.Count)
        {
            foreach (var k in _noticeAt.Keys.Where(k => !_current.Contains(k)).ToList())
            {
                _noticeAt.Remove(k);
            }
        }
    }

    private static object KeyOf(Hazard h) => h switch
    {
        CircleHazard c => (1, c.Center, c.Radius),
        RectHazard r => (2, r.Origin, r.Direction, r.Length, r.Width),
        ConeHazard c => (3, c.Origin, c.Direction, c.AngleRadians, c.Range),
        _ => h,
    };

    /// <summary>Ponto livre mais próximo fora de todos os perigos (amostragem radial), alcançável em linha reta.</summary>
    private System.Numerics.Vector2? FindEscape(BotContext ctx, float radius)
    {
        var pos = ctx.Position;
        var map = ctx.World.Map;
        System.Numerics.Vector2? best = null;
        var bestDist = float.PositiveInfinity;
        for (var i = 0; i < 24; i++)
        {
            var angle = i * MathF.Tau / 24;
            var dir = new System.Numerics.Vector2(MathF.Cos(angle), MathF.Sin(angle));
            for (var d = 0.5f; d <= 8f; d += 0.5f)
            {
                var p = pos + (dir * d);
                if (map is not null && (map.IsBlocked(p) || !map.HasLineOfSight(pos, p, ctx.Radius)))
                {
                    break;
                }

                if (_hazards.Any(h => h.Contains(p, radius)))
                {
                    continue;
                }

                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }

                break;
            }
        }

        return best;
    }
}
