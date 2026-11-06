using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Status;
using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-340: mecânicas dos monstros da Floresta Baixa. Status "não pode ser alvo" (<c>flags.untargetable</c>), fraqueza por
/// resistência negativa, e os perfis <c>ai.mist_kiter</c> (Fada da Névoa) e <c>ai.brute</c> (Ent Corrompido).
/// </summary>
public class ForestMonsterTests
{
    private static readonly StatusDefinition Mist = new()
    {
        Id = "s.mist",
        Name = "Névoa",
        DefaultDurationTicks = 40,
        Flags = new StatusFlags { Untargetable = true },
    };

    private static AbilityDefinition Ab(string id, TargetingDefinition targeting, TelegraphDefinition? telegraph = null) =>
        new() { Id = id, Name = id, Targeting = targeting, Telegraph = telegraph, Effects = [new DamageEffect { Base = 10 }] };

    private static Entity Unit(World w, Vector2 pos, Faction f, string[]? known = null, Dictionary<string, double>? resist = null)
    {
        var st = new AbilityState();
        st.Known.AddRange(known ?? []);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(100),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = f.ToString(), Resistances = resist ?? [] },
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

    private static List<GameEvent> Use(World w, Entity e, string ability, Entity? target = null, Vector2? aim = null)
    {
        w.Enqueue(new UseAbilityIntent(e.Id, ability, target?.Id, aim));
        return Run(w, 1);
    }

    // ---------- não pode ser alvo ----------

    [Fact]
    public void Untargetable_enemy_cannot_be_chosen_as_unit_target_until_the_status_ends()
    {
        var shot = Ab("hero.shot", new UnitTargeting { Range = 10, Faction = FactionFilter.Enemy });
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [shot], statuses: [Mist]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.shot"]);
        var fairy = Unit(w, new Vector2(5, 1), Faction.Monster);
        StatusManager.Apply(w, fairy, fairy, "s.mist");

        Assert.True(StatusManager.IsUntargetable(w, fairy));
        Assert.Equal(AbilityRejectReason.InvalidTarget, AbilityValidator.Validate(w, hero, "hero.shot", fairy.Id, null).Reason);
        Use(w, hero, "hero.shot", fairy);
        Assert.Equal(100, fairy.Get<Health>().Current);

        Run(w, 40); // a névoa dura 2 s
        Assert.False(StatusManager.IsUntargetable(w, fairy));
        Use(w, hero, "hero.shot", fairy);
        Assert.Equal(90, fairy.Get<Health>().Current);
    }

