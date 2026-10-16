using System.Numerics;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.ranged_kiter</c> (Salteador): mantém a distância preferida, recua quando o alvo se aproxima e usa linha de visão.
/// Enquanto recua só usa habilidades instantâneas (conjurar parado deixaria de recuar). Se encurralado, para e ataca.
/// Parâmetros: <c>preferredDistance</c> (7), <c>retreatDistance</c> (4), <c>approachSlack</c> (1.5), <c>probeLength</c> (1.5).
/// </summary>
public sealed class RangedKiterProfile : IAiProfile
{
    private static readonly float[] RetreatAngles = [0f, 0.785f, -0.785f, 1.571f, -1.571f];

    public void Think(AiContext ctx)
    {
        var target = ctx.Target!;
        var tpos = target.Get<Transform>().Position;
        var distance = ctx.DistanceTo(target);
        var preferred = (float)ctx.Param("preferredDistance", 7);
        var retreat = (float)ctx.Param("retreatDistance", 4);
        var slack = (float)ctx.Param("approachSlack", 1.5);

        if (distance < retreat)
        {
            ctx.TryUseBest(target, instantOnly: true);
            if (PickRetreat(ctx, tpos) is { } dir)
            {
                ctx.MoveDirection(dir);
            }
            else
            {
                ctx.Stop();
            }

            return;
        }

        if (ctx.TryUseBest(target) == AiUseResult.Used)
        {
            return;
        }

        if (!ctx.HasLineOfSightTo(target) || distance > preferred + slack)
        {
            ctx.MoveToward(tpos, preferred + (target.TryGet<Collider>()?.Radius ?? 0f));
        }
        else
        {
            ctx.Stop();
        }
    }

    private static Vector2? PickRetreat(AiContext ctx, Vector2 targetPos)
    {
        var pos = ctx.Position;
        var away = pos - targetPos;
        away = away.LengthSquared() < 1e-6f ? new Vector2(1, 0) : Vector2.Normalize(away);
        var probe = (float)ctx.Param("probeLength", 1.5);
        var map = ctx.World.Map;
        foreach (var angle in RetreatAngles)
        {
            var (s, c) = MathF.SinCos(angle);
            var dir = new Vector2((away.X * c) - (away.Y * s), (away.X * s) + (away.Y * c));
            var p = pos + (dir * probe);
            if (map is null || (!map.IsBlocked(p) && map.HasLineOfSight(pos, p, ctx.Radius)))
            {
                return dir;
            }
        }

        return null;
    }
}
