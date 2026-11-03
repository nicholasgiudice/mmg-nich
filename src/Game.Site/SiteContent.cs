using System.Text.Json;

namespace Game.Site;

/// <summary>Textos de apresentação de uma classe (<c>site/content/site.json</c>).</summary>
public sealed record ClassText
{
    public string Summary { get; init; } = "";
    public string Role { get; init; } = "";
    public string Strengths { get; init; } = "";
    public string Weaknesses { get; init; } = "";
}

/// <summary>
/// Textos do site que não são regra de jogo (T-071): apresentação, descrições curtas e nomes que os dados não têm.
/// Nenhum número de jogo vem daqui.
/// </summary>
public sealed record SiteContent
{
    public string Title { get; init; } = "Valdran";
    public string Tagline { get; init; } = "";
    public IReadOnlyList<string> Intro { get; init; } = [];
    public IReadOnlyDictionary<string, ClassText> Classes { get; init; } = new Dictionary<string, ClassText>();
    public IReadOnlyDictionary<string, string> Monsters { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Phases { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Ai { get; init; } = new Dictionary<string, string>();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Lê <c>site.json</c> da pasta; pasta ou arquivo ausente = textos vazios.</summary>
    public static SiteContent Load(string? directory)
    {
        var file = directory is null ? null : Path.Combine(directory, "site.json");
        if (file is null || !File.Exists(file))
        {
            return new SiteContent();
        }

        return JsonSerializer.Deserialize<SiteContent>(File.ReadAllText(file), Options) ?? new SiteContent();
    }
}
