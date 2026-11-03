using Game.Site;

// Gerador do site do jogo (T-071).
// Uso: dotnet run --project src/Game.Site -- --data data --out site/dist [--content site/content] [--art client/assets/art] [--credits CREDITS.md]
var options = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["--data"] = "data",
    ["--out"] = "site/dist",
    ["--content"] = "site/content",
    ["--art"] = "client/assets/art",
    ["--credits"] = "CREDITS.md",
};

for (var i = 0; i < args.Length; i++)
{
    if (!options.ContainsKey(args[i]) || i + 1 >= args.Length)
    {
        Console.Error.WriteLine($"argumento inválido: {args[i]}");
        Console.Error.WriteLine("uso: Game.Site --data <pasta> --out <pasta> [--content <pasta>] [--art <pasta>] [--credits <arquivo>]");
        return 2;
    }

    options[args[i]] = args[++i];
}

string? Optional(string key) => Directory.Exists(options[key]) || File.Exists(options[key]) ? options[key] : null;

var result = SiteGenerator.Generate(new SiteOptions
{
    DataDir = options["--data"],
    OutDir = options["--out"],
    ContentDir = Optional("--content"),
    ArtDir = Optional("--art"),
    CreditsFile = Optional("--credits"),
});

foreach (var w in result.Warnings)
{
    Console.WriteLine($"aviso: {w}");
}

if (!result.Success)
{
    foreach (var e in result.Errors)
    {
        Console.Error.WriteLine($"erro: {e}");
    }

    return 1;
}

Console.WriteLine($"site gerado em {Path.GetFullPath(options["--out"])}: {result.Pages.Count} páginas");
return 0;
