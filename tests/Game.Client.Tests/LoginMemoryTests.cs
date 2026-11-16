using MmoRpgClient.Account;

namespace Game.Client.Tests;

/// <summary>T-381: "Lembrar e-mail" grava só o e-mail e o estado da caixa; desmarcar apaga o e-mail.</summary>
public class LoginMemoryTests
{
    [Fact]
    public void Nothing_saved_starts_empty_and_unchecked()
    {
        var memory = new LoginMemory(new FakeSettings());

        Assert.Null(memory.Email);
        Assert.False(memory.Remember);
    }

    [Fact]
    public void Checked_login_saves_the_email_for_the_next_opening()
    {
        var settings = new FakeSettings();
        new LoginMemory(settings).AfterLogin(remember: true, "  ana@example.com ");

        var next = new LoginMemory(settings); // o jogo foi fechado e aberto de novo
        Assert.Equal("ana@example.com", next.Email);
        Assert.True(next.Remember);
    }

    [Fact]
    public void Unchecked_login_erases_the_saved_email()
    {
        var settings = new FakeSettings();
        new LoginMemory(settings).AfterLogin(remember: true, "ana@example.com");

        new LoginMemory(settings).AfterLogin(remember: false, "ana@example.com");

        var next = new LoginMemory(settings);
        Assert.Null(next.Email);
        Assert.False(next.Remember);
        Assert.False(settings.Values.ContainsKey(LoginMemory.EmailKey));
    }

    [Fact]
    public void A_saved_email_without_a_saved_state_starts_checked() =>
        Assert.True(new LoginMemory(new FakeSettings { Values = { [LoginMemory.EmailKey] = "ana@example.com" } }).Remember);

    [Fact]
    public void Only_the_email_and_the_box_state_are_saved()
    {
        var settings = new FakeSettings();
        new LoginMemory(settings).AfterLogin(remember: true, "ana@example.com");

        Assert.Equal(new[] { LoginMemory.EmailKey, LoginMemory.RememberKey }, settings.Values.Keys.Order());
    }

    private sealed class FakeSettings : IAccountSettings
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

        public string? Get(string key) => Values.GetValueOrDefault(key);

        public void Set(string key, string? value)
        {
            if (value is null)
            {
                Values.Remove(key);
            }
            else
            {
                Values[key] = value;
            }
        }
    }
}
