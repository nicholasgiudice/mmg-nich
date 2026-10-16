using System.Numerics;
using Game.Core.Authority;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace MmoRpgClient.Authority;

/// <summary>
/// Autoridade falsa, roteirizada, sem regras de jogo: prova que a apresentação funciona com
/// qualquer <see cref="IGameAuthority"/>. Usada com <c>--fake-authority</c>.
/// Um herói anda em círculo e um alvo perde vida até morrer; intenções são ignoradas.
/// </summary>
public sealed class FakeAuthority : IGameAuthority
{
    private const float TickSeconds = 0.05f;

    private readonly World _scratch = new WorldBuilder(0).Build(); // sem sistemas: só guarda o estado roteirizado
    private readonly Entity _hero;
    private readonly Entity _dummy;
    private readonly List<GameEvent> _events = new();
    private double _accumulator;

    public FakeAuthority()
    {
        _hero = _scratch.Spawn(
            new Transform { Position = new Vector2(8, 6) },
            new Health(100),
            new FactionTag(Faction.Player),
            new Collider(0.4f));
        _dummy = _scratch.Spawn(
            new Transform { Position = new Vector2(8, 6) },
            new Health(50),
            new FactionTag(Faction.Monster),
            new Collider(0.5f));
        Latest = _scratch.Snapshot();
    }

    public static GridMap Map { get; } = new(16, 12, (x, y) => x == 0 || y == 0 || x == 15 || y == 11);

    public EntityId? ControlledEntity => _hero.Id;

    public long CurrentTick { get; private set; }

    public float InterpolationFraction { get; private set; }

    public WorldSnapshot? Latest { get; private set; }

    public WorldSnapshot? Previous { get; private set; }

    public void Advance(double deltaSeconds)
    {
        _accumulator += deltaSeconds;
        while (_accumulator >= TickSeconds)
        {
            _accumulator -= TickSeconds;
            CurrentTick++;
            var angle = CurrentTick * 0.1f;
            var t = _hero.Get<Transform>();
            t.Position = new Vector2(8 + 3 * MathF.Cos(angle), 6 + 3 * MathF.Sin(angle));
            t.Facing = new Vector2(-MathF.Sin(angle), MathF.Cos(angle));
            var hp = _dummy.Get<Health>();
            if (hp.IsAlive)
            {
                hp.Current = Math.Max(0, hp.Current - 0.25f);
                if (hp.Current <= 0)
                {
                    _scratch.Kill(_dummy);
                    _events.Add(new EntityDied(_dummy.Id));
                }
            }

            Previous = Latest;
            Latest = _scratch.Snapshot();
        }

        InterpolationFraction = (float)(_accumulator / TickSeconds);
    }

    public void Send(Intent intent)
    {
    }

    public IReadOnlyList<GameEvent> TakeEvents()
    {
        var events = _events.ToArray();
        _events.Clear();
        return events;
    }
}
