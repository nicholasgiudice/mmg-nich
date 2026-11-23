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
/// T-432: mecânicas novas do Caçador — efeito em todo acerto (Flecha Envenenada), status pelo NH de outra habilidade (Veneno
/// do Caçador), dano periódico maior se o alvo andar (Ferida Aberta), cópia de status para outros monstros (Flecha Dispersa) e
/// status que pula ao acabar (Praga). Dados de teste, não as habilidades reais (T-433).
/// </summary>
public class HunterMechanicsTests
{
    private const int Second = World.TickRate;
    private const string PassiveId = "hunter.test_poisoned_arrow";
    private const string VenomTag = "hunter_venom";

    private static readonly AbilityDefinition Shot = new()
    {
        Id = "archer.test_shot",
        Name = "Disparo",
        Tags = ["basic", "ranged"],
        Targeting = new UnitTargeting { Range = 10 },
        Effects = [new DamageEffect { Base = 100 }],
    };

    private static readonly AbilityDefinition Volley = new()
    {
        Id = "hunter.test_volley",
        Name = "Rajada",
        Tags = ["ranged"],
        Targeting = new LineTargeting { Length = 10, Width = 1 },
        Effects = [new DamageEffect { Base = 50 }],
    };

    private static readonly AbilityDefinition Scatter = new()
    {
        Id = "hunter.test_scatter_arrow",
        Name = "Flecha Dispersa",
        Tags = ["ranged"],
        Targeting = new UnitTargeting { Range = 9 },
        Effects =
        [
            new DamageEffect { Base = 10 },
            new CopyStatusEffect { Tags = [VenomTag, "bleed"], Radius = 4, Count = 2 },
        ],
    };

    private static readonly AbilityDefinition Cloud = new()
    {
        Id = "hunter.test_toxic_cloud",
        Name = "Nuvem Tóxica",
        Tags = ["ranged"],
        Targeting = new CircleAtPointTargeting { Range = 9, Radius = 3 },
        Effects = [new SpawnAreaEffect { Area = "area.test_toxic_cloud" }],
    };

    private static readonly AbilityDefinition Plague = new()
    {
        Id = "hunter.test_plague",
        Name = "Praga",
        Targeting = new CircleSelfTargeting { Radius = 6 },
        Effects =
        [
            new ApplyStatusEffect { Status = "status.test_hunter_venom", Stacks = 3 },
            new ApplyStatusEffect { Status = "status.test_plague", Target = EffectTarget.Self },
        ],
    };

    private static readonly AreaDefinition CloudArea = new()
    {
        Id = "area.test_toxic_cloud",
        Radius = 3,
        DurationTicks = 6 * Second,
        IntervalTicks = 2 * Second,
        Effects = [new ApplyStatusEffect { Status = "status.test_hunter_venom" }],
    };

    private static readonly AreaDefinition SpikeArea = new()
    {
        Id = "area.test_spikes",
        Radius = 3,
        DurationTicks = 3 * Second,
        IntervalTicks = Second,
        Effects = [new DamageEffect { Base = 5 }],
    };

    /// <summary>Veneno do Caçador por NH (dano 10 + 2 por NH; pilhas 3/3/4/4/5; duração 4/4/5/5/6 s), como a tabela do design.</summary>
    private static StatusDefinition VenomAt(int rank) => new()
    {
        Id = $"status.test_hunter_venom_{rank}",
        Name = "Veneno do Caçador",
        Tags = ["debuff", "poison", "dot", VenomTag],
        Stacking = StackingMode.Stack,
        MaxStacks = rank switch { < 3 => 3, < 5 => 4, _ => 5 },
        DefaultDurationTicks = (rank switch { < 3 => 4, < 5 => 5, _ => 6 }) * Second,
        Periodic = [new PeriodicEffectDefinition { IntervalTicks = Second, Effects = [new DamageEffect { Base = 10 + (2 * (rank - 1)) }] }],
    };

    private static readonly StatusDefinition[] Venoms = [.. Enumerable.Range(1, 5).Select(VenomAt)];

    private static readonly StatusDefinition VenomAlias = new()
    {
        Id = "status.test_hunter_venom",
        Name = "Veneno do Caçador",
        DefaultDurationTicks = 1,
        ByRank = new StatusByRankDefinition { Ability = PassiveId, Statuses = [.. Venoms.Select(v => v.Id)] },
    };

