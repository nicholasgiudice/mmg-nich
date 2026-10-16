using Game.Core.CombatLog;

namespace MmoRpgClient.Authority;

/// <summary>
/// Autoridade que também fornece o log de combate formatado (T-013). O formatador precisa do
/// <c>World</c>, que só a autoridade local tem; o cliente de rede (fase 1) implementará isto com as
/// linhas vindas do servidor. O HUD funciona sem log quando a autoridade não implementa esta interface.
/// </summary>
public interface ICombatLogSource
{
    /// <summary>Linhas em ordem (as mais antigas saem quando o log enche).</summary>
    IReadOnlyList<CombatLogEntry> LogEntries { get; }

    /// <summary>Resumos de morte gerados desde a chamada anterior.</summary>
    IReadOnlyList<DeathSummary> TakeDeathSummaries();
}
