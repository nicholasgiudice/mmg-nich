using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Simulation;
using MmoRpgClient.Hud;
using MmoRpgClient.Input;
using MmoRpgClient.Match;

namespace Game.Client.Tests;

/// <summary>T-410: a segunda classe na tela de habilidades (plano de NH, textos) e na barra.</summary>
public class SecondClassPanelTests
{
    private static readonly Lazy<ContentCatalog> Content = new(SkillPlanTests.LoadContent);

    [Theory]
    [InlineData("second_class.guardian")]
    [InlineData("second_class.berserker")]
    public void The_second_class_plan_uses_its_rank_levels_and_matches_the_core(string secondClass)
    {
        var content = Content.Value;
        var cls = content.Classes["class.warrior"];
        foreach (var level in new[] { 20, 21, 24, 30, 35, 40 })
        {
            var world = WorldBuilder.CreateDefault(1, null, content);
            var entity = Leveling.SpawnCharacter(world, cls, Vector2.Zero, new CharacterBuild { Level = level });
            Assert.Null(Leveling.ChooseSecondClass(world, entity, secondClass));

            for (var step = 0; step < 60; step++)
            {
                var plan = SkillPlan.FromState(cls, content, entity.Get<AbilityState>(), level, secondClass);
                Assert.Equal(secondClass, plan.SecondClass?.Id);
                Assert.Equal(5, plan.SecondAbilities.Count);
                Assert.Equal(entity.Get<CharacterProgress>().SkillPoints, plan.PointBudget - plan.PointsUsed);

                string? raised = null;
                foreach (var ability in plan.Abilities.Concat(plan.SecondAbilities))
                {
                    var expected = plan.RaiseBlocker(ability.Id);
                    var copy = WorldBuilder.CreateDefault(1, null, content);
                    var probe = Leveling.SpawnCharacter(copy, cls, Vector2.Zero, Leveling.BuildOf(entity));
                    Assert.Equal(expected, Leveling.RaiseSkillRank(copy, probe, ability.Id));
                    raised ??= expected is null ? ability.Id : null;
                }

                if (raised is null)
                {
                    break;
                }

                Assert.Null(Leveling.RaiseSkillRank(world, entity, raised));
            }
        }
    }

    [Fact]
    public void Second_class_ranks_unlock_at_21_24_27_30_33_and_the_signature_at_35_37_40()
    {
        var content = Content.Value;
        var cls = content.Classes["class.warrior"];
        var world = WorldBuilder.CreateDefault(1, null, content);
        var entity = Leveling.SpawnCharacter(world, cls, Vector2.Zero, new CharacterBuild { Level = 20 });
        Assert.Null(Leveling.ChooseSecondClass(world, entity, "second_class.guardian"));
        var plan = SkillPlan.FromState(cls, content, entity.Get<AbilityState>(), 20, "second_class.guardian");

        var normal = plan.SecondAbilities.First(a => !a.Signature);
        var signature = plan.SecondAbilities.Single(a => a.Signature);
        Assert.Equal([21, 24, 27, 30, 33], Enumerable.Range(1, 5).Select(r => plan.RequiredLevel(normal, r)));
        Assert.Equal([35, 37, 40], Enumerable.Range(1, 3).Select(r => plan.RequiredLevel(signature, r)));
        Assert.Equal(1, plan.RequiredLevel(plan.Abilities[0], 1)); // a classe base continua com os níveis dela
        Assert.Equal(ProgressionRejectReason.LevelTooLow, plan.RaiseBlocker(normal.Id)); // no 20, a segunda classe ainda não sobe
        Assert.True(plan.IsSecond(normal.Id));
        Assert.False(plan.IsSecond(plan.Abilities[0].Id));
    }

    [Fact]
    public void Without_a_second_class_the_plan_is_the_base_class_only()
    {
        var content = Content.Value;
        var cls = content.Classes["class.warrior"];
        var world = WorldBuilder.CreateDefault(1, null, content);
        var entity = Leveling.SpawnCharacter(world, cls, Vector2.Zero, new CharacterBuild { Level = 20 });
        var plan = SkillPlan.FromState(cls, content, entity.Get<AbilityState>(), 20);

        Assert.Null(plan.SecondClass);
        Assert.Empty(plan.SecondAbilities);
        Assert.Equal(ProgressionRejectReason.NotLearnable, plan.RaiseBlocker("guardian.interception"));
    }

    [Fact]
    public void Texts_say_the_choice_is_final_and_explain_refusals()
    {
        Assert.Contains("definitiva", SecondClassText.Confirm("Guardião"));
        Assert.Contains("20", SecondClassText.Locked(20));
        Assert.NotEmpty(SecondClassText.Role("second_class.guardian"));
        Assert.NotEmpty(SecondClassText.Role("second_class.berserker"));
        Assert.True(SecondClassText.IsSecondClassRequest("second_class.guardian"));
        Assert.False(SecondClassText.IsSecondClassRequest("warrior.heavy_strike"));
        Assert.Contains("combate", SecondClassText.Rejection(ProgressionRejectReason.InCombat));
        Assert.Contains("já foi", SecondClassText.Rejection(ProgressionRejectReason.SecondClassAlreadyChosen));
    }

    [Fact]
    public void Every_second_class_in_the_data_has_a_role_text()
    {
        foreach (var second in Content.Value.Classes.Values.SelectMany(c => c.SecondClasses))
        {
            Assert.NotEmpty(SecondClassText.Role(second.Id));
        }
    }

    [Fact]
    public void A_newly_learned_ability_goes_to_the_first_empty_slot_from_6()
    {
        var bar = new HotbarLayout();
        bar.Load(["a", "b", "c", "d", "e", "x", null, null, null, null], [], _ => true);

        Assert.Equal(6, bar.PlaceInFirstEmpty("guardian.interception", HotbarLayout.RowSize)); // 6 ocupado: vai para o 7
        Assert.Equal(-1, bar.PlaceInFirstEmpty("guardian.interception", HotbarLayout.RowSize)); // já está na barra
        Assert.Equal("guardian.interception", bar[6]);

        for (var i = 7; i < HotbarLayout.Size; i++)
        {
            bar.Set(i, $"z{i}");
        }

        Assert.Equal(-1, bar.PlaceInFirstEmpty("guardian.bastion", HotbarLayout.RowSize)); // sem espaço: não mexe na barra
        Assert.Equal(-1, bar.IndexOf("guardian.bastion"));
    }
}
