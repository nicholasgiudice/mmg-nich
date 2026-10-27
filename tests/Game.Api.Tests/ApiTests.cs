using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Game.Auth;

namespace Game.Api.Tests;

/// <summary>Comportamento HTTP que não toca o banco.</summary>
public class ApiTests
{
    [Fact]
    public async Task Root_answers_without_database()
    {
        await using var api = new ApiFactory();
        var res = await api.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Characters_need_a_valid_unexpired_session()
    {
        await using var api = new ApiFactory();
        var http = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/characters")).StatusCode);

        var wrongKey = SignedToken.Sign(new SessionToken { AccountId = Guid.NewGuid(), ExpiresAtUnix = api.Time.Now.AddMinutes(5).ToUnixTimeSeconds() }, ApiFactory.TicketKey);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrongKey);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/characters")).StatusCode);

        var expired = SignedToken.Sign(new SessionToken { AccountId = Guid.NewGuid(), ExpiresAtUnix = api.Time.Now.ToUnixTimeSeconds() }, ApiFactory.SessionKey);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/characters")).StatusCode);
    }

    [Fact]
    public async Task Invalid_registration_is_rejected_with_a_code()
    {
        await using var api = new ApiFactory();
        var http = api.CreateClient();

        var badEmail = await http.PostAsJsonAsync("/accounts", new { email = "sem-arroba", password = "senha-forte-1" });
        var shortPassword = await http.PostAsJsonAsync("/accounts", new { email = "a@b.co", password = "curta" });

        Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);
        Assert.Equal("email_invalid", await CodeOf(badEmail));
        Assert.Equal("password_invalid", await CodeOf(shortPassword));
    }

    [Fact]
    public async Task Login_and_registration_are_rate_limited_per_ip()
    {
        await using var api = new ApiFactory(authRequestsPerMinute: 3);
        var http = api.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/sessions", new { email = "", password = "" })).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await http.PostAsJsonAsync("/sessions", new { email = "", password = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await http.PostAsJsonAsync("/accounts", new { email = "x", password = "" })).StatusCode);

        var otherIp = api.CreateClient();
        otherIp.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.9");
        Assert.Equal(HttpStatusCode.Unauthorized, (await otherIp.PostAsJsonAsync("/sessions", new { email = "", password = "" })).StatusCode);
    }

    internal static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<Dictionary<string, object>>())?["code"]?.ToString();
}