    [Fact]
    public void Untargetable_entity_can_still_target_itself()
    {
        var self = new AbilityDefinition
        {
            Id = "fairy.self",
            Name = "x",
            Targeting = new SelfTargeting(),
            Effects = [new HealEffect { Base = 5 }],
        };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [self], statuses: [Mist]));
        var fairy = Unit(w, new Vector2(5, 1), Faction.Monster, known: ["fairy.self"]);
        fairy.Get<Health>().Current = 50;
        StatusManager.Apply(w, fairy, fairy, "s.mist");

        Use(w, fairy, "fairy.self", fairy);

        Assert.Equal(55, fairy.Get<Health>().Current);
    }

    [Fact]
    public void Area_ability_skips_untargetable_enemies_and_hits_the_others()
    {
        var nova = Ab("hero.nova", new CircleSelfTargeting { Radius = 5 });
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [nova], statuses: [Mist]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.nova"]);
        var hidden = Unit(w, new Vector2(3, 1), Faction.Monster);
        var visible = Unit(w, new Vector2(3, 3), Faction.Monster);
        StatusManager.Apply(w, hidden, hidden, "s.mist");

        Use(w, hero, "hero.nova");

        Assert.Equal(100, hidden.Get<Health>().Current);
        Assert.Equal(90, visible.Get<Health>().Current);
    }

    [Fact]
    public void Telegraph_resolving_while_the_enemy_is_untargetable_misses_it()
    {
        var slam = Ab(
            "hero.slam",
            new CircleSelfTargeting { Radius = 3 },
            new TelegraphDefinition { Shape = TelegraphShape.Circle, Radius = 3, DelayTicks = 10 });
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [slam], statuses: [Mist]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.slam"]);
        var fairy = Unit(w, new Vector2(2, 1), Faction.Monster);

        Use(w, hero, "hero.slam");
        StatusManager.Apply(w, fairy, fairy, "s.mist"); // some antes de o telegráfo resolver
        var ev = Run(w, 12);

        Assert.Contains(ev, e => e is TelegraphResolved);
        Assert.Equal(100, fairy.Get<Health>().Current);
    }

    [Fact]
    public void Persistent_area_does_not_affect_untargetable_entities()
    {
        var pool = new AreaDefinition
        {
            Id = "area.pool",
            Radius = 2,
            DurationTicks = 100,
            IntervalTicks = 5,
            Effects = [new DamageEffect { Base = 4 }],
        };
        var cast = new AbilityDefinition
        {
            Id = "hero.pool",
            Name = "x",
            Targeting = new CircleAtPointTargeting { Range = 10, Radius = 2 },
            Effects = [new SpawnAreaEffect { Area = "area.pool" }],
        };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [cast], areas: [pool], statuses: [Mist]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.pool"]);
        var fairy = Unit(w, new Vector2(5, 1), Faction.Monster);
        StatusManager.Apply(w, fairy, fairy, "s.mist");

        Use(w, hero, "hero.pool", aim: new Vector2(5, 1));
        Run(w, 20); // dentro da névoa (2 s)

        Assert.Equal(100, fairy.Get<Health>().Current);
    }

    // ---------- resistência e fraqueza ----------

    [Theory]
    [InlineData(DamageSchool.Physical, 75)] // resistência 0,25
    [InlineData(DamageSchool.Fire, 130)] // fraqueza: resistência −0,3
    [InlineData(DamageSchool.Holy, 100)]
    public void Negative_resistance_is_a_weakness(DamageSchool school, float expected)
    {
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog());
        var ent = Unit(w, new Vector2(1, 1), Faction.Monster, resist: new() { ["physical"] = 0.25, ["fire"] = -0.3 });
        ent.Get<Health>().Max = 1000;
        ent.Get<Health>().Current = 1000;

        var dealt = DamageResolver.Deal(w, null, ent, "test", 100, school);

        Assert.Equal(expected, dealt);
    }

    // ---------- dados reais ----------

    [Fact]
    public void Corrupted_ent_is_tagged_for_the_cleric_bonus_and_resists_physical_but_burns()
    {
        var ent = RealContent.Value.Monsters["monster.corrupted_ent"];
        Assert.Contains("corrupted", ent.Tags);
        Assert.Equal(7, ent.Level);
        Assert.True(ent.Resistances["physical"] > 0);
        Assert.True(ent.Resistances["fire"] < 0);
        Assert.Equal("ai.brute", ent.Ai.Id);

        var fairy = RealContent.Value.Monsters["monster.mist_fairy"];
        Assert.Equal(6, fairy.Level);
        Assert.InRange(fairy.Radius, 0.35, 0.40);
        Assert.Equal("ai.mist_kiter", fairy.Ai.Id);
        Assert.True(RealContent.Value.Statuses["status.in_mist"].Flags.Untargetable);
    }

    // ---------- ai.mist_kiter ----------

    [Fact]
    public void Mist_fairy_vanishes_after_a_visible_cast_when_a_player_comes_close_and_does_not_attack_in_the_mist()
    {
        var c = RealContent.Value;
        var w = WorldBuilder.CreateDefault(1, null, c);
        var fairy = EntityFactory.SpawnMonster(w, c.Monsters["monster.mist_fairy"], new Vector2(10, 10));
        Unit(w, new Vector2(12, 10), Faction.Player); // a 2 tiles: perto demais

        var ev = Run(w, 3);
        var started = Assert.Single(ev.OfType<AbilityStarted>(), e => e.AbilityId == "mist_fairy.mist_veil");
        Assert.True(started.CastTicks > 0, "o véu tem conjuração: é o aviso antes de sumir");
        Assert.False(StatusManager.IsUntargetable(w, fairy));

        ev = Run(w, started.CastTicks + 1);
        Assert.True(StatusManager.IsUntargetable(w, fairy));

        ev = Run(w, 30); // ainda na névoa (2 s)
        Assert.DoesNotContain(ev, e => e is AbilityStarted a && a.Source == fairy.Id);
        Assert.True(StatusManager.IsUntargetable(w, fairy));
    }

    [Fact]
    public void Mist_fairy_shoots_from_afar_and_slows()
    {
        var c = RealContent.Value;
        var w = WorldBuilder.CreateDefault(1, null, c);
        EntityFactory.SpawnMonster(w, c.Monsters["monster.mist_fairy"], new Vector2(10, 10));
        var hero = Unit(w, new Vector2(17, 10), Faction.Player);

        var ev = Run(w, 5);

        Assert.Contains(ev, e => e is AbilityStarted { AbilityId: "mist_fairy.mist_bolt" });
        Assert.True(StatusManager.Has(w, hero, "status.mist_chill"));
    }

    // ---------- ai.brute ----------

    [Fact]
    public void Ent_does_not_slam_the_air_against_a_distant_target_but_throws_roots_and_thorns()
    {
        var c = RealContent.Value;
        var w = WorldBuilder.CreateDefault(1, null, c);
        EntityFactory.SpawnMonster(w, c.Monsters["monster.corrupted_ent"], new Vector2(10, 10));
        Unit(w, new Vector2(16, 10), Faction.Player); // longe do pisão (2,6) e dentro das raízes (9)

        var ev = Run(w, 40);
        var used = ev.OfType<AbilityStarted>().Select(a => a.AbilityId).ToList();

        Assert.DoesNotContain("corrupted_ent.trampling_slam", used);
        Assert.DoesNotContain("corrupted_ent.branch_lash", used);
        Assert.Equal("corrupted_ent.grasping_roots", used.First());
        Assert.Contains("corrupted_ent.thorn_volley", used);
    }

    [Fact]
    public void Ent_slams_around_itself_when_the_target_is_close_and_stands_still_until_it_resolves()
    {
        var c = RealContent.Value;
        var w = WorldBuilder.CreateDefault(1, null, c);
        var ent = EntityFactory.SpawnMonster(w, c.Monsters["monster.corrupted_ent"], new Vector2(10, 10));
        var hero = Unit(w, new Vector2(11.5f, 10), Faction.Player);
        hero.Get<Health>().Max = 10_000;
        hero.Get<Health>().Current = 10_000;

        var ev = Run(w, 3);
        var slam = Assert.Single(ev.OfType<TelegraphStarted>(), t => t.AbilityId == "corrupted_ent.trampling_slam");
        Assert.Equal(24, slam.ResolveAtTick - slam.Tick); // 1,2 s
        var before = ent.Get<Transform>().Position;

        hero.Get<Transform>().Position = new Vector2(14, 10); // o alvo se afasta durante o telegráfo
        ev = Run(w, (int)(slam.ResolveAtTick - w.Tick));

        Assert.Equal(before, ent.Get<Transform>().Position);
        Assert.Contains(ev, e => e is TelegraphResolved r && r.AbilityId == "corrupted_ent.trampling_slam");
        Assert.DoesNotContain(ev, e => e is DamageDealt { SourceId: "corrupted_ent.trampling_slam" });
    }
}
