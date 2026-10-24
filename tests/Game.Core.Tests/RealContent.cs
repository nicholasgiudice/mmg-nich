using Game.Core.Combat;
using Game.Data;

namespace Game.Core.Tests;

/// <summary>Conteúdo real de <c>data/</c> (mesma conversão do simulador e do cliente), carregado uma vez.</summary>
internal static class RealContent
{
    private static readonly Lazy<ContentCatalog> Lazy = new(Load);

    public static ContentCatalog Value => Lazy.Value;

    private static ContentCatalog Load()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "data", "classes.json")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        var dataDir = Path.Combine(dir ?? throw new DirectoryNotFoundException("data/ não encontrada"), "data");
        var sources = Directory.EnumerateFiles(dataDir, "*.json")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => (Path.GetFileName(f), File.ReadAllText(f)));
        var db = GameDataLoader.LoadSources(sources).GetDatabaseOrThrow();
        return new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values, db.Items.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
            Fingerprint = db.Fingerprint,
        };
    }
}
