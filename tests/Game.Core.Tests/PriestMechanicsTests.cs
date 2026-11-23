using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Items;
using Game.Core.Combat.Stats;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-421: mecânicas novas do Sacerdote — cura por segundo pela INT de quem lançou (Oração Restauradora), atributos e
/// regeneração de Mana vindos de um status (Bênção, Inspiração), deixar quem lançou de fora (Inspiração), vida mínima de 1,
/// efeito ao acabar e cura pela vida máxima (Santuário). Dados de teste, não as habilidades reais (T-423).
/// </summary>
public class PriestMechanicsTests
{
    private const int Second = World.TickRate;

    private static readonly ClassDefinition TestClass = new()
    {
        Id = "class.test_cleric",
        Name = "Clérigo",
        Speed = 4.5,
        Radius = 0.45,
        Resource = new ResourceDefinition { Type = "mana", Max = 100, Initial = 100, RegenPerSecond = 5 },
        BasicAttack = "cleric.test_strike",
        Attributes = new AttributeSet { Str = 6, Dex = 2, Agi = 4, Int = 12, Vit = 8 },
        BaseHealth = 70,
    };

    private static readonly AbilityDefinition Strike = new()
    {
        Id = "cleric.test_strike",
        Name = "Golpe",
        Targeting = new UnitTargeting { Range = 6 },
        Effects = [new DamageEffect { Base = 1 }],
    };

    private static readonly StatusDefinition Prayer = new()
    {
        Id = "status.test_prayer",
        Name = "Oração Restauradora",
        DefaultDurationTicks = 8 * Second,
        Periodic = [new PeriodicEffectDefinition { IntervalTicks = Second, Effects = [new HealEffect { Base = 4, Scaling = new Dictionary<string, double> { ["int"] = 0.4 } }] }],
    };

    private static readonly StatusDefinition Blessing = new()
    {
        Id = "status.test_blessing",
        Name = "Bênção",
        DefaultDurationTicks = 15 * Second,
        Stats = new StatusStatsDefinition
        {
            Attributes = new Dictionary<string, ScaledValueDefinition>
            {
                ["all"] = new() { Base = 2, Scaling = new Dictionary<string, double> { ["int"] = 0.08 } },
            },
        },
    };

    private static readonly StatusDefinition Inspired = new()
    {
        Id = "status.test_inspired",
        Name = "Inspiração",
        DefaultDurationTicks = 12 * Second,
        Stats = new StatusStatsDefinition { ManaRegen = new ScaledValueDefinition { Base = 4 } },
    };

    // Os aliados no raio, sem quem lança: alvo em si e alvos extras aliados (que nunca incluem o alvo principal), como o
    // Bastião do Guardião. Não precisou de campo novo.
    private static readonly AbilityDefinition Inspiration = new()
    {
        Id = "priest.test_inspiration",
        Name = "Inspiração",
        Targeting = new SelfTargeting(),
        Effects = [new ExtraTargetsEffect { Radius = 6, Count = 0, Affects = FactionFilter.Ally, Effects = [new ApplyStatusEffect { Status = "status.test_inspired" }] }],
    };

    // Com quem lança: o mesmo, mais o status em si.
    private static readonly AbilityDefinition Blessed = new()
    {
        Id = "priest.test_blessing",
        Name = "Bênção",
        Targeting = new SelfTargeting(),
        Effects =
        [
            new ApplyStatusEffect { Status = "status.test_blessing" },
            new ExtraTargetsEffect { Radius = 6, Count = 0, Affects = FactionFilter.Ally, Effects = [new ApplyStatusEffect { Status = "status.test_blessing" }] },
        ],
    };

    private static readonly StatusDefinition Sanctuary = new()
    {
        Id = "status.test_sanctuary",
        Name = "Santuário",
        Tags = ["holy"],
        DefaultDurationTicks = 6 * Second,
        Flags = new StatusFlags { Undying = true },
        OnExpire = [new HealEffect { Base = 0, MaxHealthFraction = 0.2 }],
    };

    private static readonly StatusDefinition Poison = new()
    {
        Id = "status.test_poison",
        Name = "Veneno",
        DefaultDurationTicks = 10 * Second,
        Periodic = [new PeriodicEffectDefinition { IntervalTicks = Second, Effects = [new DamageEffect { Base = 500 }] }],
    };

    private static readonly StatusDefinition Intercept = new()
    {
        Id = "status.test_intercept",
        Name = "Interceptado",
        DefaultDurationTicks = 100 * Second,
        Redirect = new RedirectDefinition { Fraction = 0.5, MaxDistance = 10 },
    };

