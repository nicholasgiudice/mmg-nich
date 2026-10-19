using Game.Core.Combat;
using Game.Core.CombatLog;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-102: o log formatado só com eventos + snapshot + conteúdo é igual ao formatado com o mundo.</summary>
public class CombatLogLookupTests
{
    [Fact]
    public void Log_from_events_and_replicated_snapshots_matches_the_log_from_the_world()
    {
        var content = RealContent.Value;
        var setup = EncounterSetup.Create(5, content, content.Encounters["encounter.forest_matriarch"]);
        var world = setup.World;
        var viewer = setup.Players[0].Id;
        var fromWorld = new CombatLogRecorder(viewer: viewer);
        var fromSnapshots = new CombatLogRecorder(viewer: viewer);
        var deathsWorld = new List<DeathSummary>();
        var deathsSnapshots = new List<DeathSummary>();

        for (var t = 0; t < 1200; t++)
        {
            UseEverything(world, setup.Players, t);
            world.Step();

            deathsWorld.AddRange(fromWorld.Record(world));
            // Cliente de rede: só os eventos do tick, o snapshot replicado e o conteúdo carregado por ele.
            var events = world.LastTickEvents.ToList();
            var snapshot = world.Snapshot(SnapshotScope.Replicated);
            deathsSnapshots.AddRange(fromSnapshots.Record(events, new SnapshotLogLookup(snapshot, content)));
        }

        Assert.True(fromWorld.Entries.Count > 200, $"poucas linhas: {fromWorld.Entries.Count}");
        Assert.Equal(Lines(fromWorld.Entries), Lines(fromSnapshots.Entries));
        Assert.Equal(deathsWorld.Count, deathsSnapshots.Count);
        for (var i = 0; i < deathsWorld.Count; i++)
        {
            Assert.Equal(deathsWorld[i].Victim, deathsSnapshots[i].Victim);
            Assert.Equal(Lines(deathsWorld[i].Entries), Lines(deathsSnapshots[i].Entries));
            Assert.Equal(
                deathsWorld[i].DamageBySources.Select(d => (d.SourceName, d.Amount)),
                deathsSnapshots[i].DamageBySources.Select(d => (d.SourceName, d.Amount)));
        }

        // Cobertura: o encontro gerou as principais categorias do log. (Status aplicados/expirados ainda não têm
        // formatador — só escudos aparecem em "Status"; pendência registrada na T-102.)
        var categories = fromWorld.Entries.Select(e => e.Category).Distinct().ToList();
        Assert.Contains(LogCategory.Dano, categories);
        Assert.Contains(LogCategory.Cura, categories);
        Assert.Contains(LogCategory.Habilidade, categories);
        Assert.Contains(LogCategory.Morte, categories);
    }

    [Fact]
    public void Recording_the_same_tick_twice_does_not_duplicate_lines()
    {
        var content = RealContent.Value;
        var world = EncounterSetup.Create(1, content, content.Encounters["encounter.three_wolves"]).World;
        var log = new CombatLogRecorder();
        for (var t = 0; t < 100; t++)
        {
            world.Step();
            var lookup = new SnapshotLogLookup(world.Snapshot(SnapshotScope.Replicated), content);
            log.Record(world.LastTickEvents, lookup);
            log.Record(world.LastTickEvents, lookup);
        }

        var reference = new CombatLogRecorder();
        var again = EncounterSetup.Create(1, content, content.Encounters["encounter.three_wolves"]).World;
        for (var t = 0; t < 100; t++)
        {
            again.Step();
            reference.Record(again);
        }

        Assert.Equal(Lines(reference.Entries), Lines(log.Entries));
    }

    /// <summary>Cada jogador vivo tenta o ataque básico e uma habilidade por vez na Matriarca (rejeições também entram no log).</summary>
    private static void UseEverything(World world, IReadOnlyList<Entity> players, int tick)
    {
        var boss = world.Entities.FirstOrDefault(e => e.TryGet<UnitInfo>()?.DefinitionId == "monster.forest_matriarch");
        foreach (var p in players.Where(p => p.IsAlive))
        {
            var abilities = p.Get<AbilityState>();
            var target = boss?.Id;
            var aim = boss?.Get<Transform>().Position;
            if (abilities.BasicAttackId is { } basic)
            {
                world.Enqueue(new UseAbilityIntent(p.Id, basic, target, aim));
            }

            if (tick % 10 == 0)
            {
                var all = abilities.Known.Concat(abilities.Consumables).ToList();
                var pick = all[tick / 10 % all.Count];
                var ally = content(world).Abilities.TryGetValue(pick, out var def) && def.Targeting is Definitions.UnitTargeting
                {
                    Faction: Definitions.FactionFilter.Ally,
                };
                world.Enqueue(new UseAbilityIntent(p.Id, pick, ally ? players[0].Id : target, aim));
            }
        }

        static ContentCatalog content(World w) => w.Content;
    }

    private static List<(long Tick, LogCategory Category, string Text)> Lines(IEnumerable<CombatLogEntry> entries) =>
        entries.Select(e => (e.Tick, e.Category, e.Text)).ToList();
}
