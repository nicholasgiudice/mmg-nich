using Game.Core.Simulation;

namespace Game.Core.Combat.Resources;

/// <summary>Estado interno do sistema de recursos por entidade (criado sob demanda).</summary>
public sealed class ResourceState : IComponent
{
    public const long Never = long.MinValue / 2;

    /// <summary>Último tick em que a entidade deu/recebeu dano, curou, protegeu ou usou habilidade.</summary>
    public long LastCombatTick = Never;

    /// <summary>Fração acumulada de regeneração ainda não aplicada.</summary>
    public float RegenRemainder;

    /// <summary>Id da regra -> primeiro tick em que pode disparar de novo.</summary>
    public SortedDictionary<string, long> RuleReadyAt { get; } = new(StringComparer.Ordinal);

    public IComponent Clone()
    {
        var c = new ResourceState { LastCombatTick = LastCombatTick, RegenRemainder = RegenRemainder };
        foreach (var kv in RuleReadyAt)
        {
            c.RuleReadyAt[kv.Key] = kv.Value;
        }

        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(LastCombatTick);
        hasher.Add(RegenRemainder);
        foreach (var kv in RuleReadyAt)
        {
            hasher.Add(kv.Key);
            hasher.Add(kv.Value);
        }
    }
}
