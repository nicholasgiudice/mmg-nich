using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;

namespace Game.Core.Tests;

/// <summary>
/// T-403: segunda classe (escolha, pontos, NH, barra, redistribuição, gravar e criar) e habilidades passivas (estado
/// permanente). Conteúdo real com habilidades de teste na segunda classe do Guerreiro (as de verdade são da T-408).
/// </summary>
public class SecondClassTests
{
    private const string Second = "second_class.test_a";
    private const string Strike = "test.sc_strike";
    private const string Shout = "test.sc_shout";
    private const string Passive = "test.sc_passive";
    private const string Signature = "test.sc_signature";
    private const string Status1 = "status.test_passive_1";
    private const string Status2 = "status.test_passive_2";

    private static readonly ContentCatalog Content = BuildContent();
    private static readonly ProgressionRules Rules = Content.Rules.Progression;
    private static readonly ClassDefinition Warrior = Content.Classes["class.warrior"];

    private static ContentCatalog BuildContent()
    {
        var real = RealContent.Value;
        static AbilityDefinition Normal(string id) => new()
        {
            Id = id,
            Name = id,
            Targeting = new SelfTargeting(),
            Ranks = [.. Enumerable.Range(2, 4).Select(r => new AbilityRankDefinition { Rank = r })],
        };

        AbilityDefinition[] extra =
        [
            Normal(Strike),
            Normal(Shout),
            new()
            {
                Id = Passive,
                Name = "Passiva de teste",
                Targeting = new SelfTargeting(),
                Passive = true,
                PassiveStatus = Status1,
                Ranks = [new AbilityRankDefinition { Rank = 2, PassiveStatus = Status2 }, new AbilityRankDefinition { Rank = 3 }],
            },
            new()
            {
                Id = Signature,
                Name = Signature,
                Targeting = new SelfTargeting(),
                Signature = true,
                Ranks = [new AbilityRankDefinition { Rank = 2 }, new AbilityRankDefinition { Rank = 3 }],
            },
        ];
        StatusDefinition[] statuses =
        [
            new() { Id = Status1, Name = "Passiva 1", Tags = ["buff"], DefaultDurationTicks = 20, Modifiers = new StatModifiers { DamageDealtMultiplier = 1.1 } },
            new() { Id = Status2, Name = "Passiva 2", Tags = ["buff"], DefaultDurationTicks = 20, Modifiers = new StatModifiers { DamageDealtMultiplier = 1.2 } },
        ];

        var classes = real.Classes.Values.Select(c => c.Id switch
        {
            "class.warrior" => c with
            {
                SecondClasses =
                [
                    new SecondClassDefinition { Id = Second, Name = "Teste A", Abilities = [Strike, Passive, Shout, Signature] },
                    new SecondClassDefinition { Id = "second_class.test_b", Name = "Teste B" },
                ],
            },
            "class.archer" => c with { SecondClasses = [new SecondClassDefinition { Id = "second_class.test_archer", Name = "Arqueiro B" }] },
            _ => c,
        });

        return new ContentCatalog(classes, real.Abilities.Values.Concat(extra), real.Statuses.Values.Concat(statuses), real.Areas.Values,
            real.Monsters.Values, real.Arenas.Values, real.Encounters.Values, real.Items.Values)
        {
            Rules = real.Rules,
            Threat = real.Threat,
            Zones = real.Zones,
            Shops = real.Shops,
        };
    }

    private static World NewWorld() => WorldBuilder.CreateDefault(7, null, Content);

    private static Entity Hero(World w, int level, string? second = null, Dictionary<string, int>? ranks = null) =>
        Leveling.SpawnCharacter(w, Warrior, new Vector2(1, 1), new CharacterBuild
        {
            Level = level,
            SecondClass = second,
            SkillRanks = ranks ?? new Dictionary<string, int>(),
        });

    private static List<GameEvent> Step(World w, params Intent[] intents)
    {
        foreach (var i in intents)
        {
            w.Enqueue(i);
        }

        w.Step();
        return w.LastTickEvents.ToList();
    }

    private static CharacterBuild Build(int level, Dictionary<string, int> ranks, string? second = Second) =>
        new() { Level = level, SecondClass = second, SkillRanks = ranks };

