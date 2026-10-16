namespace Game.Core.Events;

/// <summary>
/// Eventos do tick em andamento (<see cref="Current"/>, em ordem de publicação) e do último tick
/// concluído (<see cref="LastTick"/>). Um sistema posterior vê os eventos publicados pelos anteriores
/// no mesmo tick. Sem callbacks: a ordem de reação é a ordem dos sistemas.
/// Eventos publicados entre ticks (ex.: spawn inicial) são carimbados com o próximo tick.
/// </summary>
public sealed class EventBus
{
    private List<GameEvent> _current = new();
    private IReadOnlyList<GameEvent> _last = Array.Empty<GameEvent>();
    private long _stampTick = 1;

    public IReadOnlyList<GameEvent> Current => _current;

    public IReadOnlyList<GameEvent> LastTick => _last;

    public void Publish(GameEvent e)
    {
        e.Tick = _stampTick;
        _current.Add(e);
    }

    /// <summary>Eventos do tick em andamento de um tipo (cópia; seguro publicar durante o laço).</summary>
    public List<T> Of<T>()
        where T : GameEvent => _current.OfType<T>().ToList();

    internal void EndTick(long tick)
    {
        _last = _current;
        _current = new List<GameEvent>();
        _stampTick = tick + 1;
    }
}
