using Game.Bots;
using Game.Core.Combat;
using Game.Core.Combat.Resources;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Sim;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// Critérios de design da seção "Falha de design" de cada encontro (design/combat-encounters-v0.1.md) como testes.
/// Cada verificação roda <see cref="DesignHarness.Runs"/> seeds. Limites finais e justificativas: tasks/reports/balance-v0.1.md.
/// </summary>
public class DesignTests
{
    private const string Wolves = "encounter.three_wolves";
    private const string Bandit = "encounter.wolf_and_bandit";
    private const string Rats = "encounter.four_rats";
    private const string Guard = "encounter.skeleton_guard";
    private const string Matriarch = "encounter.forest_matriarch";

    private static string Fmt(BatchSummary s) => $"{s.Encounter} / {s.Strategy}: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s";

    // ---- Guerreiro x 3 Lobos ----

    [Fact]
    public void Warrior_rotation_beats_wolves_at_least_90_percent()
    {
        var s = Run(Wolves, "rotation");
        Assert.True(s.WinRate >= 0.90, Fmt(s));
    }

    [Fact]
    public void Warrior_threat_skill_plus_basic_is_shallow_loses_or_is_much_slower()
    {
        // Adaptação: o Guerreiro do protótipo não tem Provocação; "taunt" = Golpe Circular (ameaça em área) + básico.
        var rot = Run(Wolves, "rotation");
        var shallow = Run(Wolves, "taunt_and_basic");
        var ok = shallow.WinRate <= 0.50 || (shallow.Wins > 0 && shallow.TimeToWinMean >= 1.5 * rot.TimeToWinMean);
        Assert.True(ok, $"{Fmt(shallow)} vs {Fmt(rot)}");
    }

    // ---- Arqueiro x Lobo + Salteador ----

    [Fact]
    public void Archer_kite_beats_wolf_and_bandit_at_least_85_percent()
    {
        var s = Run(Bandit, "kite");
        Assert.True(s.WinRate >= 0.85, Fmt(s));
    }

    [Fact]
    public void Archer_standing_still_does_not_win()
    {
        // idle = parado (só atira se algo entrar no alcance); rotation = persegue sem recuar e é cercado.
        var idle = Run(Bandit, "idle");
        var rot = Run(Bandit, "rotation");
        Assert.True(idle.WinRate <= 0.40, Fmt(idle));
        Assert.True(rot.WinRate <= 0.40, Fmt(rot));
    }

    // ---- Mago x 4 Ratos ----

    [Fact]
    public void Mage_rotation_beats_rats_at_least_85_percent()
    {
        var s = Run(Rats, "rotation");
        Assert.True(s.WinRate >= 0.85, Fmt(s));
    }

    [Fact]
    public void Mage_explosion_spam_runs_out_of_mana_before_the_end_in_most_runs()
    {
        var blast = Content.Abilities["mage.elemental_blast"];
        var cost = blast.Cost?.Amount ?? 0;
        Assert.True(cost > 0);
        var encounter = Content.Encounters[Rats];
        var dry = 0;
        for (var i = 0; i < Runs; i++)
        {
            var setup = EncounterSetup.Create(1 + (ulong)i, Content, encounter);
            var world = setup.World;
            var mage = setup.Players[0];
            var director = new BotDirector(world);
            director.Add(mage, BotStrategies.Create("explosion_spam"), new BotOptions(Options));
            var casts = 0;
            var wasDry = false;
            for (var t = 0; t < 120 * World.TickRate; t++)
            {
                director.Tick();
                world.Step();
                casts += world.LastTickEvents.OfType<AbilityExecuted>().Count(e => e.AbilityId == blast.Id);
                var enemiesAlive = world.Entities.Any(e => e.IsAlive && e.TryGet<FactionTag>() is { Faction: Faction.Monster });
                if (!enemiesAlive || !mage.IsAlive)
                {
                    break;
                }

                // sem Mana: já gastou a reserva e não consegue conjurar a próxima Explosão com inimigos vivos
                if (casts > 0 && mage.Get<ResourcePool>().Current < cost)
                {
                    wasDry = true;
                }
            }

            dry += wasDry ? 1 : 0;
        }

        Assert.True(dry > Runs / 2.0, $"sem Mana em {dry}/{Runs} execuções");
    }

    // ---- Clérigo x Guarda Esquelético ----

    [Fact]
    public void Cleric_rotation_beats_guard_at_least_85_percent()
    {
        var s = Run(Guard, "rotation");
        Assert.True(s.WinRate >= 0.85, Fmt(s));
    }

    [Fact]
    public void Cleric_is_slower_than_other_classes_by_1_2_to_1_6()
    {
        var cleric = Run(Guard, "rotation");
        var others = new[] { Run(Wolves, "rotation"), Run(Bandit, "kite"), Run(Rats, "rotation") };
        Assert.All(others, o => Assert.True(o.Wins > 0, Fmt(o)));
        var mean = others.Average(o => o.TimeToWinMean);
        var ratio = cleric.TimeToWinMean / mean;
        Assert.True(ratio is >= 1.2 and <= 1.6, $"cleric {cleric.TimeToWinMean:F1}s / media {mean:F1}s = {ratio:F2}");
    }

    [Fact]
    public void Cleric_heal_only_does_not_win()
    {
        var s = Run(Guard, "heal_only");
        Assert.Equal(0, s.Wins);
    }

    // ---- Grupo x Matriarca ----

    [Fact]
    public void Matriarch_moving_balanced_group_wins_at_least_70_percent()
    {
        var s = Run(Matriarch, "kite+dodge");
        Assert.True(s.WinRate >= 0.70, Fmt(s));
    }

    [Fact]
    public void Matriarch_standing_group_wins_at_most_20_percent()
    {
        var s = Run(Matriarch, "idle");
        Assert.True(s.WinRate <= 0.20, Fmt(s));
        var r = Run(Matriarch, "rotation");
        Assert.True(r.WinRate <= 0.20, Fmt(r));
    }

    [Fact]
    public void Matriarch_group_without_cleric_wins_between_30_and_70_percent()
    {
        var s = Run(NoClericEncounter, "kite+dodge", null, ContentWithNoCleric);
        Assert.True(s.WinRate is >= 0.30 and <= 0.70, Fmt(s));
    }

    [Fact]
    public void Hazard_source_sees_matriarch_telegraphs_and_clears_them_on_resolution()
    {
        var setup = EncounterSetup.Create(1, Content, Content.Encounters[Matriarch]);
        var world = setup.World;
        var source = new TelegraphHazardSource();
        var director = new BotDirector(world, source);
        foreach (var p in setup.Players)
        {
            director.Add(p, BotStrategies.Create("kite+dodge"), new BotOptions(Options));
        }

        var seen = 0;
        var buffer = new List<Hazard>();
        for (var t = 0; t < 40 * World.TickRate; t++)
        {
            director.Tick();
            world.Step();
            buffer.Clear();
            source.Collect(world, buffer);
            seen = Math.Max(seen, buffer.Count);
            Assert.All(buffer, h => Assert.True(h.ResolveTick >= world.Tick));
        }

        Assert.True(seen > 0, "nenhum telegrafo foi visto em 40 s");
    }
}
