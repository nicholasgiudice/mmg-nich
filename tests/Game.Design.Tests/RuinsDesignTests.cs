using Game.Bots;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-341: monstros das Ruínas da Estrada Antiga (Sacerdote Morto nv10 e Cavaleiro Espectral nv11, elite;
/// <c>design/combat-encounters-v0.1.md</c> § Ruínas). Pátio dos Mortos (3 Guardas nv9 + 1 Sacerdote nv10) contra um par e
/// solo, com e sem foco no Sacerdote; o Cavaleiro contra grupos de 3 nos níveis 10 e 11 com incomuns.
/// Relatório: <c>tasks/reports/balance-v0.12.md</c>.
/// </summary>
public class RuinsDesignTests(ITestOutputHelper output)
{
    public const string Courtyard = "encounter.dead_courtyard";
    public const string Knight = "encounter.spectral_knight";
    public const string Priest = "monster.dead_priest";

    /// <summary>Nível do grupo típico do Pátio (o do Sacerdote).</summary>
    public const int CourtyardLevel = 10;

    private static readonly Dictionary<string, string> Strategy = new()
    {
        // Como na Floresta (balance-v0.11): todos desviam dos telegráfos; Arqueiro e Mago atacam de longe.
        ["class.warrior"] = "rotation+dodge",
        ["class.archer"] = "kite+dodge",
        ["class.mage"] = "kite+dodge",
        ["class.cleric"] = "rotation+dodge",
    };

    private static readonly string[] AllClasses = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    public static TheoryData<string, string> Duos() => new()
    {
        { "class.warrior", "class.cleric" }, // o par do encontro
        { "class.warrior", "class.archer" },
        { "class.warrior", "class.mage" },
        { "class.archer", "class.cleric" },
        { "class.mage", "class.cleric" },
    };

    public static TheoryData<string, string, string> KnightGroups() => new()
    {
        { "class.warrior", "class.archer", "class.cleric" }, // o grupo do encontro
        { "class.warrior", "class.mage", "class.cleric" },
    };

    private static string Fmt(BatchSummary s) =>
        $"{s.Encounter} [{s.Strategy}]: win {s.WinRate:P0}, t {s.TimeToWinMean:F1}s, dano recebido {s.DamageTaken:F0}";

    // ---------- Pátio dos Mortos ----------

    /// <summary>Um par no nível do grupo (perfil esperado, foco no mais próximo) vence o Pátio em pelo menos 80%.</summary>
    [Theory]
    [MemberData(nameof(Duos))]
    public void A_duo_beats_the_dead_courtyard_at_least_80_percent(string a, string b)
    {
        var s = Group(Courtyard, CharacterProfile.Expected(CourtyardLevel), focusPriest: false, a, b);
        output.WriteLine(Fmt(s));
        Assert.True(s.WinRate >= 0.80, Fmt(s));
    }

    /// <summary>
    /// Solo é possível, mas arriscado: a média das 4 classes sozinhas (perfil esperado, foco no mais próximo) fica entre 40% e
    /// 70%, e pelo menos uma classe vence a maior parte das vezes.
    /// </summary>
    [Fact]
    public void Solo_against_the_dead_courtyard_is_possible_but_risky()
    {
        var solos = AllClasses.Select(c => Group(Courtyard, CharacterProfile.Expected(CourtyardLevel), focusPriest: false, c)).ToList();
        var report = string.Join("; ", solos.Select(Fmt));
        output.WriteLine(report);

        Assert.InRange(solos.Average(s => s.WinRate), 0.40, 0.70);
        Assert.Contains(solos, s => s.WinRate >= 0.5);
    }

    /// <summary>
    /// Prioridade de alvo: focar o Sacerdote primeiro (opção <c>focusFirst</c> dos bots) termina o Pátio mais rápido com o par
    /// do encontro, e sozinho nenhuma classe vence menos vezes por focar nele.
    /// </summary>
    [Fact]
    public void Focusing_the_priest_first_finishes_faster()
    {
        var profile = CharacterProfile.Expected(CourtyardLevel);
        var nearest = Group(Courtyard, profile, focusPriest: false, "class.warrior", "class.cleric");
        var focused = Group(Courtyard, profile, focusPriest: true, "class.warrior", "class.cleric");
        output.WriteLine($"{Fmt(nearest)} -> {Fmt(focused)}");
        Assert.True(focused.WinRate >= nearest.WinRate, $"{Fmt(nearest)} -> {Fmt(focused)}");
        Assert.True(focused.TimeToWinMean < nearest.TimeToWinMean, $"{Fmt(nearest)} -> {Fmt(focused)}");

        foreach (var cls in AllClasses)
        {
            var solo = Group(Courtyard, profile, focusPriest: false, cls);
            var soloFocused = Group(Courtyard, profile, focusPriest: true, cls);
            output.WriteLine($"{Fmt(solo)} -> {Fmt(soloFocused)}");
            Assert.True(soloFocused.WinRate >= solo.WinRate, $"{Fmt(solo)} -> {Fmt(soloFocused)}");
        }
    }

    // ---------- Cavaleiro Espectral ----------

    /// <summary>
    /// Grupo de 3 nos níveis 10 e 11 com equipamento incomum: a vitória média (dois grupos × dois níveis) fica entre 70% e 90%,
    /// e cada lote, quando vence, leva de 60 a 120 s.
    /// </summary>
    [Fact]
    public void A_group_of_three_beats_the_spectral_knight_70_to_90_percent_in_60_to_120_seconds()
    {
        var batches = new List<BatchSummary>();
        foreach (var level in new[] { 10, 11 })
        {
            foreach (var group in KnightGroups())
            {
                var classes = group.Select(o => (string)o).ToArray();
                batches.Add(Group(Knight, CharacterProfile.Expected(level) with { Gear = ItemRarity.Uncommon }, focusPriest: false, classes));
            }
        }

        var report = string.Join("; ", batches.Select(Fmt));
        output.WriteLine(report);
        Assert.InRange(batches.Average(s => s.WinRate), 0.70, 0.90);
        Assert.All(batches, s => Assert.True(s.WinRate == 0 || s.TimeToWinMean is >= 60 and <= 120, report));
    }

    // ---------- lotes ----------

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    private static string VariantId(string encounter, string[] classes) =>
        $"{encounter}.with_{string.Join("_", classes.Select(c => c[(c.IndexOf('.') + 1)..]))}";

    /// <summary>Encontro com os jogadores trocados por <paramref name="classes"/> (na ordem; a primeira é a estratégia base).</summary>
    internal static BatchSummary Group(string encounter, CharacterProfile profile, bool focusPriest, params string[] classes)
    {
        var id = VariantId(encounter, classes);
        var key = $"{id}|{profile.Label}|{focusPriest}";
        return Cache.GetOrAdd(key, _ => new Lazy<BatchSummary>(() =>
        {
            var options = new Dictionary<string, string>(Options);
            if (focusPriest)
            {
                options["focusFirst"] = Priest;
            }

            return Batch.Run(Variants(encounter, classes), new RunSpec
            {
                EncounterId = id,
                Strategy = Strategy[classes[0]],
                Roles = classes.Skip(1).Where(c => Strategy[c] != Strategy[classes[0]]).ToDictionary(c => c, c => Strategy[c]),
                Options = new BotOptions(options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = classes.Length == 1 ? profile with { Focus = SoloFocus } : profile,
            });
        })).Value;
    }

    private static readonly PositionDefinition[] Spawns = [new(4.5, 9.5), new(3.5, 10.5), new(3.5, 8.5)];

    private static ContentCatalog Variants(string encounter, string[] classes)
    {
        var c = Content;
        var real = c.Encounters[encounter];
        var variant = real with
        {
            Id = VariantId(encounter, classes),
            Players = classes.Select((cls, i) => new PlayerSpawnDefinition { Class = cls, Position = Spawns[i] }).ToList(),
        };
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values.Append(variant), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
            Shops = c.Shops,
        };
    }
}