    private static readonly StatusDefinition HalfHealing = new()
    {
        Id = "status.test_half_healing",
        Name = "Ferida",
        DefaultDurationTicks = 100 * Second,
        Modifiers = new StatModifiers { HealingTakenMultiplier = 0.5 },
    };

    private static World NewWorld() => WorldBuilder.CreateDefault(
        7,
        null,
        new ContentCatalog(
            classes: [TestClass],
            abilities: [Strike, Inspiration, Blessed],
            statuses: [Prayer, Blessing, Inspired, Sanctuary, Poison, Intercept, HalfHealing]));

    private static Entity Player(World w, Vector2 pos, int intel = 12, params string[] known)
    {
        var p = EntityFactory.SpawnPlayer(w, TestClass, pos);
        p.Get<AbilityState>().Known.AddRange(known);
        p.Get<CharacterStats>().Int = intel;
        return p;
    }

    private static Entity Monster(World w, Vector2 pos, float hp = 1000) => w.Spawn(
        new Transform { Position = pos },
        new Health(hp),
        new FactionTag(Faction.Monster),
        new Collider(0.4f),
        new Movement(4),
        new UnitInfo { DefinitionId = "test", Name = "Lobo" },
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

    // ---------- cura por segundo ----------

    [Fact]
    public void Heal_over_time_scales_with_the_casters_int_not_the_targets()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62);
        var ally = Player(w, new Vector2(2, 1), intel: 0);
        ally.Get<Health>().Current = 10;

        StatusManager.Apply(w, priest, ally, Prayer.Id);
        var events = Run(w, Second);

