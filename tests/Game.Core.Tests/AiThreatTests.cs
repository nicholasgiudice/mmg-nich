using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Ai;
using Game.Core.Combat.Threat;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Tests;

public class AiThreatTests
{
    // ---------- helpers ----------

    private static AbilityDefinition Ab(
        string id, TargetingDefinition targeting, EffectDefinition[] effects, int cooldown = 0, int cast = 0, params string[] tags) =>
        new()
        {
            Id = id,
            Name = id,
            Targeting = targeting,
            Effects = effects,
            CooldownTicks = cooldown,
            CastTimeTicks = cast,
            Tags = tags,
        };

    private static readonly EffectDefinition Frenzy = new ApplyStatusEffect { Status = "status.test" };

    private static MonsterDefinition Mob(
        string id, string ai, double speed, double radius, string[] abilities, params (string Key, double Value)[] parameters) =>
        new()
        {
            Id = id,
            Name = id,
            Health = 100000,
            Speed = speed,
            Radius = radius,
            Abilities = abilities,
            Ai = new AiProfileDefinition { Id = ai, Parameters = parameters.ToDictionary(p => p.Key, p => p.Value) },
        };

    private static readonly AbilityDefinition Hit = Ab("p.hit", new UnitTargeting { Range = 30 }, [new DamageEffect { Base = 50 }]);

    private static readonly AbilityDefinition Provoke = Ab(
        "p.provoke", new UnitTargeting { Range = 30 }, [new ThreatEffect { Amount = 10, TauntDurationTicks = 60 }]);

    private static readonly AbilityDefinition Heal = Ab(
        "p.heal", new UnitTargeting { Range = 30, Faction = FactionFilter.Ally }, [new HealEffect { Base = 20 }]);

    private static readonly AbilityDefinition AddThreat = Ab(
        "p.threat", new UnitTargeting { Range = 30 }, [new ThreatEffect { Amount = 1000 }]);

    private static readonly AbilityDefinition Bite = Ab(
        "m.bite", new MeleeArcTargeting { Range = 1.5, Angle = 180 }, [new DamageEffect { Base = 1 }], cooldown: 20);

    private static readonly AbilityDefinition Howl = Ab(
        "m.howl", new SelfTargeting(), [Frenzy], 0, 0, "ai.rally");

    private static World NewWorld(GridMap? map, MonsterDefinition[] monsters, params AbilityDefinition[] extra)
    {
        AbilityDefinition[] all = [Hit, Provoke, Heal, AddThreat, Bite, Howl, .. extra];
        var content = new ContentCatalog(
            abilities: all,
            statuses: [new StatusDefinition { Id = "status.test", Name = "t", DefaultDurationTicks = 100 }],
            monsters: monsters);
        var w = WorldBuilder.CreateDefault(42, map, content);

        // T-009 registra o apply_status real; aqui só garantimos que exista um handler.
        if (!w.Combat.Effects.IsRegistered(typeof(ApplyStatusEffect)))
        {
            w.Combat.Effects.Register<ApplyStatusEffect>((_, _) => { });
        }

        return w;
    }

