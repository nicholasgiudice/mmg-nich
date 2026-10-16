using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.Combat.Resources;

public enum ResourceChangeReason
{
    /// <summary>Regra de geração da classe (<c>resourceRules</c>). SourceId = id da regra.</summary>
    Rule,

    /// <summary>Efeito <c>resource</c> de habilidade/status/área. SourceId = id da origem.</summary>
    Effect,

    /// <summary>Regeneração/decaimento passivo. SourceId = "passive".</summary>
    Regen,

    /// <summary>Custo de habilidade (já descontado pelo AbilitySystem). SourceId = id da habilidade.</summary>
    Cost,
}

/// <summary>
/// Recurso de uma entidade mudou. Delta = variação efetiva (após limitar a 0..máx; nunca 0).
/// Current = valor logo após a mudança. Custos de habilidade são descontados antes do sistema de recursos
/// rodar, então para <see cref="ResourceChangeReason.Cost"/> Current é o valor no momento da publicação.
/// </summary>
public sealed record ResourceChanged(
    EntityId Entity, string Resource, float Delta, float Current, float Max, ResourceChangeReason Reason, string SourceId) : GameEvent;
