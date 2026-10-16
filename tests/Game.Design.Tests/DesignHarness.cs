using Game.Bots;
using Game.Core.Combat;
using Game.Sim;

namespace Game.Design.Tests;

/// <summary>Carrega o <c>data/</c> real do repositório e roda lotes de simulação para os testes de design.</summary>
internal static class DesignHarness
{
    /// <summary>Execuções (seeds) por verificação. Padrão 100 (~2 min na VPS de 2 vCPU; o limite é 5 min); sobrescreva com DESIGN_RUNS.</summary>
    public static int Runs { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("DESIGN_RUNS"), out var n) && n > 0 ? n : 100;

    public static string DataDir { get; } = FindDataDir();

    public static ContentCatalog Content { get; } = ContentLoading.Load(DataDir);

    public const string NoClericEncounter = "encounter.test_matriarch_no_cleric";

    /// <summary>Catálogo com um encontro extra: a Matriarca contra o grupo sem o Clérigo (derivado do encontro real).</summary>
    public static ContentCatalog ContentWithNoCleric { get; } = BuildNoCleric(Content);

    // Escolhas de bots para o conteúdo real (o Guerreiro do protótipo não tem Provocação: ver relatório).
    public static readonly IReadOnlyDictionary<string, string> Options = new Dictionary<string, string>
    {
        ["class.warrior:taunt"] = "warrior.whirlwind", // "uma habilidade de ameaça + básico"
        ["class.mage:explosion"] = "mage.elemental_blast",
    };

    public static BatchSummary Run(string encounter, string strategy, IReadOnlyDictionary<string, string>? roles = null, ContentCatalog? content = null)
    {
        var spec = new RunSpec
        {
            EncounterId = encounter,
            Strategy = strategy,
            Roles = roles ?? new Dictionary<string, string>(),
            Options = new BotOptions(Options),
            Runs = Runs,
            HazardSourceFactory = () => new TelegraphHazardSource(),
        };
        return Batch.Run(content ?? Content, spec);
    }

    private static ContentCatalog BuildNoCleric(ContentCatalog c)
    {
        var real = c.Encounters["encounter.forest_matriarch"];
        var derived = real with
        {
            Id = NoClericEncounter,
            Players = real.Players.Where(p => p.Class != "class.cleric").ToList(),
        };
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Append(derived))
        {
            Rules = c.Rules,
            Threat = c.Threat,
        };
    }

    private static string FindDataDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
            {
                return Path.Combine(dir.FullName, "data");
            }
        }

        throw new DirectoryNotFoundException("MmoRpg.sln não encontrado acima de " + AppContext.BaseDirectory);
    }
}
