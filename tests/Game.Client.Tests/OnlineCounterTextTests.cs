using MmoRpgClient.Account;

namespace Game.Client.Tests;

public class OnlineCounterTextTests
{
    [Theory]
    [InlineData(null, "Consultando jogadores online...")]
    [InlineData(OnlineCounterText.Unavailable, "Contador de jogadores indisponível")]
    [InlineData(0, "Nenhum jogador online agora")]
    [InlineData(1, "1 jogador online")]
    [InlineData(2, "2 jogadores online")]
    [InlineData(137, "137 jogadores online")]
    public void Describes_the_count_in_portuguese(int? online, string expected) =>
        Assert.Equal(expected, OnlineCounterText.Describe(online));

    [Theory]
    [InlineData(null, false)]
    [InlineData(OnlineCounterText.Unavailable, false)]
    [InlineData(0, false)]
    [InlineData(3, true)]
    public void Only_a_positive_count_is_live(int? online, bool live) =>
        Assert.Equal(live, OnlineCounterText.IsLive(online));
}
