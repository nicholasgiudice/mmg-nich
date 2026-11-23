using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Stats;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-431: mecânicas novas do Atirador de Elite — dano menor em quem está atrás (Flecha Perfurante), crítico pela distância
/// até o alvo e multiplicador próprio (Mira Firme), alcance a mais vindo de um status (Mira Firme NH3, Olho de Falcão NH3),
/// ignorar parte da armadura (Tiro Certeiro) e crítico garantido (Olho de Falcão). Dados de teste, não as habilidades reais
/// (T-433).
/// </summary>
public class MarksmanMechanicsTests
{
    private const int Second = World.TickRate;

    private static readonly ClassDefinition TestClass = new()
    {
        Id = "class.test_archer",
        Name = "Arqueiro",
        Speed = 4.5,
        Radius = 0.45,
        Resource = new ResourceDefinition { Type = "focus", Max = 100, Initial = 100, RegenPerSecond = 5 },
        BasicAttack = "archer.test_shot",
        Attributes = new AttributeSet { Str = 4, Dex = 10, Agi = 8, Int = 2, Vit = 6 },
        BaseHealth = 70,
    };

    private static readonly AbilityDefinition Shot = new()
    {
        Id = "archer.test_shot",
        Name = "Disparo",
        Tags = ["basic", "ranged"],
        Targeting = new UnitTargeting { Range = 10 },
        Effects = [new DamageEffect { Base = 100 }],
    };

    // Mesmo alcance, sem a tag ranged: o alcance a mais não vale.
    private static readonly AbilityDefinition Kick = new()
    {
        Id = "archer.test_kick",
        Name = "Chute Longo",
        Targeting = new UnitTargeting { Range = 10 },
        Effects = [new DamageEffect { Base = 100 }],
    };

    private static readonly AbilityDefinition Piercing = new()
    {
        Id = "marksman.test_piercing_arrow",
        Name = "Flecha Perfurante",
        Tags = ["ranged"],
        Targeting = new LineTargeting { Length = 10, Width = 1 },
        Effects = [new DamageEffect { Base = 100, BehindFraction = 0.6 }],
    };

    private static readonly AbilityDefinition SureShot = new()
    {
        Id = "marksman.test_sure_shot",
        Name = "Tiro Certeiro",
        Tags = ["ranged"],
        Targeting = new UnitTargeting { Range = 12 },
        Effects = [new DamageEffect { Base = 100, IgnoreArmor = 0.5 }],
    };

    private static readonly StatusDefinition SteadyAim = new()
    {
        Id = "status.test_steady_aim",
        Name = "Mira Firme",
        DefaultDurationTicks = 1,
        Crit = new StatusCritDefinition { Bonus = 0.1, MinDistance = 6 },
    };

    private static readonly StatusDefinition SteadyAim5 = new()
    {
        Id = "status.test_steady_aim_5",
        Name = "Mira Firme",
        DefaultDurationTicks = 1,
        Crit = new StatusCritDefinition { Bonus = 0.12, MinDistance = 6, Multiplier = 1.65 },
        RangeBonus = 1,
    };

    private static readonly StatusDefinition HawkEye = new()
    {
        Id = "status.test_hawk_eye",
        Name = "Olho de Falcão",
        DefaultDurationTicks = 20 * Second,
        Crit = new StatusCritDefinition { Guaranteed = true },
    };

    private static readonly StatusDefinition Reach = new()
    {
        Id = "status.test_reach",
        Name = "Alcance",
        DefaultDurationTicks = 100 * Second,
        RangeBonus = 1,
    };

    private static readonly StatusDefinition Poison = new()
    {
        Id = "status.test_poison",
        Name = "Veneno",
        DefaultDurationTicks = 30 * Second,
        Periodic = [new PeriodicEffectDefinition { IntervalTicks = Second, Effects = [new DamageEffect { Base = 10 }] }],
    };

    private static World NewWorld(ulong seed = 7) => WorldBuilder.CreateDefault(
        seed,
        null,
        new ContentCatalog(
            classes: [TestClass],
            abilities: [Shot, Kick, Piercing, SureShot],
            statuses: [SteadyAim, SteadyAim5, HawkEye, Reach, Poison]));

    private static Entity Archer(World w, Vector2 pos, int agi = 8)
    {
        var p = EntityFactory.SpawnPlayer(w, TestClass, pos);
        p.Get<AbilityState>().Known.AddRange([Kick.Id, Piercing.Id, SureShot.Id]);
        p.Get<CharacterStats>().Agi = agi;
        return p;
    }

