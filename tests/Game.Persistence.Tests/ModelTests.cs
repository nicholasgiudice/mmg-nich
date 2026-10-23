using Microsoft.EntityFrameworkCore;

namespace Game.Persistence.Tests;

/// <summary>T-210: o modelo bate com as migrations e com os nomes do banco. Não precisa de PostgreSQL.</summary>
public class ModelTests
{
    private static GameDbContext Context() => new(GameDatabase.Options("Host=localhost;Database=unused"));

    [Fact]
    public void Model_has_no_changes_missing_a_migration()
    {
        using var db = Context();
        Assert.False(db.Database.HasPendingModelChanges(), "Gere uma migration: dotnet ef migrations add <Nome> --project src/Game.Persistence");
    }

    [Fact]
    public void Tables_and_columns_use_snake_case_and_xmin_is_the_version()
    {
        using var db = Context();
        var character = db.Model.FindEntityType(typeof(Character))!;

        Assert.Equal("characters", character.GetTableName());
        Assert.Equal("accounts", db.Model.FindEntityType(typeof(Account))!.GetTableName());
        Assert.Equal("normalized_name", character.FindProperty(nameof(Character.NormalizedName))!.GetColumnName());
        Assert.Equal("jsonb", character.FindProperty(nameof(Character.Progression))!.GetColumnType());

        var version = character.FindProperty(nameof(Character.Version))!;
        Assert.Equal("xmin", version.GetColumnName());
        Assert.True(version.IsConcurrencyToken);
    }
}
