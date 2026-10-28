using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Rest;

/// <summary>
/// Descanso (T-214): a entidade recupera <see cref="PerSecond"/> da vida e da Mana máximas por segundo. Quem hospeda o mundo
/// põe o componente (ex.: personagem na Cidade); não é replicado.
/// </summary>
public sealed class Resting(double perSecond) : IComponent
{
    public double PerSecond { get; } = perSecond;

    public IComponent Clone() => new Resting(PerSecond);

    public void WriteHash(StateHasher hasher) => hasher.Add((float)PerSecond);
}

/// <summary>Aplica <see cref="Resting"/> a cada tick em quem está vivo. Sem evento: é regeneração, não cura.</summary>
public sealed class RestSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var e in world.Entities)
        {
            if (e.TryGet<Resting>() is not { } rest || e.TryGet<Health>() is not { IsAlive: true } health)
            {
                continue;
            }

            var fraction = (float)(rest.PerSecond / World.TickRate);
            health.Current = Math.Min(health.Max, health.Current + (health.Max * fraction));
            if (e.TryGet<ResourcePool>() is { Type: EntityFactory.ManaResource } mana)
            {
                mana.Current = Math.Min(mana.Max, mana.Current + (mana.Max * fraction));
            }
        }
    }
}
