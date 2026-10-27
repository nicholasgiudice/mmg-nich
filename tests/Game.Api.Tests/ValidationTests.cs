namespace Game.Api.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("Aria", "Aria")]
    [InlineData("  João42 ", "João42")]
    [InlineData("Abc", "Abc")]
    [InlineData("Abcdefghijklmnop", "Abcdefghijklmnop")]
    public void Valid_names_are_trimmed_and_kept(string input, string expected) =>
        Assert.Equal(expected, Validation.NormalizeName(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ab")]
    [InlineData("Abcdefghijklmnopq")]
    [InlineData("1Aria")]
    [InlineData("Ari a")]
    [InlineData("Ari_a")]
    [InlineData("Аria")] // "А" cirílico
    public void Invalid_names_are_rejected(string? input) => Assert.Null(Validation.NormalizeName(input));

    [Fact]
    public void Decomposed_accents_are_normalized() => Assert.Equal("João", Validation.NormalizeName("João"));

    [Theory]
    [InlineData("a@b.co", true)]
    [InlineData(" Nome@Exemplo.com ", true)]
    [InlineData("sem-arroba.com", false)]
    [InlineData("a@b", false)]
    [InlineData("a b@c.com", false)]
    [InlineData(null, false)]
    public void Emails(string? email, bool valid) => Assert.Equal(valid, Validation.IsValidEmail(email));

    [Fact]
    public void Password_length_is_bounded()
    {
        Assert.False(Validation.IsValidPassword("1234567"));
        Assert.True(Validation.IsValidPassword("12345678"));
        Assert.False(Validation.IsValidPassword(new string('x', 129)));
    }

    [Fact]
    public void Password_hash_is_salted_and_verifies()
    {
        var passwords = new Passwords();
        var a = passwords.Hash("senha-forte-1");

        Assert.NotEqual(a, passwords.Hash("senha-forte-1"));
        Assert.DoesNotContain("senha-forte-1", a);
        Assert.True(passwords.Verify(a, "senha-forte-1"));
        Assert.False(passwords.Verify(a, "senha-forte-2"));
        Assert.False(passwords.Verify(null, "senha-forte-1"));
    }
}
