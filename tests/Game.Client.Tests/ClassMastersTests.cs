using MmoRpgClient.Hud;

namespace Game.Client.Tests;

/// <summary>T-541: o mestre das classes (quem é mestre e o que ele faz com o personagem).</summary>
public class ClassMastersTests
{
    [Theory]
    [InlineData("npc.trainer", true)]
    [InlineData("npc.class_master", true)]
    [InlineData("npc.merchant", false)]
    [InlineData("npc.priest", false)]
    public void Only_the_listed_npcs_are_masters(string id, bool expected) => Assert.Equal(expected, ClassMasters.IsMaster(id));

    [Theory]
    [InlineData(1, false, MasterTalk.TooLow)]
    [InlineData(19, false, MasterTalk.TooLow)]
    [InlineData(20, false, MasterTalk.ChooseSecondClass)]
    [InlineData(35, false, MasterTalk.ChooseSecondClass)]
    [InlineData(20, true, MasterTalk.OpenSkills)]
    [InlineData(5, true, MasterTalk.OpenSkills)]
    public void The_talk_depends_on_level_and_second_class(int level, bool hasSecond, MasterTalk expected) =>
        Assert.Equal(expected, ClassMasters.Talk(level, 20, hasSecond));

    [Fact]
    public void The_too_low_line_names_the_level()
    {
        var text = ClassMasters.TooLowText("Mestre de Treino", 20);
        Assert.Contains("nível 20", text);
        Assert.Contains("Mestre de Treino", text);
    }
}
