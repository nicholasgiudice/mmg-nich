using Game.Bots;
using Game.Core.Combat;
using Game.Sim;

namespace Game.Design.Tests;

/// <summary>Carrega o <c>data/</c> real do repositório e roda lotes de simulação para os testes de design.</summary>
internal static class DesignHarness
{
    /// <summary>Execuções (seeds) por verificação. Padrão 100 (~4 min na VPS de 2 vCPU desde a T-206; o limite é 5 min); sobrescreva com DESIGN_RUNS.</summary>
    public static int Runs { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("DESIGN_RUNS"), out var n) && n > 0 ? n : 100;

    public static string DataDir { get; } = FindDataDir();

    public static ContentCatalog Content { get; } = ContentLoading.Load(DataDir);

    public const string NoClericEncounter = "encounter.test_matriarch_no_cleric";

    /// <summary>Catálogo com um encontro extra: a Matriarca contra o grupo sem o Clérigo (derivado do encontro real).</summary>
    public static ContentCatalog ContentWithNoCleric { get; } = BuildNoCleric(Content);

    // Escolhas de bots para o conteúdo real. Reação (0,2-0,4 s) e poção (abaixo de 40% de vida) ficam nos padrões dos bots.
    public static readonly IReadOnlyDictionary<string, string> Options = new Dictionary<string, string>
    {
        ["class.mage:explosion"] = "mage.elemental_blast",
    };

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    /// <summary>Roda (ou reaproveita, pois o lote é determinístico) um lote de <see cref="Runs"/> seeds.</summary>
    public static BatchSummary Run(string encounter, string strategy, IReadOnlyDictionary<string, string>? roles = null, ContentCatalog? content = null)
    {
        var rolesKey = roles is null ? "" : string.Join(",", roles.OrderBy(r => r.Key).Select(r => r.Key + "=" + r.Value));
        var key = $"{encounter}|{strategy}|{rolesKey}|{(content is null ? 0 : 1)}";
        return Cache.GetOrAdd(key, _ => new Lazy<BatchSummary>(() => RunUncached(encounter, strategy, roles, content))).Value;
    }

    /// <summary>
    /// T-063: como <see cref="Run"/>, mas com NH: um perfil de <see cref="RankProfiles"/> e/ou pares
    /// <c>classe:habilidade=NH</c>, mais opções extras dos bots (ex.: qual habilidade o <c>explosion_spam</c> usa).
    /// </summary>
    public static BatchSummary RunRanked(
        string encounter, string strategy, string profile, IReadOnlyList<string>? pairs = null,
        IReadOnlyDictionary<string, string>? extraOptions = null)
    {
        pairs ??= [];
        var extraKey = extraOptions is null ? "" : string.Join(",", extraOptions.OrderBy(o => o.Key).Select(o => o.Key + "=" + o.Value));
        var key = $"ranked|{encounter}|{strategy}|{profile}|{string.Join("+", pairs)}|{extraKey}";
        return Cache.GetOrAdd(key, _ => new Lazy<BatchSummary>(() =>
        {
            var ranks = RankProfiles.For(profile, Content);
            foreach (var p in pairs)
            {
                RankProfiles.AddPair(ranks, p);
            }

            var options = new Dictionary<string, string>(Options);
            foreach (var (k, v) in extraOptions ?? new Dictionary<string, string>())
            {
                options[k] = v;
            }

            var spec = new RunSpec
            {
                EncounterId = encounter,
                Strategy = strategy,
                Options = new BotOptions(options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Ranks = ranks,
                RankLabel = pairs.Count == 0 ? profile : $"{profile}+{string.Join("+", pairs)}",
            };
            return Batch.Run(Content, spec);
        })).Value;
    }

    /// <summary>
    /// T-206: lote com perfil de personagem (todos os jogadores) e monstros num nível (<c>rules.monsters</c>).
    /// O Clérigo solo usa Corrente e Escudo como par focado (a cura do grupo não tem uso sozinho).
    /// </summary>
    public static BatchSummary RunProfile(
        string encounter, string strategy, CharacterProfile profile, int monsterLevel, IReadOnlyDictionary<string, string>? extraOptions = null)
    {
        profile = profile.Focus.Count > 0 || encounter == "encounter.forest_matriarch" ? profile : profile with { Focus = SoloFocus };
        var extraKey = extraOptions is null ? "" : string.Join(",", extraOptions.OrderBy(o => o.Key).Select(o => o.Key + "=" + o.Value));
        var ranksKey = string.Join(";", profile.Ranks.OrderBy(r => r.Key).Select(r => r.Key + ":" + string.Join(",", r.Value.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))));
        var key = $"profile|{encounter}|{strategy}|{profile.Label}|{ranksKey}|{monsterLevel}|{extraKey}";
        return Cache.GetOrAdd(key, _ => new Lazy<BatchSummary>(() =>
        {
            var options = new Dictionary<string, string>(Options);
            foreach (var (k, v) in extraOptions ?? new Dictionary<string, string>())
            {
                options[k] = v;
            }

            var spec = new RunSpec
            {
                EncounterId = encounter,
                Strategy = strategy,
                Options = new BotOptions(options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = profile,
                MonsterLevel = monsterLevel,
            };
            return Batch.Run(Content, spec);
        })).Value;
    }

    public static readonly IReadOnlyDictionary<string, string[]> SoloFocus = new Dictionary<string, string[]>
    {
        ["class.cleric"] = ["cleric.holy_chain", "cleric.sacred_shield"],
    };

    private static BatchSummary RunUncached(string encounter, string strategy, IReadOnlyDictionary<string, string>? roles, ContentCatalog? content)
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
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Append(derived), c.Items.Values)
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
