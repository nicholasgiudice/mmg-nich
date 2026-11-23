using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Status;
using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-441: mecânicas novas do Piromante — Queimadura por NH (grupo exclusivo), dano só em quem queima e tirar a queimadura
/// (Combustão, com a cópia da T-432 no NH5), disparo que explode no primeiro que acerta (Bola de Fogo) e área depois do
/// impacto (Meteoro NH3). Dados de teste, não as habilidades reais (T-443).
/// </summary>
public class PyromancerMechanicsTests
{
    private const int Second = World.TickRate;
    private const string BurnTag = "burn";

    /// <summary>Queimadura de teste por NH: 10 × NH por segundo, 4 s, grupo exclusivo pelo NH.</summary>
    private static StatusDefinition BurnAt(int rank) => new()
    {
        Id = $"status.test_burn_{rank}",
        Name = "Queimadura",
        Tags = ["debuff", BurnTag, "dot"],
        DefaultDurationTicks = 4 * Second,
        Periodic = [new PeriodicEffectDefinition { IntervalTicks = Second, Effects = [new DamageEffect { Base = 10 * rank, School = DamageSchool.Fire }] }],
        Exclusive = new StatusExclusiveDefinition { Group = "test_burn", Tier = rank },
    };

    private static readonly StatusDefinition[] Burns = [.. Enumerable.Range(1, 5).Select(BurnAt)];

    private static readonly StatusDefinition Poison = new()
    {
        Id = "status.test_poison",
        Name = "Veneno",
        Tags = ["debuff", "poison", "dot"],
        DefaultDurationTicks = 8 * Second,
    };

    private static AbilityDefinition Combustion(int rank) => new()
    {
        Id = $"pyromancer.test_combustion_{rank}",
        Name = "Combustão",
        Targeting = new CircleSelfTargeting { Radius = 8 },
        Effects =
        [
            new IfStatusEffect
            {
                Tags = [BurnTag],
                Consume = rank < 3,
                Effects = rank < 5
                    ? [new DamageEffect { Base = 100, School = DamageSchool.Fire }]
                    : [new DamageEffect { Base = 100, School = DamageSchool.Fire }, new CopyStatusEffect { Tags = [BurnTag], Radius = 2, Count = 0 }],
            },
        ],
    };

    private static readonly AbilityDefinition Combustion1 = Combustion(1);
    private static readonly AbilityDefinition Combustion3 = Combustion(3);
    private static readonly AbilityDefinition Combustion5 = Combustion(5);

    private static readonly AbilityDefinition Fireball = new()
    {
        Id = "pyromancer.test_fireball",
        Name = "Bola de Fogo",
        Tags = ["ranged"],
        Targeting = new LineTargeting { Length = 10, Width = 1, ExplodeRadius = 2 },
        Effects =
        [
            new DamageEffect { Base = 50, School = DamageSchool.Fire },
            new ApplyStatusEffect { Status = "status.test_burn_1" },
        ],
    };

    private static readonly AreaDefinition MeteorFloor = new()
    {
        Id = "area.test_meteor_floor",
        Radius = 3,
        DurationTicks = 5 * Second,
        IntervalTicks = Second,
        Effects = [new DamageEffect { Base = 5, School = DamageSchool.Fire }, new ApplyStatusEffect { Status = "status.test_burn_1" }],
    };

    private static readonly AbilityDefinition Meteor3 = new()
    {
        Id = "pyromancer.test_meteor_3",
        Name = "Meteoro",
        Targeting = new CircleAtPointTargeting { Range = 10, Radius = 6 },
        Telegraph = new TelegraphDefinition { Shape = TelegraphShape.Circle, DelayTicks = 2 * Second, Radius = 6, Anchor = TelegraphAnchor.Aim },
        Effects =
        [
            new DamageEffect { Base = 200, School = DamageSchool.Fire },
            new ApplyStatusEffect { Status = "status.test_burn_3" },
            new SpawnAreaEffect { Area = MeteorFloor.Id },
        ],
    };

