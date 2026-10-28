using Game.Core.Combat;
using Game.Data;

namespace Game.Sim;

/// <summary>
/// Carrega <c>data/</c> (ou outra pasta) e converte <see cref="GameDatabase"/> em <see cref="ContentCatalog"/>.
/// O Core não referencia Game.Data, então a conversão fica em quem carrega (aqui, no simulador).
/// </summary>
public static class ContentLoading
{
    public static ContentCatalog ToCatalog(GameDatabase db) =>
        new(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values, db.Items.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
            Fingerprint = db.Fingerprint,
            Zones = db.Zones,
        };

    /// <summary>Carrega todos os JSON da pasta; lança <see cref="InvalidDataException"/> com todos os erros de validação.</summary>
    public static ContentCatalog Load(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Pasta de dados não encontrada: {directory}");
        }

        // LoadDirectory também leria data/schemas/*.json (JSON Schema, não conteúdo); aqui esses arquivos são ignorados.
        var sources = Directory
            .EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
            .Select(f => (Name: Path.GetRelativePath(directory, f), Path: f))
            .Where(f => !f.Name.StartsWith("schemas" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => (f.Name, Json: File.ReadAllText(f.Path)));
        var result = GameDataLoader.LoadSources(sources);
        if (!result.Success)
        {
            throw new InvalidDataException(
                $"Dados inválidos em {directory}:\n" + string.Join("\n", result.Errors.Select(e => "  " + e)));
        }

        return ToCatalog(result.Database!);
    }
}
