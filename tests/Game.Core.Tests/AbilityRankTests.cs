using System.Numerics;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-060: níveis de habilidade (NH), múltiplos acertos e cargas por recarga.</summary>
public class AbilityRankTests
{
    private static DamageEffect Dmg(double b) => new() { Base = b };

    /// <summary>Tiro com 5 NH no formato do Disparo Potente: 1, 1, 2, 2, 3 flechas.</summary>
    private static readonly AbilityDefinition Shot = new()
    {
        Id = "hero.shot",
        Name = "Tiro",
        CooldownTicks = 100,
        Targeting = new UnitTargeting { Range = 10 },
        Effects = [Dmg(10)],
        Ranks =
        [
            new AbilityRankDefinition { Rank = 2, Effects = [Dmg(12)] },
            new AbilityRankDefinition { Rank = 3, Effects = [Dmg(8)], Hits = new AbilityHitsDefinition { Count = 2, IntervalTicks = 3 } },
            new AbilityRankDefinition { Rank = 4, Effects = [Dmg(10)] },
            new AbilityRankDefinition { Rank = 5, Effects = [Dmg(8)], Hits = new AbilityHitsDefinition { Count = 3, IntervalTicks = 3 } },
        ],
    };

    /// <summary>Explosão em área: 1 acerto no NH1, 3 no NH2 (8 ticks entre eles).</summary>
    private static readonly AbilityDefinition Blast = new()
    {
        Id = "hero.blast",
        Name = "Explosão",
        CooldownTicks = 100,
        Targeting = new CircleAtPointTargeting { Range = 10, Radius = 3 },
        Effects = [Dmg(20)],
        Ranks = [new AbilityRankDefinition { Rank = 2, Hits = new AbilityHitsDefinition { Count = 3, IntervalTicks = 8 } }],
    };

    /// <summary>Recuo com 2 cargas no NH2 (recarga de 40 ticks cada).</summary>
    private static readonly AbilityDefinition Dash = new()
    {
        Id = "hero.dash",
        Name = "Recuo",
        CooldownTicks = 40,
        Targeting = new SelfTargeting(),
        Effects = [new DisplacementEffect { Kind = DisplacementKind.Dash, Distance = 1, Direction = DisplacementDirection.Backward }],
        Ranks = [new AbilityRankDefinition { Rank = 2, CooldownCharges = 2 }],
    };

