using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Stats;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-442: mecânicas novas do Arcanista — canalização com pulsos (Raio Arcano, Feixe do Vazio), dano que sobe a cada pulso,
/// andar devagar canalizando, efeito ao terminar a canalização, orbes que atiram no alvo atual e regeneração de Mana no
/// próprio lançador (Sifão Arcano). Dados de teste, não as habilidades reais (T-443).
/// </summary>
public class ArcanistMechanicsTests
{
    private const int Second = World.TickRate;
    private const int Half = Second / 2;

    private static readonly ClassDefinition TestClass = new()
    {
        Id = "class.test_mage",
        Name = "Mago",
        Speed = 4.5,
        Radius = 0.45,
        Resource = new ResourceDefinition { Type = "mana", Max = 200, Initial = 200, RegenPerSecond = 5 },
        BasicAttack = "mage.test_strike",
        Attributes = new AttributeSet { Str = 2, Dex = 2, Agi = 4, Int = 12, Vit = 6 },
        BaseHealth = 60,
    };

    private static readonly AbilityDefinition Strike = new()
    {
        Id = "mage.test_strike",
        Name = "Golpe",
        Targeting = new UnitTargeting { Range = 30 },
        Effects = [new DamageEffect { Base = 1 }],
    };

    // Canalização de 6 pulsos a cada 0,5 s, sem crescer.
    private static readonly AbilityDefinition Beam = new()
    {
        Id = "arcanist.test_beam",
        Name = "Feixe",
        CooldownTicks = 8 * Second,
        Targeting = new UnitTargeting { Range = 30 },
        Effects = [new DamageEffect { Base = 10, School = DamageSchool.Arcane }],
        Channel = new ChannelDefinition { Pulses = 6, IntervalTicks = Half },
    };

    // Raio Arcano de teste: +15% por pulso, base 100.
    private static readonly AbilityDefinition Ray = new()
    {
        Id = "arcanist.test_ray",
        Name = "Raio Arcano",
        Targeting = new UnitTargeting { Range = 30 },
        Effects = [new DamageEffect { Base = 100, School = DamageSchool.Arcane }],
        Channel = new ChannelDefinition { Pulses = 6, IntervalTicks = Half, PulseGrowth = 0.15 },
    };

    // Andar devagar canalizando (Raio Arcano NH5).
    private static readonly AbilityDefinition WalkingBeam = Beam with
    {
        Id = "arcanist.test_walking_beam",
        Channel = new ChannelDefinition { Pulses = 6, IntervalTicks = Half, MoveSpeedMultiplier = 0.5 },
    };

    private static readonly StatusDefinition Exposed = new()
    {
        Id = "status.test_void_exposed",
        Name = "Exposto ao Vazio",
        DefaultDurationTicks = 6 * Second,
        Modifiers = new StatModifiers { DamageTakenMultiplier = 1.15 },
    };

    // Feixe do Vazio NH3 de teste: 8 pulsos, anda devagar, ao acabar +15% de dano recebido por 6 s.
    private static readonly AbilityDefinition VoidBeam = new()
    {
        Id = "arcanist.test_void_beam",
        Name = "Feixe do Vazio",
        Targeting = new UnitTargeting { Range = 30 },
        Effects = [new DamageEffect { Base = 20, School = DamageSchool.Arcane }],
        Channel = new ChannelDefinition
        {
            Pulses = 8,
            IntervalTicks = Half,
            MoveSpeedMultiplier = 0.5,
            OnComplete = [new ApplyStatusEffect { Status = "status.test_void_exposed" }],
        },
    };

    // Orbes de teste: 3 orbes, um disparo de cada a cada 1,5 s, por 10 s.
    private static readonly StatusDefinition OrbsStatus = new()
    {
        Id = "status.test_orbs",
        Name = "Orbes Arcanos",
        DefaultDurationTicks = 10 * Second,
        Periodic =
        [
            new PeriodicEffectDefinition
            {
                IntervalTicks = 3 * Half,
                Effects = [new CurrentTargetEffect { Range = 9, Count = 3, Effects = [new DamageEffect { Base = 10, School = DamageSchool.Arcane }] }],
            },
        ],
    };

    private static readonly AbilityDefinition Orbs = new()
    {
        Id = "arcanist.test_orbs",
        Name = "Orbes Arcanos",
        Targeting = new SelfTargeting(),
        Effects = [new ApplyStatusEffect { Status = "status.test_orbs" }],
    };

