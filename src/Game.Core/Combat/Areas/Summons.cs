using System.Numerics;
using Game.Core.Combat.Ai;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Phases;
using Game.Core.Combat.Threat;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Areas;

/// <summary>Marca uma entidade invocada e quem a invocou (para o limite de invocações vivas por fonte).</summary>
public sealed class SummonedBy : IComponent
{
    public EntityId Owner { get; init; }
    public string MonsterId { get; init; } = "";

    public IComponent Clone() => this; // imutável

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Owner.Value);
        hasher.Add(MonsterId);
    }
}

/// <summary>Um monstro foi invocado por <paramref name="Owner"/>.</summary>
public sealed record MonsterSummoned(EntityId Owner, EntityId Summon, string MonsterId, Vector2 Position) : GameEvent;

/// <summary>
/// Efeito <c>summon</c>: cria <c>count</c> monstros em volta da fonte (círculo de raio <c>radius</c>; padrão = raios somados + 0,5),
/// em ângulos igualmente espaçados a partir do facing, pulando posições em parede. Limite: <c>maxAlive</c> invocações vivas
/// do mesmo monstro por fonte (só nasce o que cabe). Bloqueado se a fase atual da fonte tem <c>noSummons</c>.
/// Os invocados entram no grupo da fonte e herdam o alvo dela (ameaça de grupo).
/// </summary>
public static class SummonModule
{
    public static void Register(CombatServices s) =>
        s.Effects.Register<SummonEffect>(OnSummon, EffectScope.PerCast);

    public static int AliveSummons(World world, EntityId owner, string monsterId)
    {
        var n = 0;
        foreach (var e in world.Entities)
        {
            if (e.IsAlive && e.TryGet<SummonedBy>() is { } sb && sb.Owner == owner && sb.MonsterId == monsterId)
            {
                n++;
            }
        }

        return n;
    }

    private static void OnSummon(EffectContext c, SummonEffect e)
    {
        var world = c.World;
        if (c.Source is not { } source || !source.IsAlive || source.TryGet<Transform>() is not { } tr
            || PhaseService.SummonsBlocked(world, source))
        {
            return;
        }

        var def = world.Content.Monsters[e.Monster];
        var count = e.Count;
        if (e.MaxAlive > 0)
        {
            count = Math.Min(count, e.MaxAlive - AliveSummons(world, source.Id, e.Monster));
        }

        if (count <= 0)
        {
            return;
        }

        var ring = e.Radius > 0 ? (float)e.Radius : (source.TryGet<Collider>()?.Radius ?? 0f) + (float)def.Radius + 0.5f;
        var baseAngle = MathF.Atan2(tr.Facing.Y, tr.Facing.X);
        var ownerAi = source.TryGet<AiState>();
        var ownerTarget = ownerAi is { } oa && !oa.Target.IsNone ? world.Find(oa.Target) : null;
        for (var i = 0; i < count; i++)
        {
            var pos = FreeSpot(world, tr.Position, ring, baseAngle + (MathF.Tau * i / count));
            var summon = EntityFactory.SpawnMonster(world, def, pos, source.TryGet<UnitInfo>()?.Level); // T-206: nível de quem invoca
            summon.Add(new SummonedBy { Owner = source.Id, MonsterId = e.Monster });
            if (AiState.Ensure(world, summon) is { } ai)
            {
                ai.GroupId = ownerAi?.GroupId ?? 0;
                if (ownerTarget is { IsAlive: true })
                {
                    ThreatService.Add(world, summon, ownerTarget, world.Content.Threat.GroupAggroThreat, AggroReason.Group, pullGroup: false);
                }
            }

            world.Events.Publish(new MonsterSummoned(source.Id, summon.Id, e.Monster, pos));
        }
    }

    /// <summary>Primeira posição livre de parede girando de 45 em 45 graus a partir de <paramref name="angle"/>; senão a do centro.</summary>
    private static Vector2 FreeSpot(World world, Vector2 center, float radius, float angle)
    {
        for (var k = 0; k < 8; k++)
        {
            var a = angle + (k * MathF.PI / 4f);
            var p = center + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius);
            if (world.Map is null || (!world.Map.IsBlocked(p) && world.Map.HasLineOfSight(center, p)))
            {
                return p;
            }
        }

        return center;
    }
}
