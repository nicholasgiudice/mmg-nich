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

    /// <summary>Redistribuição (e escolha da segunda classe, T-403) só fora de combate e sem conjurar.</summary>
    InCombat,

    /// <summary>T-403: a segunda classe já foi escolhida (a escolha é definitiva).</summary>
    SecondClassAlreadyChosen,

    /// <summary>T-403: o id não é de uma segunda classe da classe base do personagem.</summary>
    NotASecondClass,
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

/// <summary>
/// T-403: segunda classe escolhida (definitiva). <paramref name="AttributePoints"/> = pontos de atributo livres depois do
/// bônus da escolha.
/// </summary>
public sealed record SecondClassChosen(EntityId Entity, string SecondClassId, int AttributePoints) : GameEvent;

/// <summary>Todos os pontos devolvidos (redistribuição).</summary>
public sealed record PointsReset(EntityId Entity, int AttributePoints, int SkillPoints) : GameEvent;
