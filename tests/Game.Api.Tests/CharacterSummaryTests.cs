using Game.Persistence;

namespace Game.Api.Tests;

/// <summary>T-404: a lista de personagens leva a segunda classe, lida do documento de progressão.</summary>
public class CharacterSummaryTests
{
    private static Character Row(int version, string progression) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = Guid.NewGuid(),
        Name = "Heroi",
        ClassId = "class.warrior",
        Level = 22,
        ZoneId = "zone.city",
        ProgressionVersion = version,
        Progression = progression,
    };

    [Fact]
    public void Summary_has_the_second_class_from_version_3_documents()
    {
        var summary = CharacterSummary.Of(Row(3, """{"equipment":{},"secondClass":"second_class.guardian"}"""));
        Assert.Equal(("class.warrior", 22, "second_class.guardian"), (summary.ClassId, summary.Level, summary.SecondClass));
    }

    [Theory]
    [InlineData(3, "{}")]
    [InlineData(3, """{"secondClass":""}""")]
    [InlineData(3, """{"secondClass":5}""")]
    [InlineData(3, "[]")]
    [InlineData(3, "não é json")]
    [InlineData(2, """{"secondClass":"second_class.guardian"}""")]
    [InlineData(1, "{}")]
    public void Old_new_or_unreadable_documents_have_no_second_class(int version, string progression) =>
        Assert.Null(CharacterSummary.Of(Row(version, progression)).SecondClass);
}
