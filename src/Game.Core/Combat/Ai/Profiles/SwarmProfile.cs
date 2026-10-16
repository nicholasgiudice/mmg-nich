using System.Numerics;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.swarm</c> (Rato): corre até uma posição de ataque distribuída em volta do alvo (os N membros do grupo que
/// miram o mesmo alvo ocupam N ângulos igualmente espaçados, por ordem de id) e ataca. Periodicamente (RNG da
/// simulação) pode trocar de alvo por um tempo. Parâmetros: <c>ringRadius</c> (distância ao centro do alvo; padrão raio
/// do alvo + raio próprio + 0.3), <c>slotAngleOffset</c> (rad), <c>retargetIntervalSeconds</c> (2),
/// <c>retargetChance</c> (0.25), <c>retargetHoldSeconds</c> (3), <c>retargetRadius</c> (12).
/// </summary>
public sealed class SwarmProfile : IAiProfile
{
    public EntityId SelectTarget(AiContext ctx, EntityId fromTable)
    {
        var state = ctx.State;
        if (!state.OverrideTarget.IsNone)
        {
            if (ctx.Tick < state.OverrideUntilTick && ctx.World.Find(state.OverrideTarget) is { IsAlive: true })
            {
                return state.OverrideTarget;
            }

            state.OverrideTarget = EntityId.None;
        }

        if (ctx.Tick >= state.NextRetargetTick)
        {
            state.NextRetargetTick = ctx.Tick + Math.Max(1, ctx.ParamTicks("retargetIntervalSeconds", 2));
            if (ctx.World.Random.NextFloat() < ctx.Param("retargetChance", 0.25))
            {
                var candidates = ctx.World.Spatial.InRadius(
                    ctx.Position,
                    (float)ctx.Param("retargetRadius", 12),
                    e => e.IsAlive && e.Id != fromTable && e.TryGet<FactionTag>()?.Faction == Faction.Player);
                if (candidates.Count > 0)
                {
                    var pick = candidates[ctx.World.Random.NextInt(candidates.Count)];
                    state.OverrideTarget = pick.Id;
                    state.OverrideUntilTick = ctx.Tick + ctx.ParamTicks("retargetHoldSeconds", 3);
                    return pick.Id;
                }
            }
        }

        return fromTable;
    }

    public void Think(AiContext ctx)
    {
        var target = ctx.Target!;
        if (ctx.TryUseBest(target) == AiUseResult.Used)
        {
            return;
        }

        var tpos = target.Get<Transform>().Position;
        var slot = SlotFor(ctx, target, tpos);
        if (Vector2.Distance(ctx.Position, slot) > 0.3f)
        {
            ctx.MoveToward(slot, 0.15f);
        }
        else
        {
            ctx.Stop();
        }
    }

    private static Vector2 SlotFor(AiContext ctx, Entity target, Vector2 tpos)
    {
        var index = 0;
        var count = 0;
        foreach (var m in ctx.GroupMembers())
        {
            if (m.Get<AiState>().Target != target.Id)
            {
                continue;
            }

            if (m.Id == ctx.Self.Id)
            {
                index = count;
            }

            count++;
        }

        count = Math.Max(1, count);
        var ring = (float)ctx.Param("ringRadius", (target.TryGet<Collider>()?.Radius ?? 0f) + ctx.Radius + 0.3f);
        var angle = ((float)(2 * Math.PI) * index / count) + (float)ctx.Param("slotAngleOffset", 0);
        var slot = tpos + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ring);
        return ctx.World.Map is { } map && map.IsBlocked(slot) ? tpos : slot;
    }
}
