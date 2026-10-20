using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Telegraphs;

/// <summary>
/// Telegráfo em andamento (entidade sem vida: tem <c>Transform</c> = origem/direção e este componente).
/// Os efeitos são os da habilidade <see cref="AbilityId"/>; a forma é fixada no início.
/// </summary>
public sealed class TelegraphState : IComponent, IReplicated
{
    public EntityId Source { get; init; }
    public string AbilityId { get; init; } = "";

    /// <summary>NH da habilidade quando o telegráfo nasceu (T-060): a resolução usa a definição desse nível.</summary>
    public int Rank { get; init; } = 1;

    /// <summary>Potência do uso (T-061) e acerto (1..N) que este telegráfo resolve.</summary>
    public double Power { get; init; } = 1;
    public int HitIndex { get; init; } = 1;
    public TelegraphShape Shape { get; init; }
    public float Radius { get; init; }
    public float Length { get; init; }
    public float Width { get; init; }

    /// <summary>Abertura total do cone, em graus.</summary>
    public float AngleDegrees { get; init; }
    public FactionFilter Affects { get; init; }
    public bool Charge { get; init; }

    /// <summary>Facção da fonte quando o telegráfo nasceu (a fonte pode morrer/sumir antes de resolver).</summary>
    public Faction? SourceFaction { get; init; }
    public long StartTick { get; init; }
    public long ResolveAtTick { get; init; }

    public IComponent Clone() => this; // imutável

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Source.Value);
        hasher.Add(AbilityId);
        hasher.Add(Rank);
        hasher.Add((float)Power);
        hasher.Add(HitIndex);
        hasher.Add((int)Shape);
        hasher.Add(Radius);
        hasher.Add(Length);
        hasher.Add(Width);
        hasher.Add(AngleDegrees);
        hasher.Add((int)Affects);
        hasher.Add(Charge);
        hasher.Add(SourceFaction.HasValue ? (int)SourceFaction.Value : -1);
        hasher.Add(StartTick);
        hasher.Add(ResolveAtTick);
    }
}

/// <summary>Um telegráfo foi criado. Origem e direção estão fixas até a resolução (<paramref name="ResolveAtTick"/>).</summary>
public sealed record TelegraphStarted(
    EntityId Telegraph,
    EntityId Source,
    string AbilityId,
    TelegraphShape Shape,
    Vector2 Origin,
    Vector2 Direction,
    float Radius,
    float Length,
    float Width,
    float AngleDegrees,
    long ResolveAtTick) : GameEvent;

/// <summary>
/// O telegráfo foi resolvido: os efeitos foram aplicados a <paramref name="Targets"/> (quem estava dentro no tick).
/// <paramref name="Cancelled"/> = a fonte morreu/sumiu antes, nada foi aplicado.
/// </summary>
public sealed record TelegraphResolved(
    EntityId Telegraph, EntityId Source, string AbilityId, IReadOnlyList<EntityId> Targets, bool Cancelled) : GameEvent;