        var heal = Assert.Single(events.OfType<HealApplied>());
        Assert.Equal((priest.Id, ally.Id, Prayer.Id), (heal.Source, heal.Target, heal.SourceId));
        Assert.Equal(CoreEffects.Round(4 + (0.4 * 62)), heal.Amount); // 28,8 → 29 (design: 29/s no perfil do 21)
    }

    [Fact]
    public void Heal_over_time_is_halved_by_halved_healing_taken()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62);
        var ally = Player(w, new Vector2(2, 1));
        ally.Get<Health>().Current = 10;
        StatusManager.Apply(w, null, ally, HalfHealing.Id);

        StatusManager.Apply(w, priest, ally, Prayer.Id);
        var heal = Assert.Single(Run(w, Second).OfType<HealApplied>());

        Assert.Equal(CoreEffects.Round((4 + (0.4 * 62)) * 0.5), heal.Amount);
    }

    // ---------- atributos vindos de um status ----------

    [Fact]
    public void Blessing_adds_attributes_by_the_casters_int_and_max_health_follows_vit()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62);
        var ally = Player(w, new Vector2(2, 1));
        var stats = ally.Get<CharacterStats>();
        var before = (stats.Str, stats.Dex, stats.Agi, stats.Int, stats.Vit);
        var maxBefore = ally.Get<Health>().Max;

        StatusManager.Apply(w, priest, ally, Blessing.Id);

        Assert.Equal((before.Str + 7, before.Dex + 7, before.Agi + 7, before.Int + 7, before.Vit + 7), (stats.Str, stats.Dex, stats.Agi, stats.Int, stats.Vit)); // 2 + 0,08 × 62 = 6,96 → 7
        var perVit = (float)w.Content.Rules.Stats.HealthPerVit;
        Assert.Equal(maxBefore + (7 * perVit), ally.Get<Health>().Max);
    }

    [Fact]
    public void Blessing_ends_and_attributes_and_max_health_return_without_current_above_max()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62);
        var ally = Player(w, new Vector2(2, 1));
        var vit = ally.Get<CharacterStats>().Vit;
        var maxBefore = ally.Get<Health>().Max;

        StatusManager.Apply(w, priest, ally, Blessing.Id);
        ally.Get<Health>().Current = ally.Get<Health>().Max; // curado até o máximo com a Bênção
        var events = Run(w, (15 * Second) + 1);

        Assert.Contains(events.OfType<StatusExpired>(), e => e.StatusId == Blessing.Id);
        Assert.Equal(vit, ally.Get<CharacterStats>().Vit);
        Assert.Equal(maxBefore, ally.Get<Health>().Max);
        Assert.True(ally.Get<Health>().Current <= ally.Get<Health>().Max);
    }

    [Fact]
    public void Blessing_value_is_fixed_while_active_and_recalculated_on_renew()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62);
        var ally = Player(w, new Vector2(2, 1));
        var baseStr = ally.Get<CharacterStats>().Str;

        StatusManager.Apply(w, priest, ally, Blessing.Id);
        priest.Get<CharacterStats>().Int = 112; // 2 + 0,08 × 112 = 10,96 → 11
        Run(w);
        Assert.Equal(baseStr + 7, ally.Get<CharacterStats>().Str);

        StatusManager.Apply(w, priest, ally, Blessing.Id);
        Assert.Equal(baseStr + 11, ally.Get<CharacterStats>().Str);
    }

    [Fact]
    public void Removing_blessing_takes_the_attributes_back()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62);
        var ally = Player(w, new Vector2(2, 1));
        var baseInt = ally.Get<CharacterStats>().Int;

        StatusManager.Apply(w, priest, ally, Blessing.Id);
        Assert.True(StatusManager.Remove(w, ally, Blessing.Id, priest.Id));

        Assert.Equal(baseInt, ally.Get<CharacterStats>().Int);
    }

    [Fact]
    public void Equipment_recompute_keeps_the_blessing()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62);
        var ally = Player(w, new Vector2(2, 1));
        ally.Get<CharacterStats>().Int = 12;
        StatusManager.Apply(w, priest, ally, Blessing.Id);

        Gear.Refresh(w, ally); // refaz as estatísticas do zero (perfil 12 + equipamento vazio)

        Assert.Equal(12 + 7, ally.Get<CharacterStats>().Int);
    }

    [Fact]
    public void Blessing_ability_reaches_the_caster_and_allies_in_radius()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62, known: [Blessed.Id]);
        var near = Player(w, new Vector2(3, 1));
        var far = Player(w, new Vector2(20, 1));

        w.Enqueue(new UseAbilityIntent(priest.Id, Blessed.Id, null));
        Run(w);

        Assert.True(StatusManager.Has(w, priest, Blessing.Id));
        Assert.True(StatusManager.Has(w, near, Blessing.Id));
        Assert.False(StatusManager.Has(w, far, Blessing.Id));
    }

    // ---------- regeneração de Mana e deixar quem lançou de fora ----------

    [Fact]
    public void Mana_regen_status_adds_four_mana_per_second_and_stops_when_it_ends()
    {
        var w = NewWorld();
        var inspired = Player(w, new Vector2(1, 1));
        var plain = Player(w, new Vector2(3, 1));
        StatusManager.Apply(w, null, inspired, Inspired.Id);
        inspired.Get<ResourcePool>().Current = 0;
        plain.Get<ResourcePool>().Current = 0;

        Run(w, Second);
        var gain = inspired.Get<ResourcePool>().Current - plain.Get<ResourcePool>().Current;
        Assert.InRange(gain, 3.5f, 4.5f);

        Run(w, 12 * Second);
        Assert.False(StatusManager.Has(w, inspired, Inspired.Id));
        inspired.Get<ResourcePool>().Current = 0;
        plain.Get<ResourcePool>().Current = 0;
        Run(w, Second);
        Assert.InRange(inspired.Get<ResourcePool>().Current - plain.Get<ResourcePool>().Current, -1f, 1f); // só o resto por tick
    }

    [Fact]
    public void Inspiration_reaches_the_other_allies_but_not_the_caster()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), known: [Inspiration.Id]);
        var a = Player(w, new Vector2(2, 1));
        var b = Player(w, new Vector2(1, 3));

        w.Enqueue(new UseAbilityIntent(priest.Id, Inspiration.Id, null));
        Run(w);

        Assert.False(StatusManager.Has(w, priest, Inspired.Id));
        Assert.True(StatusManager.Has(w, a, Inspired.Id));
        Assert.True(StatusManager.Has(w, b, Inspired.Id));
    }

    // ---------- vida mínima de 1 ----------

    [Fact]
    public void Undying_leaves_one_health_and_without_it_the_hit_kills()
    {
        var w = NewWorld();
        var wolf = Monster(w, new Vector2(1, 1));
        var held = Player(w, new Vector2(2, 1));
        var plain = Player(w, new Vector2(3, 1));
        held.Get<Health>().Current = 100;
        plain.Get<Health>().Current = 100;
        StatusManager.Apply(w, null, held, Sanctuary.Id);

        DamageResolver.Deal(w, wolf, held, "test.bite", 500, DamageSchool.Physical, applyModifiers: false);
        DamageResolver.Deal(w, wolf, plain, "test.bite", 500, DamageSchool.Physical, applyModifiers: false);

        Assert.True(held.IsAlive);
        Assert.Equal(1, held.Get<Health>().Current);
        var hit = w.Events.Current.OfType<DamageDealt>().First(d => d.Target == held.Id);
        Assert.Equal((99f, 0f, false), (hit.Applied, hit.Overkill, hit.Killed));
        Assert.False(plain.IsAlive);
    }

    [Fact]
    public void Undying_holds_against_periodic_damage()
    {
        var w = NewWorld();
        var wolf = Monster(w, new Vector2(1, 1));
        var held = Player(w, new Vector2(2, 1));
        StatusManager.Apply(w, null, held, Sanctuary.Id);
        StatusManager.Apply(w, wolf, held, Poison.Id);

        Run(w, 3 * Second);

        Assert.True(held.IsAlive);
        Assert.Equal(1, held.Get<Health>().Current);
    }

    [Fact]
    public void Undying_holds_for_the_part_redirected_to_a_protector()
    {
        var w = NewWorld();
        var wolf = Monster(w, new Vector2(1, 1));
        var guardian = Player(w, new Vector2(2, 1));
        var ally = Player(w, new Vector2(3, 1));
        guardian.Get<Health>().Current = 50;
        StatusManager.Apply(w, null, guardian, Sanctuary.Id);
        StatusManager.Apply(w, guardian, ally, Intercept.Id);

        DamageResolver.Deal(w, wolf, ally, "test.bite", 400, DamageSchool.Physical);

        Assert.True(guardian.IsAlive);
        Assert.Equal(1, guardian.Get<Health>().Current);
    }

    // ---------- efeito ao acabar e cura pela vida máxima ----------

    [Fact]
    public void On_expire_effect_runs_when_time_runs_out_and_heals_a_fraction_of_max_health()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1));
        var ally = Player(w, new Vector2(2, 1));
        var health = ally.Get<Health>();
        health.Current = 1;
        StatusManager.Apply(w, priest, ally, Sanctuary.Id);

        var events = Run(w, (6 * Second) + 1);

        var heal = Assert.Single(events.OfType<HealApplied>());
        Assert.Equal((priest.Id, ally.Id, Sanctuary.Id), (heal.Source, heal.Target, heal.SourceId));
        Assert.Equal(CoreEffects.Round(0.2 * health.Max), heal.Amount);
        Assert.Equal(1 + heal.Amount, health.Current);
    }

    [Fact]
    public void On_expire_effect_does_not_run_when_the_status_is_removed()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1));
        var ally = Player(w, new Vector2(2, 1));
        ally.Get<Health>().Current = 1;
        StatusManager.Apply(w, priest, ally, Sanctuary.Id);

        StatusManager.RemoveByTags(w, ally, ["holy"], 0, priest.Id);
        var events = Run(w, (6 * Second) + 1);

        Assert.Empty(events.OfType<HealApplied>());
    }

    [Fact]
    public void On_expire_effect_does_not_run_when_the_holder_is_dead()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1));
        var ally = Player(w, new Vector2(2, 1));
        StatusManager.Apply(w, priest, ally, Sanctuary.Id);
        w.Kill(ally);

        var events = Run(w, (6 * Second) + 1);

        Assert.Empty(events.OfType<HealApplied>());
    }

    [Fact]
    public void Heal_of_twenty_percent_of_max_health_heals_twenty_percent()
    {
        var w = NewWorld();
        var ally = Player(w, new Vector2(2, 1));
        var health = ally.Get<Health>();
        health.Current = 1;

        EffectApplier.Apply(w, [new HealEffect { Base = 0, MaxHealthFraction = 0.2 }], null, [ally], "test.heal");

        Assert.Equal(1 + CoreEffects.Round(0.2 * health.Max), health.Current);
    }

    // ---------- determinismo ----------

    [Fact]
    public void Status_stats_are_in_the_world_hash_and_survive_a_snapshot()
    {
        var w = NewWorld();
        var priest = Player(w, new Vector2(1, 1), intel: 62);
        var ally = Player(w, new Vector2(2, 1));
        StatusManager.Apply(w, priest, ally, Blessing.Id);

        var bonuses = ally.Get<StatusStatBonuses>();
        var clone = (StatusStatBonuses)bonuses.Clone();
        Assert.Equal(bonuses.Applied, clone.Applied);
        Assert.Equal(bonuses.ByInstance, clone.ByInstance);
        Assert.Equal(new StatBlock(7, 7, 7, 7, 7, 0), bonuses.Applied);
    }
}
