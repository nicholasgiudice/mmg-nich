using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Ai;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Phases;
using Game.Core.Combat.Resources;
using Game.Core.Combat.Status;
using Game.Core.Combat.Telegraphs;
using Game.Core.Combat.Threat;
using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;
using Game.Data;

namespace Game.Core.Tests;

/// <summary>T-012: telegráfos, áreas persistentes, investida, invocação e fases de boss.</summary>
public class TelegraphPhaseTests
{
    // ---------- helpers ----------

    private static AbilityDefinition Ab(
        string id, TargetingDefinition targeting, EffectDefinition[] effects, TelegraphDefinition? telegraph = null, int cooldown = 0) =>
        new() { Id = id, Name = id, Targeting = targeting, Effects = effects, Telegraph = telegraph, CooldownTicks = cooldown };

    private static TelegraphDefinition Tg(TelegraphShape shape, int delay = 20) =>
        new() { Shape = shape, DelayTicks = delay, Radius = 3, Length = 6, Width = 2, Angle = 90 };

    private static readonly ClassDefinition Hero = new()
    {
        Id = "class.hero",
        Name = "Heroi",
        Health = 100000,
        Speed = 4,
        Radius = 0.4,
        Resource = new ResourceDefinition { Type = "vigor", Max = 100, Initial = 0 },
        BasicAttack = "hero.hit",
        ResourceRules = [new ResourceRuleDefinition { Id = "r.taken", Trigger = ResourceTrigger.DamageTaken, Ratio = 0.1 }],
    };

    private static MonsterDefinition Mob(string id, string ai = "ai.none", params string[] abilities) =>
        new()
        {
            Id = id,
            Name = id,
            Health = 100000,
            Speed = 3,
            Radius = 0.5,
            Abilities = abilities,
            Ai = new AiProfileDefinition { Id = ai },
        };

    private static readonly StatusDefinition Rooted = new()
    {
        Id = "status.rooted",
        Name = "Preso",
        DefaultDurationTicks = 60,
        Flags = new StatusFlags { Rooted = true },
    };

    private static World NewWorld(
        AbilityDefinition[] abilities,
        MonsterDefinition[]? monsters = null,
        AreaDefinition[]? areas = null,
        GridMap? map = null) =>
        WorldBuilder.CreateDefault(
            3,
            map,
            new ContentCatalog(
                classes: [Hero],
                abilities: [.. abilities, Ab("hero.hit", new UnitTargeting { Range = 40 }, [new DamageEffect { Base = 10 }])],
                statuses: [Rooted],
                areas: areas,
                monsters: monsters ?? [Mob("monster.totem")]));

    private static Entity Totem(World w, Vector2 pos, params string[] abilities)
    {
        var def = w.Content.Monsters["monster.totem"];
        var e = EntityFactory.SpawnMonster(w, def, pos);
        e.Get<AbilityState>().Known.AddRange(abilities);
        return e;
    }

