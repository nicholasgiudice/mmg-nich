using Game.Core.Simulation;
using Game.Protocol;

namespace Game.Server.Interest;

/// <summary>Componentes de uma entidade num tick, já em bytes (para comparar) e como objeto (para enviar).</summary>
public sealed class EntityState(EntityId id, IReadOnlyDictionary<byte, (byte[] Bytes, IComponent Component)> components)
{
    public EntityId Id { get; } = id;
    public IReadOnlyDictionary<byte, (byte[] Bytes, IComponent Component)> Components { get; } = components;

    /// <summary>O mesmo estado sem os componentes de <paramref name="tags"/> (ou ele mesmo, se não tem nenhum).</summary>
    public EntityState Without(IReadOnlySet<byte> tags) =>
        Components.Keys.Any(tags.Contains)
            ? new EntityState(Id, Components.Where(kv => !tags.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value))
            : this;

    /// <summary>Estado de todas as entidades do snapshot, serializando cada componente uma vez por tick.</summary>
    public static Dictionary<EntityId, EntityState> FromSnapshot(WorldSnapshot snapshot)
    {
        var result = new Dictionary<EntityId, EntityState>(snapshot.Entities.Count);
        foreach (var e in snapshot.Entities)
        {
            var comps = new Dictionary<byte, (byte[], IComponent)>();
            foreach (var c in e.Components)
            {
                if (c is IReplicated && ComponentCodecs.Tags.TryGetValue(c.GetType(), out var tag))
                {
                    comps[tag] = (ProtocolSerializer.SerializeComponent(c), c);
                }
            }

            result[e.Id] = new EntityState(e.Id, comps);
        }

        return result;
    }
}

/// <summary>
/// O que um cliente já tem (T-106): o que foi mandado em cada tick recente e o último tick que ele confirmou. Cada
/// <see cref="SnapshotDelta"/> é a diferença entre o estado visível agora e o do tick confirmado; sem confirmação
/// recente (ou nenhuma), vai completo. Como a base é sempre algo que o cliente comprovadamente tem, perder pacotes
/// só atrasa a atualização — o próximo delta traz tudo de novo.
/// </summary>
public sealed class ClientView(int maxDeltaAgeTicks)
{
    private readonly SortedDictionary<long, Dictionary<EntityId, EntityState>> _sent = new();

    public int Epoch { get; private set; }

    /// <summary>Maior tick confirmado ainda guardado (0 = nenhum).</summary>
    public long AckedTick { get; private set; }

    /// <summary>Entidades que o cliente tem segundo o último envio (para filtrar eventos de quem acabou de sair/morrer).</summary>
    public IReadOnlyCollection<EntityId> LastSent => _sent.Count > 0 ? _sent[_sent.Keys.Last()].Keys : Array.Empty<EntityId>();

    /// <summary>Recomeça (entrada ou reinício da sala): o próximo envio é completo.</summary>
    public void Reset(int epoch)
    {
        Epoch = epoch;
        AckedTick = 0;
        _sent.Clear();
    }

    public void Ack(int epoch, long tick)
    {
        if (epoch == Epoch && tick > AckedTick && _sent.ContainsKey(tick))
        {
            AckedTick = tick;
        }
    }

    /// <summary>
    /// Componentes que só o dono recebe (T-106): <c>Movement</c> (velocidade e direção desejada) serve à predição do
    /// próprio movimento (T-108); para as outras entidades o cliente interpola posições.
    /// </summary>
    public static readonly IReadOnlySet<byte> OwnerOnly = new HashSet<byte> { ComponentCodecs.Tags[typeof(Game.Core.Simulation.Components.Movement)] };

    /// <summary>Monta o delta do <paramref name="tick"/> para as entidades <paramref name="visible"/> e guarda o que foi mandado.</summary>
    public SnapshotDelta Build(long tick, IReadOnlyDictionary<EntityId, EntityState> current, IEnumerable<EntityId> visible, EntityId? owned = null)
    {
        var useBase = AckedTick > 0 && tick - AckedTick <= maxDeltaAgeTicks && _sent.ContainsKey(AckedTick);
        var baseState = useBase ? _sent[AckedTick] : null;
        var now = new Dictionary<EntityId, EntityState>();
        var entities = new List<EntityDelta>();
        foreach (var id in visible.OrderBy(i => i.Value))
        {
            if (!current.TryGetValue(id, out var full))
            {
                continue;
            }

            var state = id == owned ? full : full.Without(OwnerOnly);

            now[id] = state;
            if (baseState is null || !baseState.TryGetValue(id, out var before))
            {
                entities.Add(new EntityDelta(id, true, state.Components.Values.Select(c => c.Component).ToArray(), []));
                continue;
            }

            var changed = state.Components
                .Where(kv => !before.Components.TryGetValue(kv.Key, out var old) || !old.Bytes.AsSpan().SequenceEqual(kv.Value.Bytes))
                .Select(kv => kv.Value.Component)
                .ToArray();
            var removed = before.Components.Keys.Where(t => !state.Components.ContainsKey(t)).ToArray();
            if (changed.Length > 0 || removed.Length > 0)
            {
                entities.Add(new EntityDelta(id, false, changed, removed));
            }
        }

        var gone = baseState is null ? [] : baseState.Keys.Where(id => !now.ContainsKey(id)).OrderBy(i => i.Value).ToArray();
        _sent[tick] = now;
        while (_sent.Count > 0 && _sent.Keys.First() < tick - SnapshotReconstructor.History)
        {
            _sent.Remove(_sent.Keys.First());
        }

        return new SnapshotDelta(Epoch, tick, useBase ? AckedTick : 0, entities.ToArray(), gone);
    }
}
