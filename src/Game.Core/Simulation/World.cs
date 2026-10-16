using System.Numerics;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Simulation;

/// <summary>
/// Mundo simulado. Avança somente por <see cref="Step"/> (um tick fixo de 20 Hz).
/// Ordem de um tick: Tick++ -> aplicar intenções -> sistemas (por SystemOrder) -> fechar eventos.
/// Construa com <see cref="WorldBuilder"/>.
/// </summary>
public sealed class World
{
    public const int TickRate = 20;
    public const float TickSeconds = 1f / TickRate;

    private readonly ISimulationSystem[] _systems;
    private readonly SortedDictionary<int, Entity> _entities = new();
    private readonly Queue<Intent> _pendingIntents = new();
    private readonly List<Intent> _tickIntents = new();
    private Entity[] _entityCache = Array.Empty<Entity>();
    private bool _cacheDirty;
    private int _nextId = 1;

    internal World(ulong seed, ISimulationSystem[] systems, GridMap? map = null)
    {
        Random = new SimRandom(seed);
        _systems = systems;
        Map = map;
        Spatial = new BruteForceSpatialQuery(this);
    }

    /// <summary>Mapa de colisão do mundo (configurado no <see cref="WorldBuilder"/>); null = plano aberto sem paredes. Estático: não entra no hash.</summary>
    public GridMap? Map { get; }

    /// <summary>Consultas espaciais (raio, cone, retângulo, mais próxima).</summary>
    public ISpatialQuery Spatial { get; }

    /// <summary>Ticks já executados.</summary>
    public long Tick { get; private set; }

    public SimRandom Random { get; }

    public EventBus Events { get; } = new();

    /// <summary>Eventos do último tick concluído (para cliente, log e rede).</summary>
    public IReadOnlyList<GameEvent> LastTickEvents => Events.LastTick;

    /// <summary>Intenções válidas deste tick (já filtradas). Sistemas como o de habilidades as consomem daqui.</summary>
    public IReadOnlyList<Intent> TickIntents => _tickIntents;

    /// <summary>Entidades em ordem de id. Lista congelada: spawn/remove durante o laço é seguro.</summary>
    public IReadOnlyList<Entity> Entities
    {
        get
        {
            if (_cacheDirty)
            {
                _entityCache = _entities.Values.ToArray();
                _cacheDirty = false;
            }

            return _entityCache;
        }
    }

    public Entity Spawn(params IComponent[] components)
    {
        var e = new Entity(new EntityId(_nextId++));
        foreach (var c in components)
        {
            e.AddByRuntimeType(c);
        }

        _entities.Add(e.Id.Value, e);
        _cacheDirty = true;
        Events.Publish(new EntitySpawned(e.Id));
        return e;
    }

    public Entity? Find(EntityId id) => _entities.TryGetValue(id.Value, out var e) ? e : null;

    public bool Remove(EntityId id)
    {
        if (!_entities.Remove(id.Value, out var e))
        {
            return false;
        }

        e.IsRemoved = true;
        _cacheDirty = true;
        Events.Publish(new EntityRemoved(id));
        return true;
    }

    /// <summary>Marca como morta e publica EntityDied (uma única vez). Zera vida e movimento.</summary>
    public void Kill(Entity e)
    {
        var h = e.Get<Health>();
        if (!h.IsAlive)
        {
            return;
        }

        h.Current = 0;
        h.IsAlive = false;
        var mv = e.TryGet<Movement>();
        if (mv is not null)
        {
            mv.Direction = Vector2.Zero;
        }

        Events.Publish(new EntityDied(e.Id));
    }

    public void Enqueue(Intent intent) => _pendingIntents.Enqueue(intent);

    public void Step()
    {
        Tick++;
        ApplyIntents();
        foreach (var s in _systems)
        {
            s.Update(this);
        }

        Events.EndTick(Tick);
    }

    private void ApplyIntents()
    {
        _tickIntents.Clear();
        while (_pendingIntents.TryDequeue(out var intent))
        {
            var e = Find(intent.Entity);
            if (e is null || !e.IsAlive)
            {
                continue; // descartada: entidade inexistente ou morta
            }

            switch (intent)
            {
                case MoveIntent m:
                    var mv = e.TryGet<Movement>();
                    if (mv is null || !float.IsFinite(m.Direction.X) || !float.IsFinite(m.Direction.Y))
                    {
                        continue;
                    }

                    var len = m.Direction.Length();
                    mv.Direction = len > 1f ? m.Direction / len : m.Direction;
                    break;
                case StopIntent:
                    var stop = e.TryGet<Movement>();
                    if (stop is not null)
                    {
                        stop.Direction = Vector2.Zero;
                    }

                    break;
            }

            _tickIntents.Add(intent);
        }
    }

    /// <summary>Cópia imutável do estado, para cliente e testes.</summary>
    public WorldSnapshot Snapshot()
    {
        var list = Entities.Select(e => new EntitySnapshot(
            e.Id,
            e.OrderedComponents().ToDictionary(kv => kv.Key, kv => kv.Value.Clone()))).ToList();
        return new WorldSnapshot(Tick, list);
    }

    /// <summary>Hash de tick + RNG + todas as entidades/componentes. Mesmo estado => mesmo hash.</summary>
    public ulong ComputeStateHash()
    {
        var h = new StateHasher();
        h.Add(Tick);
        h.Add(Random.State);
        h.Add(_nextId);
        foreach (var e in Entities)
        {
            h.Add(e.Id.Value);
            foreach (var (type, c) in e.OrderedComponents())
            {
                h.Add(type.FullName!);
                c.WriteHash(h);
            }
        }

        return h.Value;
    }
}