    // Sifão de teste: conjuração 1 s, dano e +6 de Mana por segundo em si por 5 s.
    private static readonly StatusDefinition Siphoning = new()
    {
        Id = "status.test_siphon",
        Name = "Sifão Arcano",
        DefaultDurationTicks = 5 * Second,
        Stats = new StatusStatsDefinition { ManaRegen = new ScaledValueDefinition { Base = 6 } },
    };

    private static readonly AbilityDefinition Siphon = new()
    {
        Id = "arcanist.test_siphon",
        Name = "Sifão Arcano",
        CastTimeTicks = Second,
        Targeting = new UnitTargeting { Range = 9 },
        Effects = [new DamageEffect { Base = 20, School = DamageSchool.Arcane }, new ApplyStatusEffect { Status = "status.test_siphon", Target = EffectTarget.Self }],
    };

    private static readonly StatusDefinition Stun = new()
    {
        Id = "status.test_stun",
        Name = "Atordoado",
        DefaultDurationTicks = 2 * Second,
        Flags = new StatusFlags { Stunned = true },
    };

    private static readonly StatusDefinition Silence = new()
    {
        Id = "status.test_silence",
        Name = "Silenciado",
        DefaultDurationTicks = 2 * Second,
        Flags = new StatusFlags { Silenced = true },
    };

    private static World NewWorld() => WorldBuilder.CreateDefault(
        7,
        null,
        new ContentCatalog(
            classes: [TestClass],
            abilities: [Strike, Beam, Ray, WalkingBeam, VoidBeam, Orbs, Siphon],
            statuses: [Exposed, OrbsStatus, Siphoning, Stun, Silence]));

