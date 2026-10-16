using System.Numerics;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Spatial;

namespace Game.Bots;

/// <summary>Movimento até um ponto: reto quando há linha de visão, senão A* com cache refeito a cada 1 s ou se o destino muda.</summary>
public sealed class Navigator
{
    private List<Vector2> _path = [];
    private Vector2 _pathGoal;
    private long _refreshAt;

    public void Reset() => _path.Clear();

    public void MoveToward(BotContext ctx, Vector2 goal, List<Intent> output)
    {
        var pos = ctx.Position;
        var map = ctx.World.Map;
        if (map is null || map.HasLineOfSight(pos, goal, ctx.Radius))
        {
            _path.Clear();
            Emit(ctx, goal - pos, output);
            return;
        }

        if (_path.Count == 0 || ctx.World.Tick >= _refreshAt || Vector2.Distance(_pathGoal, goal) > 1f)
        {
            _path = Pathfinder.FindPath(map, pos, goal, ctx.Radius).ToList();
            _pathGoal = goal;
            _refreshAt = ctx.World.Tick + World.TickRate;
        }

        while (_path.Count > 0 && Vector2.Distance(pos, _path[0]) < 0.3f)
        {
            _path.RemoveAt(0);
        }

        if (_path.Count == 0)
        {
            output.Add(new StopIntent(ctx.Self.Id));
            return;
        }

        Emit(ctx, _path[0] - pos, output);
    }

    private static void Emit(BotContext ctx, Vector2 delta, List<Intent> output)
    {
        if (delta.LengthSquared() < 1e-6f)
        {
            output.Add(new StopIntent(ctx.Self.Id));
            return;
        }

        output.Add(new MoveIntent(ctx.Self.Id, Vector2.Normalize(delta)));
    }
}

/// <summary>Escolha de direção de fuga/kite: afasta dos inimigos, evita paredes e becos, prefere esconder-se atrás de obstáculos.</summary>
public static class KiteMath
{
    private const int Directions = 16;

    /// <summary>Devolve a melhor direção de fuga e o espaço livre nela; <c>Clear &lt; 1</c> indica encurralado.</summary>
    public static (Vector2 Direction, float Clear) BestFlee(BotContext ctx, IReadOnlyList<Entity> enemies, Vector2? previous)
    {
        var pos = ctx.Position;
        var map = ctx.World.Map;
        var radius = ctx.Radius;
        var best = Vector2.Zero;
        var bestClear = 0f;
        var bestScore = float.NegativeInfinity;
        for (var i = 0; i < Directions; i++)
        {
            var angle = i * MathF.Tau / Directions;
            var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var clear = 0f;
            for (var s = 1f; s <= 4f; s += 1f)
            {
                var p = pos + (dir * s);
                if (map is not null && (map.IsBlocked(p) || !map.HasLineOfSight(pos, p, radius)))
                {
                    break;
                }

                clear = s;
            }

            if (clear < 0.5f)
            {
                continue;
            }

            var probe = pos + (dir * MathF.Min(clear, 3f));
            var minEnemy = float.PositiveInfinity;
            var hiddenFromAll = true;
            foreach (var e in enemies)
            {
                var ep = BotContext.PositionOf(e);
                minEnemy = MathF.Min(minEnemy, Vector2.Distance(probe, ep));
                if (map is null || map.HasLineOfSight(ep, probe))
                {
                    hiddenFromAll = false;
                }
            }

            var score = minEnemy + (clear * 0.4f) + (hiddenFromAll && enemies.Count > 0 ? 1.5f : 0f);
            if (previous is { } prev && prev != Vector2.Zero)
            {
                score += Vector2.Dot(prev, dir) * 0.8f; // estabilidade: evita zigue-zague
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = dir;
                bestClear = clear;
            }
        }

        return (best, bestClear);
    }
}
