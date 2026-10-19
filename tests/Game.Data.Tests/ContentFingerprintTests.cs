namespace Game.Data.Tests;

/// <summary>T-101: impressão digital do conteúdo, igual em qualquer máquina para os mesmos dados.</summary>
public class ContentFingerprintTests
{
    private static readonly (string Name, string Json)[] Sources =
    [
        ("classes.json", "{\n  \"classes\": [ { \"id\": \"class.x\", \"health\": 100 } ]\n}\n"),
        ("monsters.json", "{ \"monsters\": [ { \"id\": \"monster.y\", \"speed\": 4.0 } ] }"),
    ];

    [Fact]
    public void Same_content_with_other_whitespace_line_endings_and_order_has_the_same_fingerprint()
    {
        var reformatted = new[]
        {
            ("monsters.json", "{\r\n\t\"monsters\":[{\"id\":\"monster.y\",\"speed\":4.0}]\r\n}"),
            ("classes.json", "{ \"classes\": [ { \"id\": \"class.x\", \"health\": 100 } ] } // fim"),
        };

        Assert.Equal(ContentFingerprint.Compute(Sources), ContentFingerprint.Compute(reformatted));
    }

    [Fact]
    public void Changing_a_number_or_a_file_name_changes_the_fingerprint()
    {
        var number = new[] { Sources[0], ("monsters.json", "{ \"monsters\": [ { \"id\": \"monster.y\", \"speed\": 4.5 } ] }") };
        var renamed = new[] { Sources[0], ("other.json", Sources[1].Json) };

        var original = ContentFingerprint.Compute(Sources);
        Assert.NotEqual(original, ContentFingerprint.Compute(number));
        Assert.NotEqual(original, ContentFingerprint.Compute(renamed));
    }

    [Fact]
    public void Path_separators_do_not_change_the_fingerprint()
    {
        var windows = new[] { ("sub\\classes.json", Sources[0].Json) };
        var linux = new[] { ("sub/classes.json", Sources[0].Json) };
        Assert.Equal(ContentFingerprint.Compute(windows), ContentFingerprint.Compute(linux));
    }
}
