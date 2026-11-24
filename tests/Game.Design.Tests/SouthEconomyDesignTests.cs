using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>
/// T-513: economia e itens da região do sul (níveis 16 a 27): a loja do Forte do Passo (<c>shop.pass_fort</c>), as poções que
/// ainda fazem diferença no nível 25 e as quedas e o ouro dos monstros novos (T-510 a T-512). Os números saem de <c>data/</c>.
/// Estende o modelo de ritmo da T-322 (<see cref="EconomyDesignTests"/>): tempo de luta do grupo (simulador, perfil esperado do
/// nível do grupo, média das 4 classes base) mais <see cref="FieldsDesignTests.OverheadSeconds"/>, sem o limite de renascimento
/// (os mapas do sul ainda não têm os pontos de nascimento; a T-550 confere no playtest). Relatório: <c>tasks/reports/balance-v0.27.md</c>.
/// </summary>
public class SouthEconomyDesignTests(ITestOutputHelper output)
{
    private const string Fort = "shop.pass_fort";
    private const string Smith = "shop.smith";

    private static readonly string[] BaseClasses = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    /// <summary>Perfil por classe base na Serra (uma segunda classe por classe base; a medida do tempo de luta é a do perfil esperado).</summary>
    private static readonly string[] RidgeSecondClasses =
        [SecondClassDesignTests.Guardian, SecondClassDesignTests.Marksman, SecondClassDesignTests.Pyromancer, SecondClassDesignTests.Priest];

    private static readonly string[] MarshGroups = [MarshDesignTests.Drowned, MarshDesignTests.Lizardmen, MarshDesignTests.Serpents, MarshDesignTests.Mixed];
    private static readonly string[] RidgeGroups = [RidgeDesignTests.Miners, RidgeDesignTests.Wolves, RidgeDesignTests.Bats];

    /// <summary>
    /// Metas por hora de caça do nível: poções "do nível" que o ouro paga. A meta do primeiro mapa (8 a 12 em 30 min = 16 a 24 por
    /// hora) não cabe no sul com um preço só: o Brejo rende ~1.500 ouro/h e a Serra ~4.000 (grupos de 4 a 6 monstros, contra 2 a 3),
    /// uma razão de 2,6×; a janela de 10 a 40 por hora cobre as duas (balance-v0.27 §2).
    /// </summary>
    private const double MinPotionsPerHour = 10;
    private const double MaxPotionsPerHour = 40;

    /// <summary>Fração mínima da vida e da Mana máximas que a melhor poção da loja do Forte devolve, em cada nível de 16 a 27.</summary>
    private const double MinPotionFraction = 0.5;

    // ---------- o que o jogador gasta e ganha ----------

    private sealed record Hunt(string Zone, int FirstLevel, int LastLevel, double GoldPerHour, double CycleSeconds, double GoldPerGroup);

    private static readonly Lazy<List<Hunt>> HuntCache = new(() =>
    [
        Measure("Brejo", 16, 20, MarshGroups, g => BaseClasses.Average(c => MarshDesignTests.Solo(g, c, CharacterProfile.Expected(MarshDesignTests.GroupLevel(g))).TimeToWinMean)),
        Measure("Serra", 21, 27, RidgeGroups, g => RidgeSecondClasses.Average(s => RidgeDesignTests.Solo(g, s, RidgeDesignTests.Expected).TimeToWinMean)),
    ]);

    private static Hunt Measure(string zone, int first, int last, string[] groups, Func<string, double> fightSeconds)
    {
        var rules = Content.Rules.Progression;
        var cycle = groups.Average(fightSeconds) + FieldsDesignTests.OverheadSeconds;
        var gold = groups.Average(g => Content.Encounters[g].Enemies.Sum(e => GoldOf(e)));
        return new Hunt(zone, first, last, gold / cycle * 3600, cycle, gold);

        double GoldOf(EnemySpawnDefinition e) => rules.GoldPerMonsterLevel * (e.Level ?? 1) * rules.GoldByRank.For(Content.Monsters[e.Monster].Rank);
    }

    private static Hunt HuntAt(int level) => HuntCache.Value.Single(h => level >= h.FirstLevel && level <= h.LastLevel);

    private static string Report() => string.Join("; ", HuntCache.Value.Select(h =>
        $"{h.Zone} nv{h.FirstLevel}-{h.LastLevel}: ciclo {h.CycleSeconds:F0} s, {h.GoldPerGroup:F0} ouro/grupo, {h.GoldPerHour:F0} ouro/h"));

    /// <summary>Preço da melhor poção da loja do Forte (a de maior efeito) de cada tipo.</summary>
    private static IEnumerable<ItemBaseDefinition> FortPotions(string abilityTag) =>
        Content.Shops[Fort].Offers.Select(o => Content.Items[o.Item]).Where(i => i.IsConsumable).DistinctBy(i => i.Id)
            .Where(i => Content.Abilities[i.UseAbility!].Tags.Contains(abilityTag));

