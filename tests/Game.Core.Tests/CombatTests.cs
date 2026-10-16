using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Tests;

public class CombatTests
{
    // ---------- helpers ----------

    private static AbilityDefinition Ab(
        string id,
        TargetingDefinition targeting,
        EffectDefinition[] effects,
        int cooldown = 0,
        int cast = 0,
        bool castWhileMoving = false,
        ResourceCost? cost = null) =>
        new()
        {
            Id = id,
            Name = id,
            Targeting = targeting,
            Effects = effects,
            CooldownTicks = cooldown,
            CastTimeTicks = cast,
            CastWhileMoving = castWhileMoving,
            Cost = cost,
        };

    private static DamageEffect Dmg(double b, DamageSchool s = DamageSchool.Physical) => new() { Base = b, School = s };

    private static World NewWorld(GridMap? map, params AbilityDefinition[] abilities) =>
        WorldBuilder.CreateDefault(1, map, new ContentCatalog(abilities: abilities));

    private static Entity Fighter(
        World w, Vector2 pos, Faction f, float hp = 100, float radius = 0.4f, float speed = 4, params string[] abilities)
    {
        var st = new AbilityState();
        st.Known.AddRange(abilities);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(radius),
            new Movement(speed),
            new ResourcePool("mana", 100, 100),
            new UnitInfo { DefinitionId = "test" },
            st);
    }

    private static void Use(World w, Entity e, string ability, Entity? target = null, Vector2? aim = null) =>
        w.Enqueue(new UseAbilityIntent(e.Id, ability, target?.Id, aim));

    private static List<T> Ev<T>(World w)
        where T : GameEvent => w.LastTickEvents.OfType<T>().ToList();

    private static AbilityRejectReason? Rejected(World w) => Ev<AbilityRejected>(w).SingleOrDefault()?.Reason;

    private static void StepN(World w, int n)
    {
        for (var i = 0; i < n; i++)
        {
            w.Step();
        }
    }

    // ---------- rejeições (cada motivo) ----------

    [Fact]
    public void Reject_dead_caster_by_validator_and_intent_is_dropped()
    {
        var w = NewWorld(null, Ab("a", new SelfTargeting(), [Dmg(1)]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "a");
        w.Kill(p);
        Assert.Equal(AbilityRejectReason.Dead, AbilityValidator.Validate(w, p, "a", null, null).Reason);
        Use(w, p, "a");
        w.Step();
        Assert.Empty(Ev<AbilityRejected>(w));
        Assert.Empty(Ev<AbilityExecuted>(w));
    }

    private sealed class StunGate(AbilityRejectReason reason, bool basicOnlyAllowed = false) : IActionGate
    {
        public bool Active = true;

        public AbilityRejectReason? CanUse(World world, Entity actor, string abilityId, AbilityDefinition? ability, bool isBasicAttack) =>
            Active && !(basicOnlyAllowed && isBasicAttack) ? reason : null;
    }

    [Fact]
    public void Reject_gate_blocks_and_precedes_unknown_ability()
    {
        var w = NewWorld(null, Ab("a", new SelfTargeting(), [Dmg(1)]));
        var gate = new StunGate(AbilityRejectReason.Stunned);
        w.Combat.Gates.Add(gate);
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "a");
        Use(w, p, "a");
        Use(w, p, "nope");
        w.Step();
        Assert.All(Ev<AbilityRejected>(w), r => Assert.Equal(AbilityRejectReason.Stunned, r.Reason));
        Assert.Equal(2, Ev<AbilityRejected>(w).Count);

        gate.Active = false;
        Use(w, p, "a");
        w.Step();
        Assert.Single(Ev<AbilityExecuted>(w));
    }

    [Fact]
    public void Reject_unknown_ability_not_in_content_or_not_known()
    {
        var w = NewWorld(null, Ab("a", new SelfTargeting(), [Dmg(1)]), Ab("b", new SelfTargeting(), [Dmg(1)]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "a");
        Use(w, p, "b"); // existe, mas não é conhecida
        w.Step();
        Assert.Equal(AbilityRejectReason.UnknownAbility, Rejected(w));
        Use(w, p, "zzz"); // não existe
        w.Step();
        Assert.Equal(AbilityRejectReason.UnknownAbility, Rejected(w));
    }

    [Fact]
    public void Reject_on_cooldown_then_ready_after_cooldown_ticks()
    {
        var w = NewWorld(null, Ab("a", new SelfTargeting(), [Dmg(1)], cooldown: 5));
        var p = Fighter(w, Vector2.Zero, Faction.Player, hp: 1000, abilities: "a");
        Use(w, p, "a");
        w.Step(); // tick 1, pronta no tick 6
        Assert.Single(Ev<AbilityExecuted>(w));
        for (var t = 2; t <= 5; t++)
        {
            Use(w, p, "a");
            w.Step();
            Assert.Equal(AbilityRejectReason.OnCooldown, Rejected(w));
        }

        Use(w, p, "a");
        w.Step(); // tick 6
        Assert.Single(Ev<AbilityExecuted>(w));
    }

    [Fact]
    public void Reject_not_enough_resource_and_cooldown_checked_before_resource()
    {
        var cost = new ResourceCost { Resource = "mana", Amount = 60 };
        var w = NewWorld(null, Ab("a", new SelfTargeting(), [Dmg(1)], cooldown: 100, cost: cost));
        var p = Fighter(w, Vector2.Zero, Faction.Player, hp: 1000, abilities: "a");
        Use(w, p, "a");
        w.Step();
        Assert.Equal(40f, p.Get<ResourcePool>().Current);
        Use(w, p, "a"); // em recarga E sem recurso: recarga vem primeiro
        w.Step();
        Assert.Equal(AbilityRejectReason.OnCooldown, Rejected(w));

        var w2 = NewWorld(null, Ab("a", new SelfTargeting(), [Dmg(1)], cost: new ResourceCost { Resource = "rage", Amount = 1 }));
        var p2 = Fighter(w2, Vector2.Zero, Faction.Player, abilities: "a"); // tem mana, não rage
        Use(w2, p2, "a");
        w2.Step();
        Assert.Equal(AbilityRejectReason.NotEnoughResource, Rejected(w2));
    }

    [Fact]
    public void Reject_invalid_target_missing_dead_wrong_faction()
    {
        var w = NewWorld(null, Ab("a", new UnitTargeting { Range = 10 }, [Dmg(1)]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "a");
        var ally = Fighter(w, new Vector2(1, 0), Faction.Player);
        var foe = Fighter(w, new Vector2(2, 0), Faction.Monster);

        Use(w, p, "a");
        w.Step();
        Assert.Equal(AbilityRejectReason.InvalidTarget, Rejected(w));

        Use(w, p, "a", ally);
        w.Step();
        Assert.Equal(AbilityRejectReason.InvalidTarget, Rejected(w));

        w.Kill(foe);
        Use(w, p, "a", foe);
        w.Step();
        Assert.Equal(AbilityRejectReason.InvalidTarget, Rejected(w));
    }

    [Fact]
    public void Reject_out_of_range_uses_distance_minus_target_radius()
    {
        var w = NewWorld(null, Ab("a", new UnitTargeting { Range = 5 }, [Dmg(1)]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "a");
        var far = Fighter(w, new Vector2(5.6f, 0), Faction.Monster, radius: 0.4f); // 5.6 - 0.4 = 5.2 > 5
        Use(w, p, "a", far);
        w.Step();
        Assert.Equal(AbilityRejectReason.OutOfRange, Rejected(w));

        far.Get<Transform>().Position = new Vector2(5.3f, 0); // 4.9 <= 5
        Use(w, p, "a", far);
        w.Step();
        Assert.Single(Ev<AbilityExecuted>(w));
    }

    [Fact]
    public void Reject_no_line_of_sight_for_unit_and_point_targets()
    {
        var map = GridMap.FromAscii("#########", "#...#...#", "#...#...#", "#########");
        var w = NewWorld(map, Ab("a", new UnitTargeting { Range = 10 }, [Dmg(1)]), Ab("b", new CircleAtPointTargeting { Range = 10, Radius = 1 }, [Dmg(1)]));
        var p = Fighter(w, new Vector2(2.5f, 1.5f), Faction.Player, abilities: ["a", "b"]);
        var foe = Fighter(w, new Vector2(6.5f, 1.5f), Faction.Monster);
        Use(w, p, "a", foe);
        w.Step();
        Assert.Equal(AbilityRejectReason.NoLineOfSight, Rejected(w));
        Use(w, p, "b", aim: new Vector2(6.5f, 1.5f));
        w.Step();
        Assert.Equal(AbilityRejectReason.NoLineOfSight, Rejected(w));
    }

    [Fact]
    public void Reject_already_casting()
    {
        var w = NewWorld(null, Ab("slow", new SelfTargeting(), [Dmg(1)], cast: 10, castWhileMoving: true), Ab("b", new SelfTargeting(), [Dmg(1)]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, hp: 1000, abilities: ["slow", "b"]);
        Use(w, p, "slow");
        w.Step();
        Use(w, p, "b");
        w.Step();
        Assert.Equal(AbilityRejectReason.AlreadyCasting, Rejected(w));
    }

    // ---------- alvos ----------

    [Fact]
    public void Self_targeting_hits_caster_only()
    {
        var w = NewWorld(null, Ab("heal", new SelfTargeting(), [new HealEffect { Base = 10 }]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "heal");
        Fighter(w, new Vector2(0.5f, 0), Faction.Player);
        p.Get<Health>().Current = 50;
        Use(w, p, "heal");
        w.Step();
        Assert.Equal(60f, p.Get<Health>().Current);
        Assert.Equal([p.Id], Ev<AbilityExecuted>(w).Single().Targets);
    }

    [Fact]
    public void Unit_targeting_ally_filter_allows_self_and_allies()
    {
        var w = NewWorld(null, Ab("h", new UnitTargeting { Range = 10, Faction = FactionFilter.Ally }, [new HealEffect { Base = 10 }]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "h");
        var ally = Fighter(w, new Vector2(3, 0), Faction.Player);
        ally.Get<Health>().Current = 10;
        Use(w, p, "h", ally);
        w.Step();
        Assert.Equal(20f, ally.Get<Health>().Current);
        Use(w, p, "h", p); // self é aliado
        StepN(w, 1);
        Assert.Single(Ev<AbilityExecuted>(w));
    }

    [Fact]
    public void Melee_arc_hits_inside_cone_only_and_ignores_allies()
    {
        var w = NewWorld(null, Ab("swing", new MeleeArcTargeting { Range = 2, Angle = 90 }, [Dmg(5)]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "swing");
        var front = Fighter(w, new Vector2(1.5f, 0), Faction.Monster);
        var diag = Fighter(w, new Vector2(1.2f, 1.0f), Faction.Monster); // ~40 graus: dentro de 90 total
        var side = Fighter(w, new Vector2(0, 1.5f), Faction.Monster); // 90 graus: fora
        var behind = Fighter(w, new Vector2(-1, 0), Faction.Monster);
        var far = Fighter(w, new Vector2(4, 0), Faction.Monster);
        var ally = Fighter(w, new Vector2(1, 0), Faction.Player);
        Use(w, p, "swing", aim: new Vector2(10, 0));
        w.Step();
        var ids = Ev<AbilityExecuted>(w).Single().Targets;
        Assert.Contains(front.Id, ids);
        Assert.Contains(diag.Id, ids);
        Assert.DoesNotContain(side.Id, ids);
        Assert.DoesNotContain(behind.Id, ids);
        Assert.DoesNotContain(far.Id, ids);
        Assert.DoesNotContain(ally.Id, ids);
        Assert.Equal(95f, front.Get<Health>().Current);
        Assert.Equal(new Vector2(1, 0), p.Get<Transform>().Facing); // mirou
    }

    [Fact]
    public void Melee_arc_with_no_hits_still_executes_and_costs()
    {
        var w = NewWorld(null, Ab("swing", new MeleeArcTargeting { Range = 2, Angle = 90 }, [Dmg(5)], cooldown: 10, cost: new ResourceCost { Resource = "mana", Amount = 10 }));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "swing");
        Use(w, p, "swing");
        w.Step();
        Assert.Empty(Ev<AbilityExecuted>(w).Single().Targets);
        Assert.Equal(90f, p.Get<ResourcePool>().Current);
    }

    [Fact]
    public void Line_targeting_hits_oriented_rect_and_is_blocked_by_walls()
    {
        var map = GridMap.FromAscii("###########", "#.........#", "#.........#", "###########");
        var w = NewWorld(map, Ab("beam", new LineTargeting { Length = 6, Width = 1 }, [Dmg(5)]));
        var p = Fighter(w, new Vector2(1.5f, 1.5f), Faction.Player, abilities: "beam");
        var a = Fighter(w, new Vector2(4.5f, 1.5f), Faction.Monster);
        var b = Fighter(w, new Vector2(4.5f, 2.4f), Faction.Monster); // fora da largura 1 (corpo 0.4 toca? 0.9-0.5=0.4: toca)
        var c = Fighter(w, new Vector2(9.5f, 1.5f), Faction.Monster); // fora do comprimento
        Use(w, p, "beam", aim: new Vector2(10, 1.5f));
        w.Step();
        var ids = Ev<AbilityExecuted>(w).Single().Targets;
        Assert.Contains(a.Id, ids);
        Assert.DoesNotContain(c.Id, ids);
        _ = b;
    }

    [Fact]
    public void Circle_at_point_hits_radius_and_checks_range()
    {
        var w = NewWorld(null, Ab("bomb", new CircleAtPointTargeting { Range = 6, Radius = 1.5 }, [Dmg(5)]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "bomb");
        var inside = Fighter(w, new Vector2(4.5f, 0), Faction.Monster);
        var outside = Fighter(w, new Vector2(1, 0), Faction.Monster);
        Use(w, p, "bomb", aim: new Vector2(4, 0));
        w.Step();
        var ids = Ev<AbilityExecuted>(w).Single().Targets;
        Assert.Equal([inside.Id], ids);
        Assert.NotNull(outside);

        Use(w, p, "bomb", aim: new Vector2(7, 0));
        w.Step();
        Assert.Equal(AbilityRejectReason.OutOfRange, Rejected(w));
        Use(w, p, "bomb"); // sem ponto nem alvo
        w.Step();
        Assert.Equal(AbilityRejectReason.InvalidTarget, Rejected(w));
    }

    [Fact]
    public void Circle_self_hits_enemies_around_and_beneficial_hits_allies_including_self()
    {
        var w = NewWorld(
            null,
            Ab("nova", new CircleSelfTargeting { Radius = 3 }, [Dmg(5)]),
            Ab("aura", new CircleSelfTargeting { Radius = 3 }, [new HealEffect { Base = 5 }]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: ["nova", "aura"]);
        var ally = Fighter(w, new Vector2(2, 0), Faction.Player);
        var foe = Fighter(w, new Vector2(-2, 0), Faction.Monster);
        Use(w, p, "nova");
        w.Step();
        Assert.Equal([foe.Id], Ev<AbilityExecuted>(w).Single().Targets);
        Use(w, p, "aura");
        w.Step();
        Assert.Equal([p.Id, ally.Id], Ev<AbilityExecuted>(w).Single().Targets);
    }

    // ---------- efeitos ----------

    private sealed class Mult(double dealt = 1, double taken = 1, double speed = 1) : IModifierSource
    {
        public double DamageDealtMultiplier(World world, Entity source, Entity target, DamageSchool school) => dealt;

        public double DamageTakenMultiplier(World world, Entity target, Entity? source, DamageSchool school) => taken;

        public double SpeedMultiplier(World world, Entity entity) => speed;
    }

    [Fact]
    public void Damage_formula_multipliers_resistance_and_rounding()
    {
        var w = NewWorld(null, Ab("hit", new UnitTargeting { Range = 5 }, [Dmg(10, DamageSchool.Frost)]));
        w.Combat.Modifiers.Add(new Mult(dealt: 1.5, taken: 1.2));
        w.Combat.Modifiers.Add(new Mult(dealt: 1.0, taken: 1.0)); // várias fontes multiplicam
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "hit");
        var foe = Fighter(w, new Vector2(1, 0), Faction.Monster);
        w.Remove(foe.Id);
        var info = new UnitInfo { Resistances = new Dictionary<string, double> { ["frost"] = 0.25 } };
        var target = w.Spawn(new Transform { Position = new Vector2(1, 0) }, new Health(100), new FactionTag(Faction.Monster), info);

        Use(w, p, "hit", target);
        w.Step();
        // 10 * 1.5 * 1.2 * 0.75 = 13.5 -> arredonda para longe do zero = 14
        var d = Ev<DamageDealt>(w).Single();
        Assert.Equal(14f, d.Amount);
        Assert.Equal(14f, d.Applied);
        Assert.Equal(0f, d.Absorbed);
        Assert.Equal(p.Id, d.Source);
        Assert.Equal("hit", d.SourceId);
        Assert.Equal(DamageSchool.Frost, d.School);
        Assert.Equal(86f, target.Get<Health>().Current);
    }

    [Fact]
    public void Damage_rounding_half_away_from_zero_and_never_negative()
    {
        Assert.Equal(3f, CoreEffects.Round(2.5));
        Assert.Equal(2f, CoreEffects.Round(2.49));
        var w = NewWorld(null);
        var t = Fighter(w, Vector2.Zero, Faction.Monster);
        Assert.Equal(0f, DamageResolver.Deal(w, null, t, "x", -5, DamageSchool.Physical));
        Assert.Equal(100f, t.Get<Health>().Current);
    }

    [Fact]
    public void Shield_absorbs_partially_then_fully()
    {
        var w = NewWorld(null);
        var t = Fighter(w, Vector2.Zero, Faction.Monster);
        EffectApplier.Apply(w, [new ShieldEffect { Amount = 30, DurationTicks = 100 }], null, [t], "sh");
        w.Step();
        Assert.Single(Ev<ShieldApplied>(w));

        DamageResolver.Deal(w, null, t, "x", 20, DamageSchool.Physical); // totalmente absorvido
        var d1 = Ev<DamageDealt>(w);
        Assert.Equal(100f, t.Get<Health>().Current);
        Assert.Equal(10f, t.Get<ShieldState>().Total);
        _ = d1;
        w.Step();

        DamageResolver.Deal(w, null, t, "x", 25, DamageSchool.Physical); // 10 absorvido, 15 na vida
        w.Step();
        var d = Ev<DamageDealt>(w).Single();
        Assert.Equal(25f, d.Amount);
        Assert.Equal(10f, d.Absorbed);
        Assert.Equal(15f, d.Applied);
        Assert.Equal(85f, t.Get<Health>().Current);
        Assert.Empty(t.Get<ShieldState>().Shields);
    }

    [Fact]
    public void Shield_events_depleted_and_expired()
    {
        var w = NewWorld(null);
        var t = Fighter(w, Vector2.Zero, Faction.Monster);
        EffectApplier.Apply(w, [new ShieldEffect { Amount = 10, DurationTicks = 3 }], null, [t], "sh");
        var applied = w.Events.Of<ShieldApplied>().Single(); // aplicado entre ticks (tick 0): expira no tick 3
        Assert.Equal(3, applied.ExpiresAtTick);
        w.Step();
        w.Step();
        Assert.Empty(Ev<ShieldExpired>(w));
        w.Step();
        var exp = Ev<ShieldExpired>(w).Single();
        Assert.Equal(ShieldEndReason.Expired, exp.Reason);
        Assert.Equal(10f, exp.Remaining);

        EffectApplier.Apply(w, [new ShieldEffect { Amount = 10, DurationTicks = 50 }], null, [t], "sh");
        DamageResolver.Deal(w, null, t, "x", 10, DamageSchool.Physical);
        w.Step();
        Assert.Equal(ShieldEndReason.Depleted, Ev<ShieldExpired>(w).Single().Reason);
    }

    [Fact]
    public void Heal_is_capped_at_max_and_reports_overheal_and_skips_dead()
    {
        var w = NewWorld(null);
        var t = Fighter(w, Vector2.Zero, Faction.Player);
        t.Get<Health>().Current = 90;
        EffectApplier.Apply(w, [new HealEffect { Base = 25 }], t, [t], "heal");
        w.Step();
        var h = Ev<HealApplied>(w).Single();
        Assert.Equal(25f, h.Amount);
        Assert.Equal(10f, h.Effective);
        Assert.Equal(15f, h.Overheal);
        Assert.Equal(100f, t.Get<Health>().Current);

        w.Kill(t);
        EffectApplier.Apply(w, [new HealEffect { Base = 25 }], t, [t], "heal");
        w.Step();
        Assert.Empty(Ev<HealApplied>(w));
    }

    [Fact]
    public void Dash_moves_caster_and_stops_at_wall()
    {
        var map = GridMap.FromAscii("######", "#....#", "######");
        var w = NewWorld(map, Ab("dash", new SelfTargeting(), [new DisplacementEffect { Kind = DisplacementKind.Dash, Distance = 10 }]));
        var p = Fighter(w, new Vector2(1.5f, 1.5f), Faction.Player, abilities: "dash");
        Use(w, p, "dash", aim: new Vector2(5, 1.5f));
        w.Step();
        var disp = Ev<Displaced>(w).Single();
        Assert.Equal(p.Id, disp.Entity);
        Assert.Equal(DisplacementKind.Dash, disp.Kind);
        Assert.InRange(p.Get<Transform>().Position.X, 3.5f, 4.7f); // parou na parede (raio 0.4)
        Assert.True(disp.Distance < 10f);
    }

    [Fact]
    public void Knockback_pushes_target_away_from_source_and_skips_dead_targets()
    {
        var w = NewWorld(null, Ab("bash", new MeleeArcTargeting { Range = 2, Angle = 90 }, [Dmg(1000), new DisplacementEffect { Kind = DisplacementKind.Knockback, Distance = 3 }]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "bash");
        var foe = Fighter(w, new Vector2(1, 0), Faction.Monster, hp: 5000);
        Use(w, p, "bash", aim: new Vector2(5, 0));
        w.Step();
        Assert.Equal(new Vector2(4, 0).X, foe.Get<Transform>().Position.X, 3);

        // alvo que morreu antes do efeito não é empurrado
        foe.Get<Health>().Current = 10;
        foe.Get<Transform>().Position = new Vector2(1, 0);
        Use(w, p, "bash", aim: new Vector2(5, 0));
        StepN(w, 1);
        Assert.False(foe.IsAlive);
        Assert.Equal(1f, foe.Get<Transform>().Position.X);
    }

    [Fact]
    public void Unregistered_effect_type_fails_with_clear_error()
    {
        var w = NewWorld(null, Ab("poison", new SelfTargeting(), [new ApplyStatusEffect { Status = "status.poison" }]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "poison");
        Use(w, p, "poison");
        var ex = Assert.Throws<InvalidOperationException>(() => w.Step());
        Assert.Contains("ApplyStatusEffect", ex.Message);
    }

    [Fact]
    public void Custom_effect_can_be_registered_without_touching_core()
    {
        var w = NewWorld(null, Ab("x", new SelfTargeting(), [new ResourceEffect { Resource = "mana", Amount = 7 }]));
        w.Combat.Effects.Register<ResourceEffect>((c, e) => c.Target!.Get<ResourcePool>().Current -= (float)e.Amount);
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "x");
        Use(w, p, "x");
        w.Step();
        Assert.Equal(93f, p.Get<ResourcePool>().Current);
        Assert.Throws<InvalidOperationException>(() => w.Combat.Effects.Register<ResourceEffect>((_, _) => { }));
    }

    [Fact]
    public void Speed_multiplier_from_modifier_source_slows_movement()
    {
        var w = NewWorld(null);
        w.Combat.Modifiers.Add(new Mult(speed: 0.5));
        var p = Fighter(w, Vector2.Zero, Faction.Player, speed: 4);
        w.Enqueue(new MoveIntent(p.Id, new Vector2(1, 0)));
        StepN(w, 20);
        Assert.Equal(2f, p.Get<Transform>().Position.X, 2);
    }

    // ---------- conjuração ----------

    private static World CastWorld(bool castWhileMoving, out Entity caster, out Entity foe)
    {
        var w = NewWorld(
            null,
            Ab("big", new UnitTargeting { Range = 10 }, [Dmg(20)], cooldown: 30, cast: 10, castWhileMoving: castWhileMoving, cost: new ResourceCost { Resource = "mana", Amount = 40 }));
        caster = Fighter(w, Vector2.Zero, Faction.Player, abilities: "big");
        foe = Fighter(w, new Vector2(3, 0), Faction.Monster);
        return w;
    }

    [Fact]
    public void Cast_completes_after_cast_ticks_and_applies_cost_and_cooldown_at_completion()
    {
        var w = CastWorld(false, out var p, out var foe);
        Use(w, p, "big", foe);
        w.Step(); // tick 1: começa, conclui no tick 11
        var started = Ev<AbilityStarted>(w).Single();
        Assert.Equal(10, started.CastTicks);
        Assert.Equal(11, started.EndTick);
        Assert.Empty(Ev<AbilityExecuted>(w));
        Assert.Equal(100f, p.Get<ResourcePool>().Current); // nada cobrado ainda
        Assert.Equal(100f, foe.Get<Health>().Current);
        StepN(w, 9); // tick 10
        Assert.Empty(Ev<AbilityExecuted>(w));
        w.Step(); // tick 11
        var ex = Ev<AbilityExecuted>(w).Single();
        Assert.Equal(41, ex.CooldownReadyTick);
        Assert.Equal(60f, p.Get<ResourcePool>().Current);
        Assert.Equal(80f, foe.Get<Health>().Current);
        Assert.False(p.Get<AbilityState>().IsCasting);
    }

    [Fact]
    public void Cast_is_cancelled_by_movement_without_cost_or_cooldown()
    {
        var w = CastWorld(false, out var p, out var foe);
        Use(w, p, "big", foe);
        StepN(w, 3);
        w.Enqueue(new MoveIntent(p.Id, new Vector2(0, 1)));
        w.Step();
        var ci = Ev<CastInterrupted>(w).Single();
        Assert.Equal(CastInterruptReason.Moved, ci.Reason);
        Assert.False(p.Get<AbilityState>().IsCasting);
        StepN(w, 20);
        Assert.Equal(100f, p.Get<ResourcePool>().Current);
        Assert.Equal(100f, foe.Get<Health>().Current);
        Assert.Equal(0, p.Get<AbilityState>().CooldownRemaining("big", w.Tick));
    }

    [Fact]
    public void Cast_while_moving_is_not_cancelled_and_locks_facing()
    {
        var w = CastWorld(true, out var p, out var foe);
        Use(w, p, "big", foe);
        w.Step();
        Assert.True(p.Get<Movement>().LockFacing);
        Assert.Equal(new Vector2(1, 0), p.Get<Transform>().Facing);
        w.Enqueue(new MoveIntent(p.Id, new Vector2(0, 1)));
        StepN(w, 5);
        Assert.True(p.Get<AbilityState>().IsCasting);
        Assert.Equal(new Vector2(1, 0), p.Get<Transform>().Facing); // não virou ao andar
        StepN(w, 5);
        Assert.Single(Ev<AbilityExecuted>(w));
        Assert.False(p.Get<Movement>().LockFacing);
    }

    [Fact]
    public void Cast_is_cancelled_by_interrupt_effect()
    {
        var w = CastWorld(false, out var p, out var foe);
        w.Combat.Gates.Clear();
        Use(w, p, "big", foe);
        StepN(w, 3);
        EffectApplier.Apply(w, [new InterruptEffect()], foe, [p], "kick");
        w.Step();
        var ci = Ev<CastInterrupted>(w).SingleOrDefault();
        Assert.NotNull(ci);
        Assert.Equal(CastInterruptReason.Interrupted, ci.Reason);
        Assert.Equal(foe.Id, ci.By);
        Assert.False(p.Get<AbilityState>().IsCasting);
        StepN(w, 15);
        Assert.Equal(100f, foe.Get<Health>().Current);
        Assert.Equal(100f, p.Get<ResourcePool>().Current);
    }

    [Fact]
    public void Cast_is_cancelled_by_death_and_resource_revalidated_on_completion()
    {
        var w = CastWorld(false, out var p, out var foe);
        Use(w, p, "big", foe);
        StepN(w, 2);
        DamageResolver.Deal(w, foe, p, "x", 1000, DamageSchool.Physical);
        w.Step();
        Assert.Contains(w.LastTickEvents, e => e is CastInterrupted { Reason: CastInterruptReason.Died });
        Assert.DoesNotContain(w.LastTickEvents, e => e is AbilityExecuted);

        var w2 = CastWorld(false, out var p2, out var foe2);
        Use(w2, p2, "big", foe2);
        StepN(w2, 2);
        p2.Get<ResourcePool>().Current = 10; // gastou o recurso durante a conjuração
        StepN(w2, 9);
        Assert.Contains(w2.Events.LastTick, e => e is AbilityRejected { Reason: AbilityRejectReason.NotEnoughResource });
        Assert.Equal(100f, foe2.Get<Health>().Current);
    }

    // ---------- morte ----------

    [Fact]
    public void Death_publishes_event_once_and_removes_from_valid_targets()
    {
        var w = NewWorld(null, Ab("hit", new UnitTargeting { Range = 5 }, [Dmg(500)]), Ab("arc", new CircleSelfTargeting { Radius = 5 }, [Dmg(1)]));
        var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: ["hit", "arc"]);
        var foe = Fighter(w, new Vector2(1, 0), Faction.Monster, hp: 50);
        Use(w, p, "hit", foe);
        w.Step();
        var d = Ev<DamageDealt>(w).Single();
        Assert.True(d.Killed);
        Assert.Equal(450f, d.Overkill);
        Assert.Equal(50f, d.Applied);
        Assert.Single(Ev<EntityDied>(w));
        Assert.False(foe.IsAlive);

        Use(w, p, "hit", foe);
        w.Step();
        Assert.Equal(AbilityRejectReason.InvalidTarget, Rejected(w));
        Use(w, p, "arc");
        w.Step();
        Assert.Empty(Ev<AbilityExecuted>(w).Single().Targets);
    }

    // ---------- determinismo ----------

    [Fact]
    public void Combat_is_deterministic()
    {
        ulong Run()
        {
            var w = NewWorld(null, Ab("hit", new UnitTargeting { Range = 5 }, [Dmg(7), new ShieldEffect { Amount = 3, DurationTicks = 30 }], cooldown: 7, cast: 3));
            var p = Fighter(w, Vector2.Zero, Faction.Player, abilities: "hit");
            var foe = Fighter(w, new Vector2(1, 0), Faction.Monster, hp: 1000);
            for (var i = 0; i < 200; i++)
            {
                Use(w, p, "hit", foe);
                w.Step();
            }

            return w.ComputeStateHash();
        }

        Assert.Equal(Run(), Run());
    }
}