    // ---------- escolha ----------

    [Fact]
    public void Choosing_at_level_19_is_refused()
    {
        var w = NewWorld();
        var hero = Hero(w, 19);

        var rejected = Assert.Single(Step(w, new ChooseSecondClassIntent(hero.Id, Second)).OfType<ProgressionRejected>());

        Assert.Equal((Second, ProgressionRejectReason.LevelTooLow), (rejected.Id, rejected.Reason));
        Assert.Null(hero.Get<CharacterProgress>().SecondClass);
    }

    [Fact]
    public void Choosing_at_level_20_gives_five_attribute_points_and_is_final()
    {
        var w = NewWorld();
        var hero = Hero(w, 20);
        var progress = hero.Get<CharacterProgress>();
        Assert.Equal(95, progress.AttributePoints);

        var chosen = Assert.Single(Step(w, new ChooseSecondClassIntent(hero.Id, Second)).OfType<SecondClassChosen>());

        Assert.Equal((hero.Id, Second, 100), (chosen.Entity, chosen.SecondClassId, chosen.AttributePoints));
        Assert.Equal(Second, progress.SecondClass);
        Assert.Equal(100, progress.AttributePoints);

        // Definitiva: escolher de novo (a mesma ou a outra) é recusado e não dá mais pontos.
        Assert.Equal(ProgressionRejectReason.SecondClassAlreadyChosen, Leveling.ChooseSecondClass(w, hero, Second));
        Assert.Equal(ProgressionRejectReason.SecondClassAlreadyChosen, Leveling.ChooseSecondClass(w, hero, "second_class.test_b"));
        Assert.Equal(Second, progress.SecondClass);
        Assert.Equal(100, progress.AttributePoints);
    }

    [Fact]
    public void Choosing_in_combat_is_refused()
    {
        var w = NewWorld();
        var hero = Hero(w, 20);
        hero.Get<CombatActivity>().LastCombatTick = w.Tick;

        Assert.Equal(ProgressionRejectReason.InCombat, Leveling.ChooseSecondClass(w, hero, Second));
        Assert.Null(hero.Get<CharacterProgress>().SecondClass);

        // Passado o tempo de combate, aceita.
        for (var t = 0; t < Rules.CombatTimeoutTicks; t++)
        {
            w.Step();
        }

        Assert.Null(Leveling.ChooseSecondClass(w, hero, Second));
    }

    [Theory]
    [InlineData("second_class.test_archer")] // de outra classe base
    [InlineData("second_class.nope")]
    [InlineData("class.warrior")]
    public void Choosing_a_second_class_of_another_base_class_is_refused(string id)
    {
        var w = NewWorld();
        var hero = Hero(w, 20);

        Assert.Equal(ProgressionRejectReason.NotASecondClass, Leveling.ChooseSecondClass(w, hero, id));
        Assert.Equal(95, hero.Get<CharacterProgress>().AttributePoints);
    }

    // ---------- NH ----------

    [Theory]
    [InlineData(Strike, 1, 21)]
    [InlineData(Strike, 2, 24)]
    [InlineData(Strike, 3, 27)]
    [InlineData(Strike, 4, 30)]
    [InlineData(Strike, 5, 33)]
    [InlineData(Passive, 1, 21)]
    [InlineData(Signature, 1, 35)]
    [InlineData(Signature, 2, 37)]
    [InlineData(Signature, 3, 40)]
    public void Second_class_ranks_use_the_second_class_levels(string ability, int rank, int level)
    {
        var ranks = new Dictionary<string, int> { [ability] = rank };

        Assert.NotNull(Leveling.Validate(Content, Warrior, Build(level - 1, ranks)));
        Assert.Null(Leveling.Validate(Content, Warrior, Build(level, ranks)));
    }

    [Fact]
    public void Raising_a_second_class_ability_needs_level_21()
    {
        var w = NewWorld();
        var at20 = Hero(w, 20, Second);
        Assert.Equal(ProgressionRejectReason.LevelTooLow, Leveling.RaiseSkillRank(w, at20, Strike));

        var at21 = Hero(w, 21, Second);
        Assert.Null(Leveling.RaiseSkillRank(w, at21, Strike));
        Assert.Equal(ProgressionRejectReason.LevelTooLow, Leveling.RaiseSkillRank(w, at21, Strike)); // NH2: 24

        // Sem segunda classe, não recebe pontos.
        var none = Hero(w, 21);
        Assert.Equal(ProgressionRejectReason.NotLearnable, Leveling.RaiseSkillRank(w, none, Strike));
    }

