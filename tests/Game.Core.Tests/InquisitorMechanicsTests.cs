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
/// T-422: mecânicas novas do Inquisidor — área que fere e cura (Consagração), vida pelo dano para quem bate no alvo marcado
/// (Selo da Luz), cura pelo dano do próprio golpe (Julgamento NH5), ataque básico trocado por um status e cura dos aliados
/// perto pelo dano (Ira Divina). Dados de teste, não as habilidades reais (T-423).
/// </summary>
public class InquisitorMechanicsTests
{
    private const int Second = World.TickRate;

    // ---------- dados de teste ----------

    private static readonly AreaDefinition ConsecrationHarm = new()
    {
        Id = "area.test_consecration_harm",
        Radius = 3,
        DurationTicks = 6 * Second,
        IntervalTicks = Second,
        Affects = FactionFilter.Enemy,
        Effects = [new DamageEffect { Base = 35, School = DamageSchool.Holy, BonusVsTags = new Dictionary<string, double> { ["undead"] = 0.5 } }],
    };

    private static readonly AreaDefinition ConsecrationMend = new()
    {
        Id = "area.test_consecration_mend",
        Radius = 3,
        DurationTicks = 6 * Second,
        IntervalTicks = Second,
        Affects = FactionFilter.Ally,
        Effects = [new HealEffect { Base = 22 }],
    };

    // Consagração = duas áreas lançadas juntas no mesmo ponto, uma para inimigos e outra para aliados (sem campo novo).
    private static readonly AbilityDefinition Consecration = new()
    {
        Id = "inquisitor.test_consecration",
        Name = "Consagração",
        Targeting = new SelfTargeting(),
        Effects = [new SpawnAreaEffect { Area = ConsecrationHarm.Id }, new SpawnAreaEffect { Area = ConsecrationMend.Id }],
    };

    private static readonly AbilityDefinition SealAbility = new()
    {
        Id = "inquisitor.test_seal",
        Name = "Selo da Luz",
        Targeting = new UnitTargeting { Range = 6 },
        Effects = [new ApplyStatusEffect { Status = "status.test_seal" }],
    };

    private static readonly StatusDefinition Seal = new()
    {
        Id = "status.test_seal",
        Name = "Selo da Luz",
        DefaultDurationTicks = 8 * Second,
        AttackerLifesteal = new LifestealDefinition { Fraction = 0.1, Ability = "inquisitor.test_seal" },
    };

    private static readonly StatusDefinition Mirror = new()
    {
        Id = "status.test_mirror",
        Name = "Espelho",
        DefaultDurationTicks = 100000,
        Reflect = new ReflectDefinition { Fraction = 0.5 },
    };

    private static readonly StatusDefinition HalfHealing = new()
    {
        Id = "status.test_half_healing",
        Name = "Ferida",
        DefaultDurationTicks = 100000,
        Modifiers = new StatModifiers { HealingTakenMultiplier = 0.5 },
    };

    private static readonly AbilityDefinition Judgment = new()
    {
        Id = "inquisitor.test_judgment",
        Name = "Julgamento",
        CooldownTicks = 7 * Second,
        Targeting = new UnitTargeting { Range = 5 },
        Effects = [new DamageEffect { Base = 200, School = DamageSchool.Holy, SelfHeal = 0.2 }],
    };

    private static readonly AbilityDefinition HolyStrike = new()
    {
        Id = "cleric.test_holy_strike",
        Name = "Golpe Sagrado",
        CooldownTicks = 30,
        Targeting = new UnitTargeting { Range = 6 },
        Effects = [new DamageEffect { Base = 50, School = DamageSchool.Holy }],
    };

    private static readonly DamageEffect WrathDamage = new()
    {
        Base = 75,
        School = DamageSchool.Holy,
        AllyHeal = new DamageAllyHealDefinition { Fraction = 0.1, Radius = 4, Ability = "inquisitor.test_divine_wrath" },
    };

    // Golpe Sagrado mais forte, que também acerta todos num raio de 2 em volta do alvo.
    private static readonly AbilityDefinition WrathStrike = new()
    {
        Id = "inquisitor.test_wrath_strike",
        Name = "Golpe Sagrado (Ira Divina)",
        CooldownTicks = 10,
        Targeting = new UnitTargeting { Range = 6 },
        Effects = [WrathDamage, new ExtraTargetsEffect { Radius = 2, Count = 0, Effects = [WrathDamage] }],
    };

