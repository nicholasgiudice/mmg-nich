using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-203: curva de XP, XP por monstro e em grupo, subir de nível, pontos de atributo e de habilidade, redistribuição.</summary>
public class ProgressionTests
{
    private static readonly ContentCatalog Content = RealContent.Value;
    private static readonly ProgressionRules Rules = Content.Rules.Progression;

    private static World NewWorld() => WorldBuilder.CreateDefault(11, null, Content);

    private static Entity Hero(World w, string cls = "class.warrior", CharacterBuild? build = null, float x = 1, float y = 1) =>
        Leveling.SpawnCharacter(w, Content.Classes[cls], new Vector2(x, y), build);

    private static Entity Monster(World w, string id, float health, float x = 2)
    {
        var m = EntityFactory.SpawnMonster(w, Content.Monsters[id], new Vector2(x, 1));
        var h = m.Get<Health>();
        h.Max = health;
        h.Current = health;
        m.Get<Movement>().Speed = 0;
        return m;
    }

    private static List<GameEvent> Step(World w, params Intent[] intents)
    {
        foreach (var i in intents)
        {
            w.Enqueue(i);
        }

        w.Step();
        return w.LastTickEvents.ToList();
    }

    /// <summary>Usa o ataque básico no alvo até ele morrer (ou o limite de ticks); devolve os eventos.</summary>
    private static List<GameEvent> AttackUntilDead(World w, Entity attacker, Entity target, int maxTicks = 200)
    {
        var events = new List<GameEvent>();
        for (var t = 0; t < maxTicks && target.IsAlive; t++)
        {
            events.AddRange(Step(w, new UseAbilityIntent(attacker.Id, attacker.Get<AbilityState>().BasicAttackId!, target.Id)));
        }

        events.AddRange(Step(w));
        Assert.False(target.IsAlive);
        return events;
    }

    // ---------- curva e fórmulas ----------

    [Fact]
    public void Xp_curve_follows_the_design_table()
    {
        Assert.Equal(50, Rules.XpToNext(1));
        Assert.Equal(174, Rules.XpToNext(2));
        Assert.Equal(361, Rules.XpToNext(3));
        Assert.Equal(3155, Rules.XpToNext(10));
        Assert.Equal(5781, Rules.XpToNext(14));
        Assert.Equal(0, Rules.XpToNext(15));
        Assert.Equal(31856, Enumerable.Range(1, 14).Sum(Rules.XpToNext));
    }

    [Theory]
    [InlineData(5, 5, 1.0)]
    [InlineData(6, 5, 1.1)]
    [InlineData(8, 5, 1.3)]
    [InlineData(12, 5, 1.3)]
    [InlineData(4, 5, 0.8)]
    [InlineData(1, 5, 0.2)]
    [InlineData(1, 6, 0.0)]
    [InlineData(1, 15, 0.0)]
    public void Level_difference_modifier(int monster, int character, double expected) =>
        Assert.Equal(expected, Rules.LevelDifferenceModifier(monster, character), 6);

