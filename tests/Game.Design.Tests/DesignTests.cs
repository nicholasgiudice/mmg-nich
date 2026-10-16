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
/// Cada verificação roda <see cref="DesignHarness.Runs"/> seeds. Limites finais e justificativas: tasks/reports/balance-v0.1.md (T-016) e balance-v0.2.md (T-018).
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
        // T-018: usa a Provocação de verdade (warrior.taunt), sem a adaptação da T-016.
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
    public void Mage_explosion_alone_wins_at_most_half_the_time_or_takes_longer_than_the_rotation()
    {
        // Design: "se Explosão Elemental resolver o encontro sozinha sem exigir movimento ou gestão de recurso, o Mago está forte demais".
        var rot = Run(Rats, "rotation");
        var spam = Run(Rats, "explosion_spam");
        Assert.True(spam.WinRate <= 0.50 || spam.TimeToWinMean > rot.TimeToWinMean, $"{Fmt(spam)} vs {Fmt(rot)}");
    }

    [Fact]
    public void Mage_rotation_takes_at_least_20_seconds_on_average()
    {
        var rot = Run(Rats, "rotation");
        Assert.True(rot.Wins > 0 && rot.TimeToWinMean >= 20, Fmt(rot));
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
    public void Matriarch_moving_balanced_group_wins_between_60_and_90_percent()
    {
        var s = Run(Matriarch, "kite+dodge");
        Assert.True(s.WinRate is >= 0.60 and <= 0.90, Fmt(s));
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
    public void Matriarch_group_without_cleric_wins_between_5_and_60_percent_and_clearly_less_than_the_full_group()
    {
        var s = Run(NoClericEncounter, "kite+dodge", null, ContentWithNoCleric);
        var full = Run(Matriarch, "kite+dodge");
        Assert.True(s.WinRate is >= 0.05 and <= 0.60, Fmt(s));
        Assert.True(s.WinRate <= full.WinRate - 0.30, $"{Fmt(s)} vs {Fmt(full)}");
    }

    // ---- T-018: resultados não binários ----

    [Fact]
    public void At_least_three_encounters_have_a_relevant_strategy_between_5_and_95_percent()
    {
        var rates = new[]
        {
            Run(Bandit, "rotation"),
            Run(Rats, "explosion_spam"),
            Run(Matriarch, "kite+dodge"),
            Run(NoClericEncounter, "kite+dodge", null, ContentWithNoCleric),
            Run(Rats, "rotation"),
        };
        var distinct = rates.Where(r => r.WinRate is >= 0.05 and <= 0.95).Select(r => r.Encounter).Distinct().ToList();
        Assert.True(distinct.Count >= 3, string.Join("; ", rates.Select(Fmt)));
    }

    // ---- T-018: bots usam as mecânicas da T-017 ----

    private static (World World, Entity Warrior, Entity Mage, Entity Cleric) Group()
    {
        var setup = EncounterSetup.Create(1, Content, Content.Encounters[Matriarch]);
        return (setup.World, setup.Players[0], setup.Players[2], setup.Players[3]);
    }

    private static Dictionary<string, long> FirstUse(World world, BotDirector director, int ticks, Action<int>? each = null)
    {
        var first = new Dictionary<string, long>();
        for (var t = 0; t < ticks; t++)
        {
            each?.Invoke(t);
            director.Tick();
            world.Step();
            foreach (var e in world.LastTickEvents.OfType<AbilityExecuted>())
            {
                first.TryAdd(e.AbilityId, world.Tick);
            }
        }

        return first;
    }

    [Fact]
    public void Cleric_purifies_a_poisoned_ally_after_the_reaction_time()
    {
        var (world, warrior, _, cleric) = Group();
        Game.Core.Combat.Status.StatusManager.Apply(world, null, warrior, "status.poison", 20 * 8);
        var director = new BotDirector(world) { Seed = 1 };
        director.Add(cleric, BotStrategies.Create("rotation"), new BotOptions(Options));
        var first = FirstUse(world, director, 3 * World.TickRate);
        Assert.True(first.TryGetValue("cleric.purify", out var tick), "Purificação não foi usada");
        Assert.InRange(tick, 4, 2 * World.TickRate);
        Assert.False(Game.Core.Combat.Status.StatusManager.HasTag(world, warrior, "poison"));
    }

    [Fact]
    public void Cleric_does_not_purify_when_nobody_has_poison_or_curse()
    {
        var (world, _, _, cleric) = Group();
        var director = new BotDirector(world) { Seed = 1 };
        director.Add(cleric, BotStrategies.Create("rotation"), new BotOptions(Options));
        var first = FirstUse(world, director, 3 * World.TickRate);
        Assert.DoesNotContain("cleric.purify", first.Keys);
    }

    private static Dictionary<string, long> PotionRun(float healthFraction, IReadOnlyDictionary<string, string> options)
    {
        var (world, warrior, _, _) = Group();
        var hp = warrior.Get<Health>();
        hp.Current = hp.Max * healthFraction;
        var director = new BotDirector(world) { Seed = 1 };
        director.Add(warrior, BotStrategies.Create("rotation"), new BotOptions(options));
        var first = FirstUse(world, director, 2 * World.TickRate);
        if (first.ContainsKey("item.minor_healing_potion"))
        {
            Assert.True(hp.Current > hp.Max * healthFraction);
        }

        return first;
    }

    [Fact]
    public void Bot_drinks_a_potion_below_the_threshold_only()
    {
        Assert.Contains("item.minor_healing_potion", PotionRun(0.3f, Options).Keys);
        Assert.DoesNotContain("item.minor_healing_potion", PotionRun(0.8f, Options).Keys);
    }

    [Fact]
    public void Bot_potion_can_be_disabled_or_moved_with_potionBelow()
    {
        var off = new Dictionary<string, string>(Options) { ["potionBelow"] = "0" };
        Assert.DoesNotContain("item.minor_healing_potion", PotionRun(0.3f, off).Keys);
        var high = new Dictionary<string, string>(Options) { ["potionBelow"] = "0.9" };
        Assert.Contains("item.minor_healing_potion", PotionRun(0.8f, high).Keys);
    }

    [Fact]
    public void Warrior_taunts_when_an_ally_is_hit_but_not_otherwise()
    {
        var (world, warrior, _, _) = Group();
        var director = new BotDirector(world) { Seed = 1 };
        director.Add(warrior, BotStrategies.Create("rotation"), new BotOptions(Options));
        Assert.DoesNotContain("warrior.taunt", FirstUse(world, director, 3 * World.TickRate).Keys);

        var (world2, warrior2, mage2, _) = Group();
        var director2 = new BotDirector(world2) { Seed = 1 };
        director2.Add(warrior2, BotStrategies.Create("rotation"), new BotOptions(Options));
        var first = FirstUse(world2, director2, 3 * World.TickRate, t =>
        {
            if (t == 10)
            {
                mage2.Get<Health>().Current -= 20; // o Mago apanha
            }
        });
        Assert.True(first.TryGetValue("warrior.taunt", out var tick), "Provocação não foi usada");
        Assert.True(tick >= 10 + 4, $"reagiu sem tempo de reação (tick {tick})");
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
