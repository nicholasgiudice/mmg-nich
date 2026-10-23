using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.Combat.Progression;

/// <summary>Motivo de rejeição de uma intenção de pontos (a UI traduz).</summary>
public enum ProgressionRejectReason
{
    /// <summary>A entidade não tem progressão (ex.: personagem de encontro de teste).</summary>
    NoProgression,
    NoAttributePoints,
    NoSkillPoints,

    /// <summary>Não é habilidade de classe do personagem (ou é o ataque básico, que não tem NH).</summary>
    NotLearnable,
    MaxRank,
    LevelTooLow,

    /// <summary>Redistribuição só fora de combate.</summary>
    InCombat,
}

/// <summary>Ação de pontos rejeitada. <paramref name="Id"/> = habilidade ou atributo pedido ("" na redistribuição).</summary>
public sealed record ProgressionRejected(EntityId Entity, string Id, ProgressionRejectReason Reason) : GameEvent;

/// <summary>XP ganha ao matar <paramref name="Monster"/> (ou de outra fonte, com <see cref="EntityId.None"/>).</summary>
public sealed record XpGained(EntityId Entity, int Amount, EntityId Monster, string SourceId) : GameEvent;

/// <summary>Subiu para <paramref name="Level"/>; pontos livres depois de subir.</summary>
public sealed record LevelUp(EntityId Entity, int Level, int AttributePoints, int SkillPoints) : GameEvent;

/// <summary>Ponto de atributo gasto; <paramref name="Value"/> = valor final do atributo.</summary>
public sealed record AttributeRaised(EntityId Entity, AttributeKind Attribute, int Value) : GameEvent;

/// <summary>NH da habilidade subiu para <paramref name="Rank"/> (1 = aprendeu).</summary>
public sealed record SkillRankRaised(EntityId Entity, string AbilityId, int Rank) : GameEvent;

/// <summary>Todos os pontos devolvidos (redistribuição).</summary>
public sealed record PointsReset(EntityId Entity, int AttributePoints, int SkillPoints) : GameEvent;
