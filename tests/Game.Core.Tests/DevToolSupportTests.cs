using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-405: métodos de apoio da ferramenta de teste do servidor (<see cref="Leveling.ExperienceToReach"/> e
/// <see cref="Leveling.UndoSecondClass"/>). Conteúdo de <see cref="SecondClassTests"/> (segunda classe de teste com
/// habilidades e passiva).
/// </summary>
public class DevToolSupportTests
{
    private static readonly ContentCatalog Content = SecondClassTests.Content;
    private static readonly ProgressionRules Rules = Content.Rules.Progression;
    private static readonly ClassDefinition Warrior = Content.Classes["class.warrior"];

    private static World NewWorld() => WorldBuilder.CreateDefault(7, null, Content);

    private static Entity Hero(World w, CharacterBuild? build = null) => Leveling.SpawnCharacter(w, Warrior, new Vector2(1, 1), build);

    [Fact]
    public void Experience_to_reach_counts_what_is_missing_and_stops_at_the_max_level()
    {
        Assert.Equal(Rules.XpToNext(1), Leveling.ExperienceToReach(Rules, 1, 0, 2));
        Assert.Equal(Rules.XpToNext(1) - 10 + Rules.XpToNext(2), Leveling.ExperienceToReach(Rules, 1, 10, 3));
        Assert.Equal(0, Leveling.ExperienceToReach(Rules, 20, 0, 20));
        Assert.Equal(0, Leveling.ExperienceToReach(Rules, 25, 0, 20));
        Assert.Equal(0, Leveling.ExperienceToReach(Rules, Rules.MaxLevel, 0, Rules.MaxLevel + 5));
        Assert.Equal(Leveling.ExperienceToReach(Rules, 38, 0, Rules.MaxLevel), Leveling.ExperienceToReach(Rules, 38, 0, Rules.MaxLevel + 5));
    }

    /// <summary>
    /// Dar de uma vez a XP que falta (ferramenta) e dar aos pedaços, do tamanho da XP de um monstro normal do nível
    /// (<see cref="ProgressionRules.MonsterXp"/>, o que a morte dá a quem lutou sozinho), chegam ao mesmo nível com os
    /// mesmos pontos e a mesma vida.
    /// </summary>
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 7)]
    [InlineData(1, 20)]
    [InlineData(20, 40)]
    [InlineData(1, 40)]
    public void Granting_what_is_missing_at_once_matches_monster_experience(int from, int to)
    {
        var w = NewWorld();
        var tool = Hero(w, new CharacterBuild { Level = from });
        var played = Hero(w, new CharacterBuild { Level = from });

        Leveling.GrantExperience(w, tool, Leveling.ExperienceToReach(Rules, from, 0, to), EntityId.None, "dev.tools");
        while (played.Get<CharacterStats>().Level < to)
        {
            var level = played.Get<CharacterStats>().Level;
            Leveling.GrantExperience(w, played, Rules.Share(Rules.MonsterXp(level, MonsterRank.Normal, level), 1), EntityId.None, "monster.test");
        }

        Assert.Equal(to, tool.Get<CharacterStats>().Level);
        Assert.Equal(to, played.Get<CharacterStats>().Level);
        var (a, b) = (tool.Get<CharacterProgress>(), played.Get<CharacterProgress>());
        Assert.Equal((Rules.AttributePointsAt(to), Rules.SkillPointsAt(to)), (a.AttributePoints, a.SkillPoints));
        Assert.Equal((b.AttributePoints, b.SkillPoints, b.ExperienceToNext), (a.AttributePoints, a.SkillPoints, a.ExperienceToNext));
        Assert.Equal(0, a.Experience); // exatamente no começo do nível
        Assert.Equal(played.Get<Health>().Max, tool.Get<Health>().Max);
    }

    [Fact]
    public void Undo_gives_back_the_second_class_ranks_and_the_free_bonus_points_and_keeps_the_rest()
    {
        var w = NewWorld();
        var hero = Hero(w, new CharacterBuild
        {
            Level = 30,
            SecondClass = SecondClassTests.Second,
            Allocated = new AttributeSet { Str = 40, Vit = 20 },
            SkillRanks = new Dictionary<string, int> { ["warrior.heavy_strike"] = 3, [SecondClassTests.Strike] = 2, [SecondClassTests.Passive] = 1 },
        });
        var p = hero.Get<CharacterProgress>();
        var freeBefore = p.AttributePoints;
        Assert.True(freeBefore >= Rules.SecondClassAttributePoints);
        Assert.Contains(SecondClassTests.Strike, hero.Get<AbilityState>().Known);
        Assert.Contains(StatusManager.Active(w, hero), s => s.StatusId == SecondClassTests.Status1);
        var str = hero.Get<CharacterStats>().Str;

        Assert.True(Leveling.UndoSecondClass(w, hero));

        Assert.Null(p.SecondClass);
        Assert.Equal(Rules.SkillPointsAt(30) - 3, p.SkillPoints);
        Assert.Equal(freeBefore - Rules.SecondClassAttributePoints, p.AttributePoints);
        Assert.Equal((40, 20), (p.AllocatedStr, p.AllocatedVit)); // os +5 estavam livres: a distribuição fica
        Assert.Equal(str, hero.Get<CharacterStats>().Str);
        var st = hero.Get<AbilityState>();
        Assert.Equal(3, st.Ranks["warrior.heavy_strike"]);
        Assert.False(st.Ranks.ContainsKey(SecondClassTests.Strike));
        Assert.DoesNotContain(SecondClassTests.Strike, st.Known);
        Assert.DoesNotContain(StatusManager.Active(w, hero), s => s.StatusId == SecondClassTests.Status1);
        Assert.Null(Leveling.Validate(Content, Warrior, Leveling.BuildOf(hero)!));

        // Dá para escolher de novo (outra ou a mesma).
        Assert.False(Leveling.UndoSecondClass(w, hero));
        Assert.Null(Leveling.ChooseSecondClass(w, hero, "second_class.test_b"));
    }

    [Fact]
    public void Undo_with_the_bonus_points_already_spent_gives_back_every_attribute_point()
    {
        var w = NewWorld();
        var all = Rules.AttributePointsAt(25, true);
        var hero = Hero(w, new CharacterBuild { Level = 25, SecondClass = SecondClassTests.Second, Allocated = new AttributeSet { Str = all - 2, Agi = 2 } });
        var baseStr = hero.Get<CharacterStats>().Str - (all - 2);
        var maxHealth = hero.Get<Health>().Max;
        Assert.Equal(0, hero.Get<CharacterProgress>().AttributePoints);

        Assert.True(Leveling.UndoSecondClass(w, hero));

        var p = hero.Get<CharacterProgress>();
        Assert.Equal(Rules.AttributePointsAt(25, false), p.AttributePoints);
        Assert.Equal(0, p.AllocatedTotal);
        Assert.Equal(baseStr, hero.Get<CharacterStats>().Str);
        Assert.True(hero.Get<Health>().Max <= maxHealth);
        Assert.Null(Leveling.Validate(Content, Warrior, Leveling.BuildOf(hero)!));
    }

    [Fact]
    public void Undo_without_a_second_class_or_progression_does_nothing()
    {
        var w = NewWorld();
        var hero = Hero(w, new CharacterBuild { Level = 20 });
        var before = Leveling.BuildOf(hero);
        Assert.False(Leveling.UndoSecondClass(w, hero));
        Assert.Equal(before!.Allocated, Leveling.BuildOf(hero)!.Allocated);
        Assert.False(Leveling.UndoSecondClass(w, EntityFactory.SpawnPlayer(w, Warrior, new Vector2(3, 3))));

        // Escolher pela intenção e desfazer devolve os +5 e deixa escolher outra.
        w.Enqueue(new ChooseSecondClassIntent(hero.Id, SecondClassTests.Second));
        w.Step();
        Assert.Equal(Rules.AttributePointsAt(20, true), hero.Get<CharacterProgress>().AttributePoints);
        Assert.True(Leveling.UndoSecondClass(w, hero));
        Assert.Equal(Rules.AttributePointsAt(20), hero.Get<CharacterProgress>().AttributePoints);
    }
}
