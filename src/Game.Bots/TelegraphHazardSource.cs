using System.Numerics;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Bots;

/// <summary>
/// Fonte de perigos a partir dos eventos da T-012: telegráfos (<see cref="TelegraphStarted"/> até <see cref="TelegraphResolved"/>)
/// e áreas de monstros (<see cref="AreaSpawned"/> até <see cref="AreaExpired"/>). Só conta perigos criados por entidades
/// de facção diferente de <see cref="Faction.Player"/> (um bot não desvia do que o próprio grupo criou).
/// Áreas persistentes (poças, teias) só entram com <c>includeAreas</c>: medido na T-016, desviar delas faz o bot oscilar na borda
/// (o movimento do bot interno o devolve para dentro) e perde dano; por padrão só telegráfos são evitados.
/// Guarda estado: use uma instância por mundo (o simulador usa <c>RunSpec.HazardSourceFactory</c>; o cliente cria uma para o mundo local).
/// Os eventos de cada tick são consumidos uma única vez, mesmo com vários bots chamando <see cref="Collect"/>.
/// </summary>
public sealed class TelegraphHazardSource(bool includeAreas = false) : IHazardSource
{
    private readonly bool _includeAreas = includeAreas;
    private readonly Dictionary<EntityId, Hazard> _active = [];
    private long _lastTick = -1;

    public void Collect(World world, List<Hazard> into)
    {
        if (world.Tick != _lastTick)
        {
            _lastTick = world.Tick;
            Consume(world);
        }

        foreach (var (_, hazard) in _active.OrderBy(kv => kv.Key.Value))
        {
            if (hazard.ResolveTick >= world.Tick)
            {
                into.Add(hazard);
            }
        }
    }

    private void Consume(World world)
    {
        foreach (var ev in world.LastTickEvents)
        {
            switch (ev)
            {
                case TelegraphStarted t when IsHostile(world, t.Source):
                    _active[t.Telegraph] = ToHazard(t);
                    break;
                case TelegraphResolved r:
                    _active.Remove(r.Telegraph);
                    break;
                case AreaSpawned a when _includeAreas && IsHostile(world, a.Owner):
                    _active[a.Area] = new CircleHazard(a.Position, a.Radius, a.ExpiresAtTick);
                    break;
                case AreaExpired x:
                    _active.Remove(x.Area);
                    break;
            }
        }
    }

    private static bool IsHostile(World world, EntityId source) =>
        world.Find(source)?.TryGet<FactionTag>() is { Faction: not Faction.Player };

    private static Hazard ToHazard(TelegraphStarted t) => t.Shape switch
    {
        TelegraphShape.Circle => new CircleHazard(t.Origin, t.Radius, t.ResolveAtTick),
        TelegraphShape.Cone => new ConeHazard(t.Origin, t.Direction, t.AngleDegrees * MathF.PI / 180f, t.Length, t.ResolveAtTick),
        _ => new RectHazard(t.Origin, t.Direction, t.Length, t.Width, t.ResolveAtTick),
    };
}
