using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.CombatLog;

/// <summary>Categoria de uma linha do log (o cliente filtra e colore por ela).</summary>
public enum LogCategory
{
    Dano,
    Cura,
    Status,
    Recurso,
    Fase,
    Morte,

    /// <summary>Ações: conjurações, rejeições, interrupções e deslocamentos forçados.</summary>
    Habilidade,
}

/// <summary>
/// Uma linha do log. Involved = ids de entidades envolvidas (para filtrar); Event = evento de origem
/// (permite agregações, como o resumo de morte).
/// </summary>
public sealed record CombatLogEntry(
    long Tick, LogCategory Category, string Text, IReadOnlyList<EntityId> Involved, GameEvent Event);

/// <summary>Dano recebido por uma entidade de uma fonte (habilidade/status/área), dentro da janela do resumo.</summary>
public sealed record DamageBySource(
    EntityId Source, string SourceId, string SourceName, float Amount, float Absorbed, int Hits);

/// <summary>Resumo de morte: últimas entradas que afetaram a vítima e dano total por fonte.</summary>
public sealed record DeathSummary(
    EntityId Victim,
    long DeathTick,
    IReadOnlyList<CombatLogEntry> Entries,
    IReadOnlyList<DamageBySource> DamageBySources);