    private static Entity Player(World w, Vector2 pos, params string[] abilities)
    {
        var e = EntityFactory.SpawnPlayer(w, Hero, pos);
        e.Get<AbilityState>().Known.AddRange(abilities);
        return e;
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

    private static void Cast(World w, Entity caster, string ability, Vector2? aim = null, Entity? target = null) =>
        w.Enqueue(new UseAbilityIntent(caster.Id, ability, target?.Id, aim));

    private static void Teleport(Entity e, Vector2 pos) => e.Get<Transform>().Position = pos;

    // ---------- telegráfos ----------

    [Fact]
    public void Telegraph_hits_who_is_inside_at_resolution_and_not_who_left_before()
    {
        var nova = Ab("totem.nova", new SelfTargeting(), [new DamageEffect { Base = 30 }], Tg(TelegraphShape.Circle));
        var w = NewWorld([nova]);
        var totem = Totem(w, new Vector2(10, 10), "totem.nova");
        var stays = Player(w, new Vector2(11, 10));
        var leaves = Player(w, new Vector2(10, 11));

        Cast(w, totem, "totem.nova");
        var started = Run(w, 1).OfType<TelegraphStarted>().Single();
        Assert.Equal(TelegraphShape.Circle, started.Shape);
        Assert.Equal(3f, started.Radius);
        Assert.Equal(started.Tick + 20, started.ResolveAtTick);
        Assert.Equal(new Vector2(10, 10), started.Origin);
        Assert.Equal(totem.Id, started.Source);

        var before = Run(w, 10);
        Assert.DoesNotContain(before, e => e is DamageDealt);
        Teleport(leaves, new Vector2(20, 20)); // saiu antes de resolver

        var rest = Run(w, 12);
        var resolved = Assert.Single(rest.OfType<TelegraphResolved>());
        Assert.Equal(started.ResolveAtTick, resolved.Tick);
        Assert.Equal([stays.Id], resolved.Targets);
        Assert.False(resolved.Cancelled);
        var damage = Assert.Single(rest.OfType<DamageDealt>());
        Assert.Equal(stays.Id, damage.Target);
        Assert.Equal(damage.Tick, resolved.Tick);
        Assert.Equal(30f, damage.Amount);
        Assert.Null(w.Find(started.Telegraph)); // a entidade do telegráfo some

        Assert.Empty(Run(w, 40).OfType<DamageDealt>()); // resolve uma única vez
    }

    [Fact]
    public void Telegraph_catches_someone_who_entered_before_resolution()
    {
        var nova = Ab("totem.nova", new SelfTargeting(), [new DamageEffect { Base = 30 }], Tg(TelegraphShape.Circle));
        var w = NewWorld([nova]);
        var totem = Totem(w, new Vector2(10, 10), "totem.nova");
        var late = Player(w, new Vector2(30, 30));

        Cast(w, totem, "totem.nova");
        Run(w, 10);
        Teleport(late, new Vector2(10, 11));
        var rest = Run(w, 15);
        Assert.Equal(late.Id, Assert.Single(rest.OfType<DamageDealt>()).Target);
    }

    [Theory]
    [InlineData(TelegraphShape.Circle, 0, 2, true)]
    [InlineData(TelegraphShape.Circle, 0, 4, false)]
    [InlineData(TelegraphShape.Cone, 0, 4, true)] // à frente (+y)
    [InlineData(TelegraphShape.Cone, 0, -2, false)] // atrás
    [InlineData(TelegraphShape.Cone, 4, 0.5, false)] // fora do arco
    [InlineData(TelegraphShape.Cone, 0, 7, false)] // além do alcance (6)
    [InlineData(TelegraphShape.Line, 0.5, 5, true)]
    [InlineData(TelegraphShape.Line, 3, 3, false)] // fora da largura
    [InlineData(TelegraphShape.Line, 0, -2, false)]
    public void Telegraph_shapes_select_targets(TelegraphShape shape, float dx, float dy, bool hit)
    {
        var ab = Ab("totem.shape", new SelfTargeting(), [new DamageEffect { Base = 5 }], Tg(shape, 2));
        var w = NewWorld([ab]);
        var totem = Totem(w, new Vector2(10, 10), "totem.shape");
        var victim = Player(w, new Vector2(10 + dx, 10 + dy));
        Cast(w, totem, "totem.shape");
        var ev = Run(w, 4);
        Assert.Equal(hit, ev.OfType<DamageDealt>().Any(d => d.Target == victim.Id));
    }

    [Fact]
    public void Telegraph_anchored_at_aim_point_is_placed_at_target_position()
    {
        var tg = Tg(TelegraphShape.Circle, 5) with { Anchor = TelegraphAnchor.Aim, Radius = 2 };
        var ab = Ab("totem.rain", new CircleAtPointTargeting { Range = 20, Radius = 2 }, [new DamageEffect { Base = 5 }], tg);
        var w = NewWorld([ab]);
        var totem = Totem(w, new Vector2(2, 2), "totem.rain");
        var far = Player(w, new Vector2(15, 15));
        var near = Player(w, new Vector2(2.5f, 2));
        Cast(w, totem, "totem.rain", target: far);
        var ev = Run(w, 8);
        Assert.Equal(new Vector2(15, 15), ev.OfType<TelegraphStarted>().Single().Origin);
        Assert.Equal([far.Id], ev.OfType<DamageDealt>().Select(d => d.Target));
        Assert.NotEqual(near.Id, far.Id);
    }

    [Fact]
    public void Telegraph_is_cancelled_when_source_dies_before_resolution()
    {
        var nova = Ab("totem.nova", new SelfTargeting(), [new DamageEffect { Base = 30 }], Tg(TelegraphShape.Circle, 10));
        var w = NewWorld([nova]);
        var totem = Totem(w, new Vector2(10, 10), "totem.nova");
        Player(w, new Vector2(10, 11));
        Cast(w, totem, "totem.nova");
        Run(w, 3);
        w.Kill(totem);
        var ev = Run(w, 10);
        Assert.Empty(ev.OfType<DamageDealt>());
        Assert.True(Assert.Single(ev.OfType<TelegraphResolved>()).Cancelled);
    }

    [Fact]
    public void Charge_hits_everyone_on_the_line_and_moves_the_source_along_it()
    {
        var tg = Tg(TelegraphShape.Line, 10) with { Charge = true, Length = 6, Width = 1.5 };
        var ab = Ab("totem.charge", new LineTargeting { Length = 6, Width = 1.5 }, [new DamageEffect { Base = 40 }], tg);
        var w = NewWorld([ab]);
        var totem = Totem(w, new Vector2(10, 10), "totem.charge");
        var a = Player(w, new Vector2(10, 12));
        var b = Player(w, new Vector2(10, 15));
        var off = Player(w, new Vector2(14, 12));

        Cast(w, totem, "totem.charge", target: a);
        var ev = Run(w, 14);
        Assert.Equal([a.Id, b.Id], ev.OfType<DamageDealt>().Select(d => d.Target).OrderBy(i => i.Value));
        Assert.DoesNotContain(ev.OfType<DamageDealt>(), d => d.Target == off.Id);
        var dash = ev.OfType<Displaced>().Single();
        Assert.Equal(DisplacementKind.Dash, dash.Kind);
        Assert.Equal(6f, dash.Distance, 3);
        Assert.Equal(new Vector2(10, 16), totem.Get<Transform>().Position);
    }

    [Fact]
    public void Charge_stops_at_a_wall()
    {
        var tg = Tg(TelegraphShape.Line, 4) with { Charge = true, Length = 6, Width = 1 };
        var ab = Ab("totem.charge", new LineTargeting { Length = 6, Width = 1 }, [new DamageEffect { Base = 1 }], tg);
        var map = GridMap.FromAscii("##########", "#........#", "#........#", "#........#", "#........#", "##########");
        var w = NewWorld([ab], map: map);
        var totem = Totem(w, new Vector2(5.5f, 1.5f), "totem.charge");
        Cast(w, totem, "totem.charge", aim: new Vector2(5.5f, 4.5f));
        Run(w, 8);
        Assert.True(totem.Get<Transform>().Position.Y < 4.6f);
        Assert.True(totem.Get<Transform>().Position.Y > 3f);
    }

    // ---------- ordem dos sistemas: telegráfo/área geram recurso e ameaça no mesmo tick ----------

    [Fact]
    public void Systems_that_resolve_telegraphs_and_areas_run_before_status_resources_and_ai()
    {
        Assert.True(SystemOrder.Telegraphs < SystemOrder.Status);
        Assert.True(SystemOrder.Areas < SystemOrder.Status);
        Assert.True(SystemOrder.Phases < SystemOrder.Status);
        Assert.True(SystemOrder.Telegraphs > SystemOrder.Abilities);
        Assert.True(SystemOrder.Status < SystemOrder.Resources);
        Assert.True(SystemOrder.Resources < SystemOrder.Ai);
    }

    [Fact]
    public void Telegraph_damage_to_player_generates_resource_in_the_same_tick()
    {
        var nova = Ab("totem.nova", new SelfTargeting(), [new DamageEffect { Base = 50 }], Tg(TelegraphShape.Circle, 5));
        var w = NewWorld([nova]);
        var totem = Totem(w, new Vector2(10, 10), "totem.nova");
        var p = Player(w, new Vector2(10, 11));
        Cast(w, totem, "totem.nova");
        var ev = Run(w, 8);
        var dmg = Assert.Single(ev.OfType<DamageDealt>());
        var change = Assert.Single(ev.OfType<ResourceChanged>(), r => r.Reason == ResourceChangeReason.Rule);
        Assert.Equal(dmg.Tick, change.Tick);
        Assert.Equal(p.Id, change.Entity);
        Assert.Equal(5f, change.Delta); // 0,1 x 50
        Assert.Equal(5f, p.Get<ResourcePool>().Current);
    }

    [Fact]
    public void Telegraph_damage_from_player_generates_threat_on_the_monster_in_the_same_tick()
    {
        var quake = Ab("hero.quake", new SelfTargeting(), [new DamageEffect { Base = 25 }], Tg(TelegraphShape.Circle, 5));
        var w = NewWorld([quake]);
        var player = Player(w, new Vector2(10, 10), "hero.quake");
        var monster = Totem(w, new Vector2(10, 11));
        Cast(w, player, "hero.quake");
        var ev = Run(w, 8);
        var dmg = Assert.Single(ev.OfType<DamageDealt>());
        Assert.Equal(monster.Id, dmg.Target);
        var table = monster.Get<ThreatTable>();
        Assert.True(table.Entries[player.Id.Value] > 0);
        Assert.Contains(ev, e => e is AggroGained { Reason: AggroReason.Damage } g && g.Tick == dmg.Tick && g.Target == player.Id);
    }

    // ---------- áreas ----------

    private static AreaDefinition Pool() => new()
    {
        Id = "area.pool",
        Radius = 2,
        DurationTicks = 120,
        IntervalTicks = 20,
        Effects = [new DamageEffect { Base = 5, School = DamageSchool.Nature }],
    };

    [Fact]
    public void Pool_damages_periodically_only_while_the_entity_is_inside()
    {
        var spawn = Ab("totem.pool", new CircleAtPointTargeting { Range = 30, Radius = 1 }, [new SpawnAreaEffect { Area = "area.pool" }]);
        var w = NewWorld([spawn], areas: [Pool()]);
        var totem = Totem(w, new Vector2(1, 1), "totem.pool");
        var p = Player(w, new Vector2(10, 10));
        Cast(w, totem, "totem.pool", aim: new Vector2(10, 10));
        var first = Run(w, 1);
        var spawned = first.OfType<AreaSpawned>().Single();
        Assert.Equal("area.pool", spawned.AreaId);
        Assert.Equal(new Vector2(10, 10), spawned.Position);
        Assert.Equal(spawned.Tick + 120, spawned.ExpiresAtTick);

        var events = new List<GameEvent>(first);
        events.AddRange(Run(w, 49)); // até spawn+49: aplicações em +20 e +40
        Teleport(p, new Vector2(20, 20)); // sai
        events.AddRange(Run(w, 20)); // +50..+69: nada
        Teleport(p, new Vector2(10, 11)); // volta
        events.AddRange(Run(w, 60)); // +70..+129: aplicações em +80, +100, +120 (tick final)

        var hits = events.OfType<DamageDealt>().Where(d => d.SourceId == "area.pool").Select(d => d.Tick - spawned.Tick).ToList();
        Assert.Equal([20L, 40L, 80L, 100L, 120L], hits);
        Assert.All(events.OfType<DamageDealt>(), d => Assert.Equal(p.Id, d.Target));
        Assert.Equal(5, events.OfType<AreaTriggered>().Count());
        var expired = Assert.Single(events.OfType<AreaExpired>());
        Assert.Equal(AreaExpireReason.Duration, expired.Reason);
        Assert.Equal(spawned.ExpiresAtTick, expired.Tick);
        Assert.Null(w.Find(spawned.Area));
    }

    [Fact]
    public void Pool_does_not_hurt_the_owners_faction_when_it_affects_enemies()
    {
        var spawn = Ab("totem.pool", new CircleAtPointTargeting { Range = 30, Radius = 1 }, [new SpawnAreaEffect { Area = "area.pool" }]);
        var w = NewWorld([spawn], areas: [Pool()]);
        var totem = Totem(w, new Vector2(10, 10), "totem.pool");
        var ally = EntityFactory.SpawnMonster(w, w.Content.Monsters["monster.totem"], new Vector2(10, 10.5f));
        Cast(w, totem, "totem.pool", aim: new Vector2(10, 10));
        var ev = Run(w, 60);
        Assert.Empty(ev.OfType<DamageDealt>());
        Assert.Equal(ally.Get<Health>().Max, ally.Get<Health>().Current);
    }

    [Fact]
    public void Pool_damage_generates_resource_and_threat_in_the_same_tick()
    {
        var spawn = Ab("hero.pool", new CircleAtPointTargeting { Range = 30, Radius = 1 }, [new SpawnAreaEffect { Area = "area.pool" }]);
        var w = NewWorld([spawn], areas: [Pool()]);
        var player = Player(w, new Vector2(1, 1), "hero.pool");
        var monster = Totem(w, new Vector2(10, 10));
        Cast(w, player, "hero.pool", aim: new Vector2(10, 10));
        var ev = Run(w, 25);
        var dmg = Assert.Single(ev.OfType<DamageDealt>());
        Assert.Equal(monster.Id, dmg.Target);
        Assert.True(monster.Get<ThreatTable>().Entries[player.Id.Value] > 0);

        // e o jogador ganha recurso quando a poça de um monstro o fere (regra damage_taken)
        var w2 = NewWorld(
            [Ab("totem.pool", new CircleAtPointTargeting { Range = 30, Radius = 1 }, [new SpawnAreaEffect { Area = "area.pool" }])],
            areas: [Pool()]);
        var totem = Totem(w2, new Vector2(1, 1), "totem.pool");
        var p = Player(w2, new Vector2(10, 10));
        Cast(w2, totem, "totem.pool", aim: new Vector2(10, 10));
        var ev2 = Run(w2, 25);
        var hit = Assert.Single(ev2.OfType<DamageDealt>());
        var change = Assert.Single(ev2.OfType<ResourceChanged>(), r => r.Reason == ResourceChangeReason.Rule);
        Assert.Equal(hit.Tick, change.Tick);
        Assert.Equal(p.Id, change.Entity);
    }

    [Fact]
    public void Trap_triggers_once_roots_and_disappears()
    {
        var trap = new AreaDefinition
        {
            Id = "area.trap",
            Radius = 1,
            DurationTicks = 600,
            Mode = AreaMode.OnEnterOnce,
            Effects = [new ApplyStatusEffect { Status = "status.rooted" }],
        };
        var set = Ab("hero.trap", new CircleAtPointTargeting { Range = 30, Radius = 1 }, [new SpawnAreaEffect { Area = "area.trap" }]);
        var w = NewWorld([set], areas: [trap]);
        var player = Player(w, new Vector2(1, 1), "hero.trap");
        var first = Totem(w, new Vector2(20, 20));
        var second = Totem(w, new Vector2(25, 25));

        Cast(w, player, "hero.trap", aim: new Vector2(10, 10));
        var ev = Run(w, 30);
        var area = ev.OfType<AreaSpawned>().Single();
        Assert.Empty(ev.OfType<AreaTriggered>()); // armada, ninguém dentro
        Assert.NotNull(w.Find(area.Area));

        Teleport(first, new Vector2(10.5f, 10));
        var trig = Run(w, 2);
        var triggered = Assert.Single(trig.OfType<AreaTriggered>());
        Assert.Equal([first.Id], triggered.Targets);
        Assert.True(StatusManager.IsRooted(w, first));
        Assert.Equal(AreaExpireReason.Triggered, Assert.Single(trig.OfType<AreaExpired>()).Reason);
        Assert.Null(w.Find(area.Area));

        Teleport(second, new Vector2(10.5f, 10)); // a armadilha já sumiu
        var later = Run(w, 5);
        Assert.Empty(later.OfType<AreaTriggered>());
        Assert.False(StatusManager.IsRooted(w, second));
    }

    [Fact]
    public void Web_slows_with_a_short_status_that_wears_off_after_leaving()
    {
        var web = new AreaDefinition
        {
            Id = "area.web",
            Radius = 3,
            DurationTicks = 400,
            IntervalTicks = 10,
            Effects = [new ApplyStatusEffect { Status = "status.slow", DurationTicks = 20 }],
        };
        var slow = new StatusDefinition
        {
            Id = "status.slow",
            Name = "Lento",
            DefaultDurationTicks = 20,
            Modifiers = new StatModifiers { SpeedMultiplier = 0.5 },
        };
        var set = Ab("totem.web", new CircleAtPointTargeting { Range = 30, Radius = 1 }, [new SpawnAreaEffect { Area = "area.web" }]);
        var w = WorldBuilder.CreateDefault(
            3,
            null,
            new ContentCatalog(
                classes: [Hero],
                abilities: [set],
                statuses: [slow],
                areas: [web],
                monsters: [Mob("monster.totem")]));
        var totem = Totem(w, new Vector2(1, 1), "totem.web");
        var p = Player(w, new Vector2(10, 10));
        Cast(w, totem, "totem.web", aim: new Vector2(10, 10));
        Run(w, 15);
        Assert.Equal(0.5, w.Combat.SpeedMultiplier(w, p));
        Teleport(p, new Vector2(30, 30));
        Run(w, 30);
        Assert.Equal(1.0, w.Combat.SpeedMultiplier(w, p));
    }

    // ---------- invocação ----------

    [Fact]
    public void Summon_respects_the_alive_limit_per_source_and_joins_the_source_group()
    {
        var spider = Mob("monster.spider", "ai.melee_chaser");
        var summon = Ab(
            "totem.summon",
            new SelfTargeting(),
            [new SummonEffect { Monster = "monster.spider", Count = 3, Radius = 2, MaxAlive = 4 }]);
        var w = NewWorld([summon], [Mob("monster.totem"), spider]);
        var totem = Totem(w, new Vector2(10, 10), "totem.summon");
        AiState.Ensure(w, totem)!.GroupId = 7;

        Cast(w, totem, "totem.summon");
        var first = Run(w, 1).OfType<MonsterSummoned>().ToList();
        Assert.Equal(3, first.Count);
        Assert.All(first, s => Assert.Equal(2f, Vector2.Distance(s.Position, new Vector2(10, 10)), 3));
        Assert.All(first, s => Assert.Equal(7, w.Find(s.Summon)!.Get<AiState>().GroupId));
        Assert.All(first, s => Assert.Equal(totem.Id, w.Find(s.Summon)!.Get<SummonedBy>().Owner));

        Cast(w, totem, "totem.summon");
        var second = Run(w, 1).OfType<MonsterSummoned>().ToList();
        Assert.Single(second); // só cabe 1 (3 + 1 = 4)
        Assert.Equal(4, SummonModule.AliveSummons(w, totem.Id, "monster.spider"));

        Cast(w, totem, "totem.summon");
        Assert.Empty(Run(w, 1).OfType<MonsterSummoned>());

        w.Kill(w.Find(first[0].Summon)!); // abriu uma vaga
        Cast(w, totem, "totem.summon");
        Assert.Single(Run(w, 1).OfType<MonsterSummoned>());
    }

    [Fact]
    public void Summons_never_spawn_inside_walls()
    {
        var map = GridMap.FromAscii("######", "#....#", "#....#", "#....#", "#....#", "######");
        var summon = Ab("totem.summon", new SelfTargeting(), [new SummonEffect { Monster = "monster.spider", Count = 8, Radius = 2 }]);
        var w = NewWorld([summon], [Mob("monster.totem"), Mob("monster.spider")], map: map);
        var totem = Totem(w, new Vector2(1.5f, 1.5f), "totem.summon");
        Cast(w, totem, "totem.summon");
        var ev = Run(w, 1).OfType<MonsterSummoned>().ToList();
        Assert.Equal(8, ev.Count);
        Assert.All(ev, s => Assert.False(map.IsBlocked(s.Position)));
    }

    // ---------- fixture da Matriarca (JSON) ----------

    private static ContentCatalog LoadMatriarch()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "matriarch.json");
        var result = GameDataLoader.LoadSources([("matriarch.json", File.ReadAllText(path))]);
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var db = result.Database!;
        return new ContentCatalog(
            db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values, db.Monsters.Values,
            db.Arenas.Values, db.Encounters.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
        };
    }

    private static EncounterWorld NewEncounter(ulong seed = 5)
    {
        var content = LoadMatriarch();
        return EncounterSetup.Create(seed, content, content.Encounters["encounter.matriarch"]);
    }

    private static void SetHealth(Entity e, double percent) => e.Get<Health>().Current = (float)(e.Get<Health>().Max * percent / 100.0);

    [Fact]
    public void Matriarch_fixture_loads_with_three_phases_telegraph_and_area_data()
    {
        var c = LoadMatriarch();
        var boss = c.Monsters["monster.matriarch"];
        Assert.Equal(["hunt", "suffocating_web", "frenzy"], boss.Phases.Select(p => p.Id));
        Assert.Equal([100.0, 66.0, 33.0], boss.Phases.Select(p => p.HealthPercent));
        Assert.True(boss.Phases[2].NoSummons);
        Assert.IsType<SummonEffect>(boss.Phases[1].OnEnter[0]);
        var charge = c.Abilities["matriarch.charge"].Telegraph!;
        Assert.Equal(TelegraphShape.Line, charge.Shape);
        Assert.Equal(20, charge.DelayTicks);
        Assert.True(charge.Charge);
        Assert.Equal(AreaMode.OnEnterOnce, c.Areas["area.trap"].Mode);
        Assert.Equal(4, ((SummonEffect)c.Abilities["matriarch.summon_lesser"].Effects[0]).MaxAlive);
    }

    [Fact]
    public void Boss_changes_phase_at_each_threshold_exactly_once_and_never_goes_back()
    {
        var enc = NewEncounter();
        var w = enc.World;
        var boss = enc.Enemies[0];
        var changes = new List<PhaseChanged>();

        changes.AddRange(Run(w, 1).OfType<PhaseChanged>());
        Assert.Equal([0], changes.Select(c => c.Index));
        Assert.Equal(-1, changes[0].Previous);

        changes.AddRange(Run(w, 100).OfType<PhaseChanged>());
        Assert.Single(changes); // nada muda enquanto a vida está acima do limiar

        SetHealth(boss, 66); // exatamente no limiar
        changes.AddRange(Run(w, 100).OfType<PhaseChanged>());
        Assert.Equal([0, 1], changes.Select(c => c.Index));
        Assert.Equal("suffocating_web", changes[1].Phase);

        SetHealth(boss, 20);
        changes.AddRange(Run(w, 100).OfType<PhaseChanged>());
        Assert.Equal([0, 1, 2], changes.Select(c => c.Index));

        SetHealth(boss, 100); // curar não volta de fase
        changes.AddRange(Run(w, 100).OfType<PhaseChanged>());
        SetHealth(boss, 20);
        changes.AddRange(Run(w, 100).OfType<PhaseChanged>());
        Assert.Equal(3, changes.Count);
        Assert.Equal(2, boss.Get<PhaseState>().Index);
    }

    [Fact]
    public void Boss_crossing_several_thresholds_at_once_passes_through_every_phase_in_order()
    {
        var enc = NewEncounter();
        var boss = enc.Enemies[0];
        SetHealth(boss, 10);
        var ev = Run(enc.World, 1).OfType<PhaseChanged>().ToList();
        Assert.Equal([0, 1, 2], ev.Select(e => e.Index));
        Assert.Equal([-1, 0, 1], ev.Select(e => e.Previous));
    }

    [Fact]
    public void Phase_entry_actions_summon_and_apply_status_on_self()
    {
        var enc = NewEncounter();
        var w = enc.World;
        var boss = enc.Enemies[0];
        Run(w, 1);

        SetHealth(boss, 60);
        var ev = Run(w, 1);
        var big = Assert.Single(ev.OfType<MonsterSummoned>(), s => s.MonsterId == "monster.spider_big");
        Assert.Equal(boss.Id, big.Owner);

        SetHealth(boss, 30);
        Run(w, 1);
        Assert.True(StatusManager.Has(w, boss, "status.fixture_frenzy"));
        Assert.Equal(1.4 * 1.0, w.Combat.SpeedMultiplier(w, boss), 3);
    }

    [Fact]
    public void Phase_three_does_not_summon_and_other_phases_do_with_the_limit()
    {
        // fases 1 e 2: invoca, respeitando o limite de 4 aranhas pequenas vivas
        var enc = NewEncounter();
        var w = enc.World;
        var boss = enc.Enemies[0];
        var summoned = Run(w, 800).OfType<MonsterSummoned>().Where(s => s.MonsterId == "monster.spider_small").ToList();
        Assert.True(summoned.Count >= 2);
        Assert.True(SummonModule.AliveSummons(w, boss.Id, "monster.spider_small") <= 4);

        // fase 3: nenhuma invocação, mesmo com tudo pronto
        var enc3 = NewEncounter(9);
        var w3 = enc3.World;
        var boss3 = enc3.Enemies[0];
        SetHealth(boss3, 10);
        var ev = Run(w3, 1000);
        Assert.Equal(2, boss3.Get<PhaseState>().Index);
        Assert.DoesNotContain(ev, e => e is MonsterSummoned { MonsterId: "monster.spider_small" });
        Assert.DoesNotContain(ev, e => e is AbilityExecuted a && a.AbilityId.Contains("summon"));
        var check = AbilityValidator.Validate(w3, boss3, "matriarch.summon_lesser", null, null);
        Assert.Equal(AbilityRejectReason.CannotAct, check.Reason);
        Assert.Contains(ev, e => e is AbilityExecuted a && a.AbilityId is "matriarch.bite" or "matriarch.charge");
    }

    [Fact]
    public void Phase_ability_set_restricts_what_the_boss_uses()
    {
        var enc = NewEncounter();
        var w = enc.World;
        var boss = enc.Enemies[0];
        var ev = Run(w, 500).OfType<AbilityExecuted>().Where(a => a.Source == boss.Id).Select(a => a.AbilityId).ToHashSet();
        Assert.Contains("matriarch.venom_pool", ev);
        Assert.DoesNotContain("matriarch.web", ev); // só na fase 2
        Assert.DoesNotContain("matriarch.venom_pool_fast", ev);

        SetHealth(boss, 60);
        var ev2 = Run(w, 600).OfType<AbilityExecuted>().Where(a => a.Source == boss.Id).Select(a => a.AbilityId).ToHashSet();
        Assert.Contains("matriarch.web", ev2);
        Assert.Contains("matriarch.venom_pool_fast", ev2);
        Assert.DoesNotContain("matriarch.venom_pool", ev2);
    }

    [Fact]
    public void Matriarch_encounter_emits_telegraphs_before_damage_and_areas_with_events()
    {
        var enc = NewEncounter();
        var w = enc.World;
        var all = Run(w, 600);
        var charge = all.OfType<TelegraphStarted>().First(t => t.AbilityId == "matriarch.charge");
        Assert.Equal(TelegraphShape.Line, charge.Shape);
        var resolved = all.OfType<TelegraphResolved>().First(t => t.AbilityId == "matriarch.charge");
        Assert.Equal(charge.ResolveAtTick, resolved.Tick);
        Assert.Contains(all.OfType<AreaSpawned>(), a => a.AreaId == "area.venom_pool");
        Assert.Contains(all.OfType<AreaTriggered>(), a => a.AreaId == "area.venom_pool");

        // todo dano de "matriarch.charge" acontece no tick de resolução do telegráfo, nunca antes
        foreach (var d in all.OfType<DamageDealt>().Where(d => d.SourceId == "matriarch.charge"))
        {
            Assert.Contains(all.OfType<TelegraphResolved>(), r => r.AbilityId == "matriarch.charge" && r.Tick == d.Tick);
        }
    }

    [Fact]
    public void New_events_have_log_formatters_registered_through_the_default_registry()
    {
        var f = CombatLogFormatters.CreateDefault();
        Assert.True(f.IsRegistered<TelegraphStarted>());
        Assert.True(f.IsRegistered<TelegraphResolved>());
        Assert.True(f.IsRegistered<AreaSpawned>());
        Assert.True(f.IsRegistered<AreaTriggered>());
        Assert.True(f.IsRegistered<AreaExpired>());
        Assert.True(f.IsRegistered<PhaseChanged>());
        Assert.True(f.IsRegistered<MonsterSummoned>());

        var enc = NewEncounter(11);
        var w = enc.World;
        var log = new CombatLogRecorder(viewer: enc.Players[0].Id);
        SetHealth(enc.Enemies[0], 60);
        for (var i = 0; i < 700; i++)
        {
            w.Step();
            log.Record(w);
        }

        Assert.DoesNotContain(typeof(TelegraphStarted), log.UnformattedTypes);
        Assert.DoesNotContain(typeof(TelegraphResolved), log.UnformattedTypes);
        Assert.DoesNotContain(typeof(AreaSpawned), log.UnformattedTypes);
        Assert.DoesNotContain(typeof(AreaTriggered), log.UnformattedTypes);
        Assert.DoesNotContain(typeof(AreaExpired), log.UnformattedTypes);
        Assert.DoesNotContain(typeof(PhaseChanged), log.UnformattedTypes);
        Assert.Contains(log.Entries, e => e.Category == LogCategory.Fase && e.Text.Contains("fase"));
        Assert.Contains(log.Entries, e => e.Text.Contains("Investida") && e.Text.Contains("prepara"));
    }

    [Fact]
    public void Matriarch_encounter_is_deterministic()
    {
        ulong Hash(ulong seed)
        {
            var enc = NewEncounter(seed);
            SetHealth(enc.Enemies[0], 70);
            Run(enc.World, 800);
            return enc.World.ComputeStateHash();
        }

        Assert.Equal(Hash(21), Hash(21));
        Assert.Equal(Hash(22), Hash(22));
    }
}
