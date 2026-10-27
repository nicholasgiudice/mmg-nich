using Game.Auth;

namespace Game.Api.Tests;

public class TokenTests
{
    private static readonly byte[] Key = ApiFactory.TicketKey;
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static GameTicket Ticket(string server = "game.a") =>
        GameTicket.Create(Guid.NewGuid(), Guid.NewGuid(), server, Now.AddSeconds(60));

    [Fact]
    public void Game_ticket_round_trips_and_is_checked_without_network()
    {
        var ticket = Ticket();
        var (result, read) = GameTickets.Validate(ticket.Sign(Key), Key, "game.a", Now);

        Assert.Equal(TicketCheck.Valid, result);
        Assert.Equal(ticket, read);
    }

    [Fact]
    public void Game_ticket_rejects_tampering_wrong_key_server_and_expiry()
    {
        var token = Ticket().Sign(Key);
        var other = Enumerable.Repeat((byte)7, 32).ToArray();
        var tampered = token[..5] + (token[5] == 'A' ? 'B' : 'A') + token[6..];

        Assert.Equal(TicketCheck.Invalid, GameTickets.Validate(tampered, Key, "game.a", Now).Result);
        Assert.Equal(TicketCheck.Invalid, GameTickets.Validate(token, other, "game.a", Now).Result);
        Assert.Equal(TicketCheck.Invalid, GameTickets.Validate("lixo", Key, "game.a", Now).Result);
        Assert.Equal(TicketCheck.Invalid, GameTickets.Validate("", Key, "game.a", Now).Result);
        Assert.Equal(TicketCheck.WrongServer, GameTickets.Validate(token, Key, "game.b", Now).Result);
        Assert.Equal(TicketCheck.Valid, GameTickets.Validate(token, Key, "game.a", Now.AddSeconds(59)).Result);
        Assert.Equal(TicketCheck.Expired, GameTickets.Validate(token, Key, "game.a", Now.AddSeconds(60)).Result);
    }

    [Fact]
    public void Session_token_never_passes_as_game_ticket_and_vice_versa()
    {
        var session = SignedToken.Sign(new SessionToken { AccountId = Guid.NewGuid(), ExpiresAtUnix = Now.AddMinutes(15).ToUnixTimeSeconds() }, Key);
        var ticket = Ticket().Sign(Key);

        Assert.Equal(TicketCheck.Invalid, GameTickets.Validate(session, Key, "game.a", Now).Result);
        Assert.Null(SessionToken.Validate(ticket, Key, Now));
        Assert.NotNull(SessionToken.Validate(session, Key, Now));
        Assert.Null(SessionToken.Validate(session, Key, Now.AddMinutes(15)));
    }

    [Fact]
    public void Replay_guard_accepts_each_ticket_once_and_forgets_expired_ones()
    {
        var time = new ManualTime(Now);
        var guard = new GameTicketReplayGuard(Key, "game.a", time);
        var first = Ticket().Sign(Key);

        Assert.Equal(TicketCheck.Valid, guard.Consume(first).Result);
        Assert.Equal(TicketCheck.AlreadyUsed, guard.Consume(first).Result);
        Assert.Equal(TicketCheck.Valid, guard.Consume(Ticket().Sign(Key)).Result);
        Assert.Equal(TicketCheck.WrongServer, guard.Consume(Ticket("game.b").Sign(Key)).Result);

        time.Advance(TimeSpan.FromSeconds(60));
        Assert.Equal(TicketCheck.Expired, guard.Consume(first).Result);
    }

    [Fact]
    public void Keys_must_be_base64_with_at_least_32_bytes()
    {
        Assert.Throws<InvalidOperationException>(() => SignedToken.KeyFromBase64(null, "X"));
        Assert.Throws<InvalidOperationException>(() => SignedToken.KeyFromBase64("não é base64", "X"));
        Assert.Throws<InvalidOperationException>(() => SignedToken.KeyFromBase64(Convert.ToBase64String(new byte[16]), "X"));
        Assert.Equal(32, SignedToken.KeyFromBase64(Convert.ToBase64String(new byte[32]), "X").Length);
    }
}
