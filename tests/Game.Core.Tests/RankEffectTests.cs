using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Resources;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-061: efeitos novos pedidos pelos níveis de habilidade (um teste ou mais por efeito).</summary>
public class RankEffectTests
{
    private static readonly ControlRules Control = new()
    {
        Diminishing = new ControlDiminishing { WindowTicks = 300, Steps = [1.0, 0.5, 0.25, 0.0] },
    };

    private static DamageEffect Dmg(double b) => new() { Base = b };

    private static StatusDefinition St(string id, int duration = 40, string[]? tags = null) =>
        new() { Id = id, Name = id, DefaultDurationTicks = duration, Tags = tags ?? [] };

    private static readonly StatusDefinition Stun = St("t.stun", tags: ["control", "stun"]) with { Flags = new StatusFlags { Stunned = true } };
    private static readonly StatusDefinition Root = St("t.root", tags: ["control", "root"]) with { Flags = new StatusFlags { Rooted = true } };
    private static readonly StatusDefinition Poison = St("t.poison", tags: ["poison"]);
    private static readonly StatusDefinition Bleed = St("t.bleed", tags: ["bleed"]);
    private static readonly StatusDefinition Mark = St("t.mark", tags: ["mark"]);
    private static readonly StatusDefinition Slow = St("t.slow", 10, ["slow"]) with { Modifiers = new StatModifiers { SpeedMultiplier = 0.8 } };

    private static AbilityDefinition Ab(string id, TargetingDefinition t, params EffectDefinition[] effects) =>
        new() { Id = id, Name = id, Targeting = t, Effects = effects };

    private static UnitTargeting Enemy(double range = 10) => new() { Range = range };

    private static UnitTargeting Ally(double range = 10) => new() { Range = range, Faction = FactionFilter.Ally };

    private static World NewWorld(
        AbilityDefinition[] abilities, StatusDefinition[]? statuses = null, AreaDefinition[]? areas = null) =>
        WorldBuilder.CreateDefault(
            1,
            null,
            new ContentCatalog(abilities: abilities, statuses: [Stun, Root, Poison, Bleed, Mark, Slow, .. statuses ?? []], areas: areas)
            {
                Rules = new RulesDefinition { Control = Control },
            });

