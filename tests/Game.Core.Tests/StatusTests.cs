using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

public class StatusTests
{
    private static readonly ControlRules Rules = new()
    {
        DurationByRank = new ControlDurationByRank { Normal = 1.0, Elite = 0.5, Boss = 0.25 },
        Diminishing = new ControlDiminishing { WindowTicks = 300, Steps = [1.0, 0.5, 0.25, 0.0] },
    };

    private static StatusDefinition St(
        string id,
        int duration = 40,
        StackingMode stacking = StackingMode.Refresh,
        int maxStacks = 1,
        string[]? tags = null,
        StatModifiers? mods = null,
        StatusFlags? flags = null,
        FrontalModifier? frontal = null,
        PeriodicEffectDefinition[]? periodic = null) =>
        new()
        {
            Id = id,
            Name = id,
            DefaultDurationTicks = duration,
            Stacking = stacking,
            MaxStacks = maxStacks,
            Tags = tags ?? [],
            Modifiers = mods ?? new StatModifiers(),
            Flags = flags ?? new StatusFlags(),
            Frontal = frontal,
            Periodic = periodic ?? [],
        };

    private static AbilityDefinition Ab(string id, TargetingDefinition t, EffectDefinition[] effects, int cast = 0) =>
        new() { Id = id, Name = id, Targeting = t, Effects = effects, CastTimeTicks = cast };

    private static World NewWorld(StatusDefinition[] statuses, AbilityDefinition[]? abilities = null) =>
        WorldBuilder.CreateDefault(
            1, null, new ContentCatalog(abilities: abilities ?? [], statuses: statuses) { Rules = new RulesDefinition { Control = Rules } });

