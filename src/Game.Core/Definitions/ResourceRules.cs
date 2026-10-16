namespace Game.Core.Definitions;

/// <summary>
/// Evento que dispara uma regra de geração de recurso (JSON em snake_case).
/// Quem ganha o recurso (o "dono") depende do gatilho:
/// <c>damage_taken</c> = quem recebeu o dano; os demais = quem causou/fez a ação
/// (<c>status_removed</c> = quem removeu o status; <c>interrupt_done</c> = quem interrompeu).
/// </summary>
public enum ResourceTrigger
{
    DamageTaken,
    DamageDealt,
    HealDone,
    ShieldApplied,
    StatusRemoved,
    InterruptDone,
    AbilityExecuted,
}

/// <summary>
/// Regra de geração de recurso de uma classe. Quantidade gerada = <see cref="Amount"/> + <see cref="Ratio"/> x valor do evento
/// (dano final, cura efetiva, valor do escudo; 0 nos demais gatilhos). "Alvo" e "fonte" nas condições são os do evento
/// (em <c>damage_taken</c> o alvo é o próprio dono).
/// </summary>
public sealed record ResourceRuleDefinition
{
    public required string Id { get; init; }
    public required ResourceTrigger Trigger { get; init; }

    /// <summary>Quantidade fixa (pode ser negativa = gasto).</summary>
    public double Amount { get; init; }

    /// <summary>Fração do valor do evento (ex.: 0.1 = 1 de recurso a cada 10 de dano).</summary>
    public double Ratio { get; init; }

    /// <summary>Valor mínimo do evento (para cura, a cura efetiva).</summary>
    public double? MinValue { get; init; }
    public string? TargetTag { get; init; }

    /// <summary>Tag da habilidade de origem (em <c>interrupt_done</c>: da habilidade interrompida).</summary>
    public string? AbilityTag { get; init; }
    public double? MinDistance { get; init; }
    public double? MaxDistance { get; init; }
    public string? TargetHasStatusTag { get; init; }

    /// <summary>O dono da regra (quem ganha o recurso) tem status com esta tag. Ex.: dano recebido enquanto com "guard" conta como bloqueio.</summary>
    public string? SourceHasStatusTag { get; init; }

    /// <summary>Só <c>heal_done</c>: cura efetiva mínima em % da vida máxima do alvo (0..100).</summary>
    public double? MinEffectiveHealPercent { get; init; }

    /// <summary>Só <c>status_removed</c>: tag do status removido.</summary>
    public string? RemovedStatusTag { get; init; }

    /// <summary>Recarga interna da regra, por dono (0 = sem).</summary>
    public int InternalCooldownTicks { get; init; }
}
