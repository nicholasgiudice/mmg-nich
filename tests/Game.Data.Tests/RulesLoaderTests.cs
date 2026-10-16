using Game.Core.Definitions;

namespace Game.Data.Tests;

public class RulesLoaderTests
{
    private static LoadResult Load(params string[] jsons) =>
        GameDataLoader.LoadSources(jsons.Select((j, i) => ($"f{i}.json", j)));

    private static string RepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir!.FullName, relative);
    }

    [Fact]
    public void Repository_rules_file_loads_with_expected_values()
    {
        var result = GameDataLoader.LoadSources([("rules.json", File.ReadAllText(RepoFile("data/rules.json")))]);
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var control = result.Database!.Rules.Control!;
        Assert.Equal(1.0, control.DurationByRank.Normal);
        Assert.Equal(0.5, control.DurationByRank.Elite);
        Assert.Equal(0.25, control.DurationByRank.Boss);
        Assert.Equal(300, control.Diminishing.WindowTicks);
        Assert.Equal([1.0, 0.5, 0.25, 0.0], control.Diminishing.Steps);
    }

    [Fact]
    public void Missing_rules_gives_neutral_defaults()
    {
        var result = Load("{}");
        Assert.True(result.Success);
        Assert.Null(result.Database!.Rules.Control);
    }

    [Fact]
    public void Rule_sections_from_different_files_coexist_but_duplicates_fail()
    {
        var a = """{ "rules": { "control": { "durationByRank": { "boss": 0.1 } } } }""";
        var b = """{ "rules": { "control": { "durationByRank": { "boss": 0.2 } } } }""";
        Assert.True(Load(a, "{}").Success);
        var dup = Load(a, b);
        Assert.False(dup.Success);
        Assert.Contains(dup.Errors, e => e.Path == "rules.control" && e.File == "f1.json");
    }

    [Fact]
    public void Invalid_rules_report_errors()
    {
        var bad = Load("""{ "rules": { "control": { "diminishing": { "window": 0, "steps": [1.0, 2.0] } } } }""");
        Assert.False(bad.Success);
        Assert.Contains(bad.Errors, e => e.Path.Contains("diminishing.steps"));
        Assert.Contains(bad.Errors, e => e.Path.Contains("diminishing.window"));

        var typo = Load("""{ "rules": { "control": { "durationByRnk": {} } } }""");
        Assert.False(typo.Success);
        Assert.Contains(typo.Errors, e => e.File == "f0.json");
    }

    [Fact]
    public void Status_frontal_modifier_loads_and_validates()
    {
        const string status = """
            { "statuses": [ { "id": "status.guard", "name": "Guarda", "defaultDuration": 5,
              "frontal": { "arcDegrees": 120, "damageTakenMultiplier": 0.5 } } ] }
            """;
        var ok = Load(status);
        Assert.True(ok.Success, string.Join("\n", ok.Errors));
        Assert.Equal(120, ok.Database!.Statuses["status.guard"].Frontal!.ArcDegrees);

        var bad = Load(status.Replace("120", "0"));
        Assert.False(bad.Success);
        Assert.Contains(bad.Errors, e => e.Path.Contains("frontal.arcDegrees"));
    }
}
