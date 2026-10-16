using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Status;
using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-017: alvo por efeito (self), bonusVsTags, cargas/consumíveis, controlDurationMultiplier, armTime e remove_status por várias tags.</summary>
public class MissingMechanicsTests
{
    private static AbilityDefinition Ab(
        string id, TargetingDefinition targeting, EffectDefinition[] effects, int cooldown = 0, int? charges = null) =>
        new() { Id = id, Name = id, Targeting = targeting, Effects = effects, CooldownTicks = cooldown, Charges = charges };

    private static Entity Unit(
        World w, Vector2 pos, Faction f, float hp = 100, string[]? tags = null, string[]? known = null, string[]? consumables = null)
    {
        var st = new AbilityState();
        st.Known.AddRange(known ?? []);
        st.Consumables.AddRange(consumables ?? []);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = f == Faction.Player ? "Heroi" : "Inimigo", Tags = tags ?? [] },
            st);
    }

    private static List<GameEvent> Run(World w, int ticks)
    {
        var all = new List<GameEvent>();
        for (var i = 0; i < ticks; i++)
        {
            w.Step();
            all.AddRange(w.LastTickEvents);
        }

        return all;
    }

    private static List<GameEvent> Use(World w, Entity e, string ability, Entity? target = null)
    {
        w.Enqueue(new UseAbilityIntent(e.Id, ability, target?.Id, null));
        return Run(w, 1);
    }

    // ---------- target: self ----------

    [Fact]
    public void Self_effect_applies_to_caster_while_other_effects_hit_the_enemy()
    {
        var chain = Ab(
            "hero.chain",
            new UnitTargeting { Range = 10, Faction = FactionFilter.Enemy },
            [new DamageEffect { Base = 20, School = DamageSchool.Holy }, new HealEffect { Base = 10, Target = EffectTarget.Self }]);
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [chain]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.chain"]);
        hero.Get<Health>().Current = 50;
        var foe = Unit(w, new Vector2(4, 1), Faction.Monster);

        var ev = Use(w, hero, "hero.chain", foe);

        Assert.Equal(80, foe.Get<Health>().Current);
        Assert.Equal(60, hero.Get<Health>().Current);
        var heal = Assert.Single(ev.OfType<HealApplied>());
        Assert.Equal(hero.Id, heal.Target);
        Assert.Equal(hero.Id, heal.Source);
    }

    [Fact]
    public void Self_effects_do_not_make_an_area_ability_beneficial()
    {
        // dano nos alvos + cura em si: a facção é decidida só pelos efeitos "targets" (dano => inimigos)
        var nova = Ab(
            "hero.nova",
            new CircleSelfTargeting { Radius = 5 },
            [new DamageEffect { Base = 10 }, new HealEffect { Base = 5, Target = EffectTarget.Self }]);
        Assert.False(TargetResolver.IsBeneficial(nova));
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [nova]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.nova"]);
        var ally = Unit(w, new Vector2(2, 1), Faction.Player);
        var foe = Unit(w, new Vector2(3, 1), Faction.Monster);
        hero.Get<Health>().Current = 50;

        Use(w, hero, "hero.nova");

        Assert.Equal(90, foe.Get<Health>().Current);
        Assert.Equal(100, ally.Get<Health>().Current);
        Assert.Equal(55, hero.Get<Health>().Current);
    }

    [Fact]
    public void Ability_with_only_self_effects_is_not_beneficial()
    {
        var onlySelf = Ab("hero.x", new SelfTargeting(), [new HealEffect { Base = 5, Target = EffectTarget.Self }]);
        Assert.False(TargetResolver.IsBeneficial(onlySelf));
    }

    // ---------- bonusVsTags ----------

    [Theory]
    [InlineData(new string[0], 10)]
    [InlineData(new[] { "undead" }, 15)]
    [InlineData(new[] { "undead", "elite" }, 17)] // bônus somam: +50% +20%
    [InlineData(new[] { "beast" }, 10)]
    public void Bonus_vs_tags_adds_up_for_matching_tags(string[] tags, int expected)
    {
        var smite = Ab(
            "hero.smite",
            new UnitTargeting { Range = 10, Faction = FactionFilter.Enemy },
            [new DamageEffect { Base = 10, BonusVsTags = new Dictionary<string, double> { ["undead"] = 0.5, ["elite"] = 0.2 } }]);
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [smite]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.smite"]);
        var foe = Unit(w, new Vector2(4, 1), Faction.Monster, tags: tags);

        var dmg = Assert.Single(Use(w, hero, "hero.smite", foe).OfType<DamageDealt>());

        Assert.Equal(expected, dmg.Amount);
    }

    // ---------- cargas ----------

    [Fact]
    public void Charges_run_out_and_report_NoCharges()
    {
        var potion = Ab("item.potion", new SelfTargeting(), [new HealEffect { Base = 10 }], cooldown: 0, charges: 2);
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [potion]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, consumables: ["item.potion"]);
        hero.Get<Health>().Current = 10;

        var first = Assert.Single(Use(w, hero, "item.potion").OfType<AbilityExecuted>());
        var second = Assert.Single(Use(w, hero, "item.potion").OfType<AbilityExecuted>());
        var third = Use(w, hero, "item.potion");

        Assert.Equal(1, first.ChargesRemaining);
        Assert.Equal(0, second.ChargesRemaining);
        Assert.Empty(third.OfType<AbilityExecuted>());
        Assert.Equal(AbilityRejectReason.NoCharges, Assert.Single(third.OfType<AbilityRejected>()).Reason);
        Assert.Equal(30, hero.Get<Health>().Current);
        Assert.Equal(0, hero.Get<AbilityState>().ChargesRemaining(potion));
    }

    [Fact]
    public void Unlimited_abilities_report_null_charges()
    {
        var hit = Ab("hero.hit", new SelfTargeting(), [new HealEffect { Base = 1 }]);
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [hit]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.hit"]);

        var ex = Assert.Single(Use(w, hero, "hero.hit").OfType<AbilityExecuted>());

        Assert.Null(ex.ChargesRemaining);
        Assert.Null(hero.Get<AbilityState>().ChargesRemaining(hit));
    }

    [Fact]
    public void Spawned_player_has_consumables_apart_from_abilities_and_cooldown_still_applies()
    {
        var potion = Ab("item.potion", new SelfTargeting(), [new HealEffect { Base = 10 }], cooldown: 100, charges: 3);
        var basic = Ab("hero.basic", new SelfTargeting(), []);
        var cls = new ClassDefinition
        {
            Id = "class.hero",
            Name = "Heroi",
            Health = 100,
            Speed = 4,
            Radius = 0.4,
            Resource = new ResourceDefinition { Type = "mana", Max = 100 },
            BasicAttack = "hero.basic",
            Abilities = [],
            Consumables = ["item.potion"],
        };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(classes: [cls], abilities: [potion, basic]));
        var hero = EntityFactory.SpawnPlayer(w, cls, new Vector2(1, 1));
        var st = hero.Get<AbilityState>();

        Assert.Equal(["item.potion"], st.Consumables);
        Assert.DoesNotContain("item.potion", st.Known);

        Assert.Single(Use(w, hero, "item.potion").OfType<AbilityExecuted>());
        var again = Use(w, hero, "item.potion");
        Assert.Equal(AbilityRejectReason.OnCooldown, Assert.Single(again.OfType<AbilityRejected>()).Reason);
        Assert.Equal(2, st.ChargesRemaining(potion));
    }

    [Fact]
    public void Charges_affect_the_state_hash()
    {
        var potion = Ab("item.potion", new SelfTargeting(), [new HealEffect { Base = 1 }], charges: 2);
        World Make()
        {
            var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [potion]));
            Unit(w, new Vector2(1, 1), Faction.Player, consumables: ["item.potion"]);
            return w;
        }

        var a = Make();
        var b = Make();
        Assert.Equal(a.ComputeStateHash(), b.ComputeStateHash());
        Use(a, a.Entities.First(), "item.potion");
        Run(b, 1);
        Assert.NotEqual(a.ComputeStateHash(), b.ComputeStateHash());
    }

    // ---------- controlDurationMultiplier ----------

    [Fact]
    public void Control_duration_multiplier_shortens_received_control_statuses_only()
    {
        var steady = new StatusDefinition
        {
            Id = "status.steady",
            Name = "Firme",
            DefaultDurationTicks = 400,
            Modifiers = new StatModifiers { ControlDurationMultiplier = 0.5 },
        };
        var root = new StatusDefinition
        {
            Id = "status.root",
            Name = "Preso",
            Tags = ["control"],
            DefaultDurationTicks = 40,
            Flags = new StatusFlags { Rooted = true },
        };
        var poison = new StatusDefinition { Id = "status.poison", Name = "Veneno", Tags = ["debuff"], DefaultDurationTicks = 40 };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(statuses: [steady, root, poison]));
        var plain = Unit(w, new Vector2(1, 1), Faction.Monster);
        var boss = Unit(w, new Vector2(5, 1), Faction.Monster);
        StatusManager.Apply(w, null, boss, "status.steady");

        var a = StatusManager.Apply(w, null, plain, "status.root")!;
        var b = StatusManager.Apply(w, null, boss, "status.root")!;
        var p = StatusManager.Apply(w, null, boss, "status.poison")!;

        Assert.Equal(40, a.ExpiresAtTick - w.Tick);
        Assert.Equal(20, b.ExpiresAtTick - w.Tick);
        Assert.Equal(40, p.ExpiresAtTick - w.Tick); // sem tag control: inalterado
    }

    // ---------- armTime ----------

    [Fact]
    public void Area_does_not_trigger_before_armed_and_announces_arming_once()
    {
        var trap = new AreaDefinition
        {
            Id = "area.trap",
            Radius = 2,
            DurationTicks = 200,
            ArmTimeTicks = 10,
            Mode = AreaMode.OnEnterOnce,
            Effects = [new DamageEffect { Base = 7 }],
        };
        var set = Ab("hero.trap", new CircleAtPointTargeting { Range = 30, Radius = 1 }, [new SpawnAreaEffect { Area = "area.trap" }]);
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [set], areas: [trap]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.trap"]);
        var foe = Unit(w, new Vector2(10, 10), Faction.Monster);

        w.Enqueue(new UseAbilityIntent(hero.Id, "hero.trap", null, new Vector2(10, 10)));
        var ev = Run(w, 1);
        var spawned = Assert.Single(ev.OfType<AreaSpawned>());
        Assert.Equal(spawned.Tick + 10, spawned.ArmedAtTick);

        ev.Clear();
        ev.AddRange(Run(w, 8)); // ainda armando, o inimigo já está dentro
        Assert.Empty(ev.OfType<AreaTriggered>());
        Assert.Empty(ev.OfType<AreaArmed>());
        Assert.Equal(100, foe.Get<Health>().Current);

        ev.AddRange(Run(w, 3));
        var armed = Assert.Single(ev.OfType<AreaArmed>());
        Assert.Equal(spawned.Tick + 10, armed.Tick);
        var trig = Assert.Single(ev.OfType<AreaTriggered>());
        Assert.Equal(armed.Tick, trig.Tick);
        Assert.Equal(93, foe.Get<Health>().Current);
    }

    [Fact]
    public void Area_without_arm_time_triggers_immediately_and_never_announces_arming()
    {
        var trap = new AreaDefinition
        {
            Id = "area.trap",
            Radius = 2,
            DurationTicks = 200,
            Mode = AreaMode.OnEnterOnce,
            Effects = [new DamageEffect { Base = 7 }],
        };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(areas: [trap]));
        var foe = Unit(w, new Vector2(10, 10), Faction.Monster);
        var owner = Unit(w, new Vector2(1, 1), Faction.Player);
        AreaService.Spawn(w, trap, owner, new Vector2(10, 10));

        var ev = Run(w, 3);

        Assert.Single(ev.OfType<AreaTriggered>());
        Assert.Empty(ev.OfType<AreaArmed>());
        Assert.Equal(93, foe.Get<Health>().Current);
    }

    // ---------- remove_status por várias tags ----------

    [Fact]
    public void Remove_status_with_tags_removes_one_status_matching_any_tag()
    {
        var poison = new StatusDefinition { Id = "status.poison", Name = "Veneno", Tags = ["poison"], DefaultDurationTicks = 400 };
        var curse = new StatusDefinition { Id = "status.curse", Name = "Maldição", Tags = ["curse"], DefaultDurationTicks = 400 };
        var slow = new StatusDefinition { Id = "status.slow", Name = "Lento", Tags = ["slow"], DefaultDurationTicks = 400 };
        var purify = Ab(
            "hero.purify",
            new UnitTargeting { Range = 10, Faction = FactionFilter.Ally },
            [new RemoveStatusEffect { Tags = ["poison", "curse"], Count = 1 }]);
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [purify], statuses: [poison, curse, slow]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.purify"]);
        var ally = Unit(w, new Vector2(3, 1), Faction.Player);
        StatusManager.Apply(w, null, ally, "status.slow");
        StatusManager.Apply(w, null, ally, "status.curse");
        StatusManager.Apply(w, null, ally, "status.poison");

        var ev = Use(w, hero, "hero.purify", ally);

        var removed = Assert.Single(ev.OfType<StatusRemoved>());
        Assert.Equal("status.curse", removed.StatusId); // o mais antigo entre os que casam
        Assert.Equal(hero.Id, removed.By);
        Assert.True(StatusManager.Has(w, ally, "status.poison"));
        Assert.True(StatusManager.Has(w, ally, "status.slow"));

        Use(w, hero, "hero.purify", ally);
        Assert.False(StatusManager.Has(w, ally, "status.poison"));
        Assert.True(StatusManager.Has(w, ally, "status.slow"));
    }

    // ---------- log ----------

    [Fact]
    public void Log_formats_area_armed_and_no_charges()
    {
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [Ab("item.potion", new SelfTargeting(), [])]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player);
        var log = new CombatLogRecorder(viewer: hero.Id);
        w.Events.Publish(new AreaArmed(EntityId.None, "area.trap", hero.Id));
        w.Events.Publish(new AbilityRejected(hero.Id, "item.potion", AbilityRejectReason.NoCharges));
        w.Step();
        log.Record(w);

        Assert.Contains(log.Entries, e => e.Text.Contains("está armada"));
        Assert.Contains(log.Entries, e => e.Text.Contains("sem cargas"));
    }
}
