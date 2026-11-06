using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-340: monstros novos da Floresta Baixa (Fada da Névoa nv6 e Ent Corrompido nv7, <c>design/combat-encounters-v0.1.md</c>
/// § Floresta Baixa). Os encontros de <c>data/encounters.json</c> rodam solo nas 4 classes (o jogador do encontro é trocado
/// pela classe) com o perfil do nível do grupo (T-206): o esperado vence ≥ 85% em até 40 s e o fraco (3 níveis abaixo)
/// no máximo 50%. A Emboscada na Névoa (2 Aranhas + Fada) mostra a vantagem de um grupo de 2 sobre o solo.
/// Relatório: <c>tasks/reports/balance-v0.11.md</c>.
/// </summary>
public class ForestDesignTests
{
    public const string Fairies = "encounter.mist_fairies";
    public const string Ent = "encounter.corrupted_ent";
    public const string Ambush = "encounter.mist_ambush";

    private static readonly (string Class, string Strategy)[] Classes =
    [
        // A Floresta ensina posicionamento: os bots desviam dos telegráfos (+dodge), como na Matriarca. O Mago ataca de longe
        // e foge, como nos Campos (balance-v0.8 §1).
        ("class.warrior", "rotation+dodge"),
        ("class.archer", "kite+dodge"),
        ("class.mage", "kite+dodge"),
        ("class.cleric", "rotation+dodge"),
    ];

    /// <summary>Nível do grupo típico = maior nível dos monstros do encontro.</summary>
    public static int GroupLevel(string encounter) => Content.Encounters[encounter].Enemies.Max(e => e.Level ?? 1);

    public static TheoryData<string, string> GroupsAndClasses()
    {
        var data = new TheoryData<string, string>();
        foreach (var encounter in new[] { Fairies, Ent })
        {
            foreach (var (cls, _) in Classes)
            {
                data.Add(encounter, cls);
            }
        }

        return data;
    }

    private static string Fmt(BatchSummary s) => $"{s.Encounter} / {s.Strategy}: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s, dano recebido {s.DamageTaken:F0}";

    [Theory]
    [MemberData(nameof(GroupsAndClasses))]
    public void Expected_profile_wins_solo_at_least_85_percent_within_40_seconds(string encounter, string cls)
    {
        var s = Solo(encounter, cls, CharacterProfile.Expected(GroupLevel(encounter)));
        Assert.True(s.WinRate >= 0.85, Fmt(s));
        Assert.True(s.TimeToWinMean <= 40, Fmt(s));
    }

    /// <summary>
    /// Perfil fraco vence no máximo metade. Exceção do Clérigo, a mesma do Guarda Esquelético (balance-v0.7 §3): ele se cura
    /// indefinidamente e vence mesmo fraco; para ele a meta é demorar pelo menos 1,3× o perfil esperado (balance-v0.11 §3).
    /// </summary>
    [Theory]
    [MemberData(nameof(GroupsAndClasses))]
    public void Weak_profile_wins_solo_at_most_half(string encounter, string cls)
    {
        var weak = Solo(encounter, cls, CharacterProfile.Weak(GroupLevel(encounter)));
        if (cls == "class.cleric")
        {
            var expected = Solo(encounter, cls, CharacterProfile.Expected(GroupLevel(encounter)));
            Assert.True(weak.WinRate == 0 || weak.TimeToWinMean >= 1.3 * expected.TimeToWinMean, $"{Fmt(weak)} vs {Fmt(expected)}");
            return;
        }

        Assert.True(weak.WinRate <= 0.50, Fmt(weak));
    }

    /// <summary>
    /// Vantagem de grupo (Emboscada na Névoa, perfil esperado nv6): o par Guerreiro + Arqueiro vence ≥ 95%, em no máximo 70%
    /// do tempo médio dos dois solos, e cada um do par recebe no máximo 60% do dano que recebe sozinho (média dos dois solos).
    /// Os solos também vencem: jogar sozinho é possível.
    /// </summary>
    [Fact]
    public void Duo_beats_solo_against_spiders_and_fairy()
    {
        var profile = CharacterProfile.Expected(GroupLevel(Ambush));
        var duo = Duo(profile);
        var solos = new[] { Solo(Ambush, "class.warrior", profile), Solo(Ambush, "class.archer", profile) };
        var report = $"{Fmt(duo)}; solos: {string.Join("; ", solos.Select(Fmt))}";

        Assert.True(duo.WinRate >= 0.95, report);
        Assert.All(solos, s => Assert.True(s.WinRate >= 0.85, report));
        Assert.True(duo.TimeToWinMean <= 0.7 * solos.Average(s => s.TimeToWinMean), report);
        Assert.True(duo.DamageTaken / 2 <= 0.6 * solos.Average(s => s.DamageTaken), report);
    }

    // ---- lotes ----

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    /// <summary>Catálogo real mais as variantes solo (<c>{encontro}.solo_{classe}</c>) de cada encontro da Floresta.</summary>
    internal static ContentCatalog Variants { get; } = BuildVariants(Content);

    internal static string SoloId(string encounter, string cls) => $"{encounter}.solo_{cls[(cls.IndexOf('.') + 1)..]}";

    internal static BatchSummary Solo(string encounter, string cls, CharacterProfile profile) =>
        Cache.GetOrAdd($"{encounter}|{cls}|{profile.Label}", _ => new Lazy<BatchSummary>(() => Batch.Run(Variants, new RunSpec
        {
            EncounterId = SoloId(encounter, cls),
            Strategy = Classes.Single(c => c.Class == cls).Strategy,
            Options = new Game.Bots.BotOptions(Options),
            Runs = Runs,
            HazardSourceFactory = () => new Game.Bots.TelegraphHazardSource(),
            Profile = profile with { Focus = SoloFocus },
        }))).Value;

    internal static BatchSummary Duo(CharacterProfile profile) =>
        Cache.GetOrAdd($"{Ambush}|duo|{profile.Label}", _ => new Lazy<BatchSummary>(() => Batch.Run(Variants, new RunSpec
        {
            EncounterId = Ambush,
            Strategy = "rotation+dodge",
            Roles = new Dictionary<string, string> { ["class.archer"] = "kite+dodge" },
            Options = new Game.Bots.BotOptions(Options),
            Runs = Runs,
            HazardSourceFactory = () => new Game.Bots.TelegraphHazardSource(),
            Profile = profile,
        }))).Value;

    private static ContentCatalog BuildVariants(ContentCatalog c)
    {
        var extra = new List<EncounterDefinition>();
        foreach (var id in new[] { Fairies, Ent, Ambush })
        {
            var real = c.Encounters[id];
            foreach (var (cls, _) in Classes)
            {
                extra.Add(real with
                {
                    Id = SoloId(id, cls),
                    Players = [new PlayerSpawnDefinition { Class = cls, Position = new PositionDefinition(5.5, 10.5) }],
                });
            }
        }

        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Concat(extra), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
        };
    }
}