    private static readonly StatusDefinition Wrath = new()
    {
        Id = "status.test_wrath",
        Name = "Ira Divina",
        DefaultDurationTicks = 100,
        BasicAttack = WrathStrike.Id,
    };

    private static readonly AbilityDefinition DivineWrath = new()
    {
        Id = "inquisitor.test_divine_wrath",
        Name = "Ira Divina",
        Targeting = new SelfTargeting(),
        Effects = [new ApplyStatusEffect { Status = Wrath.Id, Target = EffectTarget.Self }],
    };

    private static World NewWorld() => WorldBuilder.CreateDefault(
        7,
        null,
        new ContentCatalog(
            abilities: [Consecration, SealAbility, Judgment, HolyStrike, WrathStrike, DivineWrath],
            statuses: [Seal, Mirror, HalfHealing, Wrath],
            areas: [ConsecrationHarm, ConsecrationMend]));

    private static Entity Unit(World w, Vector2 pos, Faction f, string name, float hp = 1000, float current = -1, params string[] known)
    {
        var st = new AbilityState { BasicAttackId = HolyStrike.Id };
        st.Known.Add(HolyStrike.Id);
        st.Known.AddRange(known);
        var e = w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = name },
            st);
        if (current >= 0)
        {
            e.Get<Health>().Current = current;
        }

        return e;
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

    private static float Hp(Entity e) => e.Get<Health>().Current;

    private static float Lost(Entity e) => e.Get<Health>().Max - e.Get<Health>().Current;

    // ---------- Consagração ----------

    [Fact]
    public void Consecration_pulse_hurts_monsters_and_heals_allies_in_the_same_tick()
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(3, 3), Faction.Player, "Inquisidor", current: 500, known: [Consecration.Id]);
        var ally = Unit(w, new Vector2(4.5f, 3), Faction.Player, "Maga", current: 500);
        var wolf = Unit(w, new Vector2(3, 5), Faction.Monster, "Lobo");
        var farWolf = Unit(w, new Vector2(12, 12), Faction.Monster, "Lobo longe");

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, Consecration.Id));
        var first = Run(w);
        Assert.Equal(2, first.OfType<AreaSpawned>().Count());
        Assert.All(first.OfType<AreaSpawned>(), a => Assert.Equal(new Vector2(3, 3), a.Position));

        var pulse = Run(w, Second); // o primeiro pulso sai 1 s depois de lançar

        Assert.Equal(35, Lost(wolf));
        Assert.Equal(0, Lost(farWolf));
        Assert.Equal(522, Hp(ally));
        Assert.Equal(522, Hp(inquisitor));
        Assert.DoesNotContain(pulse.OfType<HealApplied>(), h => h.Target == wolf.Id);
        Assert.DoesNotContain(pulse.OfType<DamageDealt>(), d => d.Target == ally.Id || d.Target == inquisitor.Id);
        Assert.Equal(2, pulse.OfType<AreaTriggered>().Count());
    }

    [Fact]
    public void Consecration_pulses_once_per_second_for_its_duration()
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(3, 3), Faction.Player, "Inquisidor", current: 100, known: [Consecration.Id]);
        var wolf = Unit(w, new Vector2(3, 5), Faction.Monster, "Lobo");

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, Consecration.Id));
        var events = Run(w, (7 * Second) + 1);

        Assert.Equal(6 * 35, Lost(wolf));
        Assert.Equal(100 + (6 * 22), Hp(inquisitor));
        var ticksWithDamage = events.OfType<DamageDealt>().Count();
        var ticksWithHeal = events.OfType<HealApplied>().Count();
        Assert.Equal((6, 6), (ticksWithDamage, ticksWithHeal));
        Assert.Equal(2, events.OfType<AreaExpired>().Count());
    }

    [Fact]
    public void Consecration_damage_and_heal_land_on_the_same_ticks()
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(3, 3), Faction.Player, "Inquisidor", current: 100, known: [Consecration.Id]);
        var wolf = Unit(w, new Vector2(3, 5), Faction.Monster, "Lobo");

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, Consecration.Id));
        var damageTicks = new List<long>();
        var healTicks = new List<long>();
        for (var i = 0; i < 7 * Second; i++)
        {
            w.Step();
            if (w.LastTickEvents.OfType<DamageDealt>().Any(d => d.Target == wolf.Id))
            {
                damageTicks.Add(w.Tick);
            }

            if (w.LastTickEvents.OfType<HealApplied>().Any(h => h.Target == inquisitor.Id))
            {
                healTicks.Add(w.Tick);
            }
        }

        Assert.Equal(6, damageTicks.Count);
        Assert.Equal(damageTicks, healTicks);
    }

    // ---------- Selo da Luz ----------

    private static (World W, Entity Inquisitor, Entity Warrior, Entity Wolf) Sealed()
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(1, 1), Faction.Player, "Inquisidor", current: 500);
        var warrior = Unit(w, new Vector2(1, 2), Faction.Player, "Guerreiro", current: 500);
        var wolf = Unit(w, new Vector2(2, 1), Faction.Monster, "Lobo", hp: 5000);
        StatusManager.Apply(w, inquisitor, wolf, Seal.Id);
        return (w, inquisitor, warrior, wolf);
    }

    [Fact]
    public void Seal_heals_every_ally_of_the_caster_that_hits_the_marked_monster()
    {
        var (w, inquisitor, warrior, wolf) = Sealed();

        DamageResolver.Deal(w, inquisitor, wolf, "test.strike", 100, DamageSchool.Physical);
        DamageResolver.Deal(w, warrior, wolf, "test.strike", 100, DamageSchool.Physical);

        Assert.Equal((510f, 510f), (Hp(inquisitor), Hp(warrior)));
        var heals = w.Events.Current.OfType<HealApplied>().ToList();
        Assert.Equal(2, heals.Count);
        Assert.All(heals, h => Assert.Equal((h.Source, SealAbility.Id, 10f), (h.Target, h.SourceId, h.Effective)));
    }

    [Fact]
    public void Seal_used_as_an_ability_marks_the_monster()
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(1, 1), Faction.Player, "Inquisidor", current: 500, known: [SealAbility.Id]);
        var wolf = Unit(w, new Vector2(3, 1), Faction.Monster, "Lobo", hp: 5000);

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, SealAbility.Id, wolf.Id));
        Run(w);
        Assert.True(StatusManager.Has(w, wolf, Seal.Id));

        DamageResolver.Deal(w, inquisitor, wolf, "test.strike", 200, DamageSchool.Physical);
        Assert.Equal(520, Hp(inquisitor));
    }

    [Fact]
    public void Seal_does_not_heal_attackers_that_are_not_allies_of_the_caster()
    {
        var (w, _, _, wolf) = Sealed();
        var bear = Unit(w, new Vector2(3, 1), Faction.Monster, "Urso", current: 500);

        DamageResolver.Deal(w, bear, wolf, "test.bite", 100, DamageSchool.Physical);

        Assert.Equal(500, Hp(bear));
        Assert.Empty(w.Events.Current.OfType<HealApplied>());
    }

    [Fact]
    public void Seal_does_not_heal_from_reflected_damage()
    {
        var (w, _, warrior, wolf) = Sealed();
        StatusManager.Apply(w, null, warrior, Mirror.Id);

        DamageResolver.Deal(w, wolf, warrior, "test.bite", 100, DamageSchool.Physical);

        Assert.Equal(50, Lost(wolf)); // o reflexo do Guerreiro acertou o lobo marcado
        Assert.Equal(400, Hp(warrior)); // e não curou
        Assert.Empty(w.Events.Current.OfType<HealApplied>());
    }

    [Fact]
    public void Seal_respects_healing_taken_and_stops_when_it_expires()
    {
        var (w, inquisitor, warrior, wolf) = Sealed();
        StatusManager.Apply(w, null, warrior, HalfHealing.Id);

        DamageResolver.Deal(w, warrior, wolf, "test.strike", 100, DamageSchool.Physical);
        Assert.Equal(505, Hp(warrior));

        Run(w, 8 * Second);
        Assert.False(StatusManager.Has(w, wolf, Seal.Id));
        DamageResolver.Deal(w, inquisitor, wolf, "test.strike", 100, DamageSchool.Physical);
        Assert.Equal(500, Hp(inquisitor));
    }

    [Fact]
    public void Seal_on_full_health_attacker_publishes_nothing()
    {
        var (w, inquisitor, _, wolf) = Sealed();
        inquisitor.Get<Health>().Current = 1000;

        DamageResolver.Deal(w, inquisitor, wolf, "test.strike", 100, DamageSchool.Physical);

        Assert.Empty(w.Events.Current.OfType<HealApplied>());
    }

    // ---------- Julgamento ----------

    [Fact]
    public void Judgment_heals_the_attacker_for_a_fraction_of_its_damage()
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(1, 1), Faction.Player, "Inquisidor", current: 500, known: [Judgment.Id]);
        var wolf = Unit(w, new Vector2(3, 1), Faction.Monster, "Lobo", hp: 5000);

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, Judgment.Id, wolf.Id));
        var events = Run(w);

        Assert.Equal(200, Assert.Single(events.OfType<DamageDealt>()).Amount);
        Assert.Equal(540, Hp(inquisitor));
        var heal = Assert.Single(events.OfType<HealApplied>());
        Assert.Equal((inquisitor.Id, inquisitor.Id, Judgment.Id, 40f, false), (heal.Source, heal.Target, heal.SourceId, heal.Effective, heal.Critical));
    }

    [Fact]
    public void Judgment_self_heal_respects_healing_taken()
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(1, 1), Faction.Player, "Inquisidor", current: 500, known: [Judgment.Id]);
        var wolf = Unit(w, new Vector2(3, 1), Faction.Monster, "Lobo", hp: 5000);
        StatusManager.Apply(w, null, inquisitor, HalfHealing.Id);

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, Judgment.Id, wolf.Id));
        Run(w);

        Assert.Equal(520, Hp(inquisitor));
    }

    // ---------- Ira Divina: ataque básico trocado ----------

    private static (World W, Entity Inquisitor, Entity Target, Entity Near1, Entity Near2, Entity Far) WrathSetup()
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(1, 2), Faction.Player, "Inquisidor", known: [DivineWrath.Id]);
        var target = Unit(w, new Vector2(4, 2), Faction.Monster, "Alvo", hp: 5000);
        var near1 = Unit(w, new Vector2(5.5f, 2), Faction.Monster, "Perto 1", hp: 5000);
        var near2 = Unit(w, new Vector2(4, 3.8f), Faction.Monster, "Perto 2", hp: 5000);
        var far = Unit(w, new Vector2(7.5f, 2), Faction.Monster, "Longe", hp: 5000);
        return (w, inquisitor, target, near1, near2, far);
    }

    [Fact]
    public void Divine_wrath_turns_the_basic_attack_into_a_stronger_area_strike()
    {
        var (w, inquisitor, target, near1, near2, far) = WrathSetup();
        w.Enqueue(new UseAbilityIntent(inquisitor.Id, DivineWrath.Id));
        Run(w);
        Assert.True(StatusManager.Has(w, inquisitor, Wrath.Id));

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, HolyStrike.Id, target.Id));
        var events = Run(w);

        Assert.Equal((75f, 75f, 75f, 0f), (Lost(target), Lost(near1), Lost(near2), Lost(far)));
        var executed = Assert.Single(events.OfType<AbilityExecuted>());
        Assert.Equal((HolyStrike.Id, true), (executed.AbilityId, executed.IsBasicAttack)); // o cliente continua vendo o ataque básico
        Assert.Equal(w.Tick + WrathStrike.CooldownTicks, executed.CooldownReadyTick);
        Assert.All(events.OfType<DamageDealt>(), d => Assert.Equal(HolyStrike.Id, d.SourceId));
    }

    [Fact]
    public void Basic_attack_returns_to_normal_with_its_normal_cooldown_when_the_status_ends()
    {
        var (w, inquisitor, target, near1, _, _) = WrathSetup();
        w.Enqueue(new UseAbilityIntent(inquisitor.Id, DivineWrath.Id));
        Run(w, Wrath.DefaultDurationTicks + 1);
        Assert.False(StatusManager.Has(w, inquisitor, Wrath.Id));

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, HolyStrike.Id, target.Id));
        var events = Run(w);

        Assert.Equal((50f, 0f), (Lost(target), Lost(near1)));
        var executed = Assert.Single(events.OfType<AbilityExecuted>());
        Assert.Equal((HolyStrike.Id, w.Tick + HolyStrike.CooldownTicks), (executed.AbilityId, executed.CooldownReadyTick));
    }

    [Fact]
    public void Swapped_strike_keeps_the_basic_attack_cooldown_slot()
    {
        var (w, inquisitor, target, _, _, _) = WrathSetup();
        StatusManager.Apply(w, inquisitor, inquisitor, Wrath.Id);
        var st = inquisitor.Get<AbilityState>();

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, HolyStrike.Id, target.Id));
        Run(w);
        Assert.True(st.ReadyAtTick.ContainsKey(HolyStrike.Id));
        Assert.False(st.ReadyAtTick.ContainsKey(WrathStrike.Id));

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, HolyStrike.Id, target.Id));
        Assert.Equal(AbilityRejectReason.OnCooldown, Assert.Single(Run(w).OfType<AbilityRejected>()).Reason);

        // A habilidade trocada não pode ser usada direto: não é conhecida.
        w.Enqueue(new UseAbilityIntent(inquisitor.Id, WrathStrike.Id, target.Id));
        Assert.Equal(AbilityRejectReason.UnknownAbility, Assert.Single(Run(w).OfType<AbilityRejected>()).Reason);
    }

    [Fact]
    public void Swap_only_applies_to_whoever_has_the_status()
    {
        var (w, inquisitor, target, near1, _, _) = WrathSetup();
        var warrior = Unit(w, new Vector2(2, 2), Faction.Player, "Guerreiro");
        StatusManager.Apply(w, inquisitor, inquisitor, Wrath.Id);

        w.Enqueue(new UseAbilityIntent(warrior.Id, HolyStrike.Id, target.Id));
        Run(w);

        Assert.Equal((50f, 0f), (Lost(target), Lost(near1)));
    }

    // ---------- Ira Divina: cura dos aliados pelo dano ----------

    [Fact]
    public void Each_wrath_hit_heals_allies_within_four_tiles_and_not_farther()
    {
        var (w, inquisitor, target, near1, _, _) = WrathSetup();
        inquisitor.Get<Health>().Current = 900;
        var closeAlly = Unit(w, new Vector2(1, 5.5f), Faction.Player, "Maga perto", current: 500);
        var farAlly = Unit(w, new Vector2(1, 8), Faction.Player, "Maga longe", current: 500);
        var wolfNearCaster = Unit(w, new Vector2(1, 3), Faction.Monster, "Lobo junto", current: 500);
        StatusManager.Apply(w, inquisitor, inquisitor, Wrath.Id);

        w.Enqueue(new UseAbilityIntent(inquisitor.Id, HolyStrike.Id, target.Id));
        var events = Run(w);

        var hits = events.OfType<DamageDealt>().Count(d => d.Source == inquisitor.Id);
        Assert.Equal(3, hits); // alvo + 2 em volta
        Assert.Equal(500 + (3 * 8), Hp(closeAlly)); // 10% de 75 = 7,5 → 8 por acerto
        Assert.Equal(500, Hp(farAlly));
        Assert.Equal(500, Hp(wolfNearCaster)); // monstro perto não é aliado
        var closeHeals = events.OfType<HealApplied>().Where(h => h.Target == closeAlly.Id).ToList();
        Assert.Equal(3, closeHeals.Count);
        Assert.All(closeHeals, h => Assert.Equal((inquisitor.Id, DivineWrath.Id, 8f), (h.Source, h.SourceId, h.Effective)));
        Assert.Equal(3, events.OfType<HealApplied>().Count(h => h.Target == inquisitor.Id)); // inclui quem bateu
        Assert.DoesNotContain(events.OfType<HealApplied>(), h => h.Target == farAlly.Id || h.Target == near1.Id);
    }

    [Fact]
    public void Ally_heal_heals_ten_percent_of_one_hundred_and_can_leave_the_attacker_out()
    {
        var w = NewWorld();
        var dealer = Unit(w, new Vector2(1, 1), Faction.Player, "Inquisidor", current: 500);
        var ally = Unit(w, new Vector2(4.5f, 1), Faction.Player, "Maga", current: 500);
        var wolf = Unit(w, new Vector2(2, 1), Faction.Monster, "Lobo", hp: 5000);
        var effect = new DamageEffect
        {
            Base = 100,
            AllyHeal = new DamageAllyHealDefinition { Fraction = 0.1, Radius = 4, IncludeSelf = false },
        };

        EffectApplier.Apply(w, [effect], dealer, [wolf], "test.strike");

        Assert.Equal(100, Lost(wolf));
        Assert.Equal(510, Hp(ally));
        Assert.Equal(500, Hp(dealer));
        Assert.Equal("test.strike", Assert.Single(w.Events.Current.OfType<HealApplied>()).SourceId); // sem ability: a origem do dano
    }
}
