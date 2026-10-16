using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.melee_chaser</c>: ataca com a primeira habilidade disponível (lista = prioridade) e, se nada está ao alcance,
/// persegue o alvo (reto com linha de visão, A* sem). Parâmetros: <c>aggroRange</c>, <c>approachRange</c>.
/// </summary>
public class MeleeChaserProfile : IAiProfile
{
    public virtual EntityId SelectTarget(AiContext ctx, EntityId fromTable) => fromTable;

    public virtual void Think(AiContext ctx)
    {
        var target = ctx.Target!;
        if (ctx.TryUseBest(target) == AiUseResult.OutOfReach)
        {
            Chase(ctx, target);
        }
        else
        {
            ctx.Stop();
        }
    }

    protected static void Chase(AiContext ctx, Entity target)
    {
        var stop = (float)(ctx.ApproachRange() * 0.8) + (target.TryGet<Collider>()?.Radius ?? 0f);
        ctx.MoveToward(target.Get<Transform>().Position, stop);
    }
}
