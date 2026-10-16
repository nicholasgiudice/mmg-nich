namespace Game.Core.Definitions;

/// <summary>Escola de dano (valores JSON em snake_case: physical, arcane, ...).</summary>
public enum DamageSchool { Physical, Arcane, Fire, Frost, Holy, Nature }

/// <summary>A quem um efeito se aplica (T-017): aos alvos resolvidos da habilidade (padrão) ou ao conjurador.</summary>
public enum EffectTarget { Targets, Self }

/// <summary>Facção alvo/afetada em relação a quem usa.</summary>
public enum FactionFilter { Enemy, Ally, Any }

public enum DisplacementKind { Dash, Knockback }

public enum DisplacementDirection { Aim, AwayFromSource, TowardTarget, Backward }

public enum StackingMode { Refresh, Stack }

public enum AreaMode { Periodic, OnEnterOnce }

public enum MonsterRank { Normal, Elite, Boss }

public enum VictoryCondition { AllEnemiesDead }

public enum DefeatCondition { AllPlayersDead }

/// <summary>Forma de um telegráfo (JSON: circle, cone, line).</summary>
public enum TelegraphShape { Circle, Cone, Line }

/// <summary>Onde o telegráfo é ancorado: na fonte (posição da fonte, direção da mira) ou no ponto mirado (alvo/ponto).</summary>
public enum TelegraphAnchor { Source, Aim }