    private static readonly StatusDefinition PoisonedArrow = new()
    {
        Id = "status.test_poisoned_arrow",
        Name = "Flecha Envenenada",
        DefaultDurationTicks = 1,
        HitEffects = [new ApplyStatusEffect { Status = VenomAlias.Id }],
    };

    private static readonly StatusDefinition OpenWound = new()
    {
        Id = "status.test_open_wound",
        Name = "Ferida Aberta",
        Tags = ["debuff", "bleed", "dot"],
        DefaultDurationTicks = 10 * Second,
        Periodic = [new PeriodicEffectDefinition { IntervalTicks = Second, Effects = [new DamageEffect { Base = 10, MovingBonus = 0.5 }] }],
    };

    private static readonly StatusDefinition OpenWound5 = OpenWound with
    {
        Id = "status.test_open_wound_5",
        Periodic = [new PeriodicEffectDefinition { IntervalTicks = Second, Effects = [new DamageEffect { Base = 10, MovingBonus = 1 }] }],
    };

    private static readonly StatusDefinition Bleed = new()
    {
        Id = "status.test_bleed",
        Name = "Sangramento",
        Tags = ["debuff", "bleed", "dot"],
        DefaultDurationTicks = 8 * Second,
        Periodic = [new PeriodicEffectDefinition { IntervalTicks = Second, Effects = [new DamageEffect { Base = 5 }] }],
    };

    private static readonly StatusDefinition Slow = new()
    {
        Id = "status.test_slow",
        Name = "Lentidão",
        Tags = ["debuff", "slow"],
        DefaultDurationTicks = 8 * Second,
        Modifiers = new StatModifiers { SpeedMultiplier = 0.5 },
    };

    private static readonly StatusDefinition PlagueStatus = new()
    {
        Id = "status.test_plague",
        Name = "Praga",
        DefaultDurationTicks = 3 * Second,
        JumpOnExpire = new StatusJumpDefinition { Tags = [VenomTag], Radius = 4 },
    };

    private static World NewWorld(ulong seed = 7) => WorldBuilder.CreateDefault(
        seed,
        null,
        new ContentCatalog(
            abilities: [Shot, Volley, Scatter, Cloud, Plague],
            statuses: [.. Venoms, VenomAlias, PoisonedArrow, OpenWound, OpenWound5, Bleed, Slow, PlagueStatus],
            areas: [CloudArea, SpikeArea]));

    /// <summary>Caçador sem estatísticas (nunca crita, dano exato). <paramref name="rank"/> = NH da Flecha Envenenada (null = sem pontos).</summary>
    private static Entity Hunter(World w, Vector2 pos, int? rank = null, bool passive = false)
    {
        var st = new AbilityState();
        st.Known.AddRange([Shot.Id, Volley.Id, Scatter.Id, Cloud.Id, Plague.Id]);
        if (rank is { } r)
        {
            st.Ranks[PassiveId] = r;
        }

        var e = w.Spawn(
            new Transform { Position = pos },
            new Health(1000),
            new FactionTag(Faction.Player),
            new Collider(0.45f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = "Caçador" },
            st);
        if (passive)
        {
            StatusManager.ApplyPermanent(w, e, PoisonedArrow.Id);
        }

        return e;
    }

    private static Entity Ally(World w, Vector2 pos) => w.Spawn(
        new Transform { Position = pos },
        new Health(1000),
        new FactionTag(Faction.Player),
        new Collider(0.45f),
        new Movement(4),
        new UnitInfo { DefinitionId = "test", Name = "Aliado" },
        new AbilityState());

    private static Entity Monster(World w, Vector2 pos, float hp = 1_000_000) => w.Spawn(
        new Transform { Position = pos },
        new Health(hp),
        new FactionTag(Faction.Monster),
        new Collider(0.4f),
        new Movement(4),
        new UnitInfo { DefinitionId = "test", Name = "Lobo" },
        new AbilityState());

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

    private static List<GameEvent> Use(World w, Entity caster, AbilityDefinition ability, Entity? target = null, Vector2? aim = null)
    {
        w.Enqueue(new UseAbilityIntent(caster.Id, ability.Id, target?.Id, aim));
        return Run(w);
    }

