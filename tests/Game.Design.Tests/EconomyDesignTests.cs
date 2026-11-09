using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;

namespace Game.Design.Tests;

/// <summary>
/// T-322: metas da economia mínima (ouro dos monstros e lojas com preço fixo), no modelo de ritmo solo dos Campos da T-215/T-302
/// (<see cref="FieldGroups.Pace"/>). Todos os números (ouro, preços, valor dos itens, venda) saem de <c>data/</c>.
/// Relatório: <c>tasks/reports/balance-v0.10.md</c>.
/// </summary>
public class EconomyDesignTests(ITestOutputHelper output)
{
    private const string Potion = "consumable.minor_healing_potion";
    private const string Smith = "shop.smith";
    private const string Temple = "shop.temple";

    /// <summary>Mesmo ritmo do teste de nível (nível 1 ao 5 nos Campos), com o ouro e a venda por minuto.</summary>
    private static List<FieldGroups.LevelPace> Pace() => FieldsDesignTests.CachedPace();

    private static string Report(List<FieldGroups.LevelPace> pace) => string.Join("; ", pace.Select(p =>
        $"nv{p.Level} (grupos nv{p.Tier}): {p.Minutes:F1} min, {p.GoldPerMinute * 60:F0} ouro/h, venda {p.SalePerMinute * 60:F1}/h"));

    /// <summary>30 minutos de caça nos Campos (média do nível 1 ao 5, pesada pelo tempo) pagam 8–12 poções de vida.</summary>
    [Fact]
    public void Thirty_minutes_in_the_fields_pay_8_to_12_healing_potions()
    {
        var pace = Pace();
        var minutes = pace.Sum(p => p.Minutes);
        var goldPerMinute = pace.Sum(p => p.Minutes * p.GoldPerMinute) / minutes;
        var potions = 30 * goldPerMinute / Economy.Price(Temple, Potion);
        output.WriteLine(Report(pace));
        Assert.True(potions is >= 8 and <= 12, $"30 min = {30 * goldPerMinute:F0} ouro = {potions:F1} poções; {Report(pace)}");
    }

    /// <summary>
    /// Um equipamento comum do próprio nível no Ferreiro custa 20–40 min de caça nos grupos desse nível (níveis com área nos
    /// Campos e oferta no Ferreiro). Vale para todas as bases do Ferreiro naquele nível.
    /// </summary>
    [Fact]
    public void A_common_item_of_your_level_costs_20_to_40_minutes_of_hunting()
    {
        var pace = Pace();
        var offers = DesignHarness.Content.Shops[Smith].Offers;
        var levels = pace.Where(p => p.Tier == p.Level && offers.Any(o => o.Level == p.Level)).ToList();
        Assert.NotEmpty(levels);
        foreach (var p in levels)
        {
            foreach (var offer in offers.Where(o => o.Level == p.Level))
            {
                var minutes = Economy.Price(Smith, offer.Item, offer.Level) / p.GoldPerMinute;
                Assert.True(minutes is >= 20 and <= 40, $"{offer.Item} nv{offer.Level}: {minutes:F1} min; {Report(pace)}");
            }

            output.WriteLine($"nv{p.Level}: {Economy.Price(Smith, offers.First(o => o.Level == p.Level).Item, p.Level)} ouro = "
                + $"{Economy.Price(Smith, offers.First(o => o.Level == p.Level).Item, p.Level) / p.GoldPerMinute:F1} min");
        }
    }

    /// <summary>Vender tudo o que cai rende menos de 30% do ouro direto dos monstros (nível 1 ao 5 nos Campos).</summary>
    [Fact]
    public void Selling_every_drop_yields_less_than_30_percent_of_the_monster_gold()
    {
        var pace = Pace();
        var gold = pace.Sum(p => p.Minutes * p.GoldPerMinute);
        var sale = pace.Sum(p => p.Minutes * p.SalePerMinute);
        output.WriteLine($"venda/ouro = {sale / gold:P1}; {Report(pace)}");
        Assert.True(sale / gold < 0.30, $"venda = {sale / gold:P1} do ouro; {Report(pace)}");
    }