    private static Entity Unit(
        World w, Vector2 pos, Faction f = Faction.Monster, MonsterRank rank = MonsterRank.Normal, string basic = "", params string[] abilities)
    {
        var st = new AbilityState { BasicAttackId = basic == "" ? null : basic };
        st.Known.AddRange(abilities);
        if (basic != "")
        {
            st.Known.Add(basic);
        }

        return w.Spawn(
            new Transform { Position = pos },
            new Health(1000),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Rank = rank },
            st);
    }

    private static void StepN(World w, int n)
    {
        for (var i = 0; i < n; i++)
        {
            w.Step();
        }
    }

    private static List<T> Ev<T>(World w)
        where T : GameEvent => w.LastTickEvents.OfType<T>().ToList();

    // ---------- renovar vs empilhar ----------

    [Fact]
    public void Refresh_mode_renews_duration_and_does_not_stack()
    {
        var w = NewWorld([St("status.a", 40)]);
        var src = Unit(w, Vector2.Zero);
        var t = Unit(w, new Vector2(3, 0));
        StatusManager.Apply(w, src, t, "status.a");
        StepN(w, 10);
        StatusManager.Apply(w, src, t, "status.a");
        w.Step();

        var inst = Assert.Single(StatusManager.Active(w, t));
        Assert.Equal(1, inst.Stacks);
        Assert.Equal(w.Tick - 1 + 40, inst.ExpiresAtTick);
        Assert.Single(Ev<StatusRefreshed>(w));
        Assert.Empty(Ev<StatusStacked>(w));
    }

    [Fact]
    public void Stack_mode_stacks_up_to_max_then_refreshes()
    {
        var w = NewWorld([St("status.a", 40, StackingMode.Stack, 3)]);
        var src = Unit(w, Vector2.Zero);
        var t = Unit(w, new Vector2(3, 0));
        var kinds = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            StatusManager.Apply(w, src, t, "status.a");
            w.Step();
            kinds.AddRange(w.LastTickEvents.Select(e => e.GetType().Name).Where(n => n.StartsWith("Status", StringComparison.Ordinal)));
        }

        Assert.Equal(["StatusApplied", "StatusStacked", "StatusStacked", "StatusRefreshed"], kinds);
        Assert.Equal(3, StatusManager.Stacks(w, t, "status.a"));
    }

    // ---------- expiração exata ----------

    [Fact]
    public void Expires_exactly_after_duration_ticks()
    {
        var w = NewWorld([St("status.a", 40)]);
        var t = Unit(w, Vector2.Zero);
        var applied = w.Tick;
        var inst = StatusManager.Apply(w, null, t, "status.a")!;
        Assert.Equal(applied + 40, inst.ExpiresAtTick);

        StepN(w, 39);
        Assert.Equal(applied + 39, w.Tick);
        Assert.True(StatusManager.Has(w, t, "status.a"));

        w.Step(); // tick applied + 40
        Assert.False(StatusManager.Has(w, t, "status.a"));
        var expired = Assert.Single(Ev<StatusExpired>(w));
        Assert.Equal("status.a", expired.StatusId);
        Assert.Equal(applied + 40, expired.Tick);
        Assert.Empty(t.Get<StatusEffects>().Active);
    }

    [Fact]
    public void Apply_status_override_duration_is_used()
    {
        var w = NewWorld([St("status.a", 40)]);
        var t = Unit(w, Vector2.Zero);
        var inst = StatusManager.Apply(w, null, t, "status.a", 7)!;
        Assert.Equal(w.Tick + 7, inst.ExpiresAtTick);
    }

    // ---------- DoT ----------

    [Fact]
    public void Dot_ticks_with_original_source_in_events()
    {
        var dot = St("status.poison", 60, periodic: [new PeriodicEffectDefinition { IntervalTicks = 20, Effects = [new DamageEffect { Base = 5, School = DamageSchool.Nature }] }]);
        var w = NewWorld([dot]);
        var src = Unit(w, Vector2.Zero, Faction.Player);
        var t = Unit(w, new Vector2(3, 0));
        StatusManager.Apply(w, src, t, "status.poison");

        var hits = new List<DamageDealt>();
        for (var i = 0; i < 70; i++)
        {
            w.Step();
            hits.AddRange(Ev<DamageDealt>(w));
        }

        Assert.Equal(3, hits.Count);
        Assert.All(hits, h =>
        {
            Assert.Equal(src.Id, h.Source);
            Assert.Equal(t.Id, h.Target);
            Assert.Equal("status.poison", h.SourceId);
            Assert.Equal(5, h.Amount);
        });
        Assert.Equal(985, t.Get<Health>().Current);
    }

    [Fact]
    public void Dot_scales_with_stacks_and_damage_multipliers_of_source()
    {
        var dot = St("status.bleed", 60, StackingMode.Stack, 3, periodic: [new PeriodicEffectDefinition { IntervalTicks = 20, Effects = [new DamageEffect { Base = 4 }] }]);
        var buff = St("status.rage", 200, mods: new StatModifiers { DamageDealtMultiplier = 1.5 });
        var w = NewWorld([dot, buff]);
        var src = Unit(w, Vector2.Zero, Faction.Player);
        var t = Unit(w, new Vector2(3, 0));
        StatusManager.Apply(w, src, t, "status.bleed");
        StatusManager.Apply(w, src, t, "status.bleed");
        StatusManager.Apply(w, src, src, "status.rage");
        StepN(w, 19);
        w.Step();
        var hits = Ev<DamageDealt>(w);
        Assert.Equal(2, hits.Count); // um por acúmulo
        Assert.All(hits, h => Assert.Equal(6, h.Amount)); // 4 x 1.5
    }

    // ---------- flags ----------

    private static AbilityDefinition Strike => Ab("strike", new UnitTargeting { Range = 5 }, [new DamageEffect { Base = 1 }]);
    private static AbilityDefinition Basic => Ab("basic", new UnitTargeting { Range = 5 }, [new DamageEffect { Base = 1 }]);

    [Fact]
    public void Stun_blocks_abilities_basic_attack_and_movement_and_cancels_cast()
    {
        var w = NewWorld(
            [St("status.stun", 40, tags: ["control"], flags: new StatusFlags { Stunned = true })],
            [Strike, Basic, Ab("slow", new SelfTargeting(), [], cast: 30)]);
        var a = Unit(w, Vector2.Zero, Faction.Player, basic: "basic", abilities: ["strike", "slow"]);
        var b = Unit(w, new Vector2(2, 0));

        w.Enqueue(new UseAbilityIntent(a.Id, "slow"));
        w.Step();
        Assert.True(a.Get<AbilityState>().IsCasting);

        StatusManager.Apply(w, b, a, "status.stun");
        w.Step(); // eventos publicados entre ticks aparecem no próximo Step
        var interrupted = Assert.Single(Ev<CastInterrupted>(w));
        Assert.Equal(CastInterruptReason.Disabled, interrupted.Reason);
        Assert.False(a.Get<AbilityState>().IsCasting);

        w.Enqueue(new UseAbilityIntent(a.Id, "strike", b.Id));
        w.Step();
        Assert.Equal(AbilityRejectReason.Stunned, Assert.Single(Ev<AbilityRejected>(w)).Reason);
        w.Enqueue(new UseAbilityIntent(a.Id, "basic", b.Id));
        w.Step();
        Assert.Equal(AbilityRejectReason.Stunned, Assert.Single(Ev<AbilityRejected>(w)).Reason);

        var before = a.Get<Transform>().Position;
        w.Enqueue(new MoveIntent(a.Id, new Vector2(0, 1)));
        StepN(w, 5);
        Assert.Equal(before, a.Get<Transform>().Position);

        // depois de expirar, volta ao normal
        StepN(w, 40);
        w.Enqueue(new UseAbilityIntent(a.Id, "strike", b.Id));
        w.Step();
        Assert.Empty(Ev<AbilityRejected>(w));
        Assert.Single(Ev<AbilityExecuted>(w));
        Assert.NotEqual(before, a.Get<Transform>().Position);
    }

    [Fact]
    public void Root_blocks_only_movement()
    {
        var w = NewWorld([St("status.root", 100, flags: new StatusFlags { Rooted = true })], [Strike]);
        var a = Unit(w, Vector2.Zero, Faction.Player, abilities: "strike");
        var b = Unit(w, new Vector2(2, 0));
        StatusManager.Apply(w, b, a, "status.root");

        w.Enqueue(new MoveIntent(a.Id, new Vector2(0, 1)));
        StepN(w, 5);
        Assert.Equal(Vector2.Zero, a.Get<Transform>().Position);

        w.Enqueue(new UseAbilityIntent(a.Id, "strike", b.Id));
        w.Step();
        Assert.Empty(Ev<AbilityRejected>(w));
        Assert.Single(Ev<AbilityExecuted>(w));
    }

    [Fact]
    public void Silence_blocks_abilities_but_allows_basic_attack()
    {
        var w = NewWorld([St("status.silence", 100, flags: new StatusFlags { Silenced = true })], [Strike, Basic]);
        var a = Unit(w, Vector2.Zero, Faction.Player, basic: "basic", abilities: "strike");
        var b = Unit(w, new Vector2(2, 0));
        StatusManager.Apply(w, b, a, "status.silence");

        w.Enqueue(new UseAbilityIntent(a.Id, "strike", b.Id));
        w.Step();
        Assert.Equal(AbilityRejectReason.Silenced, Assert.Single(Ev<AbilityRejected>(w)).Reason);

        w.Enqueue(new UseAbilityIntent(a.Id, "basic", b.Id));
        w.Step();
        Assert.Empty(Ev<AbilityRejected>(w));
        Assert.Single(Ev<AbilityExecuted>(w));

        // movimento livre
        w.Enqueue(new MoveIntent(a.Id, new Vector2(0, 1)));
        StepN(w, 5);
        Assert.True(a.Get<Transform>().Position.Y > 0.5f);
    }

    // ---------- modificadores ----------

    [Fact]
    public void Modifiers_aggregate_multiplicatively_and_per_stack()
    {
        var w = NewWorld(
        [
            St("status.slow", 100, mods: new StatModifiers { SpeedMultiplier = 0.5 }),
            St("status.haste", 100, mods: new StatModifiers { SpeedMultiplier = 1.2 }),
            St("status.curse", 100, StackingMode.Stack, 3, mods: new StatModifiers { DamageTakenMultiplier = 1.1 }),
            St("status.shield", 100, mods: new StatModifiers { DamageTakenMultiplier = 0.5 }),
        ]);
        var t = Unit(w, Vector2.Zero);
        var src = Unit(w, new Vector2(3, 0));
        Assert.Equal(1.0, w.Combat.SpeedMultiplier(w, t));
        StatusManager.Apply(w, src, t, "status.slow");
        StatusManager.Apply(w, src, t, "status.haste");
        Assert.Equal(0.6, w.Combat.SpeedMultiplier(w, t), 9);

        StatusManager.Apply(w, src, t, "status.curse");
        StatusManager.Apply(w, src, t, "status.curse");
        StatusManager.Apply(w, src, t, "status.shield");
        Assert.Equal(1.1 * 1.1 * 0.5, w.Combat.DamageTakenMultiplier(w, t, src, DamageSchool.Physical), 9);

        // dano real: 100 x 0.605 = 60.5 -> 61 (metade para longe de zero)
        Assert.Equal(61, DamageResolver.Deal(w, src, t, "x", 100, DamageSchool.Physical));
    }

    [Fact]
    public void Slow_reduces_actual_movement_speed()
    {
        var w = NewWorld([St("status.slow", 200, mods: new StatModifiers { SpeedMultiplier = 0.5 })]);
        var fast = Unit(w, new Vector2(0, 0));
        var slow = Unit(w, new Vector2(10, 0));
        StatusManager.Apply(w, null, slow, "status.slow");
        w.Enqueue(new MoveIntent(fast.Id, new Vector2(0, 1)));
        w.Enqueue(new MoveIntent(slow.Id, new Vector2(0, 1)));
        StepN(w, 20);
        Assert.Equal(fast.Get<Transform>().Position.Y * 0.5f, slow.Get<Transform>().Position.Y, 2);
    }

    // ---------- frontal ----------

    [Fact]
    public void Frontal_modifier_reduces_damage_only_from_the_front()
    {
        var w = NewWorld([St("status.guard", 200, frontal: new FrontalModifier { ArcDegrees = 120, DamageTakenMultiplier = 0.5 })]);
        var target = Unit(w, Vector2.Zero);
        target.Get<Transform>().Facing = new Vector2(1, 0);
        var front = Unit(w, new Vector2(3, 0), Faction.Player);
        var side = Unit(w, new Vector2(0, 3), Faction.Player);
        var back = Unit(w, new Vector2(-3, 0), Faction.Player);
        var diag = Unit(w, new Vector2(2, 2.5f), Faction.Player); // ~51 graus: dentro de 60
        StatusManager.Apply(w, null, target, "status.guard");

        Assert.Equal(50, DamageResolver.Deal(w, front, target, "x", 100, DamageSchool.Physical));
        Assert.Equal(50, DamageResolver.Deal(w, diag, target, "x", 100, DamageSchool.Physical));
        Assert.Equal(100, DamageResolver.Deal(w, side, target, "x", 100, DamageSchool.Physical));
        Assert.Equal(100, DamageResolver.Deal(w, back, target, "x", 100, DamageSchool.Physical));
        Assert.Equal(100, DamageResolver.Deal(w, null, target, "x", 100, DamageSchool.Physical)); // sem fonte: sem arco
    }

    // ---------- controle: rank e retorno decrescente ----------

    private static StatusDefinition Stun(int ticks = 80) =>
        St("status.stun", ticks, tags: ["control"], flags: new StatusFlags { Stunned = true });

    [Theory]
    [InlineData(MonsterRank.Normal, 80)]
    [InlineData(MonsterRank.Elite, 40)]
    [InlineData(MonsterRank.Boss, 20)]
    public void Control_duration_scales_by_target_rank(MonsterRank rank, int expected)
    {
        var w = NewWorld([Stun()]);
        var t = Unit(w, Vector2.Zero, rank: rank);
        var inst = StatusManager.Apply(w, null, t, "status.stun")!;
        Assert.Equal(w.Tick + expected, inst.ExpiresAtTick);
    }

    [Fact]
    public void Non_control_status_ignores_rank_and_diminishing()
    {
        var w = NewWorld([St("status.poison", 80)]);
        var boss = Unit(w, Vector2.Zero, rank: MonsterRank.Boss);
        for (var i = 0; i < 6; i++)
        {
            var inst = StatusManager.Apply(w, null, boss, "status.poison")!;
            Assert.Equal(w.Tick + 80, inst.ExpiresAtTick);
        }
    }

    [Fact]
    public void Diminishing_returns_reach_immunity_then_reset_after_window()
    {
        var w = NewWorld([Stun(80)]);
        var t = Unit(w, Vector2.Zero);
        var durations = new List<long>();
        for (var i = 0; i < 3; i++)
        {
            durations.Add(StatusManager.Apply(w, null, t, "status.stun")!.ExpiresAtTick - w.Tick);
            StatusManager.Remove(w, t, "status.stun", EntityId.None);
        }

        Assert.Equal([80L, 40L, 20L], durations);

        Assert.Null(StatusManager.Apply(w, null, t, "status.stun"));
        w.Step();
        Assert.Single(Ev<StatusResisted>(w));
        Assert.False(StatusManager.Has(w, t, "status.stun"));

        StepN(w, 300); // fim da janela contada da primeira aplicação
        var again = StatusManager.Apply(w, null, t, "status.stun")!;
        Assert.Equal(w.Tick + 80, again.ExpiresAtTick);
    }

    [Fact]
    public void Boss_gets_quarter_duration_and_then_diminishing_on_top()
    {
        var w = NewWorld([Stun(80)]);
        var boss = Unit(w, Vector2.Zero, rank: MonsterRank.Boss);
        var first = StatusManager.Apply(w, null, boss, "status.stun")!;
        Assert.Equal(w.Tick + 20, first.ExpiresAtTick);
        var second = StatusManager.Apply(w, null, boss, "status.stun")!; // refresh: 80 x 0.25 x 0.5 = 10
        Assert.Equal(w.Tick + 10, second.ExpiresAtTick);
    }

    // ---------- efeitos apply_status / remove_status em habilidades ----------

    [Fact]
    public void Ability_effects_apply_and_remove_by_tag_with_remover_in_event()
    {
        var w = NewWorld(
            [St("status.poison", 200, tags: ["debuff", "poison"]), St("status.curse", 200, tags: ["debuff", "curse"]), St("status.hex", 200, tags: ["debuff"])],
            [
                Ab("cast_poison", new UnitTargeting { Range = 10 }, [new ApplyStatusEffect { Status = "status.poison", DurationTicks = 100 }]),
                Ab("cleanse", new UnitTargeting { Range = 10, Faction = FactionFilter.Ally }, [new RemoveStatusEffect { Tag = "debuff", Count = 2 }]),
            ]);
        var enemy = Unit(w, Vector2.Zero, abilities: "cast_poison");
        var healer = Unit(w, new Vector2(2, 0), Faction.Player, abilities: "cleanse");
        var ally = Unit(w, new Vector2(3, 0), Faction.Player);

        w.Enqueue(new UseAbilityIntent(enemy.Id, "cast_poison", ally.Id));
        w.Step();
        var applied = Assert.Single(Ev<StatusApplied>(w));
        Assert.Equal(enemy.Id, applied.Source);
        Assert.Equal(100, applied.DurationTicks);
        StatusManager.Apply(w, enemy, ally, "status.curse");
        StatusManager.Apply(w, enemy, ally, "status.hex");

        w.Enqueue(new UseAbilityIntent(healer.Id, "cleanse", ally.Id));
        w.Step();
        var removed = Ev<StatusRemoved>(w);
        Assert.Equal(2, removed.Count);
        Assert.All(removed, r => Assert.Equal(healer.Id, r.By));
        Assert.Equal(["status.poison", "status.curse"], removed.Select(r => r.StatusId)); // mais antigos primeiro
        Assert.True(StatusManager.Has(w, ally, "status.hex"));
    }

    [Fact]
    public void Statuses_are_part_of_state_hash_and_snapshots()
    {
        var w1 = NewWorld([St("status.a", 40)]);
        var w2 = NewWorld([St("status.a", 40)]);
        var t1 = Unit(w1, Vector2.Zero);
        var t2 = Unit(w2, Vector2.Zero);
        Assert.Equal(w1.ComputeStateHash(), w2.ComputeStateHash());
        StatusManager.Apply(w1, null, t1, "status.a");
        Assert.NotEqual(w1.ComputeStateHash(), w2.ComputeStateHash());
        StatusManager.Apply(w2, null, t2, "status.a");
        Assert.Equal(w1.ComputeStateHash(), w2.ComputeStateHash());
        Assert.Single(w1.Snapshot().Entities[0].Get<StatusEffects>().Active);
    }
}