    [Fact]
    public void Monster_xp_uses_level_and_category()
    {
        Assert.Equal(12, Rules.MonsterXp(2, MonsterRank.Normal, 2), 6);
        Assert.Equal(6 * 6 * 5, Rules.MonsterXp(6, MonsterRank.Elite, 6), 6);
        Assert.Equal(6 * 8 * 30 * 1.3, Rules.MonsterXp(8, MonsterRank.Boss, 5), 6);
    }

    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(100, 2, 55)]
    [InlineData(100, 3, 40)]
    [InlineData(100, 4, 33)]
    [InlineData(6, 2, 3)]
    public void Group_share_divides_with_bonus_per_extra_member(double xp, int members, int expected) =>
        Assert.Equal(expected, Rules.Share(xp, members));

    // ---------- XP ao matar ----------

    [Fact]
    public void Killing_a_monster_gives_xp_to_who_damaged_it()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild { Level = 2 });
        var wolf = Monster(w, "monster.grey_wolf", 1);

        var events = AttackUntilDead(w, hero, wolf);

        var xp = Assert.Single(events.OfType<XpGained>());
        Assert.Equal(hero.Id, xp.Entity);
        Assert.Equal(wolf.Id, xp.Monster);
        Assert.Equal("monster.grey_wolf", xp.SourceId);
        Assert.Equal(12, xp.Amount); // 6 × nível 2 × normal, mesmo nível
        Assert.Equal(12, hero.Get<CharacterProgress>().Experience);
    }

    [Fact]
    public void Monsters_far_below_give_nothing()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild { Level = 6 });
        var rat = Monster(w, "monster.field_rat", 1);

        var events = AttackUntilDead(w, hero, rat);

        Assert.Empty(events.OfType<XpGained>());
        Assert.Equal(0, hero.Get<CharacterProgress>().Experience);
    }

    [Fact]
    public void Group_xp_is_split_and_healers_of_the_fighters_take_part()
    {
        var w = NewWorld();
        var a = Hero(w, x: 1);
        var b = Hero(w, x: 3);
        var cleric = Hero(w, "class.cleric", new CharacterBuild { SkillRanks = new Dictionary<string, int> { ["cleric.direct_heal"] = 1 } }, x: 2, y: 4);
        var idle = Hero(w, x: 2, y: 6); // perto, mas não luta nem ajuda
        var rat = Monster(w, "monster.field_rat", 1000, x: 2);

        Step(w, new UseAbilityIntent(a.Id, "warrior.sword_strike", rat.Id), new UseAbilityIntent(b.Id, "warrior.sword_strike", rat.Id));
        Step(w, new UseAbilityIntent(cleric.Id, "cleric.direct_heal", a.Id));
        rat.Get<Health>().Current = 1;
        var events = AttackUntilDead(w, a, rat);

        var gains = events.OfType<XpGained>().ToDictionary(g => g.Entity, g => g.Amount);
        var each = Rules.Share(Rules.MonsterXp(1, MonsterRank.Normal, 1), 3); // 6 × 1,2 / 3 = 2,4 → 2
        Assert.Equal(2, each);
        Assert.Equal(each, gains[a.Id]);
        Assert.Equal(each, gains[b.Id]);
        Assert.Equal(each, gains[cleric.Id]);
        Assert.False(gains.ContainsKey(idle.Id));
    }

    [Fact]
    public void Damage_outside_the_participation_window_does_not_count()
    {
        var w = NewWorld();
        var early = Hero(w, x: 1);
        var late = Hero(w, x: 3);
        var rat = Monster(w, "monster.field_rat", 1000);

        Step(w, new UseAbilityIntent(early.Id, "warrior.sword_strike", rat.Id));
        for (var t = 0; t <= Rules.ParticipationWindowTicks; t++)
        {
            w.Step();
        }

        rat.Get<Health>().Current = 1;
        var events = AttackUntilDead(w, late, rat);

        var gain = Assert.Single(events.OfType<XpGained>());
        Assert.Equal(late.Id, gain.Entity);
        Assert.Equal(6, gain.Amount);
    }

    [Fact]
    public void Encounters_without_progression_are_unchanged()
    {
        var setup = EncounterSetup.Create(3, Content, Content.Encounters["encounter.three_wolves"]);
        var w = setup.World;
        var player = setup.Players[0];
        var wolf = setup.Enemies[0];
        wolf.Get<Health>().Current = 1;
        player.Get<Transform>().Position = wolf.Get<Transform>().Position + new Vector2(0.9f, 0);

        var events = AttackUntilDead(w, player, wolf);

        Assert.Null(player.TryGet<CharacterProgress>());
        Assert.Null(wolf.TryGet<XpContributors>());
        Assert.Empty(events.OfType<XpGained>());
        Assert.Contains(player.Get<AbilityState>().Known, id => id == "warrior.heavy_strike"); // NH1 de tudo, como no protótipo
    }

    // ---------- subir de nível ----------

    [Fact]
    public void Leveling_up_gives_points_health_and_publishes_the_event()
    {
        var w = NewWorld();
        var hero = Hero(w);
        var health = hero.Get<Health>();
        Assert.Equal(240, health.Max);

        Leveling.GrantExperience(w, hero, 60, EntityId.None, "test");
        w.Step();

        var p = hero.Get<CharacterProgress>();
        Assert.Equal(2, hero.Get<CharacterStats>().Level);
        Assert.Equal(10, p.Experience);
        Assert.Equal(174, p.ExperienceToNext);
        Assert.Equal(5, p.AttributePoints);
        Assert.Equal(2, p.SkillPoints);
        Assert.Equal(254, health.Max); // + crescimento do Guerreiro (14)
        Assert.Equal(254, health.Current);
        var up = Assert.Single(w.LastTickEvents.OfType<LevelUp>());
        Assert.Equal((hero.Id, 2, 5, 2), (up.Entity, up.Level, up.AttributePoints, up.SkillPoints));
    }

    [Fact]
    public void Big_xp_climbs_several_levels_and_stops_at_the_cap()
    {
        var w = NewWorld();
        var hero = Hero(w, "class.mage");

        Leveling.GrantExperience(w, hero, 224, EntityId.None, "test");
        Assert.Equal(3, hero.Get<CharacterStats>().Level);
        Assert.Equal(0, hero.Get<CharacterProgress>().Experience);

        Leveling.GrantExperience(w, hero, 1_000_000, EntityId.None, "test");
        w.Step();
        var p = hero.Get<CharacterProgress>();
        Assert.Equal(15, hero.Get<CharacterStats>().Level);
        Assert.Equal(0, p.Experience);
        Assert.Equal(0, p.ExperienceToNext);
        Assert.Equal(70, p.AttributePoints);
        Assert.Equal(15, p.SkillPoints);
        Assert.Equal(14, w.LastTickEvents.OfType<LevelUp>().Count());
        Assert.Equal(0, Leveling.GrantExperience(w, hero, 100, EntityId.None, "test"));
    }

    [Fact]
    public void Character_level_counts_in_the_defense_formula_and_monsters_have_their_level()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild { Level = 10 });
        var guard = Monster(w, "monster.skeleton_guard", 100);
        var dummy = Monster(w, "monster.field_rat", 100);

        Assert.Equal(9, guard.Get<UnitInfo>().Level);
        Assert.Equal(9, StatMath.LevelOf(guard));
        Assert.Equal(10, StatMath.LevelOf(hero));
        hero.Get<CharacterStats>().Armor = 60;
        Assert.Equal(60.0 / (60 + 40 + (8 * 9)), StatMath.DefenseReduction(w, hero, guard, DamageSchool.Physical), 6);
        Assert.Equal(60.0 / (60 + 40 + 8), StatMath.DefenseReduction(w, hero, dummy, DamageSchool.Physical), 6);
    }

    // ---------- pontos de atributo ----------

    [Fact]
    public void Spending_attribute_points_raises_the_stat_and_its_derived_values()
    {
        var w = NewWorld();
        var hero = Hero(w, "class.mage", new CharacterBuild { Level = 2 });
        var stats = hero.Get<CharacterStats>();
        var health = hero.Get<Health>().Max;
        var mana = hero.Get<ResourcePool>().Max;
        var intBefore = stats.Int;

        var events = Step(w, new SpendAttributePointIntent(hero.Id, AttributeKind.Vit), new SpendAttributePointIntent(hero.Id, AttributeKind.Int));

        Assert.Equal(health + 10, hero.Get<Health>().Max);
        Assert.Equal(hero.Get<Health>().Max, hero.Get<Health>().Current);
        Assert.Equal(mana + 2, hero.Get<ResourcePool>().Max);
        Assert.Equal(intBefore + 1, stats.Int);
        var p = hero.Get<CharacterProgress>();
        Assert.Equal(3, p.AttributePoints);
        Assert.Equal(1, p.AllocatedVit);
        Assert.Equal(1, p.AllocatedInt);
        Assert.Contains(events, e => e is AttributeRaised { Attribute: AttributeKind.Int } r && r.Entity == hero.Id && r.Value == intBefore + 1);
    }

    [Fact]
    public void Attribute_points_run_out()
    {
        var w = NewWorld();
        var hero = Hero(w);

        var events = Step(w, new SpendAttributePointIntent(hero.Id, AttributeKind.Str));

        var rejected = Assert.Single(events.OfType<ProgressionRejected>());
        Assert.Equal((hero.Id, "str", ProgressionRejectReason.NoAttributePoints), (rejected.Entity, rejected.Id, rejected.Reason));
        Assert.Equal(12, hero.Get<CharacterStats>().Str);
    }

    // ---------- pontos de habilidade e NH ----------

    [Fact]
    public void A_new_character_has_only_the_basic_attack_and_one_skill_point()
    {
        var w = NewWorld();
        var hero = Hero(w);
        var st = hero.Get<AbilityState>();

        Assert.Equal(["warrior.sword_strike"], st.Known);
        Assert.Equal(1, hero.Get<CharacterProgress>().SkillPoints);
        Assert.Contains(Step(w, new UseAbilityIntent(hero.Id, "warrior.heavy_strike")).OfType<AbilityRejected>(), r => r.Reason == AbilityRejectReason.UnknownAbility);

        var events = Step(w, new RaiseSkillRankIntent(hero.Id, "warrior.heavy_strike"));

        Assert.Contains(events, e => e is SkillRankRaised { AbilityId: "warrior.heavy_strike", Rank: 1 });
        Assert.Equal(["warrior.sword_strike", "warrior.heavy_strike"], st.Known);
        Assert.Equal(1, st.RankOf("warrior.heavy_strike"));
        Assert.Equal(0, hero.Get<CharacterProgress>().SkillPoints);
        Assert.Equal(ProgressionRejectReason.NoSkillPoints, Leveling.RaiseSkillRank(w, hero, "warrior.taunt"));
    }

    [Fact]
    public void Learned_skills_stay_in_class_order_on_the_bar()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild { Level = 3 });

        Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.taunt"));
        Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.heavy_strike"));

        Assert.Equal(["warrior.sword_strike", "warrior.heavy_strike", "warrior.taunt"], hero.Get<AbilityState>().Known);
    }

    [Fact]
    public void Skill_ranks_need_the_character_level()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild { Level = 4 }); // 4 pontos

        Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.heavy_strike"));
        Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.heavy_strike")); // NH2 exige nível 3
        Assert.Equal(ProgressionRejectReason.LevelTooLow, Leveling.RaiseSkillRank(w, hero, "warrior.heavy_strike")); // NH3 exige 5
        Assert.Equal(ProgressionRejectReason.LevelTooLow, Leveling.RaiseSkillRank(w, hero, "warrior.living_wall")); // assinatura: 12
        Assert.Equal(2, hero.Get<AbilityState>().RankOf("warrior.heavy_strike"));
        Assert.Equal(2, hero.Get<CharacterProgress>().SkillPoints);
    }

    [Fact]
    public void Signature_ranks_follow_their_own_levels_and_max()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild { Level = 13 });

        Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.living_wall"));
        Assert.Contains("warrior.living_wall", hero.Get<AbilityState>().Known); // na barra a partir do NH1
        Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.living_wall"));
        Assert.Equal(ProgressionRejectReason.LevelTooLow, Leveling.RaiseSkillRank(w, hero, "warrior.living_wall"));

        Leveling.GrantExperience(w, hero, 100_000, EntityId.None, "test");
        Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.living_wall"));
        Assert.Equal(ProgressionRejectReason.MaxRank, Leveling.RaiseSkillRank(w, hero, "warrior.living_wall"));
    }

    [Fact]
    public void Normal_skills_stop_at_their_max_rank()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild { Level = 15 });

        for (var i = 0; i < 5; i++)
        {
            Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.heavy_strike"));
        }

        Assert.Equal(ProgressionRejectReason.MaxRank, Leveling.RaiseSkillRank(w, hero, "warrior.heavy_strike"));
        Assert.Equal(5, hero.Get<AbilityState>().RankOf("warrior.heavy_strike"));
    }

    [Fact]
    public void Basic_attack_and_other_class_skills_cannot_take_points()
    {
        var w = NewWorld();
        var hero = Hero(w);

        Assert.Equal(ProgressionRejectReason.NotLearnable, Leveling.RaiseSkillRank(w, hero, "warrior.sword_strike"));
        Assert.Equal(ProgressionRejectReason.NotLearnable, Leveling.RaiseSkillRank(w, hero, "mage.elemental_blast"));
        Assert.Equal(ProgressionRejectReason.NotLearnable, Leveling.RaiseSkillRank(w, hero, "item.minor_healing_potion"));
        Assert.Equal(ProgressionRejectReason.NotLearnable, Leveling.RaiseSkillRank(w, hero, "nope"));
    }

    [Fact]
    public void Characters_without_progression_reject_point_intents()
    {
        var w = NewWorld();
        var plain = EntityFactory.SpawnPlayer(w, Content.Classes["class.warrior"], new Vector2(1, 1));

        var events = Step(w, new RaiseSkillRankIntent(plain.Id, "warrior.heavy_strike"), new ResetPointsIntent(plain.Id));

        Assert.All(events.OfType<ProgressionRejected>(), r => Assert.Equal(ProgressionRejectReason.NoProgression, r.Reason));
        Assert.Equal(2, events.OfType<ProgressionRejected>().Count());
    }

    // ---------- redistribuição ----------

    [Fact]
    public void Reset_returns_every_point_out_of_combat()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild
        {
            Level = 5,
            Allocated = new AttributeSet { Str = 10, Vit = 5 },
            SkillRanks = new Dictionary<string, int> { ["warrior.heavy_strike"] = 3, ["warrior.taunt"] = 1 },
        });
        var fresh = Hero(w, build: new CharacterBuild { Level = 5 });
        Assert.Equal(22, hero.Get<CharacterStats>().Str);
        Assert.Equal(fresh.Get<Health>().Max + 50, hero.Get<Health>().Max);

        var events = Step(w, new ResetPointsIntent(hero.Id));

        Assert.Contains(events, e => e is PointsReset { AttributePoints: 20, SkillPoints: 5 } r && r.Entity == hero.Id);
        var p = hero.Get<CharacterProgress>();
        Assert.Equal(20, p.AttributePoints);
        Assert.Equal(5, p.SkillPoints);
        Assert.Equal(0, p.AllocatedTotal);
        Assert.Equal(12, hero.Get<CharacterStats>().Str);
        Assert.Equal(fresh.Get<Health>().Max, hero.Get<Health>().Max);
        Assert.Equal(["warrior.sword_strike"], hero.Get<AbilityState>().Known);
        Assert.Equal(1, hero.Get<AbilityState>().RankOf("warrior.heavy_strike")); // sem NH guardado
        Assert.Equal(Leveling.BuildOf(fresh), Leveling.BuildOf(hero), BuildComparer.Instance);
    }

    [Fact]
    public void Reset_is_refused_in_combat_and_allowed_after_the_timeout()
    {
        var w = NewWorld();
        var hero = Hero(w, build: new CharacterBuild { Level = 2, Allocated = new AttributeSet { Str = 5 } });
        var rat = Monster(w, "monster.field_rat", 1000);

        Step(w, new UseAbilityIntent(hero.Id, "warrior.sword_strike", rat.Id));
        var refused = Step(w, new ResetPointsIntent(hero.Id));
        hero.Get<Transform>().Position = new Vector2(40, 1); // longe do rato (parado), sai de combate
        Assert.Contains(refused, e => e is ProgressionRejected { Id: "", Reason: ProgressionRejectReason.InCombat } r && r.Entity == hero.Id);
        Assert.Equal(5, hero.Get<CharacterProgress>().AllocatedStr);

        for (var t = 0; t < Rules.CombatTimeoutTicks; t++)
        {
            w.Step();
        }

        Assert.Contains(Step(w, new ResetPointsIntent(hero.Id)), e => e is PointsReset);
        Assert.Equal(0, hero.Get<CharacterProgress>().AllocatedStr);
    }

    // ---------- criar e salvar ----------

    [Theory]
    [InlineData("class.warrior")]
    [InlineData("class.archer")]
    [InlineData("class.mage")]
    [InlineData("class.cleric")]
    public void Level_one_with_no_points_spent_has_todays_numbers(string cls)
    {
        var w = NewWorld();
        var today = EntityFactory.SpawnPlayer(w, Content.Classes[cls], new Vector2(1, 1));
        var hero = Hero(w, cls);

        Assert.Equal(today.Get<Health>().Max, hero.Get<Health>().Max);
        Assert.Equal(today.Get<Health>().Current, hero.Get<Health>().Current);
        Assert.Equal(today.Get<ResourcePool>().Max, hero.Get<ResourcePool>().Max);
        Assert.Equal(today.Get<ResourcePool>().Current, hero.Get<ResourcePool>().Current);
        var a = new StateHasher();
        today.Get<CharacterStats>().WriteHash(a);
        var b = new StateHasher();
        hero.Get<CharacterStats>().WriteHash(b);
        Assert.Equal(a.Value, b.Value);
    }

    [Fact]
    public void Build_round_trips_through_spawn()
    {
        var w = NewWorld();
        var build = new CharacterBuild
        {
            Level = 7,
            Experience = 100,
            Allocated = new AttributeSet { Int = 20, Vit = 6, Agi = 4 },
            SkillRanks = new Dictionary<string, int> { ["mage.elemental_blast"] = 4, ["mage.ice_prison"] = 2, ["mage.arcane_barrier"] = 1 },
        };

        var hero = Hero(w, "class.mage", build);

        Assert.Equal(build, Leveling.BuildOf(hero), BuildComparer.Instance);
        var p = hero.Get<CharacterProgress>();
        Assert.Equal(0, p.AttributePoints);
        Assert.Equal(0, p.SkillPoints);
        Assert.Equal(1660, p.ExperienceToNext);
        Assert.Equal(13 + 20, hero.Get<CharacterStats>().Int);
        Assert.Equal(4, hero.Get<AbilityState>().RankOf("mage.elemental_blast"));
        Assert.Equal(hero.Get<Health>().Max, hero.Get<Health>().Current);
    }

    [Theory]
    [InlineData(0, 0, 0, null, 0)]
    [InlineData(16, 0, 0, null, 0)]
    [InlineData(1, 50, 0, null, 0)]
    [InlineData(2, 0, 6, null, 0)]
    [InlineData(2, 0, 0, "warrior.heavy_strike", 3)]
    [InlineData(5, 0, 0, "warrior.heavy_strike", 4)]
    [InlineData(1, 0, 0, "warrior.sword_strike", 1)]
    [InlineData(1, 0, 0, "warrior.heavy_strike", -1)]
    [InlineData(11, 0, 0, "warrior.living_wall", 1)]
    public void Invalid_builds_are_refused(int level, int xp, int str, string? skill, int rank)
    {
        var build = new CharacterBuild
        {
            Level = level,
            Experience = xp,
            Allocated = new AttributeSet { Str = str },
            SkillRanks = skill is null ? new Dictionary<string, int>() : new Dictionary<string, int> { [skill] = rank },
        };

        Assert.NotNull(Leveling.Validate(Content, Content.Classes["class.warrior"], build));
        Assert.Throws<ArgumentException>(() => Hero(NewWorld(), build: build));
    }

    [Fact]
    public void Too_many_skill_points_are_refused()
    {
        var build = new CharacterBuild
        {
            Level = 3,
            SkillRanks = new Dictionary<string, int> { ["warrior.heavy_strike"] = 2, ["warrior.taunt"] = 1, ["warrior.whirlwind"] = 1 },
        };

        Assert.Equal("mais pontos de habilidade do que o nível dá", Leveling.Validate(Content, Content.Classes["class.warrior"], build));
    }

    /// <summary>Compara builds pelo conteúdo (records com dicionário comparam por referência).</summary>
    private sealed class BuildComparer : IEqualityComparer<CharacterBuild?>
    {
        public static readonly BuildComparer Instance = new();

        public bool Equals(CharacterBuild? x, CharacterBuild? y) =>
            x is not null && y is not null && x.Level == y.Level && x.Experience == y.Experience && x.Allocated == y.Allocated
            && x.SkillRanks.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key).SequenceEqual(y.SkillRanks.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key));

        public int GetHashCode(CharacterBuild? obj) => 0;
    }
}
