using Game.Core.Spatial;

namespace Game.Core.Simulation;

public sealed class WorldBuilder
{
    private readonly SortedDictionary<int, ISimulationSystem> _systems = new();

    public WorldBuilder(ulong seed)
    {
        Seed = seed;
    }

    public ulong Seed { get; }

    public GridMap? Map { get; private set; }

    /// <summary>Define o mapa de colisão do mundo (ex.: arena carregada pela T-008).</summary>
    public WorldBuilder WithMap(GridMap map)
    {
        Map = map;
        return this;
    }

    public WorldBuilder AddSystem(int order, ISimulationSystem system)
    {
        if (!_systems.TryAdd(order, system))
        {
            throw new InvalidOperationException($"System order {order} already used by {_systems[order].GetType().Name}");
        }

        return this;
    }

    public World Build() => new(Seed, _systems.Values.ToArray(), Map);

    /// <summary>Mundo com todos os sistemas do jogo (via <see cref="SystemsRegistry"/>).</summary>
    public static World CreateDefault(ulong seed, GridMap? map = null)
    {
        var b = new WorldBuilder(seed);
        if (map is not null)
        {
            b.WithMap(map);
        }

        SystemsRegistry.Register(b);
        return b.Build();
    }
}
