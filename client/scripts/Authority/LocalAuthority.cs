using Game.Core.Authority;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;

namespace MmoRpgClient.Authority;

/// <summary>
/// Autoridade do protótipo: roda o <see cref="World"/> dentro do cliente, em passo fixo de 20 Hz
/// com acumulador. Não depende do Godot; quem chama <see cref="Advance"/> é o <c>_Process</c>.
/// </summary>
public sealed class LocalAuthority : IGameAuthority
{
    /// <summary>Máximo de ticks por chamada de <see cref="Advance"/>; o atraso excedente é descartado para não travar.</summary>
    public const int MaxStepsPerAdvance = 5;

    private readonly World _world;
    private readonly List<GameEvent> _pendingEvents = new();
    private double _accumulator;

    public LocalAuthority(World world, EntityId? controlledEntity)
    {
        _world = world;
        ControlledEntity = controlledEntity;
        Latest = world.Snapshot();
    }

    public EntityId? ControlledEntity { get; }

    public long CurrentTick => Latest?.Tick ?? 0;

    public float InterpolationFraction { get; private set; }

    public WorldSnapshot? Latest { get; private set; }

    public WorldSnapshot? Previous { get; private set; }

    public void Advance(double deltaSeconds)
    {
        _accumulator += Math.Max(0, deltaSeconds);
        var steps = 0;
        while (_accumulator >= World.TickSeconds && steps < MaxStepsPerAdvance)
        {
            Step();
            _accumulator -= World.TickSeconds;
            steps++;
        }

        if (_accumulator >= World.TickSeconds)
        {
            _accumulator %= World.TickSeconds;
        }

        InterpolationFraction = (float)(_accumulator / World.TickSeconds);
    }

    public void Send(Intent intent)
    {
        // Mesma regra que o servidor terá: o cliente só fala pela entidade que controla.
        if (ControlledEntity is { } id && intent.Entity == id)
        {
            _world.Enqueue(intent);
        }
    }

    public IReadOnlyList<GameEvent> TakeEvents()
    {
        if (_pendingEvents.Count == 0)
        {
            return Array.Empty<GameEvent>();
        }

        var events = _pendingEvents.ToArray();
        _pendingEvents.Clear();
        return events;
    }

    private void Step()
    {
        _world.Step();
        Previous = Latest;
        Latest = _world.Snapshot();
        _pendingEvents.AddRange(_world.LastTickEvents);
    }
}
