namespace Game.Data.Tests;

public class ThreatRulesLoaderTests
{
    private static LoadResult Load(params string[] jsons) =>
        GameDataLoader.LoadSources(jsons.Select((j, i) => ($"f{i}.json", j)));

    [Fact]
    public void Threat_section_is_loaded_independently_of_other_sections()
    {
        var r = Load("""{ "threat": { "healMultiplier": 0.25, "overtakeRatio": 1.3 } }""");
        Assert.True(r.Success, string.Join("\n", r.Errors));
        Assert.Equal(0.25, r.Database!.Threat.HealMultiplier);
        Assert.Equal(1.3, r.Database.Threat.OvertakeRatio);
        Assert.Equal(1.0, r.Database.Threat.DamageMultiplier); // padrão
    }

    [Fact]
    public void Missing_threat_section_uses_defaults()
    {
        var r = Load("{}");
        Assert.True(r.Success);
        Assert.Equal(0.5, r.Database!.Threat.HealMultiplier);
    }

    [Fact]
    public void Invalid_threat_values_are_errors()
    {
        var r = Load("""{ "threat": { "overtakeRatio": 0.5, "healMultiplier": -1 } }""");
        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path == "threat.overtakeRatio");
        Assert.Contains(r.Errors, e => e.Path == "threat.healMultiplier");
    }

    [Fact]
    public void Unknown_threat_property_is_an_error()
    {
        var r = Load("""{ "threat": { "typo": 1 } }""");
        Assert.False(r.Success);
    }

    [Fact]
    public void Repo_rules_file_is_valid()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "data", "rules.json")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        Assert.NotNull(dir);
        var r = Load(File.ReadAllText(Path.Combine(dir!, "data", "rules.json")));
        Assert.True(r.Success, string.Join("\n", r.Errors));
    }
}
