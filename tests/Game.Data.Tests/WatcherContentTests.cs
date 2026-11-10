using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-351: campos novos de dados do Vigia Sem Rosto (medo, dano recebido por escola, recarga ao ser interrompida, efeitos de
/// fase nos objetivos e objetivos de encontro), validação e o conteúdo real.
/// </summary>
public class WatcherContentTests
{
    private static readonly string ValidJson =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid", "minimal.json"));

    private static LoadResult LoadMutated(string from, string to)
    {
        Assert.Contains(from, ValidJson);
        return GameDataLoader.LoadSources([("test.json", ValidJson.Replace(from, to))]);
    }

    private const string Modifiers = "\"modifiers\": { \"speedMultiplier\": 0.9 }";
    private const string Flags = "\"flags\": { \"rooted\": false }";
    private const string Enemies = "\"enemies\": [{ \"monster\": \"monster.grey_wolf\", \"position\": { \"x\": 4.5, \"y\": 2.5 } }],";

    [Fact]
    public void Fear_flag_and_damage_taken_by_school_load()
    {
        var result = LoadMutated(
            Modifiers + ",\n      " + Flags,
            "\"modifiers\": { \"speedMultiplier\": 0.9, \"damageTakenBySchool\": { \"holy\": 1.4 } },\n      \"flags\": { \"feared\": true }");

        Assert.True(result.Success, string.Join("\n", result.Errors));
        var s = result.GetDatabaseOrThrow().Statuses["status.bleed"];
        Assert.True(s.Flags.Feared);
        Assert.Equal(1.4, s.Modifiers.DamageTakenBySchool["holy"]);
    }

    [Fact]
    public void Unknown_school_in_damage_taken_by_school_is_rejected()
    {
        var result = LoadMutated(Modifiers, "\"modifiers\": { \"damageTakenBySchool\": { \"shadow\": 1.4 } }");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Path.Contains("damageTakenBySchool.shadow", StringComparison.Ordinal));
    }

    [Fact]
    public void Encounter_objectives_load_and_must_reference_existing_monsters()
    {
        var ok = LoadMutated(Enemies, Enemies + " \"objectives\": [{ \"monster\": \"monster.grey_wolf\", \"position\": { \"x\": 2.5, \"y\": 2.5 }, \"level\": 3 }],");
        Assert.True(ok.Success, string.Join("\n", ok.Errors));
        var objective = Assert.Single(ok.GetDatabaseOrThrow().Encounters["encounter.test_wolf"].Objectives);
        Assert.Equal(("monster.grey_wolf", 3), (objective.Monster, objective.Level));

        var bad = LoadMutated(Enemies, Enemies + " \"objectives\": [{ \"monster\": \"monster.nobody\", \"position\": { \"x\": 2.5, \"y\": 2.5 } }],");
        Assert.False(bad.Success);
        Assert.Contains(bad.Errors, e => e.Path.Contains("objectives[0].monster", StringComparison.Ordinal));
    }

    [Fact]
    public void Real_watcher_data()
    {
        var result = GameDataLoader.LoadSources(GameDataLoader.EnumerateContentFiles(DataDir()).Select(f => (f.Name, File.ReadAllText(f.Path))));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var db = result.GetDatabaseOrThrow();

        var watcher = db.Monsters["monster.faceless_watcher"];
        Assert.Equal((15, MonsterRank.Boss, "ai.boss"), (watcher.Level, watcher.Rank, watcher.Ai.Id));
        Assert.Equal(["phase.iron_oath", "phase.empty_face", "phase.last_command"], watcher.Phases.Select(p => p.Id));
        Assert.NotEmpty(watcher.Phases[0].OnEnterObjectives);
        Assert.NotEmpty(watcher.Phases[2].OnEnterObjectives);
        Assert.True(watcher.Phases[2].NoSummons);
        Assert.Contains("undead", watcher.Tags);
        Assert.InRange(watcher.Radius, 0.85, 0.95); // desenhado em escala ×2, como a Matriarca
        Assert.InRange(db.Monsters["monster.shade"].Radius, 0.35, 0.45);

        var command = db.Abilities["watcher.last_command"];
        Assert.True(command.CooldownOnInterrupt);
        Assert.Contains("ai.objective", command.Tags);
        Assert.True(command.CastTimeTicks >= 60, "conjuração longa, para dar tempo de interromper");

        Assert.True(db.Statuses["status.dread"].Flags.Feared);
        Assert.Contains("control", db.Statuses["status.dread"].Tags);
        Assert.Equal(1.4, db.Statuses["status.empty_face"].Modifiers.DamageTakenBySchool["holy"]);
        Assert.True(db.Statuses["status.seal_intact"].Flags.Untargetable);

        var enc = db.Encounters["encounter.faceless_watcher"];
        Assert.Equal(3, enc.Objectives.Count(o => o.Monster == "monster.watcher_seal"));
        Assert.Equal(["class.warrior", "class.archer", "class.mage", "class.cleric"], enc.Players.Select(p => p.Class));
        Assert.Equal("map.watcher_hall", enc.Arena);
    }

    private static string DataDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
            {
                return Path.Combine(dir.FullName, "data");
            }
        }

        throw new DirectoryNotFoundException("MmoRpg.sln");
    }
}
