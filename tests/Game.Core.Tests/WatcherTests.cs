using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Ai;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Objectives;
using Game.Core.Combat.Phases;
using Game.Core.Combat.Status;
using Game.Core.Combat.Threat;
using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-351: mecânicas do Vigia Sem Rosto. Medo (<c>flags.feared</c>), objetivos a proteger (selos, <c>EncounterFailed</c>),
/// habilidade que entra em recarga quando interrompida (<c>cooldownOnInterrupt</c>, Último Comando), dano recebido por escola
/// (<c>modifiers.damageTakenBySchool</c>, Rosto Vazio), efeitos de fase nos objetivos e a tag <c>ai.objective</c> do
/// <c>ai.boss</c>; e os dados reais do chefe.
/// </summary>
public class WatcherTests
{
    private static readonly StatusDefinition Fear = new()
    {
        Id = "s.fear",
        Name = "Medo",
        Tags = ["debuff", "control", "fear"],
        DefaultDurationTicks = 40,
        Flags = new StatusFlags { Feared = true },
    };

    private static readonly StatusDefinition Brave = new()
    {
        Id = "s.brave",
        Name = "Coragem",
        DefaultDurationTicks = 1000,
        ImmuneToTags = ["fear"],
    };

    private static readonly MonsterDefinition Seal = new()
    {
        Id = "m.seal",
        Name = "Selo",
        Health = 500,
        Speed = 0,
        Radius = 0.6,
        Ai = new AiProfileDefinition { Id = "ai.none" },
    };

    private static readonly MonsterDefinition Brute = new()
    {
        Id = "m.brute",
        Name = "Bruto",
        Health = 1000,
        Speed = 4,
        Radius = 0.5,
        Abilities = ["m.hit"],
        Ai = new AiProfileDefinition { Id = "ai.melee_chaser", Parameters = new Dictionary<string, double> { ["aggroRange"] = 30 } },
    };

    private static readonly AbilityDefinition Hit = new()
    {
        Id = "m.hit",
        Name = "Golpe",
        CooldownTicks = 20,
        Targeting = new MeleeArcTargeting { Range = 1.5, Angle = 90 },
        Effects = [new DamageEffect { Base = 10 }],
    };

    private static readonly AbilityDefinition Heal = new()
    {
        Id = "hero.heal",
        Name = "Cura",
        Targeting = new UnitTargeting { Range = 20, Faction = FactionFilter.Ally },
        Effects = [new HealEffect { Base = 100 }],
    };

    private static readonly AbilityDefinition Ward = new()
    {
        Id = "hero.ward",
        Name = "Escudo",
        Targeting = new UnitTargeting { Range = 20, Faction = FactionFilter.Ally },
        Effects = [new ShieldEffect { Amount = 50, DurationTicks = 100 }],
    };

    private static readonly AbilityDefinition Bolt = new()
    {
        Id = "hero.bolt",
        Name = "Raio",
        CastTimeTicks = 40,
        Targeting = new UnitTargeting { Range = 20, Faction = FactionFilter.Enemy },
        Effects = [new DamageEffect { Base = 10 }],
    };

    private static ContentCatalog Catalog(params AbilityDefinition[] extra) =>
        new(abilities: [Hit, Heal, Ward, Bolt, .. extra], statuses: [Fear, Brave], monsters: [Seal, Brute]);

