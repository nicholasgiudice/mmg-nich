using Game.Core.Definitions;
using Game.Core.Simulation;
using Vec2 = System.Numerics.Vector2;

namespace MmoRpgClient.Hud;

/// <summary>Monstro vivo visível na tela, candidato à lista de batalha.</summary>
public sealed record BattleCandidate(EntityId Id, Vec2 Position, string Name, int Level, MonsterRank Rank, float HealthFraction);

/// <summary>Linha da lista de batalha, na ordem de exibição.</summary>
public sealed record BattleEntry(
    EntityId Id, string Name, int Level, MonsterRank Rank, float HealthFraction, bool TargetsPlayer, bool Selected);

/// <summary>
/// Lista de batalha (T-312): os monstros da tela, com o alvo atual primeiro e os outros por distância. Só lógica, sem
/// Godot. A ordem por distância não "pula": ela é refeita no máximo <see cref="ReordersPerSecond"/> vezes por
/// segundo, e dois monstros só trocam de lugar quando a diferença de distância passa de <see cref="SwapDistance"/>.
/// Tab e Shift+Tab percorrem a ordem por distância (sem a promoção do alvo atual, senão o Tab ficaria
/// alternando entre os dois primeiros).
/// </summary>
public sealed class BattleList
{
    public const double ReordersPerSecond = 4;
    public const float SwapDistance = 1f;

    /// <summary>Por quanto tempo (ticks, 20 Hz) um monstro conta como "mirando o jogador" depois de mirá-lo.</summary>
    public const long AimMemoryTicks = 60;

    private readonly List<EntityId> _byDistance = new();
    private readonly Dictionary<EntityId, long> _aimedAtPlayer = new();
    private double _sinceReorder = double.MaxValue;
    private List<BattleEntry> _entries = new();

    /// <summary>Linhas na ordem de exibição (alvo atual primeiro).</summary>
    public IReadOnlyList<BattleEntry> Entries => _entries;

    /// <summary>Ordem por distância, com histerese (a mesma que o Tab usa).</summary>
    public IReadOnlyList<EntityId> TabOrder => _byDistance;

    /// <summary>O monstro mirou o jogador neste tick (início de habilidade, alvo atingido ou dano).</summary>
    public void NoteAimAtPlayer(EntityId monster, long tick) => _aimedAtPlayer[monster] = tick;

    /// <param name="candidates">Monstros vivos na tela.</param>
    /// <param name="castingAtPlayer">Monstros conjurando com o jogador como alvo (estado replicado).</param>
    /// <param name="delta">Segundos desde a última chamada.</param>
    public void Update(
        IReadOnlyList<BattleCandidate> candidates, Vec2 playerPosition, EntityId? selected, long tick, double delta,
        IReadOnlySet<EntityId>? castingAtPlayer = null)
    {
        var byId = candidates.ToDictionary(c => c.Id);
        float Distance(EntityId id) => Vec2.Distance(byId[id].Position, playerPosition);

        _byDistance.RemoveAll(id => !byId.ContainsKey(id));
        foreach (var gone in _aimedAtPlayer.Keys.Where(id => tick - _aimedAtPlayer[id] > AimMemoryTicks).ToList())
        {
            _aimedAtPlayer.Remove(gone);
        }

        // Novos entram na posição pela distância, sem mexer nos que já estão.
        foreach (var fresh in candidates.Where(c => !_byDistance.Contains(c.Id)).OrderBy(c => Distance(c.Id)).ThenBy(c => c.Id.Value))
        {
            var d = Distance(fresh.Id);
            var at = _byDistance.FindIndex(id => Distance(id) > d);
            _byDistance.Insert(at < 0 ? _byDistance.Count : at, fresh.Id);
        }

        _sinceReorder += delta;
        if (_sinceReorder >= 1.0 / ReordersPerSecond)
        {
            _sinceReorder = 0;
            Reorder(Distance);
        }

        var display = new List<EntityId>(_byDistance);
        if (selected is { } sel && display.Remove(sel))
        {
            display.Insert(0, sel);
        }

        _entries = display.Select(id =>
        {
            var c = byId[id];
            var aiming = _aimedAtPlayer.ContainsKey(id) || (castingAtPlayer?.Contains(id) ?? false);
            return new BattleEntry(c.Id, c.Name, c.Level, c.Rank, c.HealthFraction, aiming, id == selected);
        }).ToList();
    }

    /// <summary>Próximo monstro depois de <paramref name="current"/> na ordem por distância (com volta); sem atual, o mais perto.</summary>
    public EntityId? Next(EntityId? current) => Step(current, +1);

    /// <summary>Monstro anterior a <paramref name="current"/> (com volta); sem atual, o mais longe.</summary>
    public EntityId? Previous(EntityId? current) => Step(current, -1);

    private EntityId? Step(EntityId? current, int direction)
    {
        if (_byDistance.Count == 0)
        {
            return null;
        }

        var index = current is { } c ? _byDistance.IndexOf(c) : -1;
        if (index < 0)
        {
            return direction > 0 ? _byDistance[0] : _byDistance[^1];
        }

        var count = _byDistance.Count;
        return _byDistance[(index + direction + count) % count];
    }

    /// <summary>Bolha com limiar: troca vizinhos só quando o de trás está mais perto por mais de <see cref="SwapDistance"/>.</summary>
    private void Reorder(Func<EntityId, float> distance)
    {
        bool swapped;
        do
        {
            swapped = false;
            for (var i = 0; i + 1 < _byDistance.Count; i++)
            {
                if (distance(_byDistance[i]) - distance(_byDistance[i + 1]) > SwapDistance)
                {
                    (_byDistance[i], _byDistance[i + 1]) = (_byDistance[i + 1], _byDistance[i]);
                    swapped = true;
                }
            }
        }
        while (swapped);
    }
}
