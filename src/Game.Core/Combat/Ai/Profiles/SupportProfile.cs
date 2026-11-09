using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.support</c> (Sacerdote Morto, T-341): suporte inimigo que fica atrás dos protetores.
/// <list type="number">
/// <item>Cura: a habilidade de tag <see cref="HealTag"/> vai no aliado do grupo mais ferido com vida até <c>healBelow</c>
/// (0,6) e ao alcance. Ele não cura a si mesmo: quem o alcança vence a corrida. A conjuração é o aviso; uma interrupção a
/// cancela.</item>
/// <item>Ataque: as habilidades ofensivas instantâneas na ordem da definição (maldição antes do dardo), andando ou não.</item>
/// <item>Posição: com protetores vivos (membros do grupo de outro perfil), fica <c>behindDistance</c> (3) tiles atrás do
/// centro deles, do lado oposto ao alvo. Sem protetores, fica a <c>preferredDistance</c> (7) do alvo, sem fugir. Sem linha
/// de visão para o alvo, anda até enxergá-lo.</item>
/// </list>
/// Parâmetros: <c>aggroRange</c>, <c>healBelow</c>, <c>behindDistance</c>, <c>preferredDistance</c>.
/// </summary>
public sealed class SupportProfile : IAiProfile
{
    public const string HealTag = "ai.heal";

    public void Think(AiContext ctx)
    {
        var target = ctx.Target!;
        if (ctx.FindByTag(HealTag) is { } heal && MostInjured(ctx, heal) is { } ally && ctx.Use(heal, ally))
        {
            return;
        }

        ctx.TryUseBest(target, instantOnly: true);

        var targetPos = target.Get<Transform>().Position;
        if (!ctx.HasLineOfSightTo(target))
        {
            ctx.MoveToward(targetPos, 1f);
            return;
        }

        var protectors = ctx.GroupMembers(sameProfile: false)
            .Where(e => e.Id != ctx.Self.Id && !SameProfile(ctx, e))
            .ToList();
        if (protectors.Count > 0)
        {
            var center = Vector2.Zero;
            foreach (var p in protectors)
            {
                center += p.Get<Transform>().Position;
            }

            center /= protectors.Count;
            var away = center - targetPos;
            if (away.LengthSquared() < 1e-4f)
            {
                away = ctx.Position - targetPos;
            }

            var dir = away.LengthSquared() < 1e-6f ? new Vector2(1, 0) : Vector2.Normalize(away);
            var goal = center + (dir * (float)ctx.Param("behindDistance", 3));
            if (ctx.World.Map is { } map && map.IsBlocked(goal))
            {
                goal = center;
            }

            if (Vector2.Distance(ctx.Position, goal) > 0.75f)
            {
                ctx.MoveToward(goal, 0.5f);
            }
            else
            {
                ctx.Stop();
            }

            return;
        }

        var preferred = (float)ctx.Param("preferredDistance", 7);
        if (ctx.DistanceTo(target) > preferred + 1)
        {
            ctx.MoveToward(targetPos, preferred + (target.TryGet<Collider>()?.Radius ?? 0f));
        }
        else
        {
            ctx.Stop();
        }
    }

    private static bool SameProfile(AiContext ctx, Entity e) =>
        e.TryGet<UnitInfo>() is { } info
        && ctx.World.Content.Monsters.TryGetValue(info.DefinitionId, out var def)
        && def.Ai.Id == ctx.Definition.Ai.Id;

    /// <summary>Aliado do grupo (sem o próprio) mais ferido, com vida até <c>healBelow</c> e ao alcance da cura.</summary>
    private static Entity? MostInjured(AiContext ctx, AbilityDefinition heal)
    {
        var below = ctx.Param("healBelow", 0.6);
        var reach = AiContext.ReachOf(heal) ?? 0;
        Entity? best = null;
        var bestFraction = double.MaxValue;
        foreach (var e in ctx.GroupMembers(sameProfile: false))
        {
            if (e.Id == ctx.Self.Id || e.TryGet<Health>() is not { Max: > 0 } h)
            {
                continue;
            }

            var fraction = (double)h.Current / h.Max;
            if (fraction > below || fraction >= bestFraction || ctx.DistanceTo(e) > reach)
            {
                continue;
            }

            best = e;
            bestFraction = fraction;
        }

        return best;
    }
}
