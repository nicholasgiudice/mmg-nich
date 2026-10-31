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
/// "monsters": [...], "arenas": [...], "encounters": [...], "items": [...] }</c> e valida o conjunto.
/// </summary>
public static class GameDataLoader
{
    public static LoadResult LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new LoadResult(null, [new LoadError(directory, "$", "Diretório não encontrado.")]);
        }

        var sources = EnumerateContentFiles(directory)
            .Select(f => (f.Name, Json: File.ReadAllText(f.Path)));
        return LoadSources(sources);
    }

    /// <summary>
    /// Arquivos de conteúdo de uma pasta, em ordem: <c>*.json</c> (menos <c>schemas/</c>) e mapas do Tiled <c>*.tmj</c>
    /// (T-051). Name é o caminho relativo à pasta (é o que aparece nos erros e entra na impressão digital).
    /// </summary>
    public static IEnumerable<(string Name, string Path)> EnumerateContentFiles(string directory) =>
        Directory
            .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json", StringComparison.Ordinal) || f.EndsWith(TiledMapReader.Extension, StringComparison.Ordinal))
            .Where(f => !IsSchemaFile(directory, f))
            .Select(f => (Name: Path.GetRelativePath(directory, f), Path: f))
            .OrderBy(f => f.Name, StringComparer.Ordinal);

    // A subpasta "schemas" guarda apenas JSON Schema (autocomplete), nunca conteúdo.
    private static bool IsSchemaFile(string root, string file)
    {
        var first = Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return string.Equals(first, "schemas", StringComparison.OrdinalIgnoreCase);
    }

    public static LoadResult LoadSources(IEnumerable<(string Name, string Json)> sources)
    {
        var errors = new List<LoadError>();
        var content = new RawContent();
        var materialized = sources.ToList();

        foreach (var (name, json) in materialized)
        {
            if (name.EndsWith(TiledMapReader.Extension, StringComparison.Ordinal))
            {
                if (TiledMapReader.Read(name, json, errors) is { } map)
                {
                    content.Arenas.Add(new Entry<ArenaDefinition>(map, name, "$"));
                }
            }
            else
            {
                ReadFile(name, json, content, errors);
            }
        }

        content.Rules = BuildRules(content.RuleSections, errors);
        ValidateThreat(content.Threat, errors);
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
            content.Encounters.Select(e => e.Value),
            content.Items.Select(e => e.Value))
        {
            Rules = content.Rules,
            Threat = content.Threat ?? new Game.Core.Combat.Threat.ThreatRules(),
            Fingerprint = ContentFingerprint.Compute(materialized),
            Zones = content.Zones.Select(e => e.Value).ToDictionary(z => z.Id, StringComparer.Ordinal),
        };
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
                    case "items": ReadList(file, property, content.Items, errors); break;
                    case "zones": ReadList(file, property, content.Zones, errors); break;
                    case "rules": ReadRules(file, property, content.RuleSections, errors); break;
                    case "threat": ReadObject<Game.Core.Combat.Threat.ThreatRules>(file, property, v => content.Threat = v, errors); break;
                    case "$schema": break;
                    default:
                        errors.Add(new LoadError(file, property.Name, "Seção desconhecida."));
                        break;
                }
            }
        }
    }

    /// <summary>
    /// "rules" é um objeto com uma seção por assunto (ex.: "control", "threat"). Arquivos diferentes podem
    /// contribuir com seções diferentes; a mesma seção em dois arquivos é erro. Adicionar uma seção nova
    /// exige apenas uma propriedade em <see cref="RulesDefinition"/>.
    /// </summary>
    private static void ReadRules(string file, JsonProperty property, Dictionary<string, (JsonElement Value, string File)> sections, List<LoadError> errors)
    {
        if (property.Value.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new LoadError(file, "rules", "Deve ser um objeto."));
            return;
        }

        foreach (var section in property.Value.EnumerateObject())
        {
            if (sections.TryGetValue(section.Name, out var previous))
            {
                errors.Add(new LoadError(file, "rules." + section.Name, $"Seção já definida em '{previous.File}'."));
            }
            else
            {
                sections[section.Name] = (section.Value.Clone(), file);
            }
        }
    }

    private static RulesDefinition BuildRules(Dictionary<string, (JsonElement Value, string File)> sections, List<LoadError> errors)
    {
        if (sections.Count == 0)
        {
            return new RulesDefinition();
        }

        // Valida seção por seção para apontar o arquivo certo.
        var result = new RulesDefinition();
        using var merged = new MemoryStream();
        using (var writer = new Utf8JsonWriter(merged))
        {
            writer.WriteStartObject();
            foreach (var (name, (value, _)) in sections)
            {
                writer.WritePropertyName(name);
                value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        try
        {
            result = JsonSerializer.Deserialize<RulesDefinition>(merged.ToArray(), GameJson.Options) ?? result;
        }
        catch (JsonException ex)
        {
            var path = ex.Path is { Length: > 1 } p ? p[1..] : string.Empty;
            var section = path.TrimStart('.').Split('.', '[')[0];
            var file = sections.TryGetValue(section, out var s) ? s.File : "rules";
            errors.Add(new LoadError(file, "rules" + path, Clean(ex)));
        }

        return result;
    }

    private static void ValidateThreat(Game.Core.Combat.Threat.ThreatRules? t, List<LoadError> errors)
    {
        if (t is null)
        {
            return;
        }

        void Check(string name, double value, double min)
        {
            if (!(value >= min) || double.IsInfinity(value))
            {
                errors.Add(new LoadError("rules", "threat." + name, $"Deve ser maior ou igual a {min} (valor: {value})."));
            }
        }

        Check("damageMultiplier", t.DamageMultiplier, 0);
        Check("healMultiplier", t.HealMultiplier, 0);
        Check("overtakeRatio", t.OvertakeRatio, 1);
        Check("groupAggroThreat", t.GroupAggroThreat, 0);
        Check("proximityThreat", t.ProximityThreat, 0);
    }

    private static void ReadObject<T>(string file, JsonProperty property, Action<T> assign, List<LoadError> errors)
    {
        try
        {
            var value = property.Value.Deserialize<T>(GameJson.Options);
            if (value is null)
            {
                errors.Add(new LoadError(file, property.Name, "Valor nulo."));
            }
            else
            {
                assign(value);
            }
        }
        catch (JsonException ex)
        {
            var inner = ex.Path is { Length: > 1 } p ? p[1..] : string.Empty;
            errors.Add(new LoadError(file, property.Name + inner, Clean(ex)));
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
    public List<Entry<ZoneDefinition>> Zones { get; } = [];
    public List<Entry<ItemBaseDefinition>> Items { get; } = [];

    /// <summary>Seções de "rules" (nome -> JSON, arquivo de origem).</summary>
    public Dictionary<string, (JsonElement Value, string File)> RuleSections { get; } = new(StringComparer.Ordinal);

    public RulesDefinition Rules { get; set; } = new();
    public Game.Core.Combat.Threat.ThreatRules? Threat { get; set; }
}