    private static Entity Unit(World w, Vector2 pos, Faction f, float health = 1000, string[]? known = null, MonsterRank rank = MonsterRank.Normal)
    {
        var st = new AbilityState();
        st.Known.AddRange(known ?? []);
        return w.Spawn(
            new Transform { Position = pos, Facing = new Vector2(1, 0) },
            new Health(health),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = f.ToString(), Rank = rank },
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

    // ---------- medo ----------

    [Fact]
    public void Fear_makes_the_target_run_away_from_the_origin_ignoring_its_move_intents()
    {
        var w = WorldBuilder.CreateDefault(1, null, Catalog());
        var hero = Unit(w, new Vector2(10, 10), Faction.Player, known: ["hero.bolt"]);
        var foe = Unit(w, new Vector2(20, 10), Faction.Monster);

        Assert.NotNull(StatusManager.Apply(w, null, hero, "s.fear", origin: new Vector2(8, 10)));
        w.Enqueue(new MoveIntent(hero.Id, new Vector2(-1, 0))); // tenta ir até a origem
        w.Enqueue(new UseAbilityIntent(hero.Id, "hero.bolt", foe.Id, null));
        var ev = Run(w, 10);

        var pos = hero.Get<Transform>().Position;
        Assert.True(pos.X > 11.5f, $"fugiu para a direita: {pos}");
        Assert.Equal(10f, pos.Y, 3);
        Assert.Contains(ev, e => e is AbilityRejected { AbilityId: "hero.bolt", Reason: AbilityRejectReason.CannotAct });
        Assert.Equal(new Vector2(1, 0), StatusManager.FleeDirection(w, hero));
    }

    [Fact]
    public void Fear_ends_after_its_duration_and_the_move_intent_is_obeyed_again()
    {
        var w = WorldBuilder.CreateDefault(1, null, Catalog());
        var hero = Unit(w, new Vector2(10, 10), Faction.Player);
        StatusManager.Apply(w, null, hero, "s.fear", origin: new Vector2(8, 10));
        w.Enqueue(new MoveIntent(hero.Id, new Vector2(0, 1)));
        Run(w, 40);
        var afterFear = hero.Get<Transform>().Position;

        Run(w, 10);

        var pos = hero.Get<Transform>().Position;
        Assert.Null(StatusManager.FleeDirection(w, hero));
        Assert.Equal(afterFear.X, pos.X, 3); // só a intenção (para baixo) vale de novo
        Assert.True(pos.Y > afterFear.Y + 1.5f);
    }

    [Fact]
    public void Fear_from_an_effect_runs_from_the_aim_point_and_cancels_a_cast()
    {
        var w = WorldBuilder.CreateDefault(1, null, Catalog());
        var hero = Unit(w, new Vector2(10, 10), Faction.Player, known: ["hero.bolt"]);
        var foe = Unit(w, new Vector2(20, 10), Faction.Monster);
        w.Enqueue(new UseAbilityIntent(hero.Id, "hero.bolt", foe.Id, null));
        Run(w, 1);
        Assert.True(hero.Get<AbilityState>().IsCasting);

        // Telegráfo/área centrado acima do herói: ele foge para baixo, não da posição do monstro.
        EffectApplier.Apply(w, [new ApplyStatusEffect { Status = "s.fear" }], foe, [hero], "m.dread", null, new Vector2(10, 8));

        Assert.False(hero.Get<AbilityState>().IsCasting);
        Assert.Equal(new Vector2(0, 1), StatusManager.FleeDirection(w, hero));
    }

    [Fact]
    public void Fear_follows_the_control_rules_by_rank_and_tag_immunity()
    {
        var content = Catalog();
        content = new ContentCatalog(content.Classes.Values, content.Abilities.Values, content.Statuses.Values, null, content.Monsters.Values)
        {
            Rules = new RulesDefinition { Control = new ControlRules { DurationByRank = new ControlDurationByRank { Boss = 0 } } },
        };
        var w = WorldBuilder.CreateDefault(1, null, content);
        var boss = Unit(w, new Vector2(1, 1), Faction.Monster, rank: MonsterRank.Boss);
        var brave = Unit(w, new Vector2(5, 1), Faction.Player);
        StatusManager.Apply(w, null, brave, "s.brave");

        Assert.Null(StatusManager.Apply(w, null, boss, "s.fear"));
        Assert.Null(StatusManager.Apply(w, null, brave, "s.fear"));
        Assert.Null(StatusManager.FleeDirection(w, boss));
        Assert.Null(StatusManager.FleeDirection(w, brave));
    }

    // ---------- objetivos (selos) ----------

    [Fact]
    public void Objectives_are_fixed_player_allies_that_can_be_healed_and_shielded()
    {
        var w = WorldBuilder.CreateDefault(1, null, Catalog());
        var seal = ObjectiveService.Spawn(w, Seal, new Vector2(5, 5));
        var cleric = Unit(w, new Vector2(8, 5), Faction.Player, known: ["hero.heal", "hero.ward"]);
        var brute = Unit(w, new Vector2(5.5f, 5), Faction.Monster); // sobreposto: a separação empurra só o bruto
        seal.Get<Health>().Current = 200;

        Assert.Equal(Faction.Player, seal.Get<FactionTag>().Faction);
        Assert.Null(seal.TryGet<AiState>());
        w.Enqueue(new UseAbilityIntent(cleric.Id, "hero.heal", seal.Id, null));
        Run(w, 1);
        w.Enqueue(new UseAbilityIntent(cleric.Id, "hero.ward", seal.Id, null));
        Run(w, 5);

        Assert.Equal(300, seal.Get<Health>().Current);
        Assert.Equal(50, seal.Get<ShieldState>().Total);
        Assert.Equal(new Vector2(5, 5), seal.Get<Transform>().Position);
        Assert.True(brute.Get<Transform>().Position.X > 5.5f);
    }

    [Fact]
    public void Destroying_every_objective_fails_the_encounter_once()
    {
        var w = WorldBuilder.CreateDefault(1, null, Catalog());
        var a = ObjectiveService.Spawn(w, Seal, new Vector2(5, 5));
        var b = ObjectiveService.Spawn(w, Seal, new Vector2(10, 5));
        Unit(w, new Vector2(20, 5), Faction.Player);

        DamageResolver.Deal(w, null, a, "test", 10_000, DamageSchool.Arcane);
        var first = Run(w, 1);
        Assert.Contains(first, e => e is ObjectiveDestroyed { Remaining: 1 } d && d.Entity == a.Id && d.MonsterId == "m.seal");
        Assert.DoesNotContain(first, e => e is EncounterFailed);

        DamageResolver.Deal(w, null, b, "test", 10_000, DamageSchool.Arcane);
        var second = Run(w, 1);
        Assert.Contains(second, e => e is ObjectiveDestroyed { Remaining: 0 });
        Assert.Single(second.OfType<EncounterFailed>(), e => e.Reason == EncounterFailReason.ObjectivesDestroyed);

        var later = Run(w, 20);
        Assert.DoesNotContain(later, e => e is EncounterFailed or ObjectiveDestroyed);
    }

    [Fact]
    public void A_world_without_objectives_never_fails()
    {
        var w = WorldBuilder.CreateDefault(1, null, Catalog());
        var hero = Unit(w, new Vector2(1, 1), Faction.Player);
        DamageResolver.Deal(w, null, hero, "test", 10_000, DamageSchool.Arcane);

        Assert.DoesNotContain(Run(w, 5), e => e is EncounterFailed);
    }

    [Fact]
    public void Objectives_do_not_pull_proximity_aggro_and_are_not_revived()
    {
        var revive = new AbilityDefinition
        {
            Id = "hero.revive",
            Name = "Reviver",
            Targeting = new SelfTargeting(),
            Effects = [new ReviveEffect { Radius = 20, HealthFraction = 0.5, Count = 5 }],
        };
        var w = WorldBuilder.CreateDefault(1, null, Catalog(revive));
        var seal = ObjectiveService.Spawn(w, Seal, new Vector2(5, 5));
        var brute = EntityFactory.SpawnMonster(w, Brute, new Vector2(7, 5));
        Run(w, 5);
        Assert.Empty(brute.Get<ThreatTable>().Entries);

        var cleric = Unit(w, new Vector2(40, 5), Faction.Player, known: ["hero.revive"]);
        DamageResolver.Deal(w, null, seal, "test", 10_000, DamageSchool.Arcane);
        Run(w, 1);
        w.Enqueue(new UseAbilityIntent(cleric.Id, "hero.revive", null, null));
        Run(w, 2);

        Assert.False(seal.IsAlive);
    }

    // ---------- Último Comando: recarga ao ser interrompida ----------

    [Theory]
    [InlineData(true, 200)]
    [InlineData(false, 0)]
    public void An_interrupted_cast_goes_on_cooldown_only_with_cooldownOnInterrupt(bool flag, int expectedCooldown)
    {
        var command = new AbilityDefinition
        {
            Id = "boss.command",
            Name = "Comando",
            CooldownTicks = 200,
            CastTimeTicks = 80,
            CooldownOnInterrupt = flag,
            Targeting = new UnitTargeting { Range = 40, Faction = FactionFilter.Enemy },
            Effects = [new DamageEffect { Base = 10_000, School = DamageSchool.Arcane }],
        };
        var kick = new AbilityDefinition
        {
            Id = "hero.kick",
            Name = "Chute",
            Targeting = new UnitTargeting { Range = 3, Faction = FactionFilter.Enemy },
            Effects = [new InterruptEffect()],
        };
        var w = WorldBuilder.CreateDefault(1, null, Catalog(command, kick));
        var seal = ObjectiveService.Spawn(w, Seal, new Vector2(15, 5));
        var boss = Unit(w, new Vector2(5, 5), Faction.Monster, health: 50_000, known: ["boss.command"]);
        var warrior = Unit(w, new Vector2(6, 5), Faction.Player, known: ["hero.kick"]);
        w.Enqueue(new UseAbilityIntent(boss.Id, "boss.command", seal.Id, null));
        Run(w, 20);

        w.Enqueue(new UseAbilityIntent(warrior.Id, "hero.kick", boss.Id, null));
        var ev = Run(w, 1);

        Assert.Contains(ev, e => e is CastInterrupted { AbilityId: "boss.command", Reason: CastInterruptReason.Interrupted });
        Assert.Equal(expectedCooldown, boss.Get<AbilityState>().CooldownRemaining("boss.command", w.Tick));
        Run(w, 100);
        Assert.True(seal.IsAlive);
    }

    // ---------- dano recebido por escola ----------

    [Fact]
    public void Damage_taken_by_school_only_changes_that_school()
    {
        var face = new StatusDefinition
        {
            Id = "s.face",
            Name = "Rosto",
            DefaultDurationTicks = 1000,
            Modifiers = new StatModifiers { DamageTakenBySchool = new Dictionary<string, double> { ["holy"] = 1.4 } },
        };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(statuses: [face]));
        var boss = Unit(w, new Vector2(1, 1), Faction.Monster, health: 10_000);
        StatusManager.Apply(w, boss, boss, "s.face");

        DamageResolver.Deal(w, null, boss, "x", 100, DamageSchool.Holy);
        Assert.Equal(9_860, boss.Get<Health>().Current);
        DamageResolver.Deal(w, null, boss, "x", 100, DamageSchool.Physical);
        Assert.Equal(9_760, boss.Get<Health>().Current);
    }

