using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>T-403: segundas classes em <c>classes.json</c> e habilidades passivas: carga e validação.</summary>
public class SecondClassLoaderTests
{
    private static LoadResult Load(string json) => GameDataLoader.LoadSources([("t.json", json)]);

    private static string Errors(LoadResult r) => string.Join("\n", r.Errors);

    private const string Abilities = """
        { "id": "a.basic", "name": "Básico", "targeting": { "type": "self" } },
        { "id": "a.one", "name": "Um", "targeting": { "type": "self" } },
        { "id": "s.one", "name": "S1", "targeting": { "type": "self" } },
        { "id": "s.sig1", "name": "Sig1", "signature": true, "targeting": { "type": "self" } },
        { "id": "s.sig2", "name": "Sig2", "signature": true, "targeting": { "type": "self" } },
        { "id": "s.passive", "name": "Passiva", "passive": true, "passiveStatus": "status.p1", "targeting": { "type": "self" },
          "ranks": [{ "rank": 2, "passiveStatus": "status.p2" }] }
        """;

    private const string Statuses = """
        { "id": "status.p1", "name": "P1", "defaultDuration": 1 },
        { "id": "status.p2", "name": "P2", "defaultDuration": 1 }
        """;

    private static LoadResult WithSecondClasses(string secondClasses, string extraAbilities = "") => Load($$"""
        {
          "classes": [{
            "id": "class.a", "name": "A", "health": 100, "speed": 1, "radius": 0.4, "resource": { "type": "mana", "max": 100 },
            "basicAttack": "a.basic", "abilities": ["a.one"], "secondClasses": [{{secondClasses}}]
          }],
          "abilities": [{{Abilities}}{{extraAbilities}}],
          "statuses": [{{Statuses}}]
        }
        """);

    [Fact]
    public void Second_classes_and_passives_load()
    {
        var r = WithSecondClasses("""
            { "id": "second_class.x", "name": "X", "abilities": ["s.one", "s.passive", "s.sig1"] },
            { "id": "second_class.y", "name": "Y" }
            """);

        Assert.True(r.Success, Errors(r));
        var cls = r.Database!.Classes["class.a"];
        Assert.Equal(["second_class.x", "second_class.y"], cls.SecondClasses.Select(s => s.Id));
        Assert.Equal("X", cls.SecondClasses[0].Name);
        Assert.Equal(["s.one", "s.passive", "s.sig1"], cls.SecondClasses[0].Abilities);
        Assert.Empty(cls.SecondClasses[1].Abilities);
        var passive = r.Database.Abilities["s.passive"];
        Assert.True(passive.Passive);
        Assert.Equal("status.p1", passive.PassiveStatus);
        Assert.Equal("status.p2", passive.Ranks[0].PassiveStatus);
    }

    [Fact]
    public void Real_data_has_the_two_warrior_second_classes()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "data", "classes.json")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        var sources = GameDataLoader.EnumerateContentFiles(Path.Combine(dir!, "data")).Select(f => (f.Name, File.ReadAllText(f.Path)));
        var db = GameDataLoader.LoadSources(sources).GetDatabaseOrThrow();

        Assert.Equal(
            [("second_class.guardian", "Guardião"), ("second_class.berserker", "Berserker")],
            db.Classes["class.warrior"].SecondClasses.Select(s => (s.Id, s.Name)));
    }

    [Theory]
    [InlineData("""{ "id": "second_class.x", "name": "X" }, { "id": "second_class.x", "name": "X2" }""", "secondClasses[1].id")]
    [InlineData("""{ "id": "Second Class", "name": "X" }""", "secondClasses[0].id")]
    [InlineData("""{ "id": "second_class.x", "name": " " }""", "secondClasses[0].name")]
    [InlineData("""{ "id": "second_class.x", "name": "X", "abilities": ["s.nope"] }""", "secondClasses[0].abilities[0]")]
    [InlineData("""{ "id": "second_class.x", "name": "X", "abilities": ["s.one", "s.one"] }""", "secondClasses[0].abilities[1]")]
    [InlineData("""{ "id": "second_class.x", "name": "X", "abilities": ["a.one"] }""", "secondClasses[0].abilities[0]")]
    [InlineData("""{ "id": "second_class.x", "name": "X", "abilities": ["a.basic"] }""", "secondClasses[0].abilities[0]")]
    [InlineData("""{ "id": "second_class.x", "name": "X", "abilities": ["s.sig1", "s.one", "s.sig2"] }""", "secondClasses[0].abilities")]
    public void Invalid_second_classes_are_errors(string secondClasses, string field)
    {
        var r = WithSecondClasses(secondClasses);

        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path == "classes[0]." + field);
    }

    [Theory]
    [InlineData(""" "cost": { "resource": "mana", "amount": 10 }, "targeting": { "type": "self" } """, "cost")]
    [InlineData(""" "cooldown": 5, "targeting": { "type": "self" } """, "cooldown")]
    [InlineData(""" "castTime": 1, "targeting": { "type": "self" } """, "castTime")]
    [InlineData(""" "targeting": { "type": "unit", "range": 5 } """, "targeting")]
    [InlineData(""" "targeting": { "type": "self" }, "effects": [{ "type": "heal", "base": 5 }] """, "effects")]
    [InlineData(""" "targeting": { "type": "self" }, "ranks": [{ "rank": 2, "cooldown": 3 }] """, "ranks[0].cooldown")]
    [InlineData(""" "targeting": { "type": "self" }, "ranks": [{ "rank": 2, "cost": { "resource": "mana", "amount": 1 } }] """, "ranks[0].cost")]
    [InlineData(""" "targeting": { "type": "self" }, "ranks": [{ "rank": 2, "passiveStatus": "status.nope" }] """, "ranks[0].passiveStatus")]
    public void Passive_with_cost_cooldown_or_target_is_an_error(string fields, string field)
    {
        var r = WithSecondClasses(
            """{ "id": "second_class.x", "name": "X" }""",
            $$""", { "id": "s.bad", "name": "Ruim", "passive": true, "passiveStatus": "status.p1", {{fields}} }""");

        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path == "abilities[6]." + field);
    }

    [Fact]
    public void Passive_status_is_required_and_only_for_passives()
    {
        var r = WithSecondClasses(
            """{ "id": "second_class.x", "name": "X" }""",
            """
            , { "id": "s.no_status", "name": "Sem", "passive": true, "targeting": { "type": "self" } },
              { "id": "s.missing", "name": "Falta", "passive": true, "passiveStatus": "status.nope", "targeting": { "type": "self" } },
              { "id": "s.active", "name": "Ativa", "passiveStatus": "status.p1", "targeting": { "type": "self" } }
            """);

        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.Path == "abilities[6].passiveStatus");
        Assert.Contains(r.Errors, e => e.Path == "abilities[7].passiveStatus");
        Assert.Contains(r.Errors, e => e.Path == "abilities[8].passiveStatus");
    }

    [Fact]
    public void Defaults_have_no_second_class_and_no_passive()
    {
        var a = new AbilityDefinition { Id = "x", Name = "x", Targeting = new SelfTargeting() };

        Assert.False(a.Passive);
        Assert.Null(a.PassiveStatus);
        Assert.Empty(new ClassDefinition
        {
            Id = "c",
            Name = "c",
            Speed = 1,
            Radius = 1,
            Resource = new ResourceDefinition { Type = "mana", Max = 1 },
            BasicAttack = "x",
        }.SecondClasses);
    }
}