    private static StatusInstance? VenomOn(World w, Entity e) =>
        StatusManager.Active(w, e).SingleOrDefault(s => w.Content.Statuses[s.StatusId].Tags.Contains(VenomTag));

    // ---------- efeito em todo acerto (Flecha Envenenada) ----------

    [Fact]
    public void Five_shots_in_a_row_reach_the_max_stacks_and_renew_the_duration()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1), rank: 1, passive: true);
        var wolf = Monster(w, new Vector2(6, 1));

        for (var i = 0; i < 5; i++)
        {
            var events = Use(w, hunter, Shot, wolf);
            Assert.Single(events.OfType<DamageDealt>(), d => d.SourceId == Shot.Id);
            var venom = Assert.IsType<StatusInstance>(VenomOn(w, wolf));
            Assert.Equal(Math.Min(i + 1, 3), venom.Stacks);
            Assert.Equal(w.Tick + (4 * Second), venom.ExpiresAtTick); // renovada a cada disparo
        }

        var final = VenomOn(w, wolf)!;
        Assert.Equal("status.test_hunter_venom_1", final.StatusId);
        Assert.Equal(hunter.Id, final.Source);
        Assert.DoesNotContain(StatusManager.Active(w, wolf), s => s.StatusId == VenomAlias.Id);
    }

    [Fact]
    public void A_venom_tick_does_not_add_a_stack_nor_renew()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1), rank: 1, passive: true);
        var wolf = Monster(w, new Vector2(6, 1));
        Use(w, hunter, Shot, wolf);
        var venom = VenomOn(w, wolf)!;
        var expires = venom.ExpiresAtTick;

        var ticks = Run(w, Second).OfType<DamageDealt>().Where(d => d.SourceId == venom.StatusId).ToList();

        Assert.Single(ticks);
        Assert.Equal(10, ticks[0].Amount);
        Assert.Equal(1, venom.Stacks);
        Assert.Equal(expires, venom.ExpiresAtTick);
    }

    [Fact]
    public void Every_target_of_a_multi_target_hit_gets_a_stack()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1), rank: 1, passive: true);
        var near = Monster(w, new Vector2(3, 1));
        var far = Monster(w, new Vector2(6, 1));

        Use(w, hunter, Volley, aim: new Vector2(10, 1));

        Assert.Equal(1, VenomOn(w, near)!.Stacks);
        Assert.Equal(1, VenomOn(w, far)!.Stacks);
    }

    [Fact]
    public void Area_damage_and_hits_without_the_passive_add_no_stack()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1), rank: 1, passive: true);
        var plain = Hunter(w, new Vector2(1, 3), rank: 1);
        var wolf = Monster(w, new Vector2(6, 1));
        AreaService.Spawn(w, SpikeArea, hunter, new Vector2(6, 1));

        var spikes = Run(w, Second + 1).OfType<DamageDealt>().Where(d => d.SourceId == SpikeArea.Id).ToList();
        Use(w, plain, Shot, wolf);

        Assert.NotEmpty(spikes);
        Assert.Null(VenomOn(w, wolf));
    }

    [Fact]
    public void Reflected_damage_does_not_trigger_the_hit_effects()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1), rank: 1, passive: true);
        var wolf = Monster(w, new Vector2(6, 1));

        // Dano indireto (sem modificadores, como um reflexo) de quem tem a passiva.
        DamageResolver.Deal(w, hunter, wolf, Shot.Id, 20, DamageSchool.Physical, applyModifiers: false);

        Assert.Null(VenomOn(w, wolf));
    }

    // ---------- status pelo NH de outra habilidade (Veneno do Caçador) ----------

    [Theory]
    [InlineData(1, "status.test_hunter_venom_1", 3, 4, 10)]
    [InlineData(5, "status.test_hunter_venom_5", 5, 6, 18)]
    [InlineData(0, "status.test_hunter_venom_1", 3, 4, 10)] // sem pontos (NH 0): o NH1
    [InlineData(null, "status.test_hunter_venom_1", 3, 4, 10)] // sem NH guardado: o NH1
    public void Toxic_cloud_puts_the_venom_of_the_casters_passive_rank(int? rank, string expected, int maxStacks, int seconds, int perStack)
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1), rank: rank);
        var wolf = Monster(w, new Vector2(6, 1));

        Use(w, hunter, Cloud, aim: new Vector2(6, 1));
        var applied = Run(w, 2 * Second).OfType<StatusApplied>().Single(a => a.Target == wolf.Id);

        Assert.Equal(expected, applied.StatusId);
        Assert.Equal(seconds * Second, applied.DurationTicks);
        Assert.Equal(maxStacks, w.Content.Statuses[applied.StatusId].MaxStacks);
        Assert.Equal(hunter.Id, applied.Source);
        var tick = Run(w, Second).OfType<DamageDealt>().Single(d => d.SourceId == expected);
        Assert.Equal(perStack, tick.Amount);
    }

    [Fact]
    public void Toxic_cloud_adds_one_stack_every_two_seconds()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1), rank: 3);
        var wolf = Monster(w, new Vector2(6, 1));

        Use(w, hunter, Cloud, aim: new Vector2(6, 1));
        Run(w, 2 * Second);
        Assert.Equal(1, VenomOn(w, wolf)!.Stacks);
        Run(w, 2 * Second);
        Assert.Equal(2, VenomOn(w, wolf)!.Stacks);
        Assert.Equal("status.test_hunter_venom_3", VenomOn(w, wolf)!.StatusId);
    }

    [Fact]
    public void Monster_source_uses_rank_1()
    {
        var w = NewWorld();
        var caster = Monster(w, new Vector2(1, 1));
        var target = Hunter(w, new Vector2(3, 1));

        var inst = StatusManager.Apply(w, caster, target, VenomAlias.Id);

        Assert.Equal("status.test_hunter_venom_1", inst!.StatusId);
    }

    // ---------- dano maior se o alvo andar (Ferida Aberta) ----------

    [Fact]
    public void Bleed_deals_base_on_a_still_target_and_one_and_a_half_on_a_moving_one()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1));
        var still = Monster(w, new Vector2(6, 1));
        var runner = Monster(w, new Vector2(6, 5));
        StatusManager.Apply(w, hunter, still, OpenWound.Id);
        StatusManager.Apply(w, hunter, runner, OpenWound.Id);
        w.Enqueue(new MoveIntent(runner.Id, new Vector2(1, 0))); // anda pelo movimento do Core

        var first = Run(w, Second).OfType<DamageDealt>().Where(d => d.SourceId == OpenWound.Id).ToDictionary(d => d.Target, d => d.Amount);

        Assert.True(runner.Get<Transform>().Position.X > 8);
        Assert.Equal(10, first[still.Id]);
        Assert.Equal(15, first[runner.Id]);

        w.Enqueue(new StopIntent(runner.Id)); // parou: o pulso seguinte volta ao valor base
        var second = Run(w, Second).OfType<DamageDealt>().Where(d => d.SourceId == OpenWound.Id).ToDictionary(d => d.Target, d => d.Amount);
        Assert.Equal(10, second[still.Id]);
        Assert.Equal(10, second[runner.Id]);

        w.Enqueue(new MoveIntent(runner.Id, new Vector2(0, 1))); // anda de novo
        var third = Run(w, Second).OfType<DamageDealt>().Where(d => d.SourceId == OpenWound.Id).ToDictionary(d => d.Target, d => d.Amount);
        Assert.Equal(15, third[runner.Id]);
    }

    [Fact]
    public void Bleed_rank_5_doubles_on_a_moving_target_and_counts_each_stack()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1));
        var wolf = Monster(w, new Vector2(6, 1));
        StatusManager.Apply(w, hunter, wolf, OpenWound5.Id);

        Run(w, Second - 1);
        wolf.Get<Transform>().Position += new Vector2(0, 2); // empurrado ou andando: a posição do Core mudou
        var hits = Run(w).OfType<DamageDealt>().Where(d => d.SourceId == OpenWound5.Id).ToList();

        Assert.Equal(20, Assert.Single(hits).Amount);
    }

    [Fact]
    public void Tiny_shifts_below_the_threshold_do_not_count_as_moving()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 1));
        var wolf = Monster(w, new Vector2(6, 1));
        StatusManager.Apply(w, hunter, wolf, OpenWound.Id);

        Run(w, Second - 1);
        wolf.Get<Transform>().Position += new Vector2(0.05f, 0);
        var hit = Run(w).OfType<DamageDealt>().Single(d => d.SourceId == OpenWound.Id);

        Assert.Equal(10, hit.Amount);
    }

    // ---------- copiar estados para outros monstros (Flecha Dispersa) ----------

    [Fact]
    public void Scatter_arrow_copies_venom_and_bleed_to_two_monsters_within_4_tiles()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 10), rank: 1);
        var other = Hunter(w, new Vector2(1, 20));
        var target = Monster(w, new Vector2(8, 10));
        var near = Monster(w, new Vector2(10, 10)); // a 2 tiles
        var edge = Monster(w, new Vector2(8, 14)); // a 4 tiles
        var far = Monster(w, new Vector2(13, 10)); // a 5 tiles
        var ally = Ally(w, new Vector2(8, 8)); // aliado a 2 tiles
        StatusManager.Apply(w, other, target, VenomAlias.Id);
        StatusManager.Apply(w, other, target, VenomAlias.Id); // 2 pilhas
        StatusManager.Apply(w, other, target, Bleed.Id);
        StatusManager.Apply(w, other, target, Slow.Id); // sem a etiqueta: não copia
        Run(w, Second / 2);
        var venom = VenomOn(w, target)!;
        var bleed = StatusManager.Get(w, target, Bleed.Id)!;

        Use(w, hunter, Scatter, target);

        foreach (var m in new[] { near, edge })
        {
            var v = VenomOn(w, m)!;
            Assert.Equal((venom.StatusId, 2, venom.ExpiresAtTick, hunter.Id), (v.StatusId, v.Stacks, v.ExpiresAtTick, v.Source));
            var b = StatusManager.Get(w, m, Bleed.Id)!;
            Assert.Equal((bleed.ExpiresAtTick, hunter.Id), (b.ExpiresAtTick, b.Source));
            Assert.False(StatusManager.Has(w, m, Slow.Id));
        }

        Assert.Empty(StatusManager.Active(w, far));
        Assert.Empty(StatusManager.Active(w, ally));
        Assert.Equal(2, VenomOn(w, target)!.Stacks); // o alvo continua com o dele
    }

    [Fact]
    public void Scatter_arrow_picks_the_nearest_monsters_when_there_are_more_than_count()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 10));
        var target = Monster(w, new Vector2(8, 10));
        var a = Monster(w, new Vector2(11, 10)); // 3
        var b = Monster(w, new Vector2(8, 11)); // 1
        var c = Monster(w, new Vector2(8, 8)); // 2
        StatusManager.Apply(w, hunter, target, Bleed.Id);

        Use(w, hunter, Scatter, target);

        Assert.True(StatusManager.Has(w, b, Bleed.Id));
        Assert.True(StatusManager.Has(w, c, Bleed.Id));
        Assert.False(StatusManager.Has(w, a, Bleed.Id));
    }

    [Fact]
    public void Copy_onto_a_monster_that_has_the_status_keeps_the_larger_stacks_and_time()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 10));
        var from = Monster(w, new Vector2(8, 10));
        var to = Monster(w, new Vector2(10, 10));
        for (var i = 0; i < 3; i++)
        {
            StatusManager.Apply(w, hunter, from, VenomAlias.Id);
        }

        StatusManager.Apply(w, hunter, to, VenomAlias.Id);
        Run(w, Second);
        StatusManager.Apply(w, hunter, from, VenomAlias.Id); // renova no from

        var copied = StatusCopy.Spread(w, hunter, from, [VenomTag], 4, 0);

        Assert.Equal(to.Id, Assert.Single(copied).Id);
        var v = VenomOn(w, to)!;
        Assert.Equal(3, v.Stacks);
        Assert.Equal(VenomOn(w, from)!.ExpiresAtTick, v.ExpiresAtTick);
    }

    // ---------- estado que pula ao acabar (Praga) ----------

    [Fact]
    public void With_the_plague_active_an_expiring_venom_jumps_to_the_nearest_with_all_stacks_then_stops()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 10), rank: 1);
        var first = Monster(w, new Vector2(8, 10));
        var nearest = Monster(w, new Vector2(10, 10)); // a 2 tiles
        var second = Monster(w, new Vector2(8, 13)); // a 3 tiles
        var tooFar = Monster(w, new Vector2(3, 10)); // a 5 tiles
        for (var i = 0; i < 3; i++)
        {
            StatusManager.Apply(w, hunter, first, VenomAlias.Id);
        }

        StatusManager.Apply(w, hunter, hunter, PlagueStatus.Id); // 3 s; o veneno dura 4 s
        Run(w, 2 * Second);
        StatusManager.Apply(w, hunter, hunter, PlagueStatus.Id); // renova: a Praga vai até 5 s

        var events = Run(w, 2 * Second);
        Assert.Contains(events, e => e is StatusExpired x && x.Entity == first.Id);
        Assert.Null(VenomOn(w, first));
        var jumped = VenomOn(w, nearest)!;
        Assert.Equal((3, hunter.Id, "status.test_hunter_venom_1"), (jumped.Stacks, jumped.Source, jumped.StatusId));
        Assert.Null(VenomOn(w, second));
        Assert.Null(VenomOn(w, tooFar));

        // A Praga acaba aos 5 s; o veneno que pulou (aos 4 s) acaba aos 8 s e não pula mais.
        Run(w, 5 * Second);
        Assert.False(StatusManager.Has(w, hunter, PlagueStatus.Id));
        Assert.DoesNotContain(new[] { first, nearest, second, tooFar }, m => VenomOn(w, m) is not null);
    }

    [Fact]
    public void Jump_skips_monsters_that_already_have_the_venom_and_chains_while_the_plague_lasts()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 10), rank: 1);
        var a = Monster(w, new Vector2(8, 10));
        var b = Monster(w, new Vector2(10, 10)); // a 2 de a, já envenenado
        var c = Monster(w, new Vector2(8, 13)); // a 3 de a
        StatusManager.Apply(w, hunter, a, VenomAlias.Id);
        StatusManager.Apply(w, hunter, b, VenomAlias.Id, durationTicks: 20 * Second);
        StatusManager.Apply(w, hunter, hunter, PlagueStatus.Id, durationTicks: 30 * Second);

        Run(w, 4 * Second);
        Assert.Equal(1, VenomOn(w, c)!.Stacks); // pulou para c, não para b
        Assert.Null(VenomOn(w, a));

        Run(w, 4 * Second); // o de c acaba e volta para a (que já não tem)
        Assert.NotNull(VenomOn(w, a));
        Assert.Null(VenomOn(w, c));
    }

    [Fact]
    public void Without_the_plague_the_venom_just_ends()
    {
        var w = NewWorld();
        var hunter = Hunter(w, new Vector2(1, 10), rank: 1);
        var a = Monster(w, new Vector2(8, 10));
        var b = Monster(w, new Vector2(10, 10));
        StatusManager.Apply(w, hunter, a, VenomAlias.Id);

        Run(w, 4 * Second + 1);

        Assert.Null(VenomOn(w, a));
        Assert.Null(VenomOn(w, b));
    }

    [Fact]
    public void Plague_ability_puts_three_stacks_on_everyone_in_the_radius_up_to_the_venom_max()
    {
        var w = NewWorld();
        var rank1 = Hunter(w, new Vector2(1, 10), rank: 1);
        var a = Monster(w, new Vector2(4, 10));
        var b = Monster(w, new Vector2(1, 14));
        var far = Monster(w, new Vector2(10, 10));

        Use(w, rank1, Plague);

        Assert.Equal(3, VenomOn(w, a)!.Stacks);
        Assert.Equal(3, VenomOn(w, b)!.Stacks);
        Assert.Null(VenomOn(w, far));
        Assert.True(StatusManager.Has(w, rank1, PlagueStatus.Id));

        // Mais pilhas do que o máximo do veneno daquele NH: fica no máximo (3 no NH1).
        var inst = StatusManager.Apply(w, rank1, far, VenomAlias.Id)!;
        HunterMechanics.AddStacks(w, rank1.Id, far, inst, 10);
        Assert.Equal(3, inst.Stacks);
    }

    [Fact]
    public void Same_seed_same_result()
    {
        static ulong Hash()
        {
            var w = NewWorld(11);
            var hunter = Hunter(w, new Vector2(1, 10), rank: 1, passive: true);
            var a = Monster(w, new Vector2(8, 10));
            Monster(w, new Vector2(10, 10));
            StatusManager.Apply(w, hunter, a, OpenWound.Id);
            Use(w, hunter, Plague);
            Use(w, hunter, Scatter, a);
            w.Enqueue(new MoveIntent(a.Id, new Vector2(0, 1)));
            Run(w, 10 * Second);
            return w.ComputeStateHash();
        }

        Assert.Equal(Hash(), Hash());
    }
}
