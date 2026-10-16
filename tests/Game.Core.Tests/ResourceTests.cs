using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Resources;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

public class ResourceTests
{
    private sealed class FakeStatus : IStatusQuery
    {
        public HashSet<(EntityId, string)> Has { get; } = new();

        public bool HasStatusWithTag(World world, Entity entity, string tag) => Has.Contains((entity.Id, tag));
    }

    private static ClassDefinition Cls(string id, string resource, double max, double initial, double regen, params ResourceRuleDefinition[] rules) =>
        new()
        {
            Id = id,
            Name = id,
            Health = 100,
            Speed = 4,
            Radius = 0.4,
            Resource = new ResourceDefinition { Type = resource, Max = max, Initial = initial, RegenPerSecond = regen },
            BasicAttack = "x",
            ResourceRules = rules,
        };

    private static AbilityDefinition Ab(string id, TargetingDefinition t, EffectDefinition[] fx, params string[] tags) =>
        new() { Id = id, Name = id, Targeting = t, Effects = fx, Tags = tags };

    private static readonly UnitTargeting Far = new() { Range = 20 };
    private static readonly UnitTargeting FarAlly = new() { Range = 20, Faction = FactionFilter.Ally };

    private static World NewWorld(ClassDefinition[] classes, params AbilityDefinition[] abilities) =>
        WorldBuilder.CreateDefault(1, null, new ContentCatalog(classes: classes, abilities: abilities));

    private static Entity Player(World w, ClassDefinition c, Vector2 pos, params string[] abilities)
    {
        var st = new AbilityState();
        st.Known.AddRange(abilities);
        return w.Spawn(
            new Transform { Position = pos },
            new Health((float)c.Health),
            new FactionTag(Faction.Player),
            new Collider(0.4f),
            new Movement(0),
            new ResourcePool(c.Resource.Type, (float)c.Resource.Max, (float)c.Resource.Initial),
            new UnitInfo { DefinitionId = c.Id },
            st);
    }

