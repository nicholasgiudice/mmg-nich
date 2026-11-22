using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Sim;

namespace Game.Sim.Tests;

/// <summary>T-403: perfil do simulador com segunda classe (as habilidades de verdade vêm na T-408).</summary>
public class SecondClassProfileTests
{
    private static readonly Lazy<ContentCatalog> Real = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        return ContentLoading.Load(Path.Combine(dir.FullName, "data"));
    });

    [Fact]
    public void Profile_with_a_second_class_gets_the_choice_and_the_extra_attribute_points()
    {
        var content = Real.Value;
        var warrior = content.Classes["class.warrior"];
        var profile = new CharacterProfile
        {
            Level = 20,
            SecondClass = new Dictionary<string, string> { ["class.warrior"] = "second_class.berserker" },
        };

        var build = CharacterProfiles.BuildFor(content, warrior, profile);
        var a = build.Allocated;

        Assert.Equal("second_class.berserker", build.SecondClass);
        Assert.Equal(100, a.Str + a.Dex + a.Agi + a.Int + a.Vit);
        Assert.Null(Leveling.Validate(content, warrior, build));

        // Outra classe base não pega a segunda classe do Guerreiro.
        Assert.Null(CharacterProfiles.BuildFor(content, content.Classes["class.mage"], profile).SecondClass);
    }
}