    [Fact]
    public void Invalid_second_class_builds_are_refused()
    {
        var one = new Dictionary<string, int> { [Strike] = 1 };

        Assert.Contains("não é da classe", Leveling.Validate(Content, Warrior, Build(25, [], "second_class.test_archer")));
        Assert.Contains("exige nível 20", Leveling.Validate(Content, Warrior, Build(19, [])));
        Assert.Contains("não é habilidade da classe", Leveling.Validate(Content, Warrior, Build(25, one, second: null)));
        Assert.Contains("não é habilidade da classe", Leveling.Validate(Content, Warrior, Build(25, one, "second_class.test_b")));

        // Pontos de atributo: +5 só com a segunda classe.
        var full = new CharacterBuild { Level = 20, Allocated = new AttributeSet { Str = 100 } };
        Assert.NotNull(Leveling.Validate(Content, Warrior, full));
        Assert.Null(Leveling.Validate(Content, Warrior, full with { SecondClass = Second }));
    }

    // ---------- redistribuição e barra ----------

    [Fact]
    public void Reset_returns_second_class_points_and_keeps_the_choice()
    {
        var w = NewWorld();
        var hero = Leveling.SpawnCharacter(w, Warrior, new Vector2(1, 1), new CharacterBuild
        {
            Level = 25,
            SecondClass = Second,
            Allocated = new AttributeSet { Str = 60, Vit = 65 },
            SkillRanks = new Dictionary<string, int> { ["warrior.heavy_strike"] = 5, [Strike] = 2, [Passive] = 1 },
        });
        var progress = hero.Get<CharacterProgress>();
        Assert.Equal(0, progress.AttributePoints);
        Assert.Equal(17, progress.SkillPoints);
        Assert.True(StatusManager.Has(w, hero, Status1));

        Assert.Null(Leveling.ResetPoints(w, hero));

        Assert.Equal(Second, progress.SecondClass);
        Assert.Equal((24 * 5) + 5, progress.AttributePoints);
        Assert.Equal(25, progress.SkillPoints);
        Assert.Equal(["warrior.sword_strike"], hero.Get<AbilityState>().Known);
        Assert.False(StatusManager.Has(w, hero, Status1));
        Assert.Equal(Second, Leveling.BuildOf(hero)!.SecondClass);
    }

    [Fact]
    public void Bar_has_the_base_first_then_the_second_class_without_passives()
    {
        var w = NewWorld();
        var hero = Hero(w, 40, Second, new Dictionary<string, int>
        {
            [Shout] = 1,
            [Passive] = 1,
            ["warrior.taunt"] = 1,
            [Strike] = 1,
            ["warrior.heavy_strike"] = 1,
        });

        Assert.Equal(["warrior.sword_strike", "warrior.heavy_strike", "warrior.taunt", Strike, Shout], hero.Get<AbilityState>().Known);

        // Aprender durante o jogo mantém a ordem.
        Assert.Null(Leveling.RaiseSkillRank(w, hero, Signature));
        Assert.Null(Leveling.RaiseSkillRank(w, hero, "warrior.whirlwind"));
        Assert.Equal(
            ["warrior.sword_strike", "warrior.heavy_strike", "warrior.whirlwind", "warrior.taunt", Strike, Shout, Signature],
            hero.Get<AbilityState>().Known);
    }

    // ---------- passivas ----------

    [Fact]
    public void Passive_applies_its_status_and_changes_it_with_the_rank()
    {
        var w = NewWorld();
        var hero = Hero(w, 24, Second);
        Assert.False(StatusManager.Has(w, hero, Status1));

        Assert.Null(Leveling.RaiseSkillRank(w, hero, Passive));
        var s1 = StatusManager.Get(w, hero, Status1);
        Assert.NotNull(s1);
        Assert.True(StatusManager.IsPermanent(s1));
        Assert.DoesNotContain(Passive, hero.Get<AbilityState>().Known);
        Assert.Equal(1, Leveling.BuildOf(hero)!.SkillRanks[Passive]);

        // Muito depois da duração padrão do status, continua.
        for (var t = 0; t < 100; t++)
        {
            w.Step();
        }

        Assert.True(StatusManager.Has(w, hero, Status1));

        Assert.Null(Leveling.RaiseSkillRank(w, hero, Passive));
        Assert.False(StatusManager.Has(w, hero, Status1));
        Assert.True(StatusManager.IsPermanent(StatusManager.Get(w, hero, Status2)!));
    }

