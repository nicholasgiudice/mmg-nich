using Game.Core.Combat;
using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace MmoRpgClient.Match;

/// <summary>
/// Resumo de morte de exemplo para <c>--death-sample</c> (T-397): três lobos e um sangramento, para a prévia da tela de morte
/// sem precisar morrer. Só exibição; nada disso passa pela simulação.
/// </summary>
public static class DeathSample
{
    public static DeathSummary Summary(EntityId victim)
    {
        var wolf1 = new EntityId(9001);
        var wolf2 = new EntityId(9002);
        var wolf3 = new EntityId(9003);
        var hits = new List<DamageDealt>
        {
            Hit(wolf1, victim, "grey_wolf.bite", 14),
            Hit(wolf2, victim, "grey_wolf.bite", 12),
            Hit(wolf1, victim, "status.bleed", 4),
            Hit(wolf3, victim, "grey_wolf.bite", 15),
            Hit(wolf2, victim, "grey_wolf.bite", 13),
            Hit(wolf1, victim, "grey_wolf.bite", 16, killed: true),
        };

        var entries = hits
            .Select((d, i) => new CombatLogEntry(1000 + i * 10, LogCategory.Dano, "", [d.Source, victim], d))
            .ToList();
        var bySource = hits
            .GroupBy(d => (d.Source, d.SourceId))
            .Select(g => new DamageBySource(g.Key.Source, g.Key.SourceId, "Lobo Cinzento", g.Sum(d => d.Amount), 0, g.Count()))
            .OrderByDescending(s => s.Amount)
            .ToList();
        return new DeathSummary(victim, 1060, entries, bySource);
    }

    private static DamageDealt Hit(EntityId source, EntityId target, string sourceId, float amount, bool killed = false) =>
        new(source, target, sourceId, DamageSchool.Physical, amount, 0, amount, 0, killed);
}
