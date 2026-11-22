using Game.Core.Simulation;
using Game.Protocol;
using MmoRpgClient.Match;

namespace Game.Client.Tests;

/// <summary>T-411: ferramenta de teste no menu do Esc (servidor da T-405).</summary>
public class DevToolTextTests
{
    [Fact]
    public void The_test_entry_only_exists_when_the_server_says_the_tool_is_on()
    {
        Assert.False(DevToolText.Enabled(null)); // antes de entrar
        Assert.False(DevToolText.Enabled(new Welcome(new EntityId(7), 10, "zone.city", "map.city", 1)));
        Assert.True(DevToolText.Enabled(new Welcome(new EntityId(7), 10, "zone.city", "map.city", 1, 0, DevTools: true)));
    }

    [Fact]
    public void Every_request_of_the_server_has_a_button_with_a_label()
    {
        Assert.Equal(Enum.GetValues<DevToolAction>().OrderBy(a => a), DevToolText.Actions.OrderBy(a => a));
        Assert.All(DevToolText.Actions, a => Assert.NotEqual(a.ToString(), DevToolText.Label(a)));
        Assert.Equal(DevToolText.Actions.Count, DevToolText.Actions.Select(DevToolText.Label).Distinct().Count());
    }

    [Fact]
    public void Only_undoing_the_second_class_asks_for_confirmation()
    {
        Assert.Equal([DevToolAction.UndoSecondClass], DevToolText.Actions.Where(DevToolText.NeedsConfirmation));
    }

    [Fact]
    public void Done_shows_the_new_level_and_refusals_say_why()
    {
        Assert.Equal(("Agora no nível 20.", true), DevToolText.Result(new DevToolResult(DevToolAction.ToSecondClassLevel, DevToolOutcome.Done, 20)));
        Assert.True(DevToolText.Result(new DevToolResult(DevToolAction.UndoSecondClass, DevToolOutcome.Done, 25)).Ok);

        foreach (var outcome in Enum.GetValues<DevToolOutcome>().Where(o => o != DevToolOutcome.Done))
        {
            var (text, ok) = DevToolText.Result(new DevToolResult(DevToolAction.LevelUp1, outcome, 40));
            Assert.False(ok);
            Assert.NotEqual("O servidor recusou o pedido.", text); // cada recusa tem a sua frase
        }

        Assert.Contains("combate", DevToolText.Result(new DevToolResult(DevToolAction.FreeReset, DevToolOutcome.InCombat, 30)).Text);
        Assert.Contains("desligada", DevToolText.Result(new DevToolResult(DevToolAction.LevelUp5, DevToolOutcome.Disabled, 3)).Text);
    }
}
