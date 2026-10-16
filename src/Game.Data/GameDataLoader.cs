using System.Text.Json;
using Game.Core.Definitions;

namespace Game.Data;

/// <summary>Um problema encontrado ao carregar/validar dados: arquivo, caminho dentro do arquivo e mensagem.</summary>
public sealed record LoadError(string File, string Path, string Message)
{
    public override string ToString() => $"{File}: {Path}: {Message}";
}

public sealed class LoadResult
{
    public LoadResult(GameDatabase? database, IReadOnlyList<LoadError> errors)
    {
        Database = database;
        Errors = errors;
    }

    public GameDatabase? Database { get; }
    public IReadOnlyList<LoadError> Errors { get; }
    public bool Success => Database is not null && Errors.Count == 0;

    public GameDatabase GetDatabaseOrThrow() =>
        Success
            ? Database!
            : throw new InvalidDataException(
                "Dados de jogo inválidos:" + Environment.NewLine + string.Join(Environment.NewLine, Errors));
}

/// <summary>Um item lido de um arquivo, com sua origem para mensagens de erro.</summary>
internal sealed record Entry<T>(T Value, string File, string Path);

/// <summary>
/// Lê arquivos <c>*.json</c> com objetos raiz do tipo
/// <c>{ "classes": [...], "abilities": [...], "statuses": [...], "areas": [...],
/// "monsters": [...], "arenas": [...], "encounters": [...] }</c> e valida o conjunto.
/// </summary>
public static class GameDataLoader
{
    public static LoadResult LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new LoadResult(null, [new LoadError(directory, "$", "Diretório não encontrado.")]);
        }

        var sources = Directory
            .EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => (Name: Path.GetRelativePath(directory, f), Json: File.ReadAllText(f)));
        return LoadSources(sources);
    }

    public static LoadResult LoadSources(IEnumerable<(string Name, string Json)> sources)
    {
        var errors = new List<LoadError>();
        var content = new RawContent();

        foreach (var (name, json) in sources)
        {
            ReadFile(name, json, content, errors);
        }

        GameDataValidator.Validate(content, errors);
        if (errors.Count > 0)
        {
            return new LoadResult(null, errors);
        }

        var database = new GameDatabase(
            content.Classes.Select(e => e.Value),
            content.Abilities.Select(e => e.Value),
            content.Statuses.Select(e => e.Value),
            content.Areas.Select(e => e.Value),
            content.Monsters.Select(e => e.Value),
            content.Arenas.Select(e => e.Value),
            content.Encounters.Select(e => e.Value));
        return new LoadResult(database, errors);
    }

    private static void ReadFile(string file, string json, RawContent content, List<LoadError> errors)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException ex)
        {
            errors.Add(new LoadError(file, "$", "JSON inválido: " + Clean(ex)));
            return;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new LoadError(file, "$", "A raiz do arquivo deve ser um objeto."));
                return;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "classes": ReadList(file, property, content.Classes, errors); break;
                    case "abilities": ReadList(file, property, content.Abilities, errors); break;
                    case "statuses": ReadList(file, property, content.Statuses, errors); break;
                    case "areas": ReadList(file, property, content.Areas, errors); break;
                    case "monsters": ReadList(file, property, content.Monsters, errors); break;
                    case "arenas": ReadList(file, property, content.Arenas, errors); break;
                    case "encounters": ReadList(file, property, content.Encounters, errors); break;
                    case "$schema": break;
                    default:
                        errors.Add(new LoadError(file, property.Name, "Seção desconhecida."));
                        break;
                }
            }
        }
    }

    private static void ReadList<T>(string file, JsonProperty property, List<Entry<T>> target, List<LoadError> errors)
    {
        if (property.Value.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new LoadError(file, property.Name, "Deve ser uma lista."));
            return;
        }

        var index = 0;
        foreach (var element in property.Value.EnumerateArray())
        {
            var path = $"{property.Name}[{index++}]";
            try
            {
                var value = element.Deserialize<T>(GameJson.Options);
                if (value is null)
                {
                    errors.Add(new LoadError(file, path, "Item nulo."));
                }
                else
                {
                    target.Add(new Entry<T>(value, file, path));
                }
            }
            catch (JsonException ex)
            {
                var inner = ex.Path is { Length: > 1 } p ? p[1..] : string.Empty;
                errors.Add(new LoadError(file, path + inner, Clean(ex)));
            }
        }
    }

    private static string Clean(JsonException ex)
    {
        var message = ex.Message;
        var cut = message.IndexOf(" Path:", StringComparison.Ordinal);
        return cut > 0 ? message[..cut] : message;
    }
}

internal sealed class RawContent
{
    public List<Entry<ClassDefinition>> Classes { get; } = [];
    public List<Entry<AbilityDefinition>> Abilities { get; } = [];
    public List<Entry<StatusDefinition>> Statuses { get; } = [];
    public List<Entry<AreaDefinition>> Areas { get; } = [];
    public List<Entry<MonsterDefinition>> Monsters { get; } = [];
    public List<Entry<ArenaDefinition>> Arenas { get; } = [];
    public List<Entry<EncounterDefinition>> Encounters { get; } = [];
}
