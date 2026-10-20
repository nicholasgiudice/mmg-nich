using System.Numerics;
using Game.Core.Simulation;

namespace Game.Core.Combat;

/// <summary>Um acerto extra agendado de uma habilidade com vários acertos (T-060).</summary>
public sealed record PendingHit(
    string AbilityId, int Rank, int HitIndex, int HitCount, long DueTick, EntityId? Target, Vector2? AimPoint, double Power = 1);

/// <summary>
/// Acertos extras ainda por acontecer (flechas 2 e 3, explosões repetidas...). Estado interno do servidor: o
/// cliente vê os acertos pelos eventos (<see cref="AbilityHit"/>, dano, telegráfos).
/// </summary>
public sealed class PendingHits : IComponent
{
    public List<PendingHit> Items { get; } = new();

    public IComponent Clone()
    {
        var c = new PendingHits();
        c.Items.AddRange(Items); // registros imutáveis
        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        foreach (var h in Items)
        {
            hasher.Add(h.AbilityId);
            hasher.Add(h.Rank);
            hasher.Add(h.HitIndex);
            hasher.Add(h.HitCount);
            hasher.Add(h.DueTick);
            hasher.Add(h.Target?.Value ?? 0);
            hasher.Add(h.AimPoint ?? new Vector2(float.NaN, float.NaN));
            hasher.Add((float)h.Power);
        }
    }
}
