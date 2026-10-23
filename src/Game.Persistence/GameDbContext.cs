using Microsoft.EntityFrameworkCore;

namespace Game.Persistence;

/// <summary>Banco do jogo (T-210): tabelas <c>accounts</c> e <c>characters</c>, colunas em snake_case.</summary>
public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Character> Characters => Set<Character>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(a =>
        {
            a.ToTable("accounts");
            a.HasKey(x => x.Id);
            a.Property(x => x.Email).HasMaxLength(254);
            a.HasIndex(x => x.Email).IsUnique();
            a.Property(x => x.PasswordHash).HasMaxLength(512);
            a.HasMany(x => x.Characters).WithOne(c => c.Account).HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Character>(c =>
        {
            c.ToTable("characters");
            c.HasKey(x => x.Id);
            c.Property(x => x.Name).HasMaxLength(Character.MaxNameLength);
            c.Property(x => x.NormalizedName).HasMaxLength(Character.MaxNameLength);
            c.HasIndex(x => x.NormalizedName).IsUnique();
            c.HasIndex(x => x.AccountId);
            c.Property(x => x.ClassId).HasMaxLength(64);
            c.Property(x => x.ZoneId).HasMaxLength(64);
            c.Property(x => x.Progression).HasColumnType("jsonb");
            c.Property(x => x.Version).IsRowVersion(); // Npgsql: uint + IsRowVersion = coluna de sistema xmin
        });

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => !p.IsConcurrencyToken))
            {
                property.SetColumnName(SnakeCase(property.Name));
            }
        }
    }

    private static string SnakeCase(string name) =>
        string.Concat(name.Select((ch, i) => i > 0 && char.IsUpper(ch) ? "_" + char.ToLowerInvariant(ch) : char.ToLowerInvariant(ch).ToString()));
}
