using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-407: mecânicas novas do Berserker — dano extra pela vida do alvo e recarga zerada ao matar (Execução), vida pelo dano
/// (Sede de Sangue) e área presa a quem anda (Tempestade de Aço). Dados de teste, não as habilidades reais (T-408).
/// </summary>
public class BerserkerMechanicsTests
{
    private static readonly AbilityDefinition Execute = new()
    {
        Id = "berserker.test_execute",
        Name = "Execução",
        CooldownTicks = 160,
        Targeting = new UnitTargeting { Range = 30 },
        Effects = [new DamageEffect { Base = 100, LowHealthBonus = new LowHealthBonusDefinition { BelowHealth = 0.3, Bonus = 0.5 }, ResetCooldownOnKill = true }],
    };

    private static readonly AbilityDefinition Bloodthirst = new()
    {
        Id = "berserker.test_bloodthirst",
        Name = "Sede de Sangue",
        Targeting = new SelfTargeting(),
    };

    private static readonly StatusDefinition Thirst = new()
    {
        Id = "status.test_thirst",
        Name = "Sede",
        DefaultDurationTicks = 100000,
        Lifesteal = new LifestealDefinition { Fraction = 0.1, BelowHealth = 0.3, KillHeal = 0.05, Ability = "berserker.test_bloodthirst" },
    };

    private static readonly StatusDefinition Mirror = new()
    {
        Id = "status.test_mirror",
        Name = "Espelho",
        DefaultDurationTicks = 100000,
        Reflect = new ReflectDefinition { Fraction = 0.5 },
    };

    private static readonly StatusDefinition Intercept = new()
    {
        Id = "status.test_intercept",
        Name = "Interceptado",
        DefaultDurationTicks = 100000,
        Redirect = new RedirectDefinition { Fraction = 0.3, MaxDistance = 10 },
    };

    private static readonly StatusDefinition HalfHealing = new()
    {
        Id = "status.test_half_healing",
        Name = "Ferida",
        DefaultDurationTicks = 100000,
        Modifiers = new StatModifiers { HealingTakenMultiplier = 0.5 },
    };

    private static readonly StatusDefinition Spinning = new()
    {
        Id = "status.test_spinning",
        Name = "Girando",
        DefaultDurationTicks = 80,
        Modifiers = new StatModifiers { SpeedMultiplier = 0.8 },
    };

    private static readonly AreaDefinition Storm = new()
    {
        Id = "area.test_storm",
        Radius = 1,
        DurationTicks = 80,
        IntervalTicks = 10,
        FollowOwner = true,
        Effects = [new DamageEffect { Base = 10 }],
    };

    private static readonly AbilityDefinition SteelStorm = new()
    {
        Id = "berserker.test_steel_storm",
        Name = "Tempestade de Aço",
        Targeting = new SelfTargeting(),
        Effects = [new ApplyStatusEffect { Status = "status.test_spinning", Target = EffectTarget.Self }, new SpawnAreaEffect { Area = "area.test_storm" }],
    };

    private static World NewWorld() => WorldBuilder.CreateDefault(
        7,
        null,
        new ContentCatalog(
            abilities: [Execute, Bloodthirst, SteelStorm],
            statuses: [Thirst, Mirror, Intercept, HalfHealing, Spinning],
            areas: [Storm]));