    /// <summary>Ouro por hora nos Campos fica positivo em todos os níveis e cresce com o nível dos grupos.</summary>
    [Fact]
    public void Gold_per_hour_grows_with_the_hunted_level()
    {
        var pace = Pace();
        var byTier = pace.GroupBy(p => p.Tier).OrderBy(g => g.Key).Select(g => g.Average(p => p.GoldPerMinute * 60)).ToList();
        Assert.All(byTier, g => Assert.True(g > 0));
        Assert.Equal(byTier.Order(), byTier);
        output.WriteLine(Report(pace));
    }

    // ---------- poção de Mana ----------

    private const string ManaPotion = "consumable.minor_mana_potion";

    /// <summary>
    /// Limiar do recurso para o bot beber a poção de Mana nos testes: metade da Mana. É o caso mais favorável à poção (bebe
    /// cedo, sem perder a recuperação), e o Clérigo contra o Guarda nunca desce a 30%.
    /// </summary>
    private const string ManaPotionBelow = "0.5";

    /// <summary>Conteúdo em que a classe começa também com 1 poção de Mana (1 por luta).</summary>
    private static ContentCatalog WithOneManaPotion(string cls)
    {
        var c = DesignHarness.Content;
        var classes = c.Classes.Values.Select(k => k.Id != cls ? k : k with
        {
            StartingInventory = [.. k.StartingInventory, new StartingItemDefinition { Item = ManaPotion, Count = 1 }],
        });
        return new ContentCatalog(classes, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values, c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
            Shops = c.Shops,
        };
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> ManaCache = new();

    private static BatchSummary RunWithManaPotion(string encounter, string cls, CharacterProfile profile, int level) =>
        ManaCache.GetOrAdd($"{encounter}|{profile.Label}|{level}", _ => new Lazy<BatchSummary>(() =>
        {
            var options = new Dictionary<string, string>(DesignHarness.Options) { ["manaPotionBelow"] = ManaPotionBelow };
            return Batch.Run(WithOneManaPotion(cls), new RunSpec
            {
                EncounterId = encounter,
                Strategy = "rotation",
                Options = new BotOptions(options),
                Runs = DesignHarness.Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = profile with { Focus = DesignHarness.SoloFocus },
                MonsterLevel = level,
            });
        })).Value;

    /// <summary>
    /// Poção de Mana não resolve sozinha a falta de Mana (design/progression-v0.1.md §8): nos solos do Mago e do Clérigo, o
    /// perfil fraco bebendo 1 poção por luta (com metade da Mana) continua vencendo no máximo 50%. O Clérigo fraco contra o
    /// Guarda já vence sem poção (exceção da §8, balance-v0.7 §3) e não chega a precisar dela; para ele vale a meta de tempo:
    /// pelo menos 1,3× o perfil esperado.
    /// </summary>
    [Theory]
    [InlineData("encounter.four_rats", "class.mage", 5)]
    [InlineData("encounter.four_rats", "class.mage", 9)]
    [InlineData("encounter.skeleton_guard", "class.cleric", 5)]
    [InlineData("encounter.skeleton_guard", "class.cleric", 9)]
    public void One_mana_potion_per_fight_does_not_carry_the_weak_profile(string encounter, string cls, int level)
    {
        var weak = RunWithManaPotion(encounter, cls, CharacterProfile.Weak(level), level);
        var drunk = weak.AbilityUses.GetValueOrDefault("item.minor_mana_potion");
        var text = $"{encounter} nv{level} fraco + 1 poção de Mana: win {weak.WinRate:P0}, t {weak.TimeToWinMean:F1}s, poções {drunk:F2}/luta";
        output.WriteLine(text);
        if (cls == "class.cleric")
        {
            // Contra o Guarda, a Mana do Clérigo não chega a cair à metade: ele nem bebe (balance-v0.10 §4).
            var expected = DesignHarness.RunProfile(encounter, "rotation", CharacterProfile.Expected(level), level);
            Assert.True(weak.TimeToWinMean >= 1.3 * expected.TimeToWinMean, $"{text}; esperado t {expected.TimeToWinMean:F1}s");
            return;
        }

        Assert.True(drunk > 0, "o bot não bebeu a poção de Mana: " + text);
        Assert.True(weak.WinRate <= 0.50, text);
        var without = DesignHarness.RunProfile(encounter, "rotation", CharacterProfile.Weak(level), level);
        output.WriteLine($"sem poção: win {without.WinRate:P0}");
    }
}