    private static double Heal(ItemBaseDefinition potion, double vit)
    {
        var effect = Content.Abilities[potion.UseAbility!].Effects.OfType<HealEffect>().Single();
        return effect.Base + effect.Scaling.GetValueOrDefault("vit") * vit;
    }

    private static double ManaOf(ItemBaseDefinition potion) =>
        Content.Abilities[potion.UseAbility!].Effects.OfType<ResourceEffect>().Single().Amount;

    // ---------- testes ----------

    /// <summary>
    /// A melhor poção de cura e de Mana que o Forte vende devolve pelo menos metade da vida e da Mana máximas do perfil esperado,
    /// em todas as classes e em todos os níveis de 16 a 27 (a Poção de Mana Maior de hoje não cumpre no Mago a partir do nível 15).
    /// </summary>
    [Fact]
    public void The_best_fort_potions_restore_at_least_half_of_health_and_mana_from_16_to_27()
    {
        var healing = FortPotions("heal").ToList();
        var mana = FortPotions("mana").ToList();
        var lowest = (Fraction: double.MaxValue, Text: "");
        foreach (var level in Enumerable.Range(16, 12))
        {
            foreach (var cls in BaseClasses)
            {
                var stats = LevelCurve.Measure(Content, cls, level);
                var heal = healing.Max(p => Heal(p, stats.Stats.Vit)) / stats.Health;
                var restore = mana.Max(ManaOf) / stats.Mana;
                if (level is 16 or 21 or 25 or 27)
                {
                    output.WriteLine($"nv{level} {cls}: vida {stats.Health:F0}, melhor cura {heal:P0}; Mana {stats.Mana:F0}, melhor Mana {restore:P0}");
                }

                foreach (var (fraction, what) in new[] { (heal, "cura"), (restore, "Mana") })
                {
                    if (fraction < lowest.Fraction)
                    {
                        lowest = (fraction, $"{what} nv{level} {cls}");
                    }
                }
            }
        }

        output.WriteLine($"menor fração: {lowest.Fraction:P0} ({lowest.Text})");
        Assert.True(lowest.Fraction >= MinPotionFraction, $"menor fração {lowest.Fraction:P0} em {lowest.Text}");
    }

    /// <summary>
    /// Registro da medida que motivou as poções novas: as Maiores de hoje devolvem menos de metade da Mana do Mago e da vida de
    /// alguma classe em algum nível de 16 a 27 (por isso as Superiores existem; balance-v0.27 §1).
    /// </summary>
    [Fact]
    public void The_greater_potions_alone_are_not_enough_from_16_to_27()
    {
        var greaterHeal = Content.Items["consumable.greater_healing_potion"];
        var greaterMana = Content.Items["consumable.greater_mana_potion"];
        var worstMana = Enumerable.Range(16, 12).Min(l => ManaOf(greaterMana) / LevelCurve.Measure(Content, "class.mage", l).Mana);
        var worstHeal = Enumerable.Range(16, 12).SelectMany(l => BaseClasses.Select(c =>
        {
            var s = LevelCurve.Measure(Content, c, l);
            return Heal(greaterHeal, s.Stats.Vit) / s.Health;
        })).Min();
        output.WriteLine($"Maior: pior cura {worstHeal:P0}, pior Mana (Mago) {worstMana:P0}");
        Assert.True(worstMana < MinPotionFraction, $"Mana Maior no Mago: {worstMana:P0}");
        Assert.True(worstHeal < MinPotionFraction, $"Cura Maior: {worstHeal:P0}");
    }

    /// <summary>
    /// Uma hora de caça do nível paga de 10 a 40 poções de cura do nível (<see cref="MinPotionsPerHour"/>), em todos os níveis
    /// de 16 a 27. A "poção de cura do nível" é a mais forte que o Forte vende.
    /// </summary>
    [Fact]
    public void One_hour_of_hunting_pays_10_to_40_of_the_best_healing_potions()
    {
        output.WriteLine(Report());
        var best = FortPotions("heal").OrderByDescending(p => p.Price).First();
        foreach (var level in Enumerable.Range(16, 12))
        {
            var potions = HuntAt(level).GoldPerHour / Economy.Price(Fort, best.Id, 1);
            Assert.True(potions is >= MinPotionsPerHour and <= MaxPotionsPerHour,
                $"nv{level} ({HuntAt(level).Zone}): {potions:F1} poções {best.Id} por hora; {Report()}");
        }
    }

    /// <summary>A poção de Mana mais forte custa o mesmo que a de cura mais forte (uma hora paga o mesmo número delas).</summary>
    [Fact]
    public void The_best_mana_and_healing_potions_cost_the_same()
    {
        var heal = FortPotions("heal").OrderByDescending(p => p.Price).First();
        var mana = FortPotions("mana").OrderByDescending(p => p.Price).First();
        Assert.Equal(heal.Price, mana.Price);
    }