    [Fact]
    public void Passive_status_survives_death_revive_and_respawn()
    {
        var w = NewWorld();
        var hero = Hero(w, 24, Second, new Dictionary<string, int> { [Passive] = 2 });
        Assert.True(StatusManager.Has(w, hero, Status2));

        w.Kill(hero);
        for (var t = 0; t < 100; t++)
        {
            w.Step();
        }

        Assert.True(StatusManager.Has(w, hero, Status2));

        // Renascer pela Milagre tira todos os status de antes da queda, menos os permanentes.
        Assert.True(w.Revive(hero, 10, hero.Id, "test"));
        StatusManager.RemoveAll(w, hero, hero.Id);
        StatusManager.RemoveByTags(w, hero, ["buff"], 0, hero.Id);
        Assert.False(StatusManager.Remove(w, hero, Status2, hero.Id));
        Assert.True(StatusManager.Has(w, hero, Status2));

        // Renascer no servidor recria o personagem pelo que foi salvo.
        var respawned = Leveling.SpawnCharacter(w, Warrior, new Vector2(3, 3), Leveling.BuildOf(hero));
        Assert.True(StatusManager.IsPermanent(StatusManager.Get(w, respawned, Status2)!));
        Assert.False(StatusManager.Has(w, respawned, Status1));
    }

    [Fact]
    public void Applying_the_passive_status_again_keeps_it_permanent()
    {
        var w = NewWorld();
        var hero = Hero(w, 21, Second, new Dictionary<string, int> { [Passive] = 1 });

        StatusManager.Apply(w, hero, hero, Status1);

        Assert.True(StatusManager.IsPermanent(StatusManager.Get(w, hero, Status1)!));
    }

    [Fact]
    public void Using_a_passive_is_refused()
    {
        var w = NewWorld();
        var hero = Hero(w, 21, Second, new Dictionary<string, int> { [Passive] = 1 });

        var rejected = Assert.Single(Step(w, new UseAbilityIntent(hero.Id, Passive)).OfType<AbilityRejected>());

        Assert.Equal(AbilityRejectReason.Passive, rejected.Reason);
    }

    // ---------- gravar e criar ----------

    [Fact]
    public void BuildOf_then_SpawnCharacter_gives_back_the_same_character_with_the_second_class()
    {
        var w = NewWorld();
        var build = new CharacterBuild
        {
            Level = 37,
            Experience = 123,
            SecondClass = Second,
            Allocated = new AttributeSet { Str = 90, Vit = 80, Agi = 10 },
            SkillRanks = new Dictionary<string, int>
            {
                ["warrior.heavy_strike"] = 5,
                ["warrior.taunt"] = 3,
                ["warrior.living_wall"] = 3,
                [Strike] = 4,
                [Passive] = 3,
                [Signature] = 2,
            },
        };

        var hero = Leveling.SpawnCharacter(w, Warrior, new Vector2(1, 1), build);
        var saved = Leveling.BuildOf(hero)!;
        var again = Leveling.BuildOf(Leveling.SpawnCharacter(w, Warrior, new Vector2(2, 2), saved))!;

        foreach (var b in new[] { saved, again })
        {
            Assert.Equal((build.Level, build.Experience, build.SecondClass, build.Allocated), (b.Level, b.Experience, b.SecondClass, b.Allocated));
            Assert.Equal(build.SkillRanks.OrderBy(kv => kv.Key), b.SkillRanks.OrderBy(kv => kv.Key));
        }

        var progress = hero.Get<CharacterProgress>();
        Assert.Equal((36 * 5) + 5 - 180, progress.AttributePoints);
        Assert.Equal(Second, ((CharacterProgress)progress.Clone()).SecondClass);
    }
}
