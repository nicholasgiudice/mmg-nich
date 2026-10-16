namespace Game.Core.Definitions;

/// <summary>Escola de dano (valores JSON em snake_case: physical, arcane, ...).</summary>
public enum DamageSchool { Physical, Arcane, Fire, Frost, Holy, Nature }

/// <summary>Facção alvo/afetada em relação a quem usa.</summary>
public enum FactionFilter { Enemy, Ally, Any }

public enum DisplacementKind { Dash, Knockback }

public enum DisplacementDirection { Aim, AwayFromSource, TowardTarget, Backward }

public enum StackingMode { Refresh, Stack }

public enum AreaMode { Periodic, OnEnterOnce }

public enum MonsterRank { Normal, Elite, Boss }

public enum VictoryCondition { AllEnemiesDead }

public enum DefeatCondition { AllPlayersDead }
