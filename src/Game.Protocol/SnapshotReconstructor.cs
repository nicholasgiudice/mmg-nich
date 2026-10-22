using Game.Core.Simulation;

namespace Game.Protocol;

/// <summary>
/// Lado do cliente dos snapshots delta (T-106): aplica cada <see cref="SnapshotDelta"/> sobre o estado do tick base
/// e guarda os estados recentes, porque o servidor manda diferenças em relação ao último tick <b>confirmado</b>
/// (<see cref="SnapshotAck"/>), que pode ser bem anterior ao último recebido. Delta de outra época, repetido, fora de
/// ordem ou com base que já não existe é ignorado (o próximo resolve, já que a base é sempre um tick confirmado).
/// Usado pelo cliente (T-107), pelos bots de rede (T-109) e pelos testes.
/// </summary>
public sealed class SnapshotReconstructor
{
    /// <summary>Estados guardados (ticks). O servidor não usa base mais velha que isso (ver <c>ServerOptions</c>).</summary>
    public const int History = 64;

    private static readonly IReadOnlyDictionary<byte, Type> TypeOfTag = ComponentCodecs.Tags.ToDictionary(kv => kv.Value, kv => kv.Key);

    private readonly SortedDictionary<long, Dictionary<EntityId, EntitySnapshot>> _states = new();

    public int Epoch { get; private set; } = -1;

    /// <summary>Último estado montado (null antes do primeiro).</summary>
    public WorldSnapshot? Latest { get; private set; }

    /// <summary>Começa uma época nova (chamar ao receber <see cref="Welcome"/>): descarta tudo.</summary>
    public void Reset(int epoch)
    {
        Epoch = epoch;
        _states.Clear();
        Latest = null;
    }

    /// <summary>
    /// Aplica o delta. Devolve o estado novo (para confirmar com <see cref="SnapshotAck"/>) ou null se foi ignorado.
    /// </summary>
    public WorldSnapshot? Apply(SnapshotDelta delta)
    {
        if (delta.Epoch != Epoch || (Latest is { } latest && delta.Tick <= latest.Tick))
        {
            return null;
        }

        Dictionary<EntityId, EntitySnapshot> state;
        if (delta.IsFull)
        {
            state = new Dictionary<EntityId, EntitySnapshot>();
        }
        else if (_states.TryGetValue(delta.BaseTick, out var baseState))
        {
            state = new Dictionary<EntityId, EntitySnapshot>(baseState);
        }
        else
        {
            return null; // base que não temos (já descartada): espera o próximo
        }

        foreach (var id in delta.Removed)
        {
            state.Remove(id);
        }

        foreach (var e in delta.Entities)
        {
            if (e.Full || !state.TryGetValue(e.Id, out var before))
            {
                state[e.Id] = EntitySnapshot.Create(e.Id, e.Components);
                continue;
            }

            var removed = e.RemovedComponents.Select(t => TypeOfTag[t]).ToHashSet();
            var changed = e.Components.Select(c => c.GetType()).ToHashSet();
            var merged = before.Components.Where(c => !removed.Contains(c.GetType()) && !changed.Contains(c.GetType())).Concat(e.Components);
            state[e.Id] = EntitySnapshot.Create(e.Id, merged);
        }

        _states[delta.Tick] = state;
        while (_states.Count > History)
        {
            _states.Remove(_states.Keys.First());
        }

        Latest = new WorldSnapshot(delta.Tick, state.Values.OrderBy(s => s.Id.Value).ToList());
        return Latest;
    }
}
