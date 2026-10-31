using Game.Core.Combat;
using Game.Data;
using Godot;

namespace MmoRpgClient.Data;

/// <summary>
/// Carrega o conteúdo de <c>res://data</c> (cópia de <c>../data</c> feita pelo build) e converte em
/// <see cref="ContentCatalog"/>. Usa o FileAccess do Godot para funcionar também em builds exportados.
/// </summary>
public static class ContentLoader
{
    public const string DataDirectory = "res://data";

    public static ContentCatalog Load(string directory = DataDirectory)
    {
        if (!DirAccess.DirExistsAbsolute(directory))
        {
            throw new InvalidOperationException($"Pasta de dados não encontrada: {directory} (rode o build do cliente).");
        }

        // JSON do primeiro nível (data/schemas/ é JSON Schema, não conteúdo) e mapas do Tiled em maps/ (T-051). Os nomes
        // relativos são os mesmos do servidor, para a impressão digital do conteúdo bater.
        var maps = $"{directory}/maps";
        var sources = DirAccess.GetFilesAt(directory)
            .Where(f => f.EndsWith(".json", StringComparison.Ordinal))
            .Concat(DirAccess.DirExistsAbsolute(maps)
                ? DirAccess.GetFilesAt(maps).Where(f => f.EndsWith(TiledMapReader.Extension, StringComparison.Ordinal)).Select(f => $"maps/{f}")
                : [])
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => (Name: f, Json: Godot.FileAccess.GetFileAsString($"{directory}/{f}")))
            .ToList();
        var result = GameDataLoader.LoadSources(sources);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Dados inválidos em {directory}:\n" + string.Join("\n", result.Errors.Select(e => "  " + e)));
        }

        var db = result.Database!;
        return new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values, db.Items.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
            Fingerprint = db.Fingerprint,
        };
    }
}
