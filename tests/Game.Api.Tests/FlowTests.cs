using System.Net;
using System.Net.Http.Json;
using Game.Auth;
using Game.Persistence;

namespace Game.Api.Tests;

/// <summary>Fluxo completo contra PostgreSQL real (T-211).</summary>
public class FlowTests(ApiDatabase db) : IClassFixture<ApiDatabase>
{
    private static string Email() => $"j{Guid.NewGuid():N}@exemplo.com";

    private static string Name() => "P" + Guid.NewGuid().ToString("N")[..12];

    [PostgresFact]
    public async Task Register_login_create_character_and_get_game_ticket()
    {
        var http = await db.Api.LoggedInAsync(Email());
        var name = Name();

        var created = await http.PostAsJsonAsync("/characters", new { name, classId = "class.archer" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var character = (await created.Content.ReadFromJsonAsync<CharacterSummary>())!;
        Assert.Equal(new CharacterSummary(character.Id, name, "class.archer", 1, "zone.city"), character);

        var list = await http.GetFromJsonAsync<List<CharacterSummary>>("/characters");
        Assert.Equal([character], list);

        var play = await http.PostAsync($"/characters/{character.Id}/play", null);
        Assert.Equal(HttpStatusCode.OK, play.StatusCode);
        var ticket = (await play.Content.ReadFromJsonAsync<PlayResponse>())!;
        Assert.Equal(ApiFactory.GameServer, ticket.Server);
        Assert.Equal("127.0.0.1:7777", ticket.Address);
        Assert.Equal(db.Api.Time.Now.AddSeconds(60), ticket.ExpiresAt);

        // O servidor de jogo valida só com a chave compartilhada, e uma vez só.
        var guard = new GameTicketReplayGuard(ApiFactory.TicketKey, ApiFactory.GameServer, db.Api.Time);
        var (result, read) = guard.Consume(ticket.Ticket);
        Assert.Equal(TicketCheck.Valid, result);
        Assert.Equal(character.Id, read!.CharacterId);
        Assert.Equal(TicketCheck.AlreadyUsed, guard.Consume(ticket.Ticket).Result);

        // Personagem novo: nível 1, sem progressão gravada (o servidor cria com o equipamento inicial da classe).
        var stored = await new GameRepository(GameDatabase.Options(db.ConnectionString)).LoadCharacterAsync(character.Id);
        Assert.Equal(read.AccountId, stored!.AccountId);
        Assert.Equal("{}", stored.Progression);
    }

    [PostgresFact]
    public async Task Passwords_are_hashed_and_wrong_password_or_email_fail_the_same_way()
    {
        var email = Email();
        await db.Api.LoggedInAsync(email);
        var http = db.Api.CreateClient();

        var wrongPassword = await http.PostAsJsonAsync("/sessions", new { email, password = "senha-errada" });
        var unknownEmail = await http.PostAsJsonAsync("/sessions", new { email = Email(), password = "senha-forte-1" });
        var upperCase = await http.PostAsJsonAsync("/sessions", new { email = email.ToUpperInvariant(), password = "senha-forte-1" });
        var again = await http.PostAsJsonAsync("/accounts", new { email, password = "outra-senha-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal("invalid_credentials", await ApiTests.CodeOf(wrongPassword));
        Assert.Equal("invalid_credentials", await ApiTests.CodeOf(unknownEmail));
        Assert.Equal(HttpStatusCode.OK, upperCase.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("email_taken", await ApiTests.CodeOf(again));

        var account = await new GameRepository(GameDatabase.Options(db.ConnectionString)).FindAccountByEmailAsync(email);
        Assert.DoesNotContain("senha-forte-1", account!.PasswordHash);
        Assert.NotNull(account.LastLoginAt);
    }

    [PostgresFact]
    public async Task Character_names_are_validated_and_unique_across_accounts()
    {
        var a = await db.Api.LoggedInAsync(Email());
        var b = await db.Api.LoggedInAsync(Email());
        var name = Name();

        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/characters", new { name, classId = "class.warrior" })).StatusCode);

        var taken = await b.PostAsJsonAsync("/characters", new { name = name.ToLowerInvariant(), classId = "class.mage" });
        var invalidName = await b.PostAsJsonAsync("/characters", new { name = "x", classId = "class.mage" });
        var invalidClass = await b.PostAsJsonAsync("/characters", new { name = Name(), classId = "class.dragon" });

        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        Assert.Equal("name_taken", await ApiTests.CodeOf(taken));
        Assert.Equal("name_invalid", await ApiTests.CodeOf(invalidName));
        Assert.Equal("class_invalid", await ApiTests.CodeOf(invalidClass));
    }

    [PostgresFact]
    public async Task Accounts_only_see_delete_and_play_their_own_characters()
    {
        var a = await db.Api.LoggedInAsync(Email());
        var b = await db.Api.LoggedInAsync(Email());
        var mine = (await (await a.PostAsJsonAsync("/characters", new { name = Name(), classId = "class.cleric" })).Content.ReadFromJsonAsync<CharacterSummary>())!;

        Assert.Empty((await b.GetFromJsonAsync<List<CharacterSummary>>("/characters"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/characters/{mine.Id}/play", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/characters/{mine.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync($"/characters/{mine.Id}")).StatusCode);
        Assert.Empty((await a.GetFromJsonAsync<List<CharacterSummary>>("/characters"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsync($"/characters/{mine.Id}/play", null)).StatusCode);
    }

    [PostgresFact]
    public async Task Account_has_a_character_limit()
    {
        var http = await db.Api.LoggedInAsync(Email());
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await http.PostAsJsonAsync("/characters", new { name = Name(), classId = "class.mage" })).StatusCode);
        }

        var sixth = await http.PostAsJsonAsync("/characters", new { name = Name(), classId = "class.mage" });
        Assert.Equal("character_limit", await ApiTests.CodeOf(sixth));
    }

    [PostgresFact]
    public async Task Session_expires_after_15_minutes()
    {
        var api = new ApiFactory(db.ConnectionString);
        await using var _ = api;
        var http = await api.LoggedInAsync(Email());
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/characters")).StatusCode);

        api.Time.Advance(TimeSpan.FromMinutes(15));
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/characters")).StatusCode);
    }
}
