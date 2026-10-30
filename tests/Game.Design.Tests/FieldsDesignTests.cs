using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-215: Campos da Vigília (<c>data/zones.json</c>). Cada grupo típico de cada subárea (pontos de surgimento juntos, que o
/// jogador puxa de uma vez) vira um encontro solo na arena do protótipo, com os monstros nos níveis do mapa. O perfil
/// esperado do nível do grupo (o maior nível dos monstros) vence com ≥ 85%, nas 4 classes. Com os mesmos lotes, um modelo
/// de ritmo confere que um jogador solo chega ao nível 4–5 em cerca de 1 hora. Relatório: <c>tasks/reports/balance-v0.8.md</c>.
/// </summary>
public class FieldsDesignTests
{
    /// <summary>
    /// Tempo fora da luta por grupo: ~10 s para andar até o próximo e pegar o saque e ~50 s para recuperar a vida (nos Campos
    /// não há descanso: poção ou volta à Cidade a cada poucos grupos). Premissa do modelo (balance-v0.8 §2).
    /// </summary>
    public const double OverheadSeconds = 60;

    private static readonly (string Class, string Strategy)[] Classes =
    [
        ("class.warrior", "rotation"),
        ("class.archer", "kite"),
        ("class.mage", "kite"), // ataca de longe como o Arqueiro; parado (rotation) perde para dois lobos do mesmo nível
        ("class.cleric", "rotation"),
    ];

    public static TheoryData<string, string> GroupsAndClasses()
    {
        var data = new TheoryData<string, string>();
        foreach (var group in FieldGroups.Typical)
        {
            foreach (var (cls, _) in Classes)
            {
                data.Add(group.Key, cls);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(GroupsAndClasses))]
    public void Expected_profile_of_the_group_level_clears_each_typical_group_at_least_85_percent(string group, string cls)
    {
        var s = RunGroup(FieldGroups.Typical.Single(g => g.Key == group), cls);
        Assert.True(s.WinRate >= 0.85, $"{group} / {cls}: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s");
    }

    /// <summary>
    /// Ritmo solo (design/progression-v0.1.md §2, T-215): no nível L o jogador caça os grupos do nível min(L, 4), com o tempo
    /// médio de vitória das 4 classes mais <see cref="OverheadSeconds"/>, limitado pelo renascimento (todos os grupos daquele
    /// nível, cada um renascendo no seu tempo). Ao fim de 1 hora ele está no nível 4 ou 5.
    /// </summary>
    [Fact]
    public void Solo_player_reaches_level_4_to_5_in_about_an_hour()
    {
        var pace = FieldGroups.Pace(g => Classes.Average(c => RunGroup(g, c.Class).TimeToWinMean));
        var report = string.Join("; ", pace.Select(p => $"nv{p.Level}: {p.Minutes:F1} min ({p.XpPerMinute:F0} XP/min)"));

        var toLevel4 = pace.Where(p => p.Level < 4).Sum(p => p.Minutes);
        var toLevel6 = pace.Where(p => p.Level < 6).Sum(p => p.Minutes);
        Assert.True(toLevel4 <= 60, $"nível 4 em {toLevel4:F0} min; {report}");
        Assert.True(toLevel6 > 60, $"nível 6 em {toLevel6:F0} min; {report}");
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    /// <summary>Lote do perfil esperado do nível do grupo contra o grupo (monstros nos níveis do mapa).</summary>
    internal static BatchSummary RunGroup(FieldGroup group, string cls) =>
        Cache.GetOrAdd($"{group.Key}|{cls}", _ => new Lazy<BatchSummary>(() =>
        {
            var (content, encounter) = FieldGroups.Encounter(group, cls);
            var strategy = Classes.Single(c => c.Class == cls).Strategy;
            var spec = new RunSpec
            {
                EncounterId = encounter,
                Strategy = strategy,
                Options = new Game.Bots.BotOptions(Options),
                Runs = Runs,
                HazardSourceFactory = () => new Game.Bots.TelegraphHazardSource(),
                Profile = CharacterProfile.Expected(group.Level) with { Focus = SoloFocus },
            };
            return Batch.Run(content, spec);
        })).Value;
}

/// <summary>Um grupo típico: pontos de surgimento da mesma subárea a até 4 tiles um do outro.</summary>
internal sealed record FieldGroup(string Area, IReadOnlyList<MonsterSpawnDefinition> Points)
{
    /// <summary>Composição (subárea, monstros, níveis e quantidades): grupos iguais contam uma vez.</summary>
    public string Key => $"{Area}: " + string.Join(" + ", Points.Select(p => $"{p.Count}× {p.Monster} nv{p.Level}"));

    public int Level => Points.Max(p => p.Level ?? 1);

    public int Monsters => Points.Sum(p => p.Count);

    /// <summary>Tempo até o grupo inteiro estar de volta (o ponto mais lento).</summary>
    public double RespawnSeconds => Points.Max(p => p.RespawnTicks) / (double)Core.Simulation.World.TickRate;
}

internal static class FieldGroups
{
    public const string Zone = "zone.vigil_fields";

    /// <summary>Todos os grupos dos Campos, na ordem dos dados.</summary>
    public static IReadOnlyList<FieldGroup> All { get; } = Build(DesignHarness.Content.Zones[Zone]);

    /// <summary>Um grupo por composição.</summary>
    public static IReadOnlyList<FieldGroup> Typical { get; } = All.GroupBy(g => g.Key).Select(g => g.First()).ToList();

    private static List<FieldGroup> Build(ZoneDefinition zone)
    {
        var groups = new List<List<MonsterSpawnDefinition>>();
        foreach (var p in zone.MonsterSpawns)
        {
            var near = groups.FirstOrDefault(g => g[0].Area == p.Area && g.Any(q => Distance(p.Position, q.Position) <= 4));
            if (near is null)
            {
                groups.Add([p]);
            }
            else
            {
                near.Add(p);
            }
        }

        return groups.Select(g => new FieldGroup(g[0].Area ?? "?", g)).ToList();
    }

    public static double Distance(PositionDefinition a, PositionDefinition b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    // Arena aberta do protótipo: jogador à esquerda, corpo a corpo na frente, quem ataca de longe atrás.
    private static readonly (double X, double Y)[] Front = [(17.5, 8.5), (18.5, 10.5), (17.5, 12.5), (16.5, 10.5)];
    private static readonly (double X, double Y)[] Back = [(20.5, 9.5), (20.5, 11.5), (21.5, 10.5), (21.5, 8.5)];

    /// <summary>Catálogo com o encontro do grupo contra uma classe (Defeat 120 s, como os solos do protótipo).</summary>
    public static (ContentCatalog Content, string EncounterId) Encounter(FieldGroup group, string cls)
    {
        var c = DesignHarness.Content;
        var id = $"encounter.test_fields_{Math.Abs(group.Key.GetHashCode(StringComparison.Ordinal))}_{cls[(cls.IndexOf('.') + 1)..]}";
        int front = 0, back = 0;
        var enemies = new List<EnemySpawnDefinition>();
        foreach (var p in group.Points)
        {
            var ranged = c.Monsters[p.Monster].Ai.Id == "ai.ranged_kiter";
            for (var i = 0; i < p.Count; i++)
            {
                var (x, y) = ranged ? Back[back++] : Front[front++];
                enemies.Add(new EnemySpawnDefinition { Monster = p.Monster, Position = new PositionDefinition(x, y), Level = p.Level });
            }
        }

        var encounter = new EncounterDefinition
        {
            Id = id,
            Name = group.Key,
            Arena = "arena.fields",
            Players = [new PlayerSpawnDefinition { Class = cls, Position = new PositionDefinition(5.5, 10.5) }],
            Enemies = enemies,
            Defeat = new DefeatDefinition { TimeLimitTicks = 120 * Core.Simulation.World.TickRate },
        };
        var content = new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Append(encounter), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
        };
        return (content, id);
    }

    public sealed record LevelPace(int Level, double Minutes, double XpPerMinute);

    /// <summary>Minutos em cada nível de 1 a 5 (modelo da doc de <c>Solo_player_reaches_level_4_to_5_in_about_an_hour</c>).</summary>
    public static List<LevelPace> Pace(Func<FieldGroup, double> fightSeconds)
    {
        var rules = DesignHarness.Content.Rules.Progression;
        var maxTier = All.Max(g => g.Level);
        var result = new List<LevelPace>();
        for (var level = 1; level <= 5; level++)
        {
            var tier = Math.Min(level, maxTier);
            var hunted = All.Where(g => g.Level == tier).ToList();
            var xpPerGroup = hunted.Average(g => g.Points.Sum(p =>
                p.Count * Math.Round(rules.MonsterXp(p.Level ?? 1, DesignHarness.Content.Monsters[p.Monster].Rank, level))));
            var cycle = hunted.Average(g => fightSeconds(g)) + FieldsDesignTests.OverheadSeconds;

            // Grupos por segundo: o que o jogador consegue limpar, mas no máximo o que renasce.
            var groupsPerSecond = Math.Min(1 / cycle, hunted.Sum(g => 1 / g.RespawnSeconds));
            var xpPerMinute = xpPerGroup * groupsPerSecond * 60;
            result.Add(new LevelPace(level, rules.XpToNext(level) / xpPerMinute, xpPerMinute));
        }

        return result;
    }
}
