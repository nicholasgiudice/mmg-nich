namespace Game.Core.Simulation;

public sealed class WorldBuilder
{
    private readonly SortedDictionary<int, ISimulationSystem> _systems = new();

    public WorldBuilder(ulong seed)
    {
        Seed = seed;
    }

    public ulong Seed { get; }

    public WorldBuilder AddSystem(int order, ISimulationSystem system)
    {
        if (!_systems.TryAdd(order, system))
        {
            throw new InvalidOperationException($"System order {order} already used by {_systems[order].GetType().Name}");
        }

        return this;
    }

    public World Build() => new(Seed, _systems.Values.ToArray());

    /// <summary>Mundo com todos os sistemas do jogo (via <see cref="SystemsRegistry"/>).</summary>
    public static World CreateDefault(ulong seed)
    {
        var b = new WorldBuilder(seed);
        SystemsRegistry.Register(b);
        return b.Build();
    }
}
