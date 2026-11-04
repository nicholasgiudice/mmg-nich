using MmoRpgClient.Input;

namespace Game.Client.Tests;

/// <summary>T-081: barra de atalhos 1–6 configurável (padrão, carregar, pôr, trocar, esvaziar e guardar).</summary>
public class HotbarLayoutTests
{
    private static readonly string[] Abilities = ["a.one", "a.two", "a.three", "a.four", "a.five"];
    private static readonly string[] Potion = ["item.potion"];

    private static HotbarLayout Loaded(IReadOnlyList<string?>? saved = null, Func<string, bool>? exists = null)
    {
        var bar = new HotbarLayout();
        bar.Load(saved, HotbarLayout.Defaults(Abilities, Potion), exists ?? (_ => true));
        return bar;
    }

    [Fact]
    public void DefaultsPutTheClassAbilitiesInOneToFiveAndThePotionInSix()
    {
        var bar = Loaded();

        Assert.True(bar.Loaded);
        Assert.Equal(["a.one", "a.two", "a.three", "a.four", "a.five", "item.potion"], bar.Slots);
    }

    [Fact]
    public void SavedBarWinsAndIdsThatLeftTheDataBecomeEmpty()
    {
        var bar = Loaded(["a.four", "gone.ability", null, "a.four", "", "a.one"], exists: id => id != "gone.ability");

        Assert.Equal(["a.four", null, null, null, null, "a.one"], bar.Slots);
    }

    [Fact]
    public void SettingAnIdAlreadyOnTheBarMovesIt()
    {
        var bar = Loaded();
        var changes = 0;
        bar.Changed += () => changes++;

        bar.Set(5, "a.one");

        Assert.Null(bar[0]);
        Assert.Equal("a.one", bar[5]);
        Assert.Equal(-1, bar.IndexOf("item.potion"));
        Assert.Equal(1, changes);
    }

    [Fact]
    public void SwapAndClear()
    {
        var bar = Loaded();

        bar.Swap(0, 3);
        bar.Clear(1);

        Assert.Equal(["a.four", null, "a.three", "a.one", "a.five", "item.potion"], bar.Slots);
    }

    [Fact]
    public void SerializeRoundTrips()
    {
        var bar = Loaded();
        bar.Clear(2);

        var text = bar.Serialize();
        var again = Loaded(HotbarLayout.Parse(text));

        Assert.Equal("a.one|a.two||a.four|a.five|item.potion", text);
        Assert.Equal(bar.Slots, again.Slots);
        Assert.Null(HotbarLayout.Parse(""));
    }
}