    // Sem CharacterStats: sem crítico, dano exato.
    private static Entity Unit(World w, Vector2 pos, Faction f, string name, float hp = 100000, params string[] known)
    {
        var st = new AbilityState();
        st.Known.AddRange(known);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = name },
            st);
    }

    private static Entity Arcanist(World w, Vector2 pos, params string[] known) =>
        Unit(w, pos, Faction.Player, "Arcanista", known: known);

    private static Entity Wolf(World w, Vector2 pos, string name = "Lobo") => Unit(w, pos, Faction.Monster, name);

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

    private static List<DamageDealt> Hits(IEnumerable<GameEvent> events, string sourceId) =>
        events.OfType<DamageDealt>().Where(d => d.SourceId == sourceId).ToList();

    /// <summary>Roda até sair o pulso <paramref name="pulses"/>; devolve os eventos.</summary>
    private static List<GameEvent> RunUntilPulses(World w, string abilityId, int pulses)
    {
        var all = new List<GameEvent>();
        for (var i = 0; i < 20 * Second && Hits(all, abilityId).Count < pulses; i++)
        {
            all.AddRange(Run(w));
        }

        return all;
    }

    // ---------- canalização com pulsos ----------

    [Fact]
    public void Standing_still_the_test_channel_fires_six_pulses_every_half_second()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Beam.Id);
        var wolf = Wolf(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Beam.Id, wolf.Id));
        var start = Run(w);
        var startTick = w.Tick;
        Assert.Single(start.OfType<AbilityExecuted>());
        Assert.Empty(Hits(start, Beam.Id)); // nada sai no uso: o primeiro pulso é 0,5 s depois
        var st = arcanist.Get<AbilityState>();
        Assert.True(st.IsOnCooldown(Beam, w.Tick)); // recarga desde o início
        Assert.Equal(startTick + (6 * Half), st.Cast!.EndTick); // a barra vai até o último pulso

        var events = Run(w, 6 * Half);
        Assert.Equal(6, Hits(events, Beam.Id).Count);
        Assert.Equal([1, 2, 3, 4, 5, 6], events.OfType<AbilityHit>().Where(h => h.AbilityId == Beam.Id).Select(h => h.HitIndex));
        Assert.All(events.OfType<AbilityHit>(), h => Assert.Equal(6, h.HitCount));
        Assert.Null(st.Cast);
        Assert.Equal(6 * 10f, wolf.Get<Health>().Max - wolf.Get<Health>().Current);

        Assert.Empty(Hits(Run(w, 2 * Second), Beam.Id)); // acabou: nada mais
    }

    [Fact]
    public void Walking_after_the_third_pulse_cuts_the_channel_and_only_three_land()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Beam.Id);
        var wolf = Wolf(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Beam.Id, wolf.Id));
        var events = RunUntilPulses(w, Beam.Id, 3);
        w.Enqueue(new MoveIntent(arcanist.Id, new Vector2(0, 1)));
        events.AddRange(Run(w, 3 * Second));

        Assert.Equal(3, Hits(events, Beam.Id).Count);
        var cut = Assert.Single(events.OfType<CastInterrupted>());
        Assert.Equal((arcanist.Id, Beam.Id, CastInterruptReason.Moved), (cut.Entity, cut.AbilityId, cut.Reason));
        Assert.Null(arcanist.Get<AbilityState>().Cast);
    }

    [Theory]
    [InlineData("status.test_stun")]
    [InlineData("status.test_silence")]
    public void Stun_or_silence_cuts_the_channel(string statusId)
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Beam.Id);
        var wolf = Wolf(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Beam.Id, wolf.Id));
        var events = RunUntilPulses(w, Beam.Id, 2);
        StatusManager.Apply(w, wolf, arcanist, statusId); // o evento sai junto com o próximo tick
        events.AddRange(Run(w, 3 * Second));

        Assert.Equal(2, Hits(events, Beam.Id).Count);
        Assert.Equal(CastInterruptReason.Disabled, Assert.Single(events.OfType<CastInterrupted>()).Reason);
    }

    [Fact]
    public void Channel_ends_quietly_when_the_target_dies()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Beam.Id);
        var wolf = Unit(w, new Vector2(5, 1), Faction.Monster, "Rato", hp: 25);

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Beam.Id, wolf.Id));
        var events = Run(w, 4 * Second);

        Assert.Equal(3, Hits(events, Beam.Id).Count); // 10 + 10 + 10 ≥ 25
        Assert.False(wolf.IsAlive);
        Assert.Null(arcanist.Get<AbilityState>().Cast);
        Assert.Empty(events.OfType<CastInterrupted>());
    }

    [Fact]
    public void Other_abilities_wait_while_channeling()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Beam.Id, Strike.Id);
        var wolf = Wolf(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Beam.Id, wolf.Id));
        Run(w, Second);
        w.Enqueue(new UseAbilityIntent(arcanist.Id, Strike.Id, wolf.Id));

        Assert.Equal(AbilityRejectReason.AlreadyCasting, Assert.Single(Run(w).OfType<AbilityRejected>()).Reason);
    }

    // ---------- dano que sobe a cada pulso ----------

    [Fact]
    public void Each_pulse_deals_fifteen_percent_more_than_the_previous()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Ray.Id);
        var wolf = Wolf(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Ray.Id, wolf.Id));
        var amounts = Hits(Run(w, 4 * Second), Ray.Id).Select(d => d.Amount).ToList();

        // pulso n = 100 × 1,15^(n−1), arredondado no fim (metade para longe do zero): 100; 115; 132,25; 152,09; 174,90; 201,14.
        Assert.Equal([100f, 115f, 132f, 152f, 175f, 201f], amounts);
        Assert.Equal(1.15 * 1.15, Channeling.PulseFactor(Ray.Channel!, 3), 9);
    }

    // ---------- andar devagar canalizando ----------

    [Fact]
    public void Walking_beam_does_not_cut_and_halves_the_speed_until_the_channel_ends()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), WalkingBeam.Id);
        var wolf = Wolf(w, new Vector2(1, 15));
        var tr = arcanist.Get<Transform>();

        w.Enqueue(new UseAbilityIntent(arcanist.Id, WalkingBeam.Id, wolf.Id));
        var events = Run(w);
        w.Enqueue(new MoveIntent(arcanist.Id, new Vector2(1, 0)));
        events.AddRange(Run(w));

        var before = tr.Position.X;
        events.AddRange(Run(w));
        var channelingStep = tr.Position.X - before;
        Assert.Equal(4f * 0.5f / Second, channelingStep, 4); // metade de 4 tiles/s
        Assert.NotNull(arcanist.Get<AbilityState>().Cast);

        events.AddRange(Run(w, 3 * Second));
        Assert.Equal(6, Hits(events, WalkingBeam.Id).Count); // andar não cortou
        Assert.Empty(events.OfType<CastInterrupted>());
        Assert.Null(arcanist.Get<AbilityState>().Cast);

        before = tr.Position.X;
        Run(w);
        Assert.Equal(4f / Second, tr.Position.X - before, 4); // velocidade normal de volta
    }

    // ---------- efeito ao terminar a canalização ----------

    [Fact]
    public void Void_beam_to_the_end_leaves_the_target_taking_fifteen_percent_more_for_six_seconds()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), VoidBeam.Id);
        var wolf = Wolf(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, VoidBeam.Id, wolf.Id));
        var events = Run(w, 1 + (7 * Half));
        Assert.Equal(7, Hits(events, VoidBeam.Id).Count);
        Assert.False(StatusManager.Has(w, wolf, Exposed.Id)); // ainda não acabou

        events.AddRange(Run(w, Half));
        Assert.Equal(8, Hits(events, VoidBeam.Id).Count);
        var exposed = StatusManager.Get(w, wolf, Exposed.Id)!;
        Assert.Equal(6 * Second, exposed.ExpiresAtTick - w.Tick);
        Assert.Equal(arcanist.Id, exposed.Source);

        // de todos: um terceiro também causa 15% a mais
        var other = Unit(w, new Vector2(6, 1), Faction.Player, "Guerreiro");
        Assert.Equal(115f, DamageResolver.Deal(w, other, wolf, "test.strike", 100, DamageSchool.Physical));

        Run(w, (6 * Second) + 1);
        Assert.False(StatusManager.Has(w, wolf, Exposed.Id));
    }

    [Fact]
    public void Void_beam_cut_in_the_middle_leaves_nothing()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), VoidBeam.Id);
        var wolf = Wolf(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, VoidBeam.Id, wolf.Id));
        var events = RunUntilPulses(w, VoidBeam.Id, 5);
        StatusManager.Apply(w, wolf, arcanist, Stun.Id);
        events.AddRange(Run(w, 4 * Second));

        Assert.Equal(5, Hits(events, VoidBeam.Id).Count);
        Assert.False(StatusManager.Has(w, wolf, Exposed.Id));
    }

    // ---------- orbes no alvo atual ----------

    [Fact]
    public void Three_orbs_shoot_the_current_target_three_times_every_one_and_a_half_seconds()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Strike.Id, Orbs.Id);
        var wolf = Wolf(w, new Vector2(4, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Strike.Id, wolf.Id)); // escolhe o alvo
        Run(w);
        w.Enqueue(new UseAbilityIntent(arcanist.Id, Orbs.Id));
        var events = Run(w, 3 * Half); // o tick do uso e os 29 seguintes
        Assert.Empty(Hits(events, OrbsStatus.Id));

        events = Run(w);
        var volley = Hits(events, OrbsStatus.Id);
        Assert.Equal(3, volley.Count);
        Assert.All(volley, d => Assert.Equal((arcanist.Id, wolf.Id, 10f), (d.Source, d.Target, d.Amount))); // dano do Arcanista

        Assert.Empty(Hits(Run(w, (3 * Half) - 1), OrbsStatus.Id));
        Assert.Equal(3, Hits(Run(w), OrbsStatus.Id).Count);
    }

    [Fact]
    public void Changing_target_moves_the_orbs_to_the_new_target()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Strike.Id, Orbs.Id);
        var first = Wolf(w, new Vector2(4, 1), "Lobo");
        var second = Wolf(w, new Vector2(1, 4), "Urso");

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Strike.Id, first.Id));
        Run(w);
        w.Enqueue(new UseAbilityIntent(arcanist.Id, Orbs.Id));
        var firstVolley = Hits(Run(w, (3 * Half) + 1), OrbsStatus.Id);
        Assert.Equal(3, firstVolley.Count);
        Assert.All(firstVolley, d => Assert.Equal(first.Id, d.Target));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Strike.Id, second.Id));
        var secondVolley = Hits(Run(w, 3 * Half), OrbsStatus.Id);
        Assert.Equal(3, secondVolley.Count);
        Assert.All(secondVolley, d => Assert.Equal(second.Id, d.Target));
    }

    [Fact]
    public void Without_a_target_the_orbs_do_not_shoot()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Orbs.Id);
        Wolf(w, new Vector2(3, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Orbs.Id));
        var events = Run(w, 10 * Second);

        Assert.Contains(events.OfType<StatusApplied>(), s => s.StatusId == OrbsStatus.Id);
        Assert.Empty(Hits(events, OrbsStatus.Id));
    }

    [Fact]
    public void Orbs_skip_a_dead_or_out_of_range_target()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Strike.Id, Orbs.Id);
        var far = Wolf(w, new Vector2(20, 1), "Lobo longe"); // o Golpe alcança 30, os orbes 9
        var dying = Unit(w, new Vector2(3, 1), Faction.Monster, "Rato", hp: 1);

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Strike.Id, far.Id));
        Run(w);
        w.Enqueue(new UseAbilityIntent(arcanist.Id, Orbs.Id));
        Assert.Empty(Hits(Run(w, 3 * Half), OrbsStatus.Id));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Strike.Id, dying.Id)); // o Golpe mata o rato
        Run(w);
        Assert.False(dying.IsAlive);
        Assert.Empty(Hits(Run(w, 3 * Second), OrbsStatus.Id));
    }

    // ---------- Sifão: regeneração de Mana no próprio lançador ----------

    [Fact]
    public void Siphon_gives_the_arcanist_six_mana_per_second_for_five_seconds()
    {
        var w = NewWorld();
        var arcanist = EntityFactory.SpawnPlayer(w, TestClass, new Vector2(1, 1));
        arcanist.Get<AbilityState>().Known.Add(Siphon.Id);
        var plain = EntityFactory.SpawnPlayer(w, TestClass, new Vector2(1, 3));
        var wolf = Wolf(w, new Vector2(4, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Siphon.Id, wolf.Id));
        var events = Run(w, Second + 1); // conjuração de 1 s
        Assert.Single(Hits(events, Siphon.Id));
        var status = StatusManager.Get(w, arcanist, Siphoning.Id)!;
        Assert.Equal(arcanist.Id, status.Source);
        Assert.Equal(6f, arcanist.Get<CharacterStats>().ManaRegen - plain.Get<CharacterStats>().ManaRegen, 3);

        // A regeneração sai em Mana inteira (o resto fica para o tick seguinte): ±1 na janela.
        arcanist.Get<ResourcePool>().Current = 0;
        plain.Get<ResourcePool>().Current = 0;
        Run(w, Second);
        Assert.InRange(arcanist.Get<ResourcePool>().Current - plain.Get<ResourcePool>().Current, 5f, 7f);

        var left = (int)(status.ExpiresAtTick - w.Tick);
        arcanist.Get<ResourcePool>().Current = 0;
        plain.Get<ResourcePool>().Current = 0;
        Run(w, left);
        var expected = 6f * (left - 1) / Second; // no tick em que acaba, o status sai antes da regeneração
        Assert.InRange(arcanist.Get<ResourcePool>().Current - plain.Get<ResourcePool>().Current, expected - 1, expected + 1);
        Assert.False(StatusManager.Has(w, arcanist, Siphoning.Id)); // 5 s
        arcanist.Get<ResourcePool>().Current = 0;
        plain.Get<ResourcePool>().Current = 0;
        Run(w, Second);
        Assert.InRange(arcanist.Get<ResourcePool>().Current - plain.Get<ResourcePool>().Current, -1f, 1f);
    }

    // ---------- estado ----------

    [Fact]
    public void Channel_progress_and_current_target_are_internal_and_copied()
    {
        var w = NewWorld();
        var arcanist = Arcanist(w, new Vector2(1, 1), Beam.Id);
        var wolf = Wolf(w, new Vector2(5, 1));

        w.Enqueue(new UseAbilityIntent(arcanist.Id, Beam.Id, wolf.Id));
        RunUntilPulses(w, Beam.Id, 2);

        var progress = (ChannelProgress)arcanist.Get<ChannelProgress>().Clone();
        Assert.Equal((Beam.Id, 2), (progress.AbilityId, progress.PulsesDone));
        Assert.Equal(wolf.Id, ((CurrentTarget)arcanist.Get<CurrentTarget>().Clone()).Target);
        Assert.False(typeof(IReplicated).IsAssignableFrom(typeof(ChannelProgress)));
        Assert.False(typeof(IReplicated).IsAssignableFrom(typeof(CurrentTarget)));
    }
}