    private static World NewWorld(ulong seed = 7) => WorldBuilder.CreateDefault(
        seed,
        null,
        new ContentCatalog(
            abilities: [Combustion1, Combustion3, Combustion5, Fireball, Meteor3],
            statuses: [.. Burns, Poison],
            areas: [MeteorFloor]));

    /// <summary>Piromante sem estatísticas (nunca crita, dano exato).</summary>
    private static Entity Pyromancer(World w, Vector2 pos)
    {
        var st = new AbilityState();
        st.Known.AddRange([Combustion1.Id, Combustion3.Id, Combustion5.Id, Fireball.Id, Meteor3.Id]);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(1000),
            new FactionTag(Faction.Player),
            new Collider(0.45f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = "Piromante" },
            st);
    }

    private static Entity Monster(World w, Vector2 pos) => w.Spawn(
        new Transform { Position = pos },
        new Health(1_000_000),
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

    private static StatusInstance? BurnOn(World w, Entity e) =>
        StatusManager.Active(w, e).SingleOrDefault(s => w.Content.Statuses[s.StatusId].Tags.Contains(BurnTag));

    private static float DamageFrom(IEnumerable<GameEvent> events, Entity target, string sourceId) =>
        events.OfType<DamageDealt>().Where(d => d.Target == target.Id && d.SourceId == sourceId).Sum(d => d.Amount);

    // ---------- Combustão: dano só em quem queima, e tirar a queimadura ----------

    [Theory]
    [InlineData(1, false)]
    [InlineData(3, true)]
    public void Combustion_hits_only_the_burning_monsters_and_ends_the_burn_only_below_rank_3(int rank, bool burnStays)
    {
        var w = NewWorld();
        var ability = Combustion(rank);
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var a = Monster(w, new Vector2(4, 1));
        var b = Monster(w, new Vector2(4, 4));
        var c = Monster(w, new Vector2(6, 1));
        StatusManager.Apply(w, pyro, a, "status.test_burn_1");
        StatusManager.Apply(w, pyro, b, "status.test_burn_2");
        StatusManager.Apply(w, pyro, c, Poison.Id); // outro estado não conta

        var events = Use(w, pyro, ability);

        Assert.Equal(100, DamageFrom(events, a, ability.Id));
        Assert.Equal(100, DamageFrom(events, b, ability.Id));
        Assert.Equal(0, DamageFrom(events, c, ability.Id));
        Assert.Equal(burnStays, BurnOn(w, a) is not null);
        Assert.Equal(burnStays, BurnOn(w, b) is not null);
        Assert.True(StatusManager.Has(w, c, Poison.Id));
        Assert.Equal(burnStays ? 0 : 2, events.OfType<StatusRemoved>().Count(r => r.By == pyro.Id));
    }

    [Fact]
    public void Combustion_without_burning_monsters_does_nothing()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var a = Monster(w, new Vector2(4, 1));

        var events = Use(w, pyro, Combustion1);

        Assert.Single(events.OfType<AbilityExecuted>(), x => x.AbilityId == Combustion1.Id);
        Assert.Equal(0, DamageFrom(events, a, Combustion1.Id));
    }

    [Fact]
    public void Combustion_rank_5_spreads_the_burn_to_monsters_within_2_tiles_but_not_3()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var burning = Monster(w, new Vector2(5, 5));
        var near = Monster(w, new Vector2(7, 5)); // 2 tiles do que queima
        var far = Monster(w, new Vector2(5, 8)); // 3 tiles
        StatusManager.Apply(w, pyro, burning, "status.test_burn_4");
        Run(w, Second / 2);
        var source = BurnOn(w, burning)!;

        var events = Use(w, pyro, Combustion5);

        Assert.Equal(100, DamageFrom(events, burning, Combustion5.Id));
        Assert.NotNull(BurnOn(w, burning)); // NH5: a queimadura não acaba
        var copy = Assert.IsType<StatusInstance>(BurnOn(w, near));
        Assert.Equal("status.test_burn_4", copy.StatusId);
        Assert.Equal(source.ExpiresAtTick, copy.ExpiresAtTick); // o tempo que falta
        Assert.Equal(pyro.Id, copy.Source);
        Assert.Null(BurnOn(w, far));
        Assert.Equal(0, DamageFrom(events, near, Combustion5.Id)); // pegou fogo no mesmo uso: não leva o dano da Combustão
    }

    // ---------- Queimadura por NH ----------

    [Fact]
    public void A_lower_rank_burn_keeps_the_stronger_one_and_renews_its_duration()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var wolf = Monster(w, new Vector2(4, 1));
        var strong = StatusManager.Apply(w, pyro, wolf, "status.test_burn_3")!;
        Run(w, 2 * Second);

        var kept = StatusManager.Apply(w, pyro, wolf, "status.test_burn_1");

        Assert.Same(strong, kept);
        var burn = Assert.IsType<StatusInstance>(BurnOn(w, wolf)); // só uma queimadura
        Assert.Equal("status.test_burn_3", burn.StatusId);
        Assert.Equal(w.Tick + (4 * Second), burn.ExpiresAtTick); // duração renovada

        // e continua causando o dano do NH3
        var ticks = Run(w, Second).OfType<DamageDealt>().Where(d => d.Target == wolf.Id).ToList();
        Assert.Equal(30, Assert.Single(ticks).Amount);
    }

    [Fact]
    public void A_higher_rank_burn_replaces_the_weaker_one()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var wolf = Monster(w, new Vector2(4, 1));
        StatusManager.Apply(w, pyro, wolf, "status.test_burn_1");
        Run(w, Second / 2);

        StatusManager.Apply(w, pyro, wolf, "status.test_burn_5");

        var burn = Assert.IsType<StatusInstance>(BurnOn(w, wolf));
        Assert.Equal("status.test_burn_5", burn.StatusId);
        Assert.Equal(w.Tick + (4 * Second), burn.ExpiresAtTick);
        Assert.Single(w.Events.Current.OfType<StatusRemoved>(), r => r.StatusId == "status.test_burn_1");
    }

    [Fact]
    public void A_renewal_never_shortens_the_burn()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var wolf = Monster(w, new Vector2(4, 1));
        StatusManager.Apply(w, pyro, wolf, "status.test_burn_5", 6 * Second); // Rajada de Chamas NH5: 6 s
        var expires = BurnOn(w, wolf)!.ExpiresAtTick;
        Run(w, Second);

        StatusManager.Apply(w, pyro, wolf, "status.test_burn_5"); // mesmo NH, 4 s
        Assert.Equal(expires, BurnOn(w, wolf)!.ExpiresAtTick);

        StatusManager.Apply(w, pyro, wolf, "status.test_burn_2"); // mais fraca, 4 s
        Assert.Equal(expires, BurnOn(w, wolf)!.ExpiresAtTick);
        Assert.Equal("status.test_burn_5", BurnOn(w, wolf)!.StatusId);
    }

    [Fact]
    public void Statuses_without_the_group_are_not_affected()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var wolf = Monster(w, new Vector2(4, 1));
        StatusManager.Apply(w, pyro, wolf, Poison.Id);
        StatusManager.Apply(w, pyro, wolf, "status.test_burn_2");

        Assert.True(StatusManager.Has(w, wolf, Poison.Id));
        Assert.Equal("status.test_burn_2", BurnOn(w, wolf)!.StatusId);
    }

    // ---------- Bola de Fogo: explode no primeiro que acerta ----------

    [Fact]
    public void Fireball_explodes_on_the_first_monster_and_hits_those_in_the_radius()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var first = Monster(w, new Vector2(5, 1));
        var behind = Monster(w, new Vector2(6.5f, 1)); // na fila, a 1,5 do primeiro
        var aside = Monster(w, new Vector2(5, 2.5f)); // fora da linha, a 1,5 do primeiro
        var beyond = Monster(w, new Vector2(9, 1)); // na linha, mas a 4 do primeiro

        var events = Use(w, pyro, Fireball, aim: new Vector2(11, 1));

        Assert.Equal(50, DamageFrom(events, first, Fireball.Id));
        Assert.Equal(50, DamageFrom(events, behind, Fireball.Id));
        Assert.Equal(50, DamageFrom(events, aside, Fireball.Id));
        Assert.Equal(0, DamageFrom(events, beyond, Fireball.Id));
        Assert.NotNull(BurnOn(w, first));
        Assert.NotNull(BurnOn(w, behind));
        Assert.Null(BurnOn(w, beyond));
    }

    [Fact]
    public void Fireball_with_the_second_monster_out_of_the_radius_hits_only_the_first()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var first = Monster(w, new Vector2(4, 1));
        var second = Monster(w, new Vector2(7, 1)); // 3 tiles: corpo 0,4, fora do raio 2

        var events = Use(w, pyro, Fireball, aim: new Vector2(11, 1));

        Assert.Equal(50, DamageFrom(events, first, Fireball.Id));
        Assert.Equal(0, DamageFrom(events, second, Fireball.Id));
    }

    [Fact]
    public void Fireball_without_a_monster_in_the_line_does_not_explode()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var nearTheEnd = Monster(w, new Vector2(11, 2.5f)); // fora da linha, perto do fim do alcance

        var events = Use(w, pyro, Fireball, aim: new Vector2(11, 1));

        Assert.Single(events.OfType<AbilityExecuted>(), x => x.AbilityId == Fireball.Id); // usada, sem alvo
        Assert.Empty(events.OfType<DamageDealt>());
        Assert.Null(BurnOn(w, nearTheEnd));
    }

    // ---------- Meteoro NH3: área depois do impacto ----------

    [Fact]
    public void Meteor_rank_3_leaves_a_burning_floor_after_the_impact()
    {
        var w = NewWorld();
        var pyro = Pyromancer(w, new Vector2(1, 1));
        var center = Monster(w, new Vector2(8, 1));
        var edge = Monster(w, new Vector2(8, 6)); // no raio do impacto, fora do chão em chamas
        var aim = new Vector2(8, 1);

        var events = Use(w, pyro, Meteor3, aim: aim);
        Assert.Single(events.OfType<TelegraphStarted>());
        Assert.Empty(events.OfType<AreaSpawned>());

        events = Run(w, 2 * Second);
        Assert.Single(events.OfType<TelegraphResolved>());
        Assert.Equal(200, DamageFrom(events, center, Meteor3.Id));
        Assert.Equal(200, DamageFrom(events, edge, Meteor3.Id));
        var floor = Assert.Single(events.OfType<AreaSpawned>());
        Assert.Equal(aim, floor.Position);
        Assert.Equal(MeteorFloor.Id, floor.AreaId);

        // Um monstro que entra no chão depois do impacto pega fogo.
        var late = Monster(w, aim + new Vector2(0, 1));
        events = Run(w, 6 * Second);

        Assert.Equal(25, DamageFrom(events, late, MeteorFloor.Id)); // 5 pulsos de 5 em 5 s
        Assert.Equal(25, DamageFrom(events, center, MeteorFloor.Id));
        Assert.Equal(0, DamageFrom(events, edge, MeteorFloor.Id));
        Assert.Contains(events.OfType<StatusApplied>(), s => s.Target == late.Id && s.StatusId == "status.test_burn_1");
        Assert.Single(events.OfType<AreaExpired>(), x => x.AreaId == MeteorFloor.Id);
    }

    [Fact]
    public void Same_seed_same_result()
    {
        static ulong Hash()
        {
            var w = NewWorld(11);
            var pyro = Pyromancer(w, new Vector2(1, 1));
            var a = Monster(w, new Vector2(5, 1));
            Monster(w, new Vector2(6.5f, 1));
            Monster(w, new Vector2(5, 3));
            Use(w, pyro, Fireball, aim: new Vector2(11, 1));
            StatusManager.Apply(w, pyro, a, "status.test_burn_4");
            Use(w, pyro, Combustion5);
            Run(w, 5 * Second);
            return w.ComputeStateHash();
        }

        Assert.Equal(Hash(), Hash());
    }
}
