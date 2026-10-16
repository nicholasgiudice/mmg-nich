using Game.Core.Simulation.Components;

namespace Game.Core.Simulation.Systems;

/// <summary>Vida menor ou igual a zero vira morte (via <see cref="World.Kill"/>, que publica EntityDied uma vez).</summary>
public sealed class DeathSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var e in world.Entities)
        {
            var h = e.TryGet<Health>();
            if (h is { IsAlive: true, Current: <= 0 })
            {
                world.Kill(e);
            }
        }
    }
}
