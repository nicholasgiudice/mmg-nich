using System.Numerics;
using Game.Bots;
using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.NetBots;

/// <summary>
/// Perigos lidos do <b>estado</b> (T-109): cada entidade com <see cref="TelegraphState"/> de uma fonte que não é jogador
/// vira um perigo até o tick de resolução. O <see cref="TelegraphHazardSource"/> do <c>Game.Bots</c> lê os eventos
/// (<c>TelegraphStarted</c>/<c>TelegraphResolved</c>), que um mundo espelhado (<see cref="World.Mirror"/>) não tem —
/// sem isto os bots de rede nunca desviam das investidas e poças da Matriarca.
/// </summary>
public sealed class SnapshotHazardSource : IHazardSource
{
    public void Collect(World world, List<Hazard> into)
    {
        foreach (var e in world.Entities)
        {
            if (e.IsRemoved || e.TryGet<TelegraphState>() is not { } t || e.TryGet<Transform>() is not { } tr)
            {
                continue;
            }

            if (t.SourceFaction is Faction.Player || t.ResolveAtTick < world.Tick)
            {
                continue; // do próprio grupo, ou já resolvido
            }

            var dir = tr.Facing.LengthSquared() > 1e-8f ? Vector2.Normalize(tr.Facing) : new Vector2(0, 1);
            into.Add(t.Shape switch
            {
                TelegraphShape.Circle => new CircleHazard(tr.Position, t.Radius, t.ResolveAtTick),
                TelegraphShape.Cone => new ConeHazard(tr.Position, dir, t.AngleDegrees * MathF.PI / 180f, t.Length, t.ResolveAtTick),
                _ => new RectHazard(tr.Position, dir, t.Length, t.Width, t.ResolveAtTick),
            });
        }
    }
}