    private static World NewWorld() =>
        WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [Shot, Blast, Dash]));

    private static Entity Hero(World w, Vector2 pos, IReadOnlyDictionary<string, int> ranks)
    {
        var st = new AbilityState();
        st.Known.AddRange([Shot.Id, Blast.Id, Dash.Id]);
        foreach (var (id, rank) in ranks)
        {
            st.Ranks[id] = rank;
        }

        return w.Spawn(
            new Transform { Position = pos },
            new Health(100),
            new FactionTag(Faction.Player),
            new Collider(0.4f),
            new Movement(4),
            st);
    }

    private static Entity Dummy(World w, Vector2 pos, float hp = 1000) =>
        w.Spawn(new Transform { Position = pos }, new Health(hp), new FactionTag(Faction.Monster), new Collider(0.4f));

    private static List<DamageDealt> Run(World w, int ticks)
    {
        var all = new List<DamageDealt>();
        for (var i = 0; i < ticks; i++)
        {
            w.Step();
            all.AddRange(w.LastTickEvents.OfType<DamageDealt>());
        }

        return all;
    }

    [Fact]
    public void Ranks_inherit_from_the_previous_rank_and_are_clamped()
    {
        var content = new ContentCatalog(abilities: [Shot]);

        Assert.Equal(10, Assert.IsType<DamageEffect>(content.AbilityAt("hero.shot", 1)!.Effects[0]).Base);
        Assert.Equal(1, content.AbilityAt("hero.shot", 2)!.HitCount);

        // NH4 só troca o dano: herda as 2 flechas do NH3.
        var r4 = content.AbilityAt("hero.shot", 4)!;
        Assert.Equal(10, Assert.IsType<DamageEffect>(r4.Effects[0]).Base);
        Assert.Equal(2, r4.HitCount);
        Assert.Equal(3, r4.Hits!.IntervalTicks);

        Assert.Equal(3, content.AbilityAt("hero.shot", 9)!.HitCount); // acima do máximo = NH5
        Assert.Equal(1, content.AbilityAt("hero.shot", 0)!.HitCount); // abaixo = NH1
        Assert.Empty(content.AbilityAt("hero.shot", 5)!.Ranks);
        Assert.Null(content.AbilityAt("nope", 1));
    }

    [Theory]
    [InlineData(1, new[] { 10f })]
    [InlineData(2, new[] { 12f })]
    [InlineData(3, new[] { 8f, 8f })]
    [InlineData(5, new[] { 8f, 8f, 8f })]
    public void Each_rank_fires_its_number_of_arrows_with_its_damage(int rank, float[] expected)
    {
        var w = NewWorld();
        var hero = Hero(w, Vector2.Zero, new Dictionary<string, int> { [Shot.Id] = rank });
        var dummy = Dummy(w, new Vector2(3, 0));

        w.Enqueue(new UseAbilityIntent(hero.Id, Shot.Id, dummy.Id));
        var hits = Run(w, 20);

        Assert.Equal(expected, hits.Select(h => h.Amount));
        Assert.All(hits, h => Assert.Equal(dummy.Id, h.Target));
    }

    [Fact]
    public void Extra_arrows_come_at_the_interval_and_publish_ability_hit()
    {
        var w = NewWorld();
        var hero = Hero(w, Vector2.Zero, new Dictionary<string, int> { [Shot.Id] = 5 });
        var dummy = Dummy(w, new Vector2(3, 0));
        w.Enqueue(new UseAbilityIntent(hero.Id, Shot.Id, dummy.Id));

        var damageTicks = new List<long>();
        var extra = new List<AbilityHit>();
        for (var i = 0; i < 20; i++)
        {
            w.Step();
            damageTicks.AddRange(w.LastTickEvents.OfType<DamageDealt>().Select(d => w.Tick));
            extra.AddRange(w.LastTickEvents.OfType<AbilityHit>());
        }

        Assert.Equal([1L, 4L, 7L], damageTicks);
        Assert.Equal([2, 3], extra.Select(h => h.HitIndex));
        Assert.All(extra, h => Assert.Equal(3, h.HitCount));
        Assert.Equal(1, w.Find(hero.Id)!.Get<AbilityState>().CooldownRemaining(Shot.Id, w.Tick) > 0 ? 1 : 0);
    }

    [Fact]
    public void Extra_arrow_moves_to_the_nearest_enemy_when_the_target_dies()
    {
        var w = NewWorld();
        var hero = Hero(w, Vector2.Zero, new Dictionary<string, int> { [Shot.Id] = 3 });
        var weak = Dummy(w, new Vector2(3, 0), hp: 5);
        var other = Dummy(w, new Vector2(0, 4));

        w.Enqueue(new UseAbilityIntent(hero.Id, Shot.Id, weak.Id));
        var hits = Run(w, 20);

        Assert.Equal([weak.Id, other.Id], hits.Select(h => h.Target));
    }

    [Fact]
    public void Extra_hit_is_lost_when_no_enemy_is_left()
    {
        var w = NewWorld();
        var hero = Hero(w, Vector2.Zero, new Dictionary<string, int> { [Shot.Id] = 5 });
        var weak = Dummy(w, new Vector2(3, 0), hp: 5);

        w.Enqueue(new UseAbilityIntent(hero.Id, Shot.Id, weak.Id));
        var hits = Run(w, 20);

        Assert.Single(hits);
    }

    [Fact]
    public void Area_with_three_hits_hits_everyone_inside_each_time()
    {
        var w = NewWorld();
        var hero = Hero(w, Vector2.Zero, new Dictionary<string, int> { [Blast.Id] = 2 });
        var a = Dummy(w, new Vector2(5, 0));
        var b = Dummy(w, new Vector2(6, 1));

        w.Enqueue(new UseAbilityIntent(hero.Id, Blast.Id, AimPoint: new Vector2(5.5f, 0.5f)));
        var hits = Run(w, 30);

        Assert.Equal(6, hits.Count);
        Assert.Equal(3, hits.Count(h => h.Target == a.Id));
        Assert.Equal(3, hits.Count(h => h.Target == b.Id));
    }

    [Fact]
    public void Extra_hits_stop_when_the_caster_dies()
    {
        var w = NewWorld();
        var hero = Hero(w, Vector2.Zero, new Dictionary<string, int> { [Blast.Id] = 2 });
        Dummy(w, new Vector2(5, 0));

        w.Enqueue(new UseAbilityIntent(hero.Id, Blast.Id, AimPoint: new Vector2(5, 0)));
        w.Step(); // primeiro acerto
        w.Kill(hero);
        var later = Run(w, 30);

        Assert.Empty(later);
    }

    [Fact]
    public void Cooldown_charges_allow_two_uses_then_come_back_one_at_a_time()
    {
        var w = NewWorld();
        var hero = Hero(w, new Vector2(5, 5), new Dictionary<string, int> { [Dash.Id] = 2 });
        var rejected = new List<AbilityRejected>();
        var executed = 0;

        void Use()
        {
            w.Enqueue(new UseAbilityIntent(hero.Id, Dash.Id));
            w.Step();
            rejected.AddRange(w.LastTickEvents.OfType<AbilityRejected>());
            executed += w.LastTickEvents.OfType<AbilityExecuted>().Count();
        }

        Use();
        Use();
        Use(); // sem cargas
        Assert.Equal(2, executed);
        Assert.Equal(AbilityRejectReason.OnCooldown, Assert.Single(rejected).Reason);

        // primeira carga volta 40 ticks depois do primeiro uso; a segunda, 40 depois disso
        while (w.Tick < 41)
        {
            w.Step();
        }

        Use();
        Assert.Equal(3, executed);
        Use();
        Assert.Equal(3, executed); // a segunda carga ainda está recarregando
    }

    [Fact]
    public void Rank_one_has_a_normal_cooldown_and_no_charges()
    {
        var w = NewWorld();
        var hero = Hero(w, new Vector2(5, 5), new Dictionary<string, int>());
        w.Enqueue(new UseAbilityIntent(hero.Id, Dash.Id));
        w.Step();
        w.Enqueue(new UseAbilityIntent(hero.Id, Dash.Id));
        w.Step();

        Assert.Equal(AbilityRejectReason.OnCooldown, Assert.Single(w.LastTickEvents.OfType<AbilityRejected>()).Reason);
        Assert.False(hero.Get<AbilityState>().HasCooldownChargesSpent);
    }

    [Fact]
    public void Ranks_from_the_encounter_reach_the_player_and_are_replicated()
    {
        var hero = new ClassDefinition
        {
            Id = "class.hero",
            Name = "Herói",
            Health = 100,
            Speed = 4,
            Radius = 0.4,
            Resource = new ResourceDefinition { Type = "mana", Max = 100, Initial = 100 },
            BasicAttack = Shot.Id,
            Abilities = [Blast.Id],
        };
        var content = new ContentCatalog(classes: [hero], abilities: [Shot, Blast, Dash]);
        var w = WorldBuilder.CreateDefault(1, null, content);

        var player = EntityFactory.SpawnPlayer(w, hero, Vector2.Zero, new Dictionary<string, int> { [Blast.Id] = 2, [Shot.Id] = 99 });
        var st = player.Get<AbilityState>();

        Assert.Equal(2, st.RankOf(Blast.Id));
        Assert.Equal(5, st.RankOf(Shot.Id)); // limitado ao máximo da habilidade
        Assert.Equal(1, st.RankOf(Dash.Id));

        var snapshot = w.Snapshot(SnapshotScope.Replicated).Entities.Single(e => e.Id == player.Id);
        Assert.Equal(2, snapshot.Get<AbilityState>().RankOf(Blast.Id));
    }
}