    // ---------- fases nos objetivos e ai.objective ----------

    [Fact]
    public void Phase_effects_reach_the_living_objectives_and_the_boss_aims_its_objective_ability_at_the_weakest()
    {
        var mark = new StatusDefinition { Id = "s.mark", Name = "Marca", DefaultDurationTicks = 10_000 };
        var command = new AbilityDefinition
        {
            Id = "boss.command",
            Name = "Comando",
            Tags = ["ai.objective"],
            CooldownTicks = 200,
            CastTimeTicks = 40,
            Targeting = new UnitTargeting { Range = 40, Faction = FactionFilter.Enemy },
            Effects = [new DamageEffect { Base = 10_000, School = DamageSchool.Arcane }],
        };
        var boss = new MonsterDefinition
        {
            Id = "m.boss",
            Name = "Chefe",
            Health = 1000,
            Speed = 3,
            Radius = 0.9,
            Rank = MonsterRank.Boss,
            Abilities = ["boss.command", "m.hit"],
            Ai = new AiProfileDefinition { Id = "ai.boss", Parameters = new Dictionary<string, double> { ["aggroRange"] = 40 } },
            Phases =
            [
                new PhaseDefinition { Id = "p1", HealthPercent = 100, Abilities = ["m.hit"] },
                new PhaseDefinition
                {
                    Id = "p2",
                    HealthPercent = 50,
                    OnEnterObjectives = [new ApplyStatusEffect { Status = "s.mark" }],
                },
            ],
        };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [Hit, command], statuses: [mark], monsters: [Seal, boss]));
        var full = ObjectiveService.Spawn(w, Seal, new Vector2(20, 5));
        var hurt = ObjectiveService.Spawn(w, Seal, new Vector2(20, 15));
        var dead = ObjectiveService.Spawn(w, Seal, new Vector2(20, 25));
        hurt.Get<Health>().Current = 100;
        DamageResolver.Deal(w, null, dead, "x", 10_000, DamageSchool.Arcane);
        var b = EntityFactory.SpawnMonster(w, boss, new Vector2(5, 10));
        Unit(w, new Vector2(7, 10), Faction.Player, health: 100_000);
        Run(w, 10);
        Assert.False(StatusManager.Has(w, full, "s.mark"));
        Assert.False(b.Get<AbilityState>().IsCasting); // fase 1 não tem o Comando

        b.Get<Health>().Current = 400;
        var ev = Run(w, 5);

        Assert.True(StatusManager.Has(w, full, "s.mark"));
        Assert.True(StatusManager.Has(w, hurt, "s.mark"));
        Assert.False(StatusManager.Has(w, dead, "s.mark"));
        Assert.Contains(ev, e => e is AbilityStarted { AbilityId: "boss.command" } s && s.Target == hurt.Id);
    }

    // ---------- dados reais ----------

    [Fact]
    public void Real_watcher_encounter_spawns_three_seals_that_are_safe_until_the_last_command()
    {
        var content = RealContent.Value;
        var setup = EncounterSetup.Create(1, content, content.Encounters["encounter.faceless_watcher"]);
        var w = setup.World;
        var boss = setup.Enemies.Single();
        Assert.Equal(3, setup.Objectives.Count);
        Assert.All(setup.Objectives, s => Assert.Equal("monster.watcher_seal", s.Get<UnitInfo>().DefinitionId));

        w.Step();
        Assert.All(setup.Objectives, s => Assert.True(StatusManager.IsUntargetable(w, s)));
        Assert.Equal(0, PhaseService.Current(w, boss) is { } p ? content.Monsters["monster.faceless_watcher"].Phases.ToList().IndexOf(p) : -1);

        boss.Get<Health>().Current = boss.Get<Health>().Max * 0.6f;
        w.Step();
        Assert.True(StatusManager.Has(w, boss, "status.empty_face"));
        var holy = w.Combat.DamageTakenMultiplier(w, boss, null, DamageSchool.Holy);
        Assert.Equal(1.4, holy, 3);

        boss.Get<Health>().Current = boss.Get<Health>().Max * 0.2f;
        w.Step();
        Assert.All(setup.Objectives, s =>
        {
            Assert.False(StatusManager.IsUntargetable(w, s));
            Assert.True(StatusManager.Has(w, s, "status.seal_crumbling"));
        });
    }

    [Fact]
    public void Real_last_command_destroys_a_full_seal_unless_the_warrior_heavy_strike_interrupts_it()
    {
        var content = RealContent.Value;
        var command = content.Abilities["watcher.last_command"];
        Assert.True(command.CooldownOnInterrupt);
        Assert.Contains(BossTag, command.Tags);

        foreach (var interrupt in new[] { false, true })
        {
            var w = WorldBuilder.CreateDefault(1, null, content);
            var seal = ObjectiveService.Spawn(w, content.Monsters["monster.watcher_seal"], new Vector2(20, 5), 15);
            var boss = EntityFactory.SpawnMonster(w, content.Monsters["monster.faceless_watcher"], new Vector2(5, 5), 15);
            boss.Get<AbilityState>().Known.Clear();
            boss.Get<AbilityState>().Known.Add("watcher.last_command");
            boss.Get<Health>().Current = boss.Get<Health>().Max * 0.2f; // o Último Comando é da fase 3
            w.Step();
            var warrior = EntityFactory.SpawnPlayer(w, content.Classes["class.warrior"], new Vector2(6.5f, 5));
            w.Enqueue(new UseAbilityIntent(boss.Id, "watcher.last_command", seal.Id, null));
            Run(w, 10);
            if (interrupt)
            {
                w.Enqueue(new UseAbilityIntent(warrior.Id, "warrior.heavy_strike", boss.Id, null));
            }

            var ev = Run(w, command.CastTimeTicks);

            Assert.Equal(!interrupt, ev.Any(e => e is ObjectiveDestroyed));
            Assert.Equal(!interrupt, ev.Any(e => e is EncounterFailed));
            Assert.Equal(interrupt, seal.IsAlive);
            if (interrupt)
            {
                Assert.True(boss.Get<AbilityState>().CooldownRemaining("watcher.last_command", w.Tick) > 0);
            }
        }
    }

    [Fact]
    public void Real_dread_ground_fears_the_players_inside_the_circle()
    {
        var content = RealContent.Value;
        var w = WorldBuilder.CreateDefault(1, null, content);
        var boss = EntityFactory.SpawnMonster(w, content.Monsters["monster.faceless_watcher"], new Vector2(5, 5), 1);
        boss.Get<AbilityState>().Known.Clear();
        boss.Get<AbilityState>().Known.Add("watcher.dread_ground");
        boss.Get<Health>().Current = boss.Get<Health>().Max * 0.5f; // fase 2 (Rosto Vazio); nível 1 para o Mago aguentar o dano
        w.Step();
        var mage = EntityFactory.SpawnPlayer(w, content.Classes["class.mage"], new Vector2(12, 5));
        var center = new Vector2(12, 6);
        w.Enqueue(new UseAbilityIntent(boss.Id, "watcher.dread_ground", mage.Id, center));
        Run(w, 30);

        Assert.True(StatusManager.Has(w, mage, "status.dread"));
        Assert.Equal(new Vector2(0, -1), StatusManager.FleeDirection(w, mage));
    }

    [Fact]
    public void Objective_events_have_log_lines()
    {
        var content = RealContent.Value;
        var setup = EncounterSetup.Create(1, content, content.Encounters["encounter.faceless_watcher"]);
        var w = setup.World;
        var recorder = new CombatLogRecorder();
        DamageResolver.Deal(w, null, setup.Objectives[0], "test", 1_000_000, DamageSchool.Arcane);
        w.Step();
        recorder.Record(w);
        foreach (var s in setup.Objectives.Skip(1))
        {
            DamageResolver.Deal(w, null, s, "test", 1_000_000, DamageSchool.Arcane);
        }

        w.Step();
        recorder.Record(w);

        Assert.Contains(recorder.Entries, e => e.Text.Contains("foi destruído (restam 2)", StringComparison.Ordinal));
        Assert.Contains(recorder.Entries, e => e.Text == "Falha: todos os objetivos foram destruídos");
    }

    private const string BossTag = Combat.Ai.Profiles.BossProfile.ObjectiveTag;
}