    private static Entity Unit(World w, Vector2 pos, Faction f, string name, float hp = 1000, params string[] known)
    {
        var st = new AbilityState();
        st.Known.AddRange(known);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = name },
            st);
    }

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

    private static float Lost(Entity e) => e.Get<Health>().Max - e.Get<Health>().Current;

    // ---------- Execução ----------

    [Theory]
    [InlineData(500, 100)] // 50% de vida: dano normal
    [InlineData(300, 100)] // exatamente 30%: não está abaixo
    [InlineData(200, 150)] // 20%: +50%
    public void Execute_deals_bonus_damage_below_the_health_fraction(float current, float expected)
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(1, 1), Faction.Player, "Berserker", known: [Execute.Id]);
        var wolf = Unit(w, new Vector2(2, 1), Faction.Monster, "Lobo");
        wolf.Get<Health>().Current = current;

        w.Enqueue(new UseAbilityIntent(berserker.Id, Execute.Id, wolf.Id));
        var events = Run(w);

        Assert.Equal(expected, Assert.Single(events.OfType<DamageDealt>()).Amount);
    }

    [Fact]
    public void Killing_blow_resets_the_cooldown_and_a_non_killing_one_does_not()
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(1, 1), Faction.Player, "Berserker", known: [Execute.Id]);
        var weak = Unit(w, new Vector2(2, 1), Faction.Monster, "Rato", hp: 50);
        var strong = Unit(w, new Vector2(1, 2), Faction.Monster, "Urso", hp: 5000);
        var st = berserker.Get<AbilityState>();

        w.Enqueue(new UseAbilityIntent(berserker.Id, Execute.Id, weak.Id));
        Run(w);
        Assert.False(weak.IsAlive);
        Assert.False(st.IsOnCooldown(Execute, w.Tick));

        w.Enqueue(new UseAbilityIntent(berserker.Id, Execute.Id, strong.Id));
        var second = Run(w);
        Assert.Single(second.OfType<AbilityExecuted>());
        Assert.True(strong.IsAlive);
        Assert.True(st.IsOnCooldown(Execute, w.Tick));

        w.Enqueue(new UseAbilityIntent(berserker.Id, Execute.Id, strong.Id));
        var third = Run(w);
        Assert.Equal(AbilityRejectReason.OnCooldown, Assert.Single(third.OfType<AbilityRejected>()).Reason);
    }

    // ---------- vida pelo dano ----------

    private static (World W, Entity Berserker, Entity Wolf) Lifesteal(float current)
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(1, 1), Faction.Player, "Berserker");
        var wolf = Unit(w, new Vector2(2, 1), Faction.Monster, "Lobo", hp: 5000);
        StatusManager.Apply(w, berserker, berserker, Thirst.Id);
        berserker.Get<Health>().Current = current;
        return (w, berserker, wolf);
    }

    [Fact]
    public void Ten_percent_lifesteal_heals_ten_from_one_hundred_damage()
    {
        var (w, berserker, wolf) = Lifesteal(500);

        DamageResolver.Deal(w, berserker, wolf, "test.strike", 100, DamageSchool.Physical);

        Assert.Equal(510, berserker.Get<Health>().Current);
        var heal = Assert.Single(w.Events.Current.OfType<HealApplied>());
        Assert.Equal((berserker.Id, berserker.Id, Bloodthirst.Id, 10f), (heal.Source, heal.Target, heal.SourceId, heal.Effective));
    }

    [Fact]
    public void Lifesteal_doubles_below_the_health_fraction()
    {
        var (w, berserker, wolf) = Lifesteal(200);

        DamageResolver.Deal(w, berserker, wolf, "test.strike", 100, DamageSchool.Physical);

        Assert.Equal(220, berserker.Get<Health>().Current);
    }

    [Fact]
    public void Killing_a_monster_heals_a_fraction_of_max_health()
    {
        var (w, berserker, _) = Lifesteal(500);
        var rat = Unit(w, new Vector2(1, 2), Faction.Monster, "Rato", hp: 100);

        DamageResolver.Deal(w, berserker, rat, "test.strike", 100, DamageSchool.Physical);

        Assert.False(rat.IsAlive);
        Assert.Equal(500 + 10 + 50, berserker.Get<Health>().Current); // 10% de 100 + 5% de 1000
    }

    [Fact]
    public void Lifesteal_respects_healing_taken()
    {
        var (w, berserker, wolf) = Lifesteal(500);
        StatusManager.Apply(w, null, berserker, HalfHealing.Id);

        DamageResolver.Deal(w, berserker, wolf, "test.strike", 100, DamageSchool.Physical);

        Assert.Equal(505, berserker.Get<Health>().Current);
    }

    [Fact]
    public void Reflected_damage_does_not_heal()
    {
        var (w, berserker, wolf) = Lifesteal(500);
        StatusManager.Apply(w, null, berserker, Mirror.Id);

        DamageResolver.Deal(w, wolf, berserker, "test.bite", 100, DamageSchool.Physical);

        Assert.Equal(50, Lost(wolf)); // o reflexo saiu do Berserker
        Assert.Equal(400, berserker.Get<Health>().Current); // e não curou
        Assert.Empty(w.Events.Current.OfType<HealApplied>());
    }

    [Fact]
    public void Redirected_part_does_not_heal_the_attacker()
    {
        var w = NewWorld();
        var vampire = Unit(w, new Vector2(1, 2), Faction.Monster, "Vampiro");
        var guardian = Unit(w, new Vector2(2, 1), Faction.Player, "Guardião");
        var ally = Unit(w, new Vector2(1, 1), Faction.Player, "Maga");
        StatusManager.Apply(w, vampire, vampire, Thirst.Id);
        StatusManager.Apply(w, guardian, ally, Intercept.Id);
        vampire.Get<Health>().Current = 500;

        DamageResolver.Deal(w, vampire, ally, "test.bite", 100, DamageSchool.Physical);

        Assert.Equal(30, Lost(guardian));
        Assert.Equal(507, vampire.Get<Health>().Current); // só 10% dos 70 do aliado
    }

    [Fact]
    public void Full_health_lifesteal_publishes_nothing()
    {
        var (w, berserker, wolf) = Lifesteal(1000);

        DamageResolver.Deal(w, berserker, wolf, "test.strike", 100, DamageSchool.Physical);

        Assert.Empty(w.Events.Current.OfType<HealApplied>());
    }

    // ---------- área presa a quem anda ----------

    [Fact]
    public void Attached_area_follows_the_caster_and_hits_around_the_new_position()
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(2, 2), Faction.Player, "Berserker", known: [SteelStorm.Id]);
        var near = Unit(w, new Vector2(2, 3.2f), Faction.Monster, "Perto");
        var far = Unit(w, new Vector2(6, 3), Faction.Monster, "Longe");

        w.Enqueue(new UseAbilityIntent(berserker.Id, SteelStorm.Id));
        var events = Run(w, 11); // primeiro giro, parado
        var area = w.Find(Assert.Single(events.OfType<AreaSpawned>()).Area)!;
        Assert.Equal(10, Lost(near));
        Assert.Equal(0, Lost(far));

        w.Enqueue(new MoveIntent(berserker.Id, new Vector2(1, 0)));
        var startX = berserker.Get<Transform>().Position.X;
        events.AddRange(Run(w, 30));

        var pos = berserker.Get<Transform>().Position;
        Assert.Equal(pos, area.Get<Transform>().Position);
        Assert.Equal(startX + (30 * 4 * 0.8f / World.TickRate), pos.X, 2); // anda 20% mais devagar
        Assert.Equal(10, Lost(near)); // ficou para trás
        Assert.True(Lost(far) > 0); // acertado em volta da posição nova
    }

    [Fact]
    public void Attached_area_ends_with_its_duration()
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(2, 2), Faction.Player, "Berserker", known: [SteelStorm.Id]);
        Unit(w, new Vector2(2, 3), Faction.Monster, "Lobo");

        w.Enqueue(new UseAbilityIntent(berserker.Id, SteelStorm.Id));
        var events = Run(w, 90);

        Assert.Equal(8, events.OfType<AreaTriggered>().Count());
        Assert.Equal(AreaExpireReason.Duration, Assert.Single(events.OfType<AreaExpired>()).Reason);
    }

    [Fact]
    public void Attached_area_ends_when_the_owner_dies()
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(2, 2), Faction.Player, "Berserker", known: [SteelStorm.Id]);
        var wolf = Unit(w, new Vector2(2, 3), Faction.Monster, "Lobo");

        w.Enqueue(new UseAbilityIntent(berserker.Id, SteelStorm.Id));
        Run(w, 11);
        var lost = Lost(wolf);
        w.Kill(berserker);
        var events = Run(w, 30);

        Assert.Equal(AreaExpireReason.OwnerDied, Assert.Single(events.OfType<AreaExpired>()).Reason);
        Assert.Empty(events.OfType<AreaTriggered>());
        Assert.Equal(lost, Lost(wolf));
    }
}
