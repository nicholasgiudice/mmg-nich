using System.Numerics;
using Game.Core.Combat.Status;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.mist_kiter</c> (Fada da Névoa, T-340): atira de longe e foge pela névoa. Quando alguém chega perto ou ela já
/// perdeu vida, usa a habilidade de tag <see cref="VanishTag"/>: a conjuração é o aviso de que ela vai sumir, e o efeito
/// aplica nela um status que não pode ser alvo (<c>flags.untargetable</c>). Na névoa ela não ataca: só se afasta do alvo
/// até a distância preferida. Visível, ela não corre de quem se aproxima (com o véu em recarga, fica e atira): esse é o
/// momento de acertá-la. Longe demais, aproxima-se até a distância preferida; sem linha de visão, anda até enxergar o alvo.
/// Parâmetros: <c>preferredDistance</c> (7), <c>approachSlack</c> (1.5), <c>vanishDistance</c> (4, distância de borda que
/// dispara a névoa) e <c>vanishBelowHealth</c> (0,75, fração de vida abaixo da qual ela usa a névoa assim que puder).
/// </summary>
public sealed class MistKiterProfile : IAiProfile
{
    public const string VanishTag = "ai.vanish";

    public void Think(AiContext ctx)
    {
        var target = ctx.Target!;
        if (StatusManager.IsUntargetable(ctx.World, ctx.Self))
        {
            DriftAway(ctx, target.Get<Transform>().Position);
            return;
        }

        var close = ctx.DistanceTo(target) < ctx.Param("vanishDistance", 4);
        var hurt = ctx.Self.TryGet<Health>() is { Max: > 0 } h && h.Current / h.Max <= ctx.Param("vanishBelowHealth", 0.75);
        if ((close || hurt) && ctx.UseTagged(VanishTag, ctx.Self))
        {
            return;
        }

        if (ctx.TryUseBest(target) == AiUseResult.Used)
        {
            return;
        }

        var preferred = (float)ctx.Param("preferredDistance", 7);
        if (!ctx.HasLineOfSightTo(target))
        {
            // Sem linha de visão, a distância preferida não basta: anda até enxergar o alvo (contorna o obstáculo).
            ctx.MoveToward(target.Get<Transform>().Position, 1f);
        }
        else if (ctx.DistanceTo(target) > preferred + ctx.Param("approachSlack", 1.5))
        {
            ctx.MoveToward(target.Get<Transform>().Position, preferred + (target.TryGet<Collider>()?.Radius ?? 0f));
        }
        else
        {
            ctx.Stop();
        }
    }

    /// <summary>Na névoa: anda para longe do alvo até a distância preferida (sem atacar).</summary>
    private static void DriftAway(AiContext ctx, Vector2 targetPos)
    {
        var away = ctx.Position - targetPos;
        var preferred = (float)ctx.Param("preferredDistance", 7);
        if (away.Length() >= preferred)
        {
            ctx.Stop();
            return;
        }

        var dir = away.LengthSquared() < 1e-6f ? new Vector2(1, 0) : Vector2.Normalize(away);
        var goal = ctx.Position + (dir * 2f);
        if (ctx.World.Map is { } map && (map.IsBlocked(goal) || !map.HasLineOfSight(ctx.Position, goal, ctx.Radius)))
        {
            // Encostada numa parede: anda de lado (perpendicular ao alvo).
            dir = new Vector2(-dir.Y, dir.X);
        }

        ctx.MoveDirection(dir);
    }
}