    /// <summary>Atirador sem estatísticas (nunca crita): conta de dano exata.</summary>
    private static Entity Shooter(World w, Vector2 pos)
    {
        var st = new AbilityState();
        st.Known.AddRange([Shot.Id, Kick.Id, Piercing.Id, SureShot.Id]);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(1000),
            new FactionTag(Faction.Player),
            new Collider(0.45f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = "Atirador" },
            st);
    }

    private static Entity Monster(World w, Vector2 pos, float hp = 1_000_000, double armor = 0) => w.Spawn(
        new Transform { Position = pos },
        new Health(hp),
        new FactionTag(Faction.Monster),
        new Collider(0.4f),
        new Movement(4),
        new UnitInfo { DefinitionId = "test", Name = "Lobo", Armor = armor },
        new AbilityState());

    private static List<GameEvent> Run(World w, int ticks = 1)
    {
        var all = new List<GameEvent>();
        for (var i = 0; i < ticks; i++)
        {
            w.Step();
            all.AddRange(w.LastTickEvents);
        }

        return all;
    }

    /// <summary>Um disparo por tick (recarga 0); devolve os danos na ordem.</summary>
    private static List<DamageDealt> Shoot(World w, Entity archer, Entity target, int count, string ability = "archer.test_shot")
    {
        var hits = new List<DamageDealt>();
        for (var i = 0; i < count; i++)
        {
            w.Enqueue(new UseAbilityIntent(archer.Id, ability, target.Id));
            hits.AddRange(Run(w).OfType<DamageDealt>());
        }

        return hits;
    }

    // ---------- dano menor em quem está atrás ----------

    [Fact]
    public void Line_hits_the_nearest_target_in_full_and_the_ones_behind_at_the_fraction()
    {
        var w = NewWorld();
        var shooter = Shooter(w, new Vector2(1, 1));
        var far = Monster(w, new Vector2(7, 1)); // criados fora de ordem: quem vale é a distância
        var middle = Monster(w, new Vector2(5, 1));
        var near = Monster(w, new Vector2(3, 1));

        w.Enqueue(new UseAbilityIntent(shooter.Id, Piercing.Id, null, new Vector2(10, 1)));
        var hits = Run(w).OfType<DamageDealt>().ToDictionary(d => d.Target, d => d.Amount);

        Assert.Equal(3, hits.Count);
        Assert.Equal(100, hits[near.Id]);
        Assert.Equal(60, hits[middle.Id]);
        Assert.Equal(60, hits[far.Id]);
    }

    [Fact]
    public void First_target_dying_on_the_hit_still_counts_as_the_first()
    {
        var w = NewWorld();
        var shooter = Shooter(w, new Vector2(1, 1));
        var near = Monster(w, new Vector2(3, 1), hp: 50);
        var behind = Monster(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(shooter.Id, Piercing.Id, null, new Vector2(10, 1)));
        var hits = Run(w).OfType<DamageDealt>().ToDictionary(d => d.Target, d => d.Amount);

        Assert.False(near.IsAlive);
        Assert.Equal(100, hits[near.Id]);
        Assert.Equal(60, hits[behind.Id]);
    }

    [Fact]
    public void Single_target_takes_full_damage_with_behind_fraction()
    {
        var w = NewWorld();
        var shooter = Shooter(w, new Vector2(1, 1));
        var only = Monster(w, new Vector2(6, 1));

        w.Enqueue(new UseAbilityIntent(shooter.Id, Piercing.Id, null, new Vector2(10, 1)));

        Assert.Equal(100, Assert.Single(Run(w).OfType<DamageDealt>(), d => d.Target == only.Id).Amount);
    }

    // ---------- crítico pela distância ----------

    [Fact]
    public void Steady_aim_adds_crit_chance_only_against_far_targets()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1));
        var far = Monster(w, new Vector2(8, 1)); // a 7 tiles
        var close = Monster(w, new Vector2(6, 1)); // a 5 tiles
        var baseChance = StatMath.CritChance(archer.Get<CharacterStats>(), w.Content.Rules.Stats);
        Assert.Equal(baseChance, StatusCrit.Chance(w, archer, far, Shot), 6);

        StatusManager.ApplyPermanent(w, archer, SteadyAim.Id);

        Assert.Equal(baseChance + 0.1, StatusCrit.Chance(w, archer, far, Shot), 6);
        Assert.Equal(baseChance, StatusCrit.Chance(w, archer, close, Shot), 6);
    }

    [Fact]
    public void Steady_aim_5_crit_deals_1_65_at_distance_and_1_5_up_close()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1));
        var far = Monster(w, new Vector2(8, 1));
        var close = Monster(w, new Vector2(6, 1));
        StatusManager.ApplyPermanent(w, archer, SteadyAim5.Id);
        StatusManager.Apply(w, archer, archer, HawkEye.Id); // todos críticos, para comparar os multiplicadores

        var atFar = Assert.Single(Shoot(w, archer, far, 1));
        var atClose = Assert.Single(Shoot(w, archer, close, 1));

        Assert.True(atFar.Critical && atClose.Critical);
        Assert.Equal(165, atFar.Amount);
        Assert.Equal(150, atClose.Amount);
    }

    [Fact]
    public void Steady_aim_5_rolled_crits_use_its_multiplier()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1));
        var far = Monster(w, new Vector2(8, 1));
        StatusManager.ApplyPermanent(w, archer, SteadyAim5.Id);

        var hits = Shoot(w, archer, far, 60);

        Assert.Contains(hits, h => h.Critical);
        Assert.Contains(hits, h => !h.Critical);
        Assert.All(hits, h => Assert.Equal(h.Critical ? 165 : 100, h.Amount));
    }

    // ---------- alcance a mais ----------

    [Fact]
    public void Range_bonus_lets_a_range_10_shot_reach_11_but_not_12()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1));
        var at11 = Monster(w, new Vector2(12, 1));
        var at12 = Monster(w, new Vector2(1, 13));

        w.Enqueue(new UseAbilityIntent(archer.Id, Shot.Id, at11.Id));
        Assert.Equal(AbilityRejectReason.OutOfRange, Assert.Single(Run(w).OfType<AbilityRejected>()).Reason);

        StatusManager.Apply(w, archer, archer, Reach.Id);
        Assert.Equal(1, RangeBonus.For(w, archer, Shot));
        Assert.Single(Shoot(w, archer, at11, 1));

        w.Enqueue(new UseAbilityIntent(archer.Id, Shot.Id, at12.Id));
        Assert.Equal(AbilityRejectReason.OutOfRange, Assert.Single(Run(w).OfType<AbilityRejected>()).Reason);
    }

    [Fact]
    public void Range_bonus_does_not_change_an_ability_without_the_ranged_tag()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1));
        var at11 = Monster(w, new Vector2(12, 1));
        StatusManager.Apply(w, archer, archer, Reach.Id);

        w.Enqueue(new UseAbilityIntent(archer.Id, Kick.Id, at11.Id));

        Assert.Equal(0, RangeBonus.For(w, archer, Kick));
        Assert.Equal(AbilityRejectReason.OutOfRange, Assert.Single(Run(w).OfType<AbilityRejected>()).Reason);
    }

    [Fact]
    public void Range_bonus_lengthens_lines()
    {
        var w = NewWorld();
        var shooter = Shooter(w, new Vector2(1, 1));
        var at11 = Monster(w, new Vector2(12, 1));

        w.Enqueue(new UseAbilityIntent(shooter.Id, Piercing.Id, null, new Vector2(20, 1)));
        Assert.Empty(Run(w).OfType<DamageDealt>());

        StatusManager.Apply(w, shooter, shooter, Reach.Id);
        w.Enqueue(new UseAbilityIntent(shooter.Id, Piercing.Id, null, new Vector2(20, 1)));
        Assert.Equal(at11.Id, Assert.Single(Run(w).OfType<DamageDealt>()).Target);
    }

    [Fact]
    public void Range_bonus_ends_with_the_status()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1));
        StatusManager.Apply(w, archer, archer, Reach.Id);
        Assert.Equal(1, RangeBonus.For(w, archer, Shot));

        StatusManager.Remove(w, archer, Reach.Id, archer.Id);

        Assert.Equal(0, RangeBonus.For(w, archer, Shot));
    }

    // ---------- ignorar armadura ----------

    [Fact]
    public void Sure_shot_ignoring_half_the_armor_deals_the_damage_of_half_the_armor()
    {
        var w = NewWorld();
        var shooter = Shooter(w, new Vector2(1, 1));
        var armored = Monster(w, new Vector2(5, 1), armor: 100);
        var r = w.Content.Rules.Stats;
        double Expected(double armor) => CoreEffects.Round(100 * (1 - Math.Min(r.DefenseMax, armor / (armor + r.DefenseBase + r.DefensePerAttackerLevel))));

        w.Enqueue(new UseAbilityIntent(shooter.Id, SureShot.Id, armored.Id));
        var sure = Assert.Single(Run(w).OfType<DamageDealt>());
        w.Enqueue(new UseAbilityIntent(shooter.Id, Kick.Id, armored.Id));
        var normal = Assert.Single(Run(w).OfType<DamageDealt>());

        Assert.Equal(Expected(50), sure.Amount); // 100 × (1 − 50 / 98) = 48,98 → 49
        Assert.Equal(Expected(100), normal.Amount); // 100 × (1 − 100 / 148) = 32,4 → 32
        Assert.Equal(49, sure.Amount);
    }

    // ---------- crítico garantido ----------

    [Fact]
    public void Hawk_eye_makes_100_shots_in_a_row_critical_even_at_the_crit_cap()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1), agi: 1000);
        var wolf = Monster(w, new Vector2(4, 1));
        var r = w.Content.Rules.Stats;
        Assert.Equal(r.CritMax, StatusCrit.Chance(w, archer, wolf, Shot), 6);

        StatusManager.Apply(w, archer, archer, HawkEye.Id, durationTicks: 1000 * Second);
        Assert.Equal(1, StatusCrit.Chance(w, archer, wolf, Shot));
        var during = Shoot(w, archer, wolf, 100);

        Assert.Equal(100, during.Count);
        Assert.All(during, h => Assert.True(h.Critical));

        StatusManager.Remove(w, archer, HawkEye.Id, archer.Id);
        Assert.Equal(r.CritMax, StatusCrit.Chance(w, archer, wolf, Shot), 6);
        var after = Shoot(w, archer, wolf, 100);
        Assert.Contains(after, h => !h.Critical);
        Assert.Contains(after, h => h.Critical);
    }

    [Fact]
    public void Hawk_eye_does_not_make_periodic_damage_always_critical()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1));
        var wolf = Monster(w, new Vector2(4, 1));
        StatusManager.Apply(w, archer, archer, HawkEye.Id, durationTicks: 1000 * Second);

        StatusManager.Apply(w, archer, wolf, Poison.Id);
        var ticks = Run(w, 30 * Second).OfType<DamageDealt>().Where(d => d.SourceId == Poison.Id).ToList();

        Assert.True(ticks.Count >= 25);
        Assert.Contains(ticks, d => !d.Critical); // segue a regra de sempre (chance pela AGI)
    }

    [Fact]
    public void Hawk_eye_only_changes_whoever_has_it()
    {
        var w = NewWorld();
        var archer = Archer(w, new Vector2(1, 1));
        var other = Archer(w, new Vector2(1, 3));
        var wolf = Monster(w, new Vector2(4, 1));
        StatusManager.Apply(w, archer, archer, HawkEye.Id);

        Assert.Equal(1, StatusCrit.Chance(w, archer, wolf, Shot));
        Assert.Equal(StatMath.CritChance(other.Get<CharacterStats>(), w.Content.Rules.Stats), StatusCrit.Chance(w, other, wolf, Shot), 6);
        Assert.Equal(0, StatusCrit.Chance(w, wolf, archer, Shot)); // sem estatísticas, não crita
    }

    // ---------- RNG da simulação ----------

    private static List<bool> CritSequence(ulong seed, string? status, float targetX = 8)
    {
        var w = NewWorld(seed);
        var archer = Archer(w, new Vector2(1, 1));
        var wolf = Monster(w, new Vector2(targetX, 1));
        if (status is not null)
        {
            StatusManager.ApplyPermanent(w, archer, status);
        }

        return Shoot(w, archer, wolf, 80).Select(h => h.Critical).ToList();
    }

    [Fact]
    public void Crits_follow_the_simulation_rng()
    {
        var a = CritSequence(7, SteadyAim.Id);
        var b = CritSequence(7, SteadyAim.Id);
        var other = CritSequence(8, SteadyAim.Id);

        Assert.Equal(a, b);
        Assert.NotEqual(a, other);
        Assert.Contains(true, a);
    }

    [Fact]
    public void Status_that_does_not_apply_keeps_the_same_rolls_as_without_it()
    {
        // Alvo a 5 tiles: a Mira Firme não vale e o sorteio é o mesmo de quem não tem o status.
        Assert.Equal(CritSequence(7, null, targetX: 6), CritSequence(7, SteadyAim.Id, targetX: 6));
    }
}
