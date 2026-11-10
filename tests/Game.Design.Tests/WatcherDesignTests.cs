using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-351: o Vigia Sem Rosto (nível 15, chefe; <c>design/combat-encounters-v0.1.md</c> § Vigia) contra um grupo de 4 nos
/// níveis 14 e 15 com raros (<c>design/progression-v0.1.md</c> §8: vitória 50–80%, 4–6 min). A estratégia correta: o Guerreiro
/// guarda o Golpe Pesado para interromper (<c>interrupt=save</c>) e o Clérigo vai até os selos feridos
/// (<c>followInjured</c>). Comparações: sem Clérigo, sem Guerreiro e sem interromper o Último Comando.
/// Relatório: <c>tasks/reports/balance-v0.13.md</c>.
/// </summary>
public class WatcherDesignTests(ITestOutputHelper output)
{
    public const string Encounter = "encounter.faceless_watcher";

    private static readonly string[] FullGroup = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    private static readonly Dictionary<string, string> Strategy = new()
    {
        ["class.warrior"] = "rotation+dodge",
        ["class.archer"] = "kite+dodge",
        ["class.mage"] = "kite+dodge",
        ["class.cleric"] = "rotation+dodge",
    };

    /// <summary>Lotes de comparação, longe do limite (0–10% contra 30%): metade das execuções basta.</summary>
    private static int ComparisonRuns => Math.Max(20, Runs / 2);

    private static string Fmt(BatchSummary s) =>
        $"{s.Encounter} [{s.Strategy}]: win {s.WinRate:P0}, t {s.TimeToWinMean:F0}s, selos perdidos {s.ObjectivesLost:F2}, falhas {s.Failures}, mortes {s.PlayerDeaths:F2}";

    private static CharacterProfile Profile(int level) => CharacterProfile.Expected(level) with { Gear = ItemRarity.Rare };

    /// <summary>
    /// Grupo de 4 nos níveis 14 e 15 com raros: a vitória média dos dois níveis fica entre 50% e 80% e cada nível vence em 4–6
    /// min. Há um degrau entre os níveis (vida, atributos e itens; a assinatura quase não pesa: balance-v0.13 §3 e §4), então cada nível individual
    /// só precisa ficar entre 40% e 90%.
    /// </summary>
    [Fact]
    public void A_group_of_four_with_rares_beats_the_watcher_50_to_80_percent_in_4_to_6_minutes()
    {
        var batches = new[] { 14, 15 }.Select(l => Group(l, "save", Runs, FullGroup)).ToList();
        var report = string.Join("; ", batches.Select(Fmt));
        output.WriteLine(report);

        Assert.InRange(batches.Average(s => s.WinRate), 0.50, 0.80);
        Assert.All(batches, s =>
        {
            Assert.True(s.WinRate is >= 0.40 and <= 0.90, report);
            Assert.True(s.TimeToWinMean is >= 240 and <= 360, report);
        });
    }

    /// <summary>Sem o Clérigo (Guerreiro + Arqueiro + Mago + Mago), ninguém segura os selos: a vitória cai de forma clara.</summary>
    [Fact]
    public void Without_the_cleric_the_win_rate_drops_clearly()
    {
        var full = Group(15, "save", Runs, FullGroup);
        var noCleric = Group(15, "save", ComparisonRuns, "class.warrior", "class.archer", "class.mage", "class.mage");
        output.WriteLine($"{Fmt(full)} -> {Fmt(noCleric)}");

        Assert.True(noCleric.WinRate <= Math.Min(0.30, full.WinRate - 0.30), $"{Fmt(full)} -> {Fmt(noCleric)}");
    }

    /// <summary>Sem o Guerreiro (Arqueiro + Arqueiro + Mago + Clérigo), ninguém interrompe nem segura o chefe: a vitória cai de forma clara.</summary>
    [Fact]
    public void Without_the_warrior_the_win_rate_drops_clearly()
    {
        var full = Group(15, "save", Runs, FullGroup);
        var noWarrior = Group(15, "save", ComparisonRuns, "class.archer", "class.archer", "class.mage", "class.cleric");
        output.WriteLine($"{Fmt(full)} -> {Fmt(noWarrior)}");

        Assert.True(noWarrior.WinRate <= Math.Min(0.30, full.WinRate - 0.30), $"{Fmt(full)} -> {Fmt(noWarrior)}");
    }

    /// <summary>
    /// Sem interromper o Último Comando (o Guerreiro nunca usa o Golpe Pesado no chefe enquanto ele conjura), a vitória fica
    /// abaixo de 30% nos dois níveis.
    /// </summary>
    [Theory]
    [InlineData(14)]
    [InlineData(15)]
    public void Without_interrupting_the_last_command_the_win_rate_stays_below_30_percent(int level)
    {
        var s = Group(level, "never", ComparisonRuns, FullGroup);
        output.WriteLine(Fmt(s));
        Assert.True(s.WinRate < 0.30, Fmt(s));
    }

    // ---------- lotes ----------

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    private static string VariantId(string[] classes) =>
        classes.SequenceEqual(FullGroup) ? Encounter : $"{Encounter}.with_{string.Join("_", classes.Select(c => c[(c.IndexOf('.') + 1)..]))}";

    /// <summary>O Vigia contra <paramref name="classes"/> (nas posições do encontro), perfil do nível com raros.</summary>
    internal static BatchSummary Group(int level, string interrupt, int runs, params string[] classes)
    {
        var id = VariantId(classes);
        var key = $"{id}|{level}|{interrupt}|{runs}";
        return Cache.GetOrAdd(key, _ => new Lazy<BatchSummary>(() =>
        {
            var options = new Dictionary<string, string>(Options)
            {
                ["class.warrior:interrupt"] = interrupt,
                ["class.cleric:followInjured"] = "1",
            };
            return Batch.Run(Variant(classes), new RunSpec
            {
                EncounterId = id,
                Strategy = "rotation+dodge",
                Roles = classes.Distinct().Where(c => Strategy[c] != "rotation+dodge").ToDictionary(c => c, c => Strategy[c]),
                Options = new BotOptions(options),
                Runs = runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = Profile(level),
            });
        })).Value;
    }

    private static ContentCatalog Variant(string[] classes)
    {
        var c = Content;
        if (classes.SequenceEqual(FullGroup))
        {
            return c;
        }

        var real = c.Encounters[Encounter];
        var variant = real with
        {
            Id = VariantId(classes),
            Players = classes.Select((cls, i) => real.Players[i] with { Class = cls }).ToList(),
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