    /// <summary>
    /// Equipamento comum do nível 16 e do 21 do Forte: custa de 10 a 40 minutos de caça no nível dele (a meta do primeiro mapa é
    /// 20–40; no sul os grupos são maiores e o ouro por hora sobe mais que o preço do item, que cresce linear com o nível, então
    /// o piso é menor: balance-v0.27 §3).
    /// </summary>
    [Fact]
    public void A_common_fort_item_of_your_level_costs_10_to_40_minutes_of_hunting()
    {
        var offers = Content.Shops[Fort].Offers.Where(o => !Content.Items[o.Item].IsConsumable).ToList();
        Assert.NotEmpty(offers);
        foreach (var level in offers.Select(o => o.Level).Distinct())
        {
            output.WriteLine($"nv{level}: {Economy.Price(Fort, offers.First(o => o.Level == level).Item, level)} ouro = "
                + $"{Economy.Price(Fort, offers.First(o => o.Level == level).Item, level) / HuntAt(level).GoldPerHour * 60:F1} min");
        }

        foreach (var offer in offers)
        {
            var minutes = Economy.Price(Fort, offer.Item, offer.Level) / HuntAt(offer.Level).GoldPerHour * 60;
            Assert.True(minutes is >= 10 and <= 40, $"{offer.Item} nv{offer.Level}: {minutes:F1} min; {Report()}");
        }
    }

    /// <summary>O Intendente vende, nos níveis 16 e 21, as armas e as armaduras que as 4 classes base usam, e as poções.</summary>
    [Fact]
    public void The_fort_shop_sells_gear_for_the_four_classes_at_levels_16_and_21_and_potions()
    {
        var shop = Content.Shops[Fort];
        foreach (var level in new[] { 16, 21 })
        {
            foreach (var cls in BaseClasses)
            {
                var usable = shop.Offers.Where(o => o.Level == level && Content.Items[o.Item].Slot is { } slot
                    && Game.Core.Combat.Items.ItemMath.CanUse(Content.Rules.Items, Content.Items[o.Item], cls)).ToList();
                Assert.Contains(usable, o => Content.Items[o.Item].Slot == EquipSlot.Weapon);
                foreach (var slot in new[] { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Hands, EquipSlot.Feet })
                {
                    Assert.Contains(usable, o => Content.Items[o.Item].Slot == slot);
                }
            }
        }

        Assert.Contains(shop.Offers, o => o.Item == "consumable.greater_healing_potion");
        Assert.Contains(shop.Offers, o => o.Item == "consumable.superior_mana_potion");
        // O Ferreiro da Cidade continua vendendo só os níveis 2 a 6 (o equipamento do 16 em diante é do Forte).
        Assert.All(Content.Shops[Smith].Offers, o => Assert.InRange(o.Level, 2, 6));
    }

    /// <summary>
    /// Quedas e ouro (T-513): toda a tabela é por categoria (<c>rules.items.drops</c>, <c>rules.progression.goldByRank</c>), então
    /// os monstros novos já têm queda e ouro; aqui se confere que cada categoria tem queda não vazia e ouro positivo, e que a
    /// Bruxa e o Capataz (chefes) levam a tabela de chefe: 100%, 2 itens, nunca comum, o primeiro pelo menos incomum.
    /// </summary>
    [Fact]
    public void Every_monster_from_16_to_27_has_a_drop_table_and_gold_and_the_bosses_use_the_boss_table()
    {
        var rules = Content.Rules.Progression;
        var southern = Content.Monsters.Values.Where(m => m.Level is >= 16 and <= 27).ToList();
        Assert.Equal(9, southern.Count); // Afogado, Homem-lagarto, Serpente, Bruxa, Mineiro, Lobo da Serra, Morcego, Golem, Capataz
        foreach (var m in southern)
        {
            var table = Content.Rules.Items.Drops.For(m.Rank);
            Assert.True(table.Chance > 0 && table.Count > 0 && table.Items.Count > 0, $"{m.Id}: tabela de queda vazia");
            Assert.True(rules.GoldPerMonsterLevel * m.Level * rules.GoldByRank.For(m.Rank) > 0, $"{m.Id}: sem ouro");
        }

        foreach (var boss in new[] { "monster.marsh_witch", "monster.forged_overseer" })
        {
            var m = Content.Monsters[boss];
            var table = Content.Rules.Items.Drops.For(m.Rank);
            Assert.Equal(MonsterRank.Boss, m.Rank);
            Assert.Equal((1.0, 2, 0.0), (table.Chance, table.Count, table.Rarity.For(ItemRarity.Common)));
            Assert.Equal(ItemRarity.Uncommon, table.GuaranteedRarity);
            // 20 × nível (Bruxa 400, Capataz 540), dividido entre o grupo.
            Assert.Equal(20 * m.Level, rules.GoldPerMonsterLevel * m.Level * rules.GoldByRank.For(m.Rank));
        }

        Assert.Equal(MonsterRank.Elite, Content.Monsters["monster.stone_golem"].Rank);
    }
}