    private static Entity Unit(World w, Vector2 pos, Faction f, float hp = 100, params string[] abilities)
    {
        var st = new AbilityState();
        st.Known.AddRange(abilities);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new ResourcePool("mana", 100, 50),
            st);
    }

    private static Entity Hero(World w, Vector2 pos, params string[] abilities) => Unit(w, pos, Faction.Player, 100, abilities);

    private static Entity Mob(World w, Vector2 pos, float hp = 1000) => Unit(w, pos, Faction.Monster, hp);

    private static List<GameEvent> Use(World w, Entity caster, string ability, Entity? target = null, Vector2? aim = null, int ticks = 1)
    {
        w.Enqueue(new UseAbilityIntent(caster.Id, ability, target?.Id, aim));
        return Run(w, ticks);
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

    private static float DamageTo(IEnumerable<GameEvent> events, Entity e) =>
        events.OfType<DamageDealt>().Where(d => d.Target == e.Id).Sum(d => d.Amount);

    // ---------- 1. atordoar junto do dano ----------

    [Fact]
    public void Stun_with_damage_uses_control_diminishing_returns()
    {
        var heavy = Ab(
            "t.heavy",
            new MeleeArcTargeting { Range = 2, Angle = 120 },
            Dmg(10),
            new InterruptEffect(),
            new ApplyStatusEffect { Status = Stun.Id, DurationTicks = 10 });
        var w = NewWorld([heavy]);
        var hero = Hero(w, Vector2.Zero, heavy.Id);
        var mob = Mob(w, new Vector2(1, 0));

        var first = Use(w, hero, heavy.Id, mob);
        Assert.Equal(10, DamageTo(first, mob));
        Assert.Equal(10, Assert.Single(first.OfType<StatusApplied>()).DurationTicks);
        Assert.True(StatusManager.IsStunned(w, mob));

        Run(w, 12); // atordoamento acaba
        Assert.False(StatusManager.IsStunned(w, mob));

        var second = Use(w, hero, heavy.Id, mob);
        Assert.Equal(5, Assert.Single(second.OfType<StatusApplied>()).DurationTicks); // retorno decrescente: metade
    }

    // ---------- 2. saltos ----------

    [Fact]
    public void Chain_jumps_to_the_nearest_unhit_enemies_with_a_fraction_of_the_damage()
    {
        var chain = Ab(
            "t.chain",
            Enemy(8),
            Dmg(20),
            new ChainEffect { Jumps = 3, Radius = 4, Multiplier = 0.7, Effects = [Dmg(20)] });
        var w = NewWorld([chain]);
        var hero = Hero(w, Vector2.Zero, chain.Id);
        var a = Mob(w, new Vector2(3, 0));
        var b = Mob(w, new Vector2(6, 0));
        var c = Mob(w, new Vector2(9, 0));
        var far = Mob(w, new Vector2(20, 0));
        var ally = Hero(w, new Vector2(4, 1)); // aliado perto não recebe salto

        var ev = Use(w, hero, chain.Id, a);

        Assert.Equal(20, DamageTo(ev, a));
        Assert.Equal(14, DamageTo(ev, b));
        Assert.Equal(14, DamageTo(ev, c));
        Assert.Equal(0, DamageTo(ev, far));
        Assert.Equal(0, DamageTo(ev, ally));
        var jumps = ev.OfType<ChainJumped>().ToList();
        Assert.Equal([(a.Id, b.Id, 1), (b.Id, c.Id, 2)], jumps.Select(j => (j.From, j.To, j.Jump)));
    }

    // ---------- 3. segundo alvo automático ----------

    [Fact]
    public void Extra_target_marks_also_the_enemy_nearest_to_the_target()
    {
        var mark = Ab(
            "t.mark_shot",
            Enemy(),
            new ApplyStatusEffect { Status = Mark.Id },
            new ExtraTargetsEffect { Radius = 5, Effects = [new ApplyStatusEffect { Status = Mark.Id }] });
        var w = NewWorld([mark]);
        var hero = Hero(w, Vector2.Zero, mark.Id);
        var a = Mob(w, new Vector2(3, 0));
        var near = Mob(w, new Vector2(4.5f, 0));
        var other = Mob(w, new Vector2(6, 0));

        Use(w, hero, mark.Id, a);

        Assert.True(StatusManager.Has(w, a, Mark.Id));
        Assert.True(StatusManager.Has(w, near, Mark.Id));
        Assert.False(StatusManager.Has(w, other, Mark.Id));
    }

    [Fact]
    public void Extra_target_heals_also_the_most_injured_ally_at_half_power()
    {
        var heal = Ab(
            "t.heal",
            Ally(),
            new HealEffect { Base = 40 },
            new ExtraTargetsEffect
            {
                Radius = 6,
                Select = TargetSelect.MostInjured,
                Affects = FactionFilter.Ally,
                Multiplier = 0.5,
                Effects = [new HealEffect { Base = 40 }],
            });
        var w = NewWorld([heal]);
        var cleric = Hero(w, Vector2.Zero, heal.Id);
        var target = Hero(w, new Vector2(3, 0));
        var hurt = Hero(w, new Vector2(5, 0));
        var scratched = Hero(w, new Vector2(3.5f, 0));
        var enemy = Mob(w, new Vector2(3, 1), hp: 100);
        target.Get<Health>().Current = 10;
        hurt.Get<Health>().Current = 50;
        scratched.Get<Health>().Current = 80;
        enemy.Get<Health>().Current = 5;

        var ev = Use(w, cleric, heal.Id, target);

        var heals = ev.OfType<HealApplied>().ToDictionary(h => h.Target, h => h.Amount);
        Assert.Equal(40, heals[target.Id]);
        Assert.Equal(20, heals[hurt.Id]);
        Assert.Equal(2, heals.Count); // nem o arranhado, nem o inimigo
    }

    [Fact]
    public void Self_shield_also_protects_the_nearest_ally_at_half_value()
    {
        var barrier = Ab(
            "t.barrier",
            new SelfTargeting(),
            new ShieldEffect { Amount = 30, DurationTicks = 100 },
            new ExtraTargetsEffect
            {
                Radius = 8,
                Affects = FactionFilter.Ally,
                Multiplier = 0.5,
                Effects = [new ShieldEffect { Amount = 30, DurationTicks = 100 }],
            });
        var w = NewWorld([barrier]);
        var mage = Hero(w, Vector2.Zero, barrier.Id);
        var near = Hero(w, new Vector2(2, 0));
        var far = Hero(w, new Vector2(4, 0));

        Use(w, mage, barrier.Id);

        Assert.Equal(30, mage.Get<ShieldState>().Total);
        Assert.Equal(15, near.Get<ShieldState>().Total);
        Assert.Null(far.TryGet<ShieldState>());
    }

    // ---------- 4. área em volta do alvo ----------

    [Fact]
    public void Status_area_around_the_target_freezes_everyone_close_but_damages_only_the_target()
    {
        var prison = Ab(
            "t.prison",
            Enemy(),
            Dmg(5),
            new ApplyStatusEffect { Status = Root.Id },
            new ExtraTargetsEffect { Radius = 2, Count = 0, Effects = [new ApplyStatusEffect { Status = Root.Id }] });
        var w = NewWorld([prison]);
        var hero = Hero(w, Vector2.Zero, prison.Id);
        var a = Mob(w, new Vector2(5, 0));
        var b = Mob(w, new Vector2(6.5f, 0));
        var c = Mob(w, new Vector2(5, 1.8f));
        var outside = Mob(w, new Vector2(8.5f, 0));

        var ev = Use(w, hero, prison.Id, a);

        Assert.All([a, b, c], m => Assert.True(StatusManager.IsRooted(w, m)));
        Assert.False(StatusManager.IsRooted(w, outside));
        Assert.Equal(5, DamageTo(ev, a));
        Assert.Single(ev.OfType<DamageDealt>());
    }

    // ---------- 5. devolver Mana quando o escudo acaba ----------

    [Fact]
    public void Shield_refunds_mana_from_what_is_left_when_it_expires()
    {
        var barrier = Ab(
            "t.barrier",
            new SelfTargeting(),
            new ShieldEffect { Amount = 40, DurationTicks = 10, Refund = new ShieldRefundDefinition { Resource = "mana", Fraction = 0.3 } });
        var w = NewWorld([barrier]);
        var mage = Hero(w, Vector2.Zero, barrier.Id);
        var mob = Mob(w, new Vector2(1, 0));

        Use(w, mage, barrier.Id);
        DamageResolver.Deal(w, mob, mage, "t.hit", 10, DamageSchool.Physical);
        var ev = Run(w, 12);

        var refund = Assert.Single(ev.OfType<ResourceChanged>(), r => r.Entity == mage.Id);
        Assert.Equal(9, refund.Delta); // 30 que sobraram × 30%
        Assert.Equal(barrier.Id, refund.SourceId);
        Assert.Equal(59, mage.Get<ResourcePool>().Current);
    }

    [Fact]
    public void Depleted_shield_refunds_nothing()
    {
        var barrier = Ab(
            "t.barrier",
            new SelfTargeting(),
            new ShieldEffect { Amount = 40, DurationTicks = 10, Refund = new ShieldRefundDefinition { Resource = "mana", Fraction = 0.3 } });
        var w = NewWorld([barrier]);
        var mage = Hero(w, Vector2.Zero, barrier.Id);
        var mob = Mob(w, new Vector2(1, 0));

        Use(w, mage, barrier.Id);
        DamageResolver.Deal(w, mob, mage, "t.hit", 60, DamageSchool.Physical);
        var ev = Run(w, 12);

        Assert.DoesNotContain(ev.OfType<ResourceChanged>(), r => r.Entity == mage.Id);
    }

    // ---------- 6. refletir dano corpo a corpo bloqueado ----------

    private static readonly StatusDefinition Guard = St("t.guard", 200) with
    {
        Frontal = new FrontalModifier { ArcDegrees = 180, DamageTakenMultiplier = 0.4, ReflectMeleeFraction = 0.2 },
    };

    private static (World W, Entity Warrior, Entity Mob) GuardSetup(AbilityDefinition attack, Vector2 mobPos)
    {
        var w = NewWorld([attack], [Guard]);
        var warrior = Hero(w, Vector2.Zero);
        warrior.Get<Transform>().Facing = new Vector2(1, 0);
        var mob = Unit(w, mobPos, Faction.Monster, 1000, attack.Id);
        StatusManager.Apply(w, warrior, warrior, Guard.Id);
        return (w, warrior, mob);
    }

    [Fact]
    public void Firm_guard_reflects_part_of_the_blocked_melee_damage()
    {
        var bite = Ab("t.bite", new MeleeArcTargeting { Range = 2, Angle = 120 }, Dmg(50));
        var (w, warrior, mob) = GuardSetup(bite, new Vector2(1, 0));

        var ev = Use(w, mob, bite.Id, warrior);

        Assert.Equal(20, DamageTo(ev, warrior)); // 50 × 0,4
        var reflected = Assert.Single(ev.OfType<DamageDealt>(), d => d.Target == mob.Id);
        Assert.Equal(6, reflected.Amount); // 20% dos 30 bloqueados
        Assert.Equal(Guard.Id, reflected.SourceId);
        Assert.Equal(warrior.Id, reflected.Source);
    }

    [Fact]
    public void Firm_guard_does_not_reflect_ranged_hits_or_hits_from_behind()
    {
        var shot = Ab("t.shot", Enemy(), Dmg(50));
        var (w, warrior, mob) = GuardSetup(shot, new Vector2(5, 0));
        var ranged = Use(w, mob, shot.Id, warrior);
        Assert.Equal(20, DamageTo(ranged, warrior));
        Assert.Equal(0, DamageTo(ranged, mob));

        var bite = Ab("t.bite", new MeleeArcTargeting { Range = 2, Angle = 120 }, Dmg(50));
        var (w2, warrior2, behind) = GuardSetup(bite, new Vector2(-1, 0));
        var back = Use(w2, behind, bite.Id, warrior2);
        Assert.Equal(50, DamageTo(back, warrior2));
        Assert.Equal(0, DamageTo(back, behind));
    }

    // ---------- 7. imunidade temporária a tags ----------

    [Fact]
    public void Purify_can_grant_temporary_immunity_to_status_tags()
    {
        var purified = St("t.purified", 40) with { ImmuneToTags = ["poison", "curse"] };
        var purify = Ab(
            "t.purify",
            Ally(),
            new RemoveStatusEffect { Tags = ["poison", "curse"], Count = 2 },
            new ApplyStatusEffect { Status = purified.Id });
        var w = NewWorld([purify], [purified]);
        var cleric = Hero(w, Vector2.Zero, purify.Id);
        var ally = Hero(w, new Vector2(2, 0));
        var mob = Mob(w, new Vector2(4, 0));
        StatusManager.Apply(w, mob, ally, Poison.Id);

        Use(w, cleric, purify.Id, ally);
        Assert.False(StatusManager.Has(w, ally, Poison.Id));

        Assert.Null(StatusManager.Apply(w, mob, ally, Poison.Id));
        Assert.Contains(w.Events.Current.OfType<StatusResisted>(), r => r.StatusId == Poison.Id);
        Assert.NotNull(StatusManager.Apply(w, mob, ally, Bleed.Id)); // outras tags pegam normalmente

        Run(w, 41); // a imunidade acaba
        Assert.NotNull(StatusManager.Apply(w, mob, ally, Poison.Id));
    }

    // ---------- 8. bônus no próximo ataque depois do deslocamento ----------

    private static readonly AbilityDefinition Shot = Ab("t.shot", Enemy(), Dmg(10)) with
    {
        Hits = new AbilityHitsDefinition { Count = 2, IntervalTicks = 2 },
    };

    private static readonly StatusDefinition Focus = St("t.focus", 40) with
    {
        Empower = new EmpowerDefinition { Abilities = [Shot.Id], Multiplier = 1.3 },
    };

    private static readonly AbilityDefinition Retreat = Ab(
        "t.retreat",
        new SelfTargeting(),
        new DisplacementEffect { Kind = DisplacementKind.Dash, Distance = 1, Direction = DisplacementDirection.Backward },
        new ApplyStatusEffect { Status = Focus.Id, Target = EffectTarget.Self });

    [Fact]
    public void Retreat_empowers_every_hit_of_the_next_shot_only()
    {
        var w = NewWorld([Shot, Retreat], [Focus]);
        var archer = Hero(w, new Vector2(5, 5), Shot.Id, Retreat.Id);
        var mob = Mob(w, new Vector2(5, 9));

        Use(w, archer, Retreat.Id, aim: new Vector2(5, 9));
        Assert.True(StatusManager.Has(w, archer, Focus.Id));

        var empowered = Use(w, archer, Shot.Id, mob, ticks: 5);
        Assert.Equal([13f, 13f], empowered.OfType<DamageDealt>().Select(d => d.Amount));
        Assert.False(StatusManager.Has(w, archer, Focus.Id));

        var normal = Use(w, archer, Shot.Id, mob, ticks: 5);
        Assert.Equal([10f, 10f], normal.OfType<DamageDealt>().Select(d => d.Amount));
    }

    [Fact]
    public void Empower_is_lost_when_it_expires()
    {
        var w = NewWorld([Shot, Retreat], [Focus]);
        var archer = Hero(w, new Vector2(5, 5), Shot.Id, Retreat.Id);
        var mob = Mob(w, new Vector2(5, 9));

        Use(w, archer, Retreat.Id, aim: new Vector2(5, 9));
        Run(w, 41);

        var late = Use(w, archer, Shot.Id, mob, ticks: 5);
        Assert.Equal([10f, 10f], late.OfType<DamageDealt>().Select(d => d.Amount));
    }

    // ---------- 9. levantar aliado caído ----------

    [Fact]
    public void Miracle_raises_the_nearest_fallen_ally_with_a_fraction_of_health()
    {
        var miracle = Ab(
            "t.miracle",
            Ally(),
            new HealEffect { Base = 10 },
            new ReviveEffect { Radius = 4, HealthFraction = 0.3 });
        var w = NewWorld([miracle]);
        var cleric = Hero(w, Vector2.Zero, miracle.Id);
        var target = Hero(w, new Vector2(2, 0));
        var fallen = Hero(w, new Vector2(4, 0));
        var fallenFar = Hero(w, new Vector2(9, 0));
        var deadMob = Mob(w, new Vector2(3, 0));
        var mob = Mob(w, new Vector2(12, 0));
        StatusManager.Apply(w, mob, fallen, Poison.Id, 200);
        foreach (var e in new[] { fallen, fallenFar, deadMob })
        {
            w.Kill(e);
        }

        Run(w, 5);
        Assert.NotNull(w.Find(fallen.Id)); // caído continua no mundo
        Assert.False(fallen.IsAlive);

        var ev = Use(w, cleric, miracle.Id, target);

        Assert.True(fallen.IsAlive);
        Assert.Equal(30, fallen.Get<Health>().Current);
        var revived = Assert.Single(ev.OfType<EntityRevived>());
        Assert.Equal((fallen.Id, cleric.Id, miracle.Id), (revived.Entity, revived.By, revived.SourceId));
        Assert.False(StatusManager.Has(w, fallen, Poison.Id)); // volta sem os status de antes
        Assert.False(fallenFar.IsAlive);
        Assert.False(deadMob.IsAlive);
    }

    // ---------- 10. remover controle dos aliados ----------

    [Fact]
    public void Living_wall_removes_every_control_status_from_allies_only()
    {
        var wall = Ab(
            "t.wall",
            new CircleSelfTargeting { Radius = 7 },
            new ShieldEffect { Amount = 20, DurationTicks = 100 },
            new RemoveStatusEffect { Tags = ["control"], Count = 0 });
        var w = NewWorld([wall]);
        var warrior = Hero(w, Vector2.Zero, wall.Id);
        var ally = Hero(w, new Vector2(3, 0));
        var mob = Mob(w, new Vector2(2, 2));
        StatusManager.Apply(w, mob, ally, Stun.Id);
        StatusManager.Apply(w, mob, ally, Root.Id);
        StatusManager.Apply(w, mob, ally, Poison.Id);
        StatusManager.Apply(w, warrior, mob, Stun.Id);

        Use(w, warrior, wall.Id);

        Assert.False(StatusManager.IsStunned(w, ally));
        Assert.False(StatusManager.IsRooted(w, ally));
        Assert.True(StatusManager.Has(w, ally, Poison.Id));
        Assert.True(StatusManager.IsStunned(w, mob));
        Assert.Equal(20, ally.Get<ShieldState>().Total);
    }

    // ---------- 11. área persistente deixada pela habilidade ----------

    [Fact]
    public void Multi_wave_ability_leaves_a_single_slowing_area_from_its_first_wave()
    {
        var zone = new AreaDefinition
        {
            Id = "t.slow_zone",
            Radius = 3,
            DurationTicks = 40,
            IntervalTicks = 5,
            Effects = [new ApplyStatusEffect { Status = Slow.Id }],
        };
        var rain = Ab(
            "t.rain",
            new CircleAtPointTargeting { Range = 10, Radius = 3 },
            Dmg(5),
            new SpawnAreaEffect { Area = zone.Id, OnHit = 1 }) with
        {
            Hits = new AbilityHitsDefinition { Count = 3, IntervalTicks = 10 },
        };
        var w = NewWorld([rain], areas: [zone]);
        var archer = Hero(w, Vector2.Zero, rain.Id);
        var mob = Mob(w, new Vector2(6, 0));

        var ev = Use(w, archer, rain.Id, aim: new Vector2(6, 0), ticks: 30);

        Assert.Equal(15, DamageTo(ev, mob)); // 3 ondas
        var spawned = Assert.Single(ev.OfType<AreaSpawned>());
        Assert.Equal(new Vector2(6, 0), spawned.Position);
        Assert.True(StatusManager.Has(w, mob, Slow.Id));
    }

    [Fact]
    public void Telegraphed_blast_leaves_a_burning_floor_at_the_impact_point()
    {
        var fire = new AreaDefinition
        {
            Id = "t.fire",
            Radius = 2,
            DurationTicks = 80,
            IntervalTicks = 20,
            Effects = [new DamageEffect { Base = 7, School = DamageSchool.Fire }],
        };
        var cataclysm = Ab(
            "t.cataclysm",
            new CircleAtPointTargeting { Range = 10, Radius = 2 },
            new DamageEffect { Base = 70, School = DamageSchool.Fire },
            new SpawnAreaEffect { Area = fire.Id }) with
        {
            Telegraph = new TelegraphDefinition { Shape = TelegraphShape.Circle, DelayTicks = 10, Radius = 2, Anchor = TelegraphAnchor.Aim },
        };
        var w = NewWorld([cataclysm], areas: [fire]);
        var mage = Hero(w, Vector2.Zero, cataclysm.Id);
        var mob = Mob(w, new Vector2(7, 0));

        var ev = Use(w, mage, cataclysm.Id, aim: new Vector2(7, 0), ticks: 100);

        var hits = ev.OfType<DamageDealt>().Where(d => d.Target == mob.Id).ToList();
        Assert.Equal(70, hits[0].Amount);
        Assert.Equal(4, hits.Count(h => h.SourceId == fire.Id && h.Amount == 7)); // 80 ticks / 20
        Assert.Equal(new Vector2(7, 0), Assert.Single(ev.OfType<AreaSpawned>()).Position);
    }
}
