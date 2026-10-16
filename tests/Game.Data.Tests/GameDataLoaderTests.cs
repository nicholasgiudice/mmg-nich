using System.Text.Json;
using Game.Core.Definitions;

namespace Game.Data.Tests;

public class GameDataLoaderTests
{
    private static readonly string FixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid");
    private static readonly string ValidJson = File.ReadAllText(Path.Combine(FixtureDir, "minimal.json"));

    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("test.json", json)]);

    private static LoadResult LoadMutated(string from, string to)
    {
        Assert.Contains(from, ValidJson);
        return Load(ValidJson.Replace(from, to));
    }

    private static void AssertError(LoadResult result, string pathContains, string messageContains)
    {
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e =>
            e.Path.Contains(pathContains, StringComparison.Ordinal) &&
            e.Message.Contains(messageContains, StringComparison.OrdinalIgnoreCase));
    }

    // ---------- regras de recurso (T-010) ----------

    private const string WarriorAbilities = "\"abilities\": [\"warrior.heavy_strike\", \"warrior.taunt\"]";

    private static LoadResult WithRules(string rulesJson) =>
        LoadMutated(WarriorAbilities, WarriorAbilities + ", \"resourceRules\": " + rulesJson);

    [Fact]
    public void ResourceRules_load_with_ticks_conversion()
    {
        var result = WithRules(
            "[{ \"id\": \"block\", \"trigger\": \"damage_taken\", \"amount\": 5, \"ratio\": 0.1, \"sourceHasStatusTag\": \"guard\", \"minDistance\": 1, \"maxDistance\": 8, \"internalCooldown\": 0.5 }]");
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var rule = Assert.Single(result.GetDatabaseOrThrow().Classes["class.warrior"].ResourceRules);
        Assert.Equal(ResourceTrigger.DamageTaken, rule.Trigger);
        Assert.Equal("guard", rule.SourceHasStatusTag);
        Assert.Equal(10, rule.InternalCooldownTicks);
        Assert.Equal(0.1, rule.Ratio);
    }

    [Fact]
    public void ResourceRules_invalid_values_are_rejected()
    {
        AssertError(WithRules("[{ \"id\": \"a\", \"trigger\": \"damage_taken\" }]"), "resourceRules[0]", "amount");
        AssertError(WithRules("[{ \"id\": \"a\", \"trigger\": \"damage_dealt\", \"amount\": 1, \"minDistance\": 5, \"maxDistance\": 2 }]"), "minDistance", "maxDistance");
        AssertError(WithRules("[{ \"id\": \"a\", \"trigger\": \"damage_dealt\", \"amount\": 1, \"minEffectiveHealPercent\": 5 }]"), "minEffectiveHealPercent", "heal_done");
        AssertError(WithRules("[{ \"id\": \"a\", \"trigger\": \"interrupt_done\", \"ratio\": 1 }]"), "ratio", "proporção");
        AssertError(WithRules("[{ \"id\": \"a\", \"trigger\": \"damage_dealt\", \"amount\": 1 }, { \"id\": \"a\", \"trigger\": \"damage_dealt\", \"amount\": 1 }]"), "id", "duplicado");
        AssertError(WithRules("[{ \"id\": \"a\", \"trigger\": \"nope\", \"amount\": 1 }]"), "", "");
    }

    // ---------- carregamento válido ----------

    [Fact]
    public void ValidFixture_Loads()
    {
        var result = GameDataLoader.LoadDirectory(FixtureDir);
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var db = result.GetDatabaseOrThrow();
        Assert.Single(db.Classes);
        Assert.Equal(5, db.Abilities.Count);
        Assert.Single(db.Statuses);
        Assert.Single(db.Areas);
        Assert.Single(db.Monsters);
        Assert.Single(db.Arenas);
        Assert.Single(db.Encounters);
    }

    [Fact]
    public void ValidFixture_MapsFieldsAndConvertsSecondsToTicks()
    {
        var db = Load(ValidJson).GetDatabaseOrThrow();

        var warrior = db.Classes["class.warrior"];
        Assert.Equal(-2, warrior.Resource.RegenPerSecond);

        var heavy = db.Abilities["warrior.heavy_strike"];
        Assert.Equal(120, heavy.CooldownTicks);
        Assert.Equal(8, heavy.CastTimeTicks);
        Assert.Equal("vigor", heavy.Cost!.Resource);

        var taunt = (ThreatEffect)db.Abilities["warrior.taunt"].Effects[0];
        Assert.Equal(60, taunt.TauntDurationTicks);

        var bleed = db.Statuses["status.bleed"];
        Assert.Equal(120, bleed.DefaultDurationTicks);
        Assert.Equal(20, bleed.Periodic[0].IntervalTicks);
        Assert.Equal(StackingMode.Stack, bleed.Stacking);
        Assert.Equal(0.9, bleed.Modifiers.SpeedMultiplier);
        Assert.Equal(1, bleed.Modifiers.DamageTakenMultiplier);

        Assert.Equal(10, db.Areas["area.howl_field"].IntervalTicks);
        Assert.Equal(2400, db.Encounters["encounter.test_wolf"].Defeat.TimeLimitTicks);
        Assert.Equal(0.25, db.Monsters["monster.grey_wolf"].Resistances["frost"]);
        Assert.Equal(8, db.Monsters["monster.grey_wolf"].Ai.Parameters["aggroRange"]);
    }

    [Fact]
    public void Polymorphism_ResolvesEffectAndTargetingTypes()
    {
        var db = Load(ValidJson).GetDatabaseOrThrow();
        var heavy = db.Abilities["warrior.heavy_strike"];

        Assert.IsType<MeleeArcTargeting>(heavy.Targeting);
        Assert.IsType<DamageEffect>(heavy.Effects[0]);
        Assert.IsType<InterruptEffect>(heavy.Effects[1]);
        var apply = Assert.IsType<ApplyStatusEffect>(heavy.Effects[2]);
        Assert.Null(apply.DurationTicks);
        var disp = Assert.IsType<DisplacementEffect>(heavy.Effects[3]);
        Assert.Equal(DisplacementKind.Knockback, disp.Kind);
        Assert.Equal(DisplacementDirection.AwayFromSource, disp.Direction);

        Assert.IsType<SelfTargeting>(db.Abilities["wolf.howl"].Targeting);
        Assert.IsType<UnitTargeting>(db.Abilities["warrior.taunt"].Targeting);
        Assert.IsType<SpawnAreaEffect>(db.Abilities["wolf.howl"].Effects[0]);
        Assert.IsType<SummonEffect>(db.Abilities["wolf.howl"].Effects[1]);
    }

    [Fact]
    public void Polymorphism_TypeMayAppearAfterOtherProperties()
    {
        var result = LoadMutated(
            "{ \"type\": \"damage\", \"school\": \"physical\", \"base\": 10 }",
            "{ \"base\": 10, \"school\": \"fire\", \"type\": \"damage\" }");
        var db = result.GetDatabaseOrThrow();
        var dmg = Assert.IsType<DamageEffect>(db.Abilities["warrior.slash"].Effects[0]);
        Assert.Equal(DamageSchool.Fire, dmg.School);
    }

    // ---------- segundos -> ticks ----------

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 20)]
    [InlineData(0.05, 1)]
    [InlineData(0.4, 8)]
    [InlineData(0.024, 0)]   // 0.48 tick -> 0
    [InlineData(0.025, 1)]   // 0.5 tick -> sobe
    [InlineData(0.074, 1)]   // 1.48 -> 1
    [InlineData(0.075, 2)]   // 1.5 -> 2
    [InlineData(1.51, 30)]   // 30.2 -> 30
    [InlineData(-0.025, -1)] // metades arredondam para longe do zero
    public void SecondsToTicks_RoundsToNearestTick(double seconds, int expected)
    {
        Assert.Equal(expected, TickConversion.SecondsToTicks(seconds));
    }

    [Fact]
    public void Rounding_AppliesWhenLoadingJson()
    {
        var db = LoadMutated("\"cooldown\": 0.8,", "\"cooldown\": 0.075,").GetDatabaseOrThrow();
        Assert.Equal(2, db.Abilities["warrior.slash"].CooldownTicks);
    }

    [Fact]
    public void PositiveDurationThatRoundsToZeroTicks_IsRejected()
    {
        AssertError(LoadMutated("\"defaultDuration\": 6,", "\"defaultDuration\": 0.01,"),
            "statuses[0].defaultDuration", "1 tick");
    }

    // ---------- ids ----------

    [Fact]
    public void DuplicateIds_AreReported()
    {
        var json = ValidJson.Replace("\"id\": \"wolf.bite\"", "\"id\": \"warrior.slash\"");
        AssertError(Load(json), "abilities[3].id", "duplicado");
    }

    [Fact]
    public void DuplicateIdsAcrossFiles_AreReported()
    {
        var result = GameDataLoader.LoadSources([("a.json", ValidJson), ("b.json", ValidJson)]);
        Assert.Contains(result.Errors, e => e.File == "b.json" && e.Message.Contains("duplicado"));
    }

    [Fact]
    public void InvalidIdFormat_IsReported()
    {
        AssertError(LoadMutated("\"id\": \"wolf.bite\"", "\"id\": \"Wolf Bite\""), "abilities[3].id", "snake_case");
    }

    // ---------- referências ----------

    [Theory]
    [InlineData("\"basicAttack\": \"warrior.slash\"", "\"basicAttack\": \"warrior.nope\"", "basicAttack", "warrior.nope")]
    [InlineData("\"abilities\": [\"warrior.heavy_strike\", \"warrior.taunt\"]", "\"abilities\": [\"warrior.heavy_strike\", \"warrior.ghost\"]", "classes[0].abilities[1]", "warrior.ghost")]
    [InlineData("\"status\": \"status.bleed\"", "\"status\": \"status.ghost\"", "effects[2].status", "status.ghost")]
    [InlineData("\"area\": \"area.howl_field\"", "\"area\": \"area.ghost\"", "effects[0].area", "area.ghost")]
    [InlineData("\"monster\": \"monster.grey_wolf\", \"count\"", "\"monster\": \"monster.ghost\", \"count\"", "effects[1].monster", "monster.ghost")]
    [InlineData("\"abilities\": [\"wolf.bite\", \"wolf.howl\"]", "\"abilities\": [\"wolf.bite\", \"wolf.ghost\"]", "monsters[0].abilities[1]", "wolf.ghost")]
    [InlineData("\"arena\": \"arena.test\"", "\"arena\": \"arena.ghost\"", "encounters[0].arena", "arena.ghost")]
    [InlineData("\"class\": \"class.warrior\"", "\"class\": \"class.ghost\"", "players[0].class", "class.ghost")]
    [InlineData("\"monster\": \"monster.grey_wolf\", \"position\"", "\"monster\": \"monster.ghost\", \"position\"", "enemies[0].monster", "monster.ghost")]
    public void UnresolvedReferences_AreReported(string from, string to, string path, string id)
    {
        var result = LoadMutated(from, to);
        AssertError(result, path, id);
    }

    [Fact]
    public void StatusReferenceInsidePeriodicEffect_IsChecked()
    {
        var json = ValidJson.Replace(
            "\"effects\": [{ \"type\": \"damage\", \"base\": 3 }]",
            "\"effects\": [{ \"type\": \"apply_status\", \"status\": \"status.ghost\" }]");
        AssertError(Load(json), "periodic[0].effects[0].status", "status.ghost");
    }

    // ---------- faixas numéricas ----------

    [Theory]
    [InlineData("\"health\": 120", "\"health\": 0", "classes[0].health")]
    [InlineData("\"health\": 60", "\"health\": -5", "monsters[0].health")]
    [InlineData("\"speed\": 4.5", "\"speed\": -1", "classes[0].speed")]
    [InlineData("\"radius\": 0.4,\n      \"resource\"", "\"radius\": 0,\n      \"resource\"", "classes[0].radius")]
    [InlineData("\"max\": 100", "\"max\": 0", "resource.max")]
    [InlineData("\"initial\": 0", "\"initial\": 150", "resource.initial")]
    [InlineData("\"cooldown\": 6.0", "\"cooldown\": -1", "abilities[1].cooldown")]
    [InlineData("\"castTime\": 0.4", "\"castTime\": -0.4", "abilities[1].castTime")]
    [InlineData("\"amount\": 30", "\"amount\": -30", "cost.amount")]
    [InlineData("\"range\": 1.6, \"angle\": 90", "\"range\": 0, \"angle\": 90", "targeting.range")]
    [InlineData("\"range\": 1.6, \"angle\": 90", "\"range\": 1.6, \"angle\": 400", "targeting.angle")]
    [InlineData("\"distance\": 1.5", "\"distance\": 0", "distance")]
    [InlineData("\"base\": 42", "\"base\": -42", "effects[0].base")]
    [InlineData("\"tauntDuration\": 3", "\"tauntDuration\": 0", "tauntDuration")]
    [InlineData("\"maxStacks\": 3", "\"maxStacks\": 0", "maxStacks")]
    [InlineData("\"interval\": 1,", "\"interval\": 0,", "periodic[0].interval")]
    [InlineData("\"radius\": 3,\n      \"duration\"", "\"radius\": 0,\n      \"duration\"", "areas[0].radius")]
    [InlineData("\"interval\": 0.5,", "\"interval\": 0,", "areas[0].interval")]
    [InlineData("\"count\": 1, \"radius\": 2", "\"count\": 0, \"radius\": 2", "count")]
    [InlineData("\"timeLimit\": 120", "\"timeLimit\": 0", "defeat.timeLimit")]
    public void OutOfRangeNumbers_AreReported(string from, string to, string path)
    {
        var result = LoadMutated(from, to);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Path.Contains(path, StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownResistanceSchool_IsReported()
    {
        AssertError(LoadMutated("\"frost\": 0.25", "\"shadow\": 0.25"), "resistances.shadow", "escola");
    }

    // ---------- arena ----------

    [Fact]
    public void NonRectangularArena_IsReported()
    {
        AssertError(LoadMutated("\"#.....#\",\n        \"#.....#\"", "\"#.....#\",\n        \"#....#\""),
            "arenas[0].lines[2]", "retangular");
    }

    [Fact]
    public void ArenaWithoutFreeCell_IsReported()
    {
        var json = ValidJson
            .Replace("\"#.....#\"", "\"#######\"")
            .Replace("\"position\": { \"x\": 1.5, \"y\": 1.5 }", "\"position\": { \"x\": 1.5, \"y\": 1.5 }");
        AssertError(Load(json), "arenas[0].lines", "célula livre");
    }

    [Fact]
    public void SpawnInsideWallOrOutsideArena_IsReported()
    {
        AssertError(LoadMutated("\"x\": 1.5, \"y\": 1.5", "\"x\": 0.5, \"y\": 0.5"), "players[0].position", "parede");
        AssertError(LoadMutated("\"x\": 4.5, \"y\": 2.5", "\"x\": 40, \"y\": 2.5"), "enemies[0].position", "fora da arena");
    }

    // ---------- JSON malformado e erros acumulados ----------

    [Fact]
    public void UnknownEffectType_IsReportedWithPath()
    {
        var result = LoadMutated("\"type\": \"interrupt\"", "\"type\": \"explode\"");
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.File == "test.json" && e.Path.StartsWith("abilities[1]"));
    }

    [Fact]
    public void MissingRequiredField_IsReported()
    {
        var result = LoadMutated("\"name\": \"Guerreiro\",", "");
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Path.StartsWith("classes[0]") && e.Message.Contains("name"));
    }

    [Fact]
    public void UnknownProperty_IsReported()
    {
        var result = LoadMutated("\"castWhileMoving\": false", "\"castWhileMovin\": false");
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("castWhileMovin"));
    }

    [Fact]
    public void UnknownSection_AndBrokenJson_AreReported()
    {
        var result = GameDataLoader.LoadSources([("a.json", "{ \"weapons\": [] }"), ("b.json", "{ not json")]);
        Assert.Contains(result.Errors, e => e.File == "a.json" && e.Path == "weapons");
        Assert.Contains(result.Errors, e => e.File == "b.json" && e.Message.Contains("JSON inválido"));
    }

    [Fact]
    public void AllErrorsAreCollected_NotJustTheFirst()
    {
        var json = ValidJson
            .Replace("\"health\": 120", "\"health\": 0")
            .Replace("\"health\": 60", "\"health\": 0")
            .Replace("\"cooldown\": 6.0", "\"cooldown\": -1")
            .Replace("\"arena\": \"arena.test\"", "\"arena\": \"arena.ghost\"");
        var result = Load(json);
        Assert.False(result.Success);
        Assert.True(result.Errors.Count >= 4, string.Join("\n", result.Errors));
        Assert.Throws<InvalidDataException>(() => result.GetDatabaseOrThrow());
    }

    [Fact]
    public void MissingDirectory_ReturnsError()
    {
        var result = GameDataLoader.LoadDirectory(Path.Combine(FixtureDir, "does-not-exist"));
        Assert.False(result.Success);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Database_DictionariesAreReadOnly()
    {
        var db = Load(ValidJson).GetDatabaseOrThrow();
        Assert.False(db.Abilities is IDictionary<string, AbilityDefinition> { IsReadOnly: false });
    }

    // ---------- schemas ----------

    [Fact]
    public void SchemaFiles_AreValidJson()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "schemas");
        var files = Directory.GetFiles(dir, "*.schema.json");
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }
    }
}