    private static Entity Monster(World w, Vector2 pos, float hp, params string[] abilities)
    {
        var st = new AbilityState();
        st.Known.AddRange(abilities);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(Faction.Monster),
            new Collider(0.4f),
            new Movement(0),
            new UnitInfo { DefinitionId = "monster.test", Tags = ["undead"] },
            st);
    }

    private static void Use(World w, Entity e, string ability, Entity? target = null) =>
        w.Enqueue(new UseAbilityIntent(e.Id, ability, target?.Id, null));

    private static float Res(Entity e) => e.Get<ResourcePool>().Current;

    private static List<ResourceChanged> Changes(World w) => w.LastTickEvents.OfType<ResourceChanged>().ToList();

    private static ResourceRuleDefinition Rule(string id, ResourceTrigger t, double amount = 0, double ratio = 0) =>
        new() { Id = id, Trigger = t, Amount = amount, Ratio = ratio };

    [Fact]
    public void Warrior_gains_more_vigor_blocking_with_guard_than_without()
    {
        var warrior = Cls("class.warrior", "vigor", 100, 0, 0,
            Rule("taken", ResourceTrigger.DamageTaken, ratio: 0.1),
            Rule("block", ResourceTrigger.DamageTaken, amount: 8) with { SourceHasStatusTag = "guard", MinValue = 10 });
        var hit = Ab("m.hit", Far, [new DamageEffect { Base = 20 }]);

        float Run(bool guarded)
        {
            var w = NewWorld([warrior], hit);
            var status = new FakeStatus();
            ResourceModule.For(w.Combat).StatusQuery = status;
            var p = Player(w, warrior, new Vector2(1, 1));
            var m = Monster(w, new Vector2(2, 1), 100, "m.hit");
            if (guarded)
            {
                status.Has.Add((p.Id, "guard"));
            }

            Use(w, m, "m.hit", p);
            w.Step();
            return Res(p);
        }

        var plain = Run(false);
        var blocked = Run(true);
        Assert.Equal(2, plain);
        Assert.Equal(10, blocked);
        Assert.True(blocked > plain);
    }

    [Fact]
    public void Focus_is_only_generated_beyond_minimum_distance_and_on_marked_target()
    {
        var archer = Cls("class.archer", "focus", 100, 0, 0,
            Rule("safe_hit", ResourceTrigger.DamageDealt, amount: 5) with { MinDistance = 6 },
            Rule("marked_hit", ResourceTrigger.DamageDealt, amount: 3) with { TargetHasStatusTag = "mark" });
        var shot = Ab("a.shot", Far, [new DamageEffect { Base = 5 }]);

        var w = NewWorld([archer], shot);
        var status = new FakeStatus();
        ResourceModule.For(w.Combat).StatusQuery = status;
        var a = Player(w, archer, new Vector2(1, 1), "a.shot");
        var near = Monster(w, new Vector2(4, 1), 100);
        var far = Monster(w, new Vector2(10, 1), 100);

        Use(w, a, "a.shot", near);
        w.Step();
        Assert.Equal(0, Res(a));

        Use(w, a, "a.shot", far);
        w.Step();
        Assert.Equal(5, Res(a));
        var ev = Assert.Single(Changes(w));
        Assert.Equal(ResourceChangeReason.Rule, ev.Reason);
        Assert.Equal("safe_hit", ev.SourceId);

        status.Has.Add((near.Id, "mark"));
        Use(w, a, "a.shot", near);
        w.Step();
        Assert.Equal(8, Res(a));
    }

    [Fact]
    public void Full_overheal_gives_no_faith_but_cleanse_does()
    {
        var cleric = Cls("class.cleric", "faith", 100, 0, 0,
            Rule("heal", ResourceTrigger.HealDone, ratio: 0.5) with { MinEffectiveHealPercent = 5 },
            Rule("purify", ResourceTrigger.AbilityExecuted, amount: 12) with { AbilityTag = "cleanse" });
        var heal = Ab("c.heal", FarAlly, [new HealEffect { Base = 30 }], "heal");
        var cleanse = Ab("c.cleanse", FarAlly, [new HealEffect { Base = 1 }], "cleanse");

        var w = NewWorld([cleric], heal, cleanse);
        var c = Player(w, cleric, new Vector2(1, 1), "c.heal", "c.cleanse");
        var ally = Player(w, cleric, new Vector2(2, 1));

        Use(w, c, "c.heal", ally); // alvo com vida cheia: tudo excedente
        w.Step();
        Assert.Equal(0, Res(c));
        Assert.Empty(Changes(w));

        Use(w, c, "c.cleanse", ally); // purificação: gera mesmo sem cura efetiva
        w.Step();
        Assert.Equal(12, Res(c));

        ally.Get<Health>().Current = 50;
        Use(w, c, "c.heal", ally); // 30 efetivos = 30% da vida máxima
        w.Step();
        Assert.Equal(12 + 15, Res(c));

        ally.Get<Health>().Current = 98; // 2 efetivos = 2% < 5%
        Use(w, c, "c.heal", ally);
        w.Step();
        Assert.Equal(27, Res(c));
    }

    [Fact]
    public void Internal_cooldown_prevents_repeated_generation()
    {
        var mage = Cls("class.mage", "mana", 100, 0, 0,
            Rule("r", ResourceTrigger.DamageDealt, amount: 4) with { InternalCooldownTicks = 10 });
        var zap = Ab("m.zap", Far, [new DamageEffect { Base = 1 }]);
        var w = NewWorld([mage], zap);
        var p = Player(w, mage, new Vector2(1, 1), "m.zap");
        var m = Monster(w, new Vector2(3, 1), 1000);

        Use(w, p, "m.zap", m);
        w.Step(); // tick 1
        Use(w, p, "m.zap", m);
        w.Step(); // tick 2: em recarga interna
        Assert.Equal(4, Res(p));

        for (var i = 0; i < 8; i++)
        {
            w.Step(); // ticks 3..10
        }

        Use(w, p, "m.zap", m);
        w.Step(); // tick 11: pronto (1 + 10)
        Assert.Equal(8, Res(p));
    }

    [Fact]
    public void Interrupt_shield_and_target_tag_triggers()
    {
        var warrior = Cls("class.warrior", "vigor", 100, 0, 0,
            Rule("intr", ResourceTrigger.InterruptDone, amount: 15),
            Rule("undead", ResourceTrigger.DamageDealt, amount: 2) with { TargetTag = "undead" },
            Rule("shield", ResourceTrigger.ShieldApplied, ratio: 0.1));
        var intr = Ab("w.bash", Far, [new InterruptEffect()]);
        var slow = new AbilityDefinition
        {
            Id = "m.cast",
            Name = "m.cast",
            Targeting = Far,
            CastTimeTicks = 40,
            Effects = [new DamageEffect { Base = 1 }],
        };
        var hit = Ab("w.hit", Far, [new DamageEffect { Base = 1 }]);
        var shield = Ab("w.shield", new SelfTargeting(), [new ShieldEffect { Amount = 30, DurationTicks = 100 }]);
        var w = NewWorld([warrior], intr, slow, hit, shield);
        var p = Player(w, warrior, new Vector2(1, 1), "w.bash", "w.hit", "w.shield");
        var m = Monster(w, new Vector2(2, 1), 100, "m.cast");

        Use(w, m, "m.cast", p);
        w.Step();
        Use(w, p, "w.bash", m);
        w.Step();
        Assert.Equal(15, Res(p));

        Use(w, p, "w.hit", m);
        w.Step();
        Assert.Equal(17, Res(p));

        Use(w, p, "w.shield");
        w.Step();
        Assert.Equal(20, Res(p));
    }

    [Fact]
    public void Resource_effect_gains_and_spends_and_clamps_with_events()
    {
        var c = Cls("class.x", "mana", 50, 40, 0);
        var gain = Ab("x.gain", new SelfTargeting(), [new ResourceEffect { Resource = "mana", Amount = 30 }]);
        var spend = Ab("x.spend", new SelfTargeting(), [new ResourceEffect { Resource = "mana", Amount = -100 }]);
        var other = Ab("x.other", new SelfTargeting(), [new ResourceEffect { Resource = "faith", Amount = 5 }]);
        var w = NewWorld([c], gain, spend, other);
        var p = Player(w, c, Vector2.One, "x.gain", "x.spend", "x.other");

        Use(w, p, "x.gain");
        w.Step();
        Assert.Equal(50, Res(p));
        var ev = Assert.Single(Changes(w));
        Assert.Equal(10, ev.Delta);
        Assert.Equal(ResourceChangeReason.Effect, ev.Reason);
        Assert.Equal("x.gain", ev.SourceId);

        Use(w, p, "x.other"); // recurso diferente: ignorado
        w.Step();
        Assert.Empty(Changes(w));

        Use(w, p, "x.spend");
        w.Step();
        Assert.Equal(0, Res(p));
        Assert.Equal(-50, Assert.Single(Changes(w)).Delta);
    }

    [Fact]
    public void Ability_cost_publishes_resource_changed()
    {
        var c = Cls("class.x", "mana", 50, 50, 0);
        var spell = Ab("x.spell", new SelfTargeting(), [new HealEffect { Base = 1 }]) with { Cost = new ResourceCost { Resource = "mana", Amount = 10 } };
        var w = NewWorld([c], spell);
        var p = Player(w, c, Vector2.One, "x.spell");
        Use(w, p, "x.spell");
        w.Step();
        var ev = Assert.Single(Changes(w));
        Assert.Equal(ResourceChangeReason.Cost, ev.Reason);
        Assert.Equal(-10, ev.Delta);
        Assert.Equal(40, Res(p));
    }

    [Fact]
    public void Passive_regen_and_decay_emit_events_and_respect_combat_flag()
    {
        var mage = Cls("class.mage", "mana", 100, 0, 10); // +10/s = 0.5 por tick
        var warrior = Cls("class.warrior", "vigor", 100, 50, -20) with
        {
            Resource = new ResourceDefinition
            {
                Type = "vigor",
                Max = 100,
                Initial = 50,
                RegenPerSecond = -20,
                PassiveOnlyOutOfCombat = true,
                CombatTimeoutTicks = 10,
            },
        };
        var hit = Ab("m.hit", Far, [new DamageEffect { Base = 1 }]);
        var w = NewWorld([mage, warrior], hit);
        var mg = Player(w, mage, Vector2.One);
        var wr = Player(w, warrior, new Vector2(3, 1));
        var m = Monster(w, new Vector2(5, 1), 100, "m.hit");

        w.Step();
        w.Step();
        Assert.Equal(1, Res(mg));
        Assert.Contains(w.LastTickEvents.OfType<ResourceChanged>(), e => e.Reason == ResourceChangeReason.Regen && e.Entity == mg.Id);
        Assert.Equal(50 - 2, Res(wr)); // fora de combate: decai 1 por tick

        Use(w, m, "m.hit", wr);
        w.Step(); // tick 3: levou dano, entra em combate
        var afterHit = Res(wr);
        for (var i = 0; i < 5; i++)
        {
            w.Step();
        }

        Assert.Equal(afterHit, Res(wr)); // em combate: sem decaimento
        for (var i = 0; i < 20; i++)
        {
            w.Step();
        }

        Assert.True(Res(wr) < afterHit);
    }

    [Fact]
    public void Status_removed_trigger_works_via_notification()
    {
        var cleric = Cls("class.cleric", "faith", 100, 0, 0,
            Rule("purge", ResourceTrigger.StatusRemoved, amount: 7) with { RemovedStatusTag = "debuff" });
        var w = NewWorld([cleric]);
        var c = Player(w, cleric, Vector2.One);
        var ally = Player(w, cleric, new Vector2(2, 1));
        var svc = ResourceModule.For(w.Combat);

        svc.NotifyStatusRemoved(new StatusRemovalNotice(c.Id, ally.Id, "status.poison", ["dot"], "c.cleanse"));
        w.Step();
        Assert.Equal(0, Res(c));

        svc.NotifyStatusRemoved(new StatusRemovalNotice(c.Id, ally.Id, "status.poison", ["debuff"], "c.cleanse"));
        w.Step();
        Assert.Equal(7, Res(c));
    }

    [Fact]
    public void Resource_state_is_deterministic()
    {
        ulong Run()
        {
            var mage = Cls("class.mage", "mana", 100, 0, 3,
                Rule("r", ResourceTrigger.DamageDealt, amount: 2) with { InternalCooldownTicks = 4 });
            var zap = Ab("m.zap", Far, [new DamageEffect { Base = 1 }]);
            var w = NewWorld([mage], zap);
            var p = Player(w, mage, Vector2.One, "m.zap");
            var m = Monster(w, new Vector2(3, 1), 1000);
            for (var i = 0; i < 200; i++)
            {
                if (i % 3 == 0)
                {
                    Use(w, p, "m.zap", m);
                }

                w.Step();
            }

            return w.ComputeStateHash();
        }

        Assert.Equal(Run(), Run());
    }
}
