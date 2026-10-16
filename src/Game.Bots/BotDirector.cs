using Game.Core.Intents;
using Game.Core.Simulation;

namespace Game.Bots;

/// <summary>
/// Liga controladores a entidades jogador e, a cada tick, enfileira as intenções no mundo (<see cref="World.Enqueue"/>).
/// Chame <see cref="Tick"/> antes de cada <see cref="World.Step"/>. Serve ao simulador, aos companheiros de IA do cliente e a clientes de carga.
/// </summary>
public sealed class BotDirector(World world, IHazardSource? hazards = null)
{
    private readonly List<(Entity Entity, IBotController Controller, BotOptions Options)> _bots = [];
    private readonly List<Intent> _buffer = [];
    private readonly IHazardSource _hazards = hazards ?? NoHazards.Instance;

    public IReadOnlyList<(Entity Entity, IBotController Controller, BotOptions Options)> Bots => _bots;

    public void Add(Entity entity, IBotController controller, BotOptions? options = null) =>
        _bots.Add((entity, controller, options ?? BotOptions.Empty));

    public void Tick()
    {
        foreach (var (entity, controller, options) in _bots)
        {
            if (!entity.IsAlive)
            {
                continue;
            }

            _buffer.Clear();
            controller.Think(new BotContext(world, entity, options, _hazards), _buffer);
            foreach (var intent in _buffer)
            {
                world.Enqueue(intent);
            }
        }
    }
}