    private static Entity Player(World w, Vector2 pos, params string[] abilities)
    {
        var st = new AbilityState();
        st.Known.AddRange(abilities);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(1_000_000),
            new FactionTag(Faction.Player),
            new Collider(0.4f),
            new Movement(4),
            new ResourcePool("mana", 100, 100),
            new UnitInfo { DefinitionId = "test.player" },
            st);
    }

    private static Entity Mon(World w, MonsterDefinition def, Vector2 pos) => EntityFactory.SpawnMonster(w, def, pos);

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

    private static EntityId TargetOf(Entity monster) => monster.Get<AiState>().Target;

    private static void Cast(World w, Entity caster, string ability, Entity? target) =>
        w.Enqueue(new UseAbilityIntent(caster.Id, ability, target?.Id, target?.Get<Transform>().Position));

    private static readonly MonsterDefinition Dummy = Mob("monster.dummy", "ai.melee_chaser", 0, 0.4, []);

    // ---------- ameaça ----------

    [Fact]
    public void Damage_generates_threat_and_aggro_selects_highest()
    {
        var w = NewWorld(null, [Dummy]);
        var a = Player(w, new Vector2(10, 10), "p.hit");
        var b = Player(w, new Vector2(10, 14), "p.hit");
        var m = Mon(w, Dummy, new Vector2(30, 30));

        Cast(w, a, "p.hit", m);
        var ev = Run(w, 1);
        Assert.Contains(ev, e => e is AggroGained { Reason: AggroReason.Damage } g && g.Target == a.Id);
        Assert.Equal(a.Id, TargetOf(m));
        Assert.Equal(50f, m.Get<ThreatTable>().Entries[a.Id.Value]);

        Cast(w, b, "p.hit", m);
        Cast(w, b, "p.hit", m); // instantâneo: os dois usos passam no mesmo tick -> 100 de ameaça
        Run(w, 1);
        Assert.True(m.Get<ThreatTable>().Entries[b.Id.Value] > m.Get<ThreatTable>().Entries[a.Id.Value] * 1.1f);
        Assert.Equal(b.Id, TargetOf(m));
    }

    [Fact]
    public void Target_is_kept_until_overtaken_by_ratio()
    {
        var w = NewWorld(null, [Dummy]);
        var a = Player(w, new Vector2(10, 10), "p.hit");
        var b = Player(w, new Vector2(10, 14), "p.hit");
        var m = Mon(w, Dummy, new Vector2(30, 30));
        Cast(w, a, "p.hit", m);
        Run(w, 1);
        Cast(w, b, "p.hit", m); // 50 vs 50: empate, não passa de 110%
        var ev = Run(w, 1);
        Assert.Equal(a.Id, TargetOf(m));
        Assert.DoesNotContain(ev, e => e is TargetChanged);
    }

    [Fact]
    public void Threat_effect_adds_threat()
    {
        var w = NewWorld(null, [Dummy]);
        var a = Player(w, new Vector2(10, 10), "p.threat");
        var b = Player(w, new Vector2(10, 14), "p.hit");
        var m = Mon(w, Dummy, new Vector2(30, 30));
        Cast(w, b, "p.hit", m);
        Run(w, 1);
        Assert.Equal(b.Id, TargetOf(m));
        Cast(w, a, "p.threat", m);
        Run(w, 1);
        Assert.Equal(a.Id, TargetOf(m));
    }

    [Fact]
    public void Taunt_forces_target_for_duration_then_returns_to_threat_table()
    {
        var w = NewWorld(null, [Dummy]);
        var tank = Player(w, new Vector2(10, 10), "p.provoke");
        var dps = Player(w, new Vector2(10, 14), "p.hit");
        var m = Mon(w, Dummy, new Vector2(30, 30));

        Cast(w, dps, "p.hit", m);
        Run(w, 1);
        Assert.Equal(dps.Id, TargetOf(m));

        Cast(w, tank, "p.provoke", m);
        var ev = Run(w, 1);
        var taunted = Assert.Single(ev.OfType<Taunted>());
        Assert.Equal(tank.Id, taunted.Taunter);
        Assert.True(taunted.UntilTick > w.Tick + 50);
        Assert.Equal(tank.Id, TargetOf(m));
        Assert.Contains(ev, e => e is TargetChanged tc && tc.Current == tank.Id);

        // o dps continua causando muito mais ameaça, mas o alvo fica forçado
        for (var i = 0; i < 50; i++)
        {
            if (i % 5 == 0)
            {
                Cast(w, dps, "p.hit", m);
            }

            w.Step();
            Assert.Equal(tank.Id, TargetOf(m));
        }

        Run(w, 15);
        Assert.Equal(dps.Id, TargetOf(m));
        Assert.True(m.Get<ThreatTable>().TauntTarget.IsNone);
    }

    [Fact]
    public void Heal_generates_reduced_threat_for_engaged_monsters()
    {
        var w = NewWorld(null, [Dummy]);
        var tank = Player(w, new Vector2(10, 10), "p.hit");
        var healer = Player(w, new Vector2(10, 14), "p.heal");
        var m = Mon(w, Dummy, new Vector2(30, 30));
        var bystander = Mon(w, Dummy, new Vector2(35, 35));
        AiState.Ensure(w, bystander)!.GroupId = 7; // outro grupo: não engajado com o tanque
        Cast(w, tank, "p.hit", m);
        Run(w, 1);
        tank.Get<Health>().Current = 10;

        Cast(w, healer, "p.heal", tank);
        var ev = Run(w, 1);
        Assert.Contains(ev, e => e is AggroGained { Reason: AggroReason.Heal } g && g.Target == healer.Id && g.Monster == m.Id);
        Assert.Equal(10f, m.Get<ThreatTable>().Entries[healer.Id.Value]); // 20 efetivos x 0,5
        Assert.Equal(50f, m.Get<ThreatTable>().Entries[tank.Id.Value]);
        Assert.False(bystander.Get<ThreatTable>().Entries.ContainsKey(healer.Id.Value));
    }

    [Fact]
    public void Proximity_aggro_and_group_joins_together()
    {
        var wolf = Mob("monster.wolf", "ai.melee_chaser", 0, 0.4, [], ("aggroRange", 6));
        var w = NewWorld(null, [wolf]);
        var p = Player(w, new Vector2(10, 10));
        var near = Mon(w, wolf, new Vector2(14, 10));
        var far = Mon(w, wolf, new Vector2(30, 30));

        var ev = Run(w, 1);
        Assert.Contains(ev, e => e is AggroGained { Reason: AggroReason.Proximity } g && g.Monster == near.Id && g.Target == p.Id);
        Assert.Contains(ev, e => e is AggroGained { Reason: AggroReason.Group } g && g.Monster == far.Id && g.Target == p.Id);
        Assert.Equal(p.Id, TargetOf(near));
        Assert.Equal(p.Id, TargetOf(far));
    }

    [Fact]
    public void Separate_groups_do_not_join()
    {
        var wolf = Mob("monster.wolf", "ai.melee_chaser", 0, 0.4, [], ("aggroRange", 6));
        var w = NewWorld(null, [wolf]);
        var p = Player(w, new Vector2(10, 10));
        var a = Mon(w, wolf, new Vector2(14, 10));
        var b = Mon(w, wolf, new Vector2(30, 30));
        AiState.Ensure(w, a)!.GroupId = 1;
        AiState.Ensure(w, b)!.GroupId = 2;
        Run(w, 3);
        Assert.Equal(p.Id, TargetOf(a));
        Assert.True(TargetOf(b).IsNone);
    }

    [Fact]
    public void Monster_drops_target_when_it_dies()
    {
        var w = NewWorld(null, [Dummy]);
        var a = Player(w, new Vector2(10, 10), "p.hit");
        var m = Mon(w, Dummy, new Vector2(30, 30));
        Cast(w, a, "p.hit", m);
        Run(w, 1);
        w.Kill(a);
        var ev = Run(w, 1);
        Assert.True(TargetOf(m).IsNone);
        Assert.Contains(ev, e => e is TargetChanged tc && tc.Current.IsNone);
    }

    // ---------- melee chaser ----------

    [Fact]
    public void Melee_chaser_pathfinds_around_wall_and_attacks()
    {
        var chaser = Mob("monster.chaser", "ai.melee_chaser", 4, 0.3, ["m.bite"], ("aggroRange", 50));
        string[] lines =
        [
            "###########",
            "#....#....#",
            "#....#....#",
            "#.........#",
            "#....#....#",
            "###########",
        ];
        var w = NewWorld(GridMap.FromAscii(lines), [chaser]);
        var p = Player(w, new Vector2(9.5f, 1.5f), "p.hit");
        var m = Mon(w, chaser, new Vector2(1.5f, 1.5f));
        ThreatService.Add(w, m, p, 10, AggroReason.Damage); // sem linha de visão: aggro forçado
        var ev = Run(w, 200);
        Assert.Contains(ev, e => e is DamageDealt d && d.Source == m.Id && d.Target == p.Id);
    }

    // ---------- pack ----------

    [Fact]
    public void Wolves_focus_same_target_and_rally_when_one_dies()
    {
        var wolf = Mob("monster.wolf", "ai.pack", 0, 0.4, ["m.bite", "m.howl"], ("aggroRange", 3), ("focusSeconds", 4));
        var w = NewWorld(null, [wolf]);
        var a = Player(w, new Vector2(10, 10), "p.hit");
        var b = Player(w, new Vector2(10, 12), "p.hit");
        var w1 = Mon(w, wolf, new Vector2(11, 10));
        var w2 = Mon(w, wolf, new Vector2(11, 11));
        var w3 = Mon(w, wolf, new Vector2(11, 9));

        Cast(w, a, "p.hit", w1);
        Run(w, 1);
        Cast(w, b, "p.hit", w3); // w3 sozinho preferiria b (50 > 1)
        Cast(w, b, "p.hit", w3);
        Run(w, 3);
        Assert.True(w3.Get<ThreatTable>().Entries[b.Id.Value] > w3.Get<ThreatTable>().Entries[a.Id.Value]);
        Assert.Equal(a.Id, TargetOf(w1));
        Assert.Equal(a.Id, TargetOf(w2));
        Assert.Equal(a.Id, TargetOf(w3)); // segue o líder (w1)

        // morte de w2: os outros dois usam a habilidade de agressividade
        w.Kill(w2);
        var ev = Run(w, 5);
        var howls = ev.OfType<AbilityExecuted>().Where(e => e.AbilityId == "m.howl").Select(e => e.Source).ToList();
        Assert.Equal(new[] { w1.Id, w3.Id }.OrderBy(i => i.Value), howls.OrderBy(i => i.Value));
    }

    [Fact]
    public void Wolves_do_not_rally_again_without_new_deaths()
    {
        var wolf = Mob("monster.wolf", "ai.pack", 0, 0.4, ["m.bite", "m.howl"], ("aggroRange", 3));
        var w = NewWorld(null, [wolf]);
        Player(w, new Vector2(10, 10));
        var w1 = Mon(w, wolf, new Vector2(11, 10));
        var w2 = Mon(w, wolf, new Vector2(11, 11));
        Run(w, 3);
        w.Kill(w2);
        var ev = Run(w, 60);
        Assert.Equal(1, ev.OfType<AbilityExecuted>().Count(e => e.AbilityId == "m.howl" && e.Source == w1.Id));
    }

    // ---------- kiter ----------

    [Fact]
    public void Kiter_keeps_distance_from_approaching_player_in_open_field()
    {
        var shoot = Ab("m.shoot", new UnitTargeting { Range = 9 }, [new DamageEffect { Base = 1 }], cooldown: 20);
        var bandit = Mob("monster.bandit", "ai.ranged_kiter", 4.5, 0.4, ["m.shoot"], ("aggroRange", 9));
        var w = NewWorld(new GridMap(40, 40), [bandit], shoot);
        var p = Player(w, new Vector2(26.5f, 20));
        var m = Mon(w, bandit, new Vector2(20, 20));

        var min = float.MaxValue;
        var shots = 0;
        for (var i = 0; i < 70; i++)
        {
            var dir = m.Get<Transform>().Position - p.Get<Transform>().Position;
            w.Enqueue(new MoveIntent(p.Id, Vector2.Normalize(dir)));
            w.Step();
            shots += w.LastTickEvents.OfType<DamageDealt>().Count(d => d.Source == m.Id);
            if (i > 5)
            {
                min = Math.Min(min, Vector2.Distance(m.Get<Transform>().Position, p.Get<Transform>().Position));
            }
        }

        Assert.True(min >= 3.5f, $"min distance {min}");
        Assert.True(shots >= 1);
    }

    [Fact]
    public void Kiter_approaches_when_target_is_far_and_stays_at_preferred_distance()
    {
        var shoot = Ab("m.shoot", new UnitTargeting { Range = 9 }, [new DamageEffect { Base = 1 }], cooldown: 20);
        var bandit = Mob("monster.bandit", "ai.ranged_kiter", 4, 0.4, ["m.shoot"], ("aggroRange", 30));
        var w = NewWorld(new GridMap(60, 60), [bandit], shoot);
        var p = Player(w, new Vector2(10, 30));
        var m = Mon(w, bandit, new Vector2(40, 30));
        Run(w, 300);
        var d = Vector2.Distance(m.Get<Transform>().Position, p.Get<Transform>().Position);
        Assert.InRange(d, 5.5f, 9f);
    }

    // ---------- swarm ----------

    [Fact]
    public void Rats_spread_around_target()
    {
        var gnaw = Ab("m.gnaw", new MeleeArcTargeting { Range = 1.2, Angle = 360 }, [new DamageEffect { Base = 1 }], cooldown: 20);
        var rat = Mob("monster.rat", "ai.swarm", 6, 0.3, ["m.gnaw"], ("aggroRange", 40), ("retargetChance", 0));
        var w = NewWorld(new GridMap(40, 40), [rat], gnaw);
        var p = Player(w, new Vector2(20, 20));
        var rats = new[]
        {
            Mon(w, rat, new Vector2(10, 10)),
            Mon(w, rat, new Vector2(10.3f, 10)),
            Mon(w, rat, new Vector2(10, 10.3f)),
            Mon(w, rat, new Vector2(10.3f, 10.3f)),
        };

        Run(w, 250);

        var center = p.Get<Transform>().Position;
        var angles = rats
            .Select(r =>
            {
                var d = r.Get<Transform>().Position - center;
                Assert.InRange(d.Length(), 0.5f, 1.8f);
                return Math.Atan2(d.Y, d.X);
            })
            .OrderBy(x => x)
            .ToList();
        var minGap = double.MaxValue;
        for (var i = 0; i < angles.Count; i++)
        {
            var next = angles[(i + 1) % angles.Count] + (i == angles.Count - 1 ? 2 * Math.PI : 0);
            minGap = Math.Min(minGap, next - angles[i]);
        }

        Assert.True(minGap > Math.PI / 4, $"min angular gap {minGap}");
    }

    [Fact]
    public void Rat_occasionally_switches_target_using_sim_rng()
    {
        var rat = Mob(
            "monster.rat", "ai.swarm", 3, 0.3, [],
            ("aggroRange", 40), ("retargetChance", 1), ("retargetIntervalSeconds", 1), ("retargetHoldSeconds", 2));
        var w = NewWorld(new GridMap(40, 40), [rat]);
        var a = Player(w, new Vector2(15, 20));
        var b = Player(w, new Vector2(25, 20));
        var m = Mon(w, rat, new Vector2(20, 20));
        var ev = Run(w, 200);
        var targets = ev.OfType<TargetChanged>().Where(t => t.Monster == m.Id).Select(t => t.Current).Distinct().ToList();
        Assert.Contains(b.Id, targets); // a tabela só tem o alvo de proximidade; o rato trocou por sorteio
        Assert.DoesNotContain(b.Id.Value, m.Get<ThreatTable>().Entries.Keys);
    }

    // ---------- guardian ----------

    [Fact]
    public void Guardian_alternates_guard_and_heavy_strike()
    {
        var guard = Ab("m.guard", new SelfTargeting(), [Frenzy], 0, 0, "ai.guard");
        var heavy = Ab(
            "m.heavy", new MeleeArcTargeting { Range = 2, Angle = 120 }, [new DamageEffect { Base = 10 }], 0, 10, "ai.heavy");
        var guardian = Mob(
            "monster.guardian", "ai.guardian", 2, 0.5, ["m.guard", "m.heavy"],
            ("aggroRange", 8), ("guardSeconds", 1), ("recoverSeconds", 0.5), ("faceIntervalSeconds", 0.5));
        var w = NewWorld(new GridMap(20, 20), [guardian], guard, heavy);
        var p = Player(w, new Vector2(10, 10));
        var m = Mon(w, guardian, new Vector2(12.5f, 10));

        var ev = Run(w, 260);
        var seq = ev.OfType<AbilityStarted>().Where(a => a.Source == m.Id).Select(a => a.AbilityId).Take(6).ToList();
        Assert.Equal(["m.guard", "m.heavy", "m.guard", "m.heavy", "m.guard", "m.heavy"], seq);

        // golpe pesado tem conjuração visível (AbilityStarted antes de AbilityExecuted)
        var started = ev.OfType<AbilityStarted>().First(a => a.AbilityId == "m.heavy");
        var executed = ev.OfType<AbilityExecuted>().First(a => a.AbilityId == "m.heavy");
        Assert.True(executed.Tick - started.Tick >= 9);
        Assert.Contains(ev, e => e is DamageDealt d && d.Source == m.Id && d.Target == p.Id);
    }

    // ---------- determinismo e registro ----------

    private static ulong RunScenarioHash()
    {
        var gnaw = Ab("m.gnaw", new MeleeArcTargeting { Range = 1.2, Angle = 360 }, [new DamageEffect { Base = 1 }], cooldown: 20);
        var shoot = Ab("m.shoot", new UnitTargeting { Range = 9 }, [new DamageEffect { Base = 1 }], cooldown: 20);
        var rat = Mob("monster.rat", "ai.swarm", 6, 0.3, ["m.gnaw"], ("aggroRange", 40), ("retargetChance", 0.7), ("retargetIntervalSeconds", 0.5));
        var bandit = Mob("monster.bandit", "ai.ranged_kiter", 4.5, 0.4, ["m.shoot"], ("aggroRange", 30));
        var w = NewWorld(new GridMap(40, 40), [rat, bandit], gnaw, shoot);
        var a = Player(w, new Vector2(20, 20), "p.hit");
        Player(w, new Vector2(24, 20), "p.hit");
        Mon(w, bandit, new Vector2(30, 30));
        for (var i = 0; i < 4; i++)
        {
            Mon(w, rat, new Vector2(8 + i, 8));
        }

        for (var t = 0; t < 400; t++)
        {
            if (t % 10 == 0)
            {
                w.Enqueue(new MoveIntent(a.Id, new Vector2(MathF.Sin(t / 30f), MathF.Cos(t / 30f))));
            }

            w.Step();
        }

        return w.ComputeStateHash();
    }

    [Fact]
    public void Ai_scenario_is_deterministic()
    {
        Assert.Equal(RunScenarioHash(), RunScenarioHash());
    }

    [Fact]
    public void Unknown_ai_profile_fails_with_clear_error()
    {
        var bad = Mob("monster.bad", "ai.does_not_exist", 1, 0.4, []);
        var w = NewWorld(null, [bad]);
        Mon(w, bad, new Vector2(5, 5));
        var ex = Assert.Throws<InvalidOperationException>(() => w.Step());
        Assert.Contains("ai.does_not_exist", ex.Message);
    }

    [Fact]
    public void New_profile_can_be_registered_and_produces_intents()
    {
        var content = new ContentCatalog(monsters: [Mob("monster.custom", "ai.custom", 2, 0.4, [], ("aggroRange", 20))]);
        var registry = AiProfileRegistry.CreateDefault().Register("ai.custom", new CustomWalkEast());
        var b = new WorldBuilder(1).WithContent(content);
        b.AddSystem(SystemOrder.Movement, new Game.Core.Simulation.Systems.MovementSystem());
        b.AddSystem(SystemOrder.Ai, new AiSystem(registry));
        var w = b.Build();
        Player(w, new Vector2(10, 10));
        var m = Mon(w, content.Monsters["monster.custom"], new Vector2(5, 10));
        Run(w, 20);
        Assert.True(m.Get<Transform>().Position.X > 5.5f);
    }

    private sealed class CustomWalkEast : IAiProfile
    {
        public void Think(AiContext ctx) => ctx.MoveDirection(new Vector2(1, 0));
    }
}
