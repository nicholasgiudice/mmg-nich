using Game.Core.Combat;
using Game.Core.Combat.Objectives;
using Game.Core.Events;
using Game.Protocol;
using MmoRpgClient.Hud;
using MmoRpgClient.Match;

namespace Game.Client.Tests;

/// <summary>T-345: o fim com falha do Vigia na tela de resultado e os avisos de instância.</summary>
public class WatcherReadingTests
{
    private static readonly Lazy<Game.Core.Combat.ContentCatalog> Content = new(SkillPlanTests.LoadContent);

    private static EncounterWorld Watcher() =>
        EncounterSetup.Create(1, Content.Value, Content.Value.Encounters["encounter.faceless_watcher"]);

    [Fact]
    public void Encounter_failed_ends_the_match_as_failed_even_with_players_alive()
    {
        var setup = Watcher();
        var tracker = new MatchTracker(setup.Players[0].Id, null);

        tracker.Consume([], setup.World.Snapshot());
        Assert.Equal(MatchOutcome.InProgress, tracker.Outcome);

        tracker.Consume([new EncounterFailed(EncounterFailReason.ObjectivesDestroyed)], setup.World.Snapshot());
        Assert.Equal(MatchOutcome.Failed, tracker.Outcome);
    }

    [Fact]
    public void Seals_are_not_players_so_the_party_dying_is_a_defeat()
    {
        var setup = Watcher();
        Assert.Equal(3, setup.Objectives.Count);
        foreach (var player in setup.Players)
        {
            setup.World.Kill(player);
        }

        var tracker = new MatchTracker(setup.Players[0].Id, null);
        tracker.Consume(Array.Empty<GameEvent>(), setup.World.Snapshot());
        Assert.Equal(MatchOutcome.Defeat, tracker.Outcome);
    }

    [Theory]
    [InlineData(InstanceNoticeKind.LeavingSoon, 600, "30 s")]
    [InlineData(InstanceNoticeKind.Removed, 0, "fora da masmorra")]
    [InlineData(InstanceNoticeKind.StayCancelled, 0, "continua")]
    public void Instance_notices_have_a_text_for_the_player(InstanceNoticeKind kind, int ticks, string expected) =>
        Assert.Contains(expected, InstanceText.Notice(kind, ticks), StringComparison.Ordinal);
}
