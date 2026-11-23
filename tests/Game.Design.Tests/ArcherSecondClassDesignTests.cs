using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;
using static Game.Design.Tests.SecondClassDesignTests;

namespace Game.Design.Tests;

/// <summary>
/// T-433: metas da segunda classe do Arqueiro (<c>design/progression-v0.1.md</c> §8, "Metas da segunda classe") com as
/// habilidades reais do Atirador de Elite e do Caçador. Relatório: <c>tasks/reports/balance-v0.21.md</c>. As metas gerais
/// (nível 21, distribuição de pontos, diferença entre segundas classes) estão em <see cref="SecondClassDesignTests"/>, com
/// as duas em <see cref="SecondClassDesignTests.Specs"/>.
/// </summary>
public class ArcherSecondClassDesignTests(ITestOutputHelper output)
{
    /// <summary>Grupo típico: os Três Lobos do nível, com o Arqueiro.</summary>
    public static string Pack => SoloOf(Archer);

    /// <summary>Alvo único forte: a Aranha Grande (elite, a única elite que se enfrenta sozinho) do nível, nos Campos.</summary>
    public const string Elite = "encounter.test_elite_spider.class.archer";

    /// <summary>Alvo que não morre, a 9 tiles, por 20 s: o dano com e sem o Olho de Falcão.</summary>
    public const string Dummy = "encounter.test_hawk_eye_dummy";

    public const string DummyMonster = "monster.test_hawk_eye_dummy";

    /// <summary>
    /// Meta de tempo da segunda classe sobre só a base (§8): no máximo 85% do 30 em diante. No 25 o perfil tem só 2 NH da
    /// segunda classe (os pontos vão primeiro ao que falta da base) e o Arqueiro não tem Mana sobrando para habilidades
    /// novas: a meta lá é não ser mais lento que só a base (relatório, §3).
    /// </summary>
    public static double TimeLimit(int level) => level >= 30 ? 0.85 : 1.0;

    /// <summary>
    /// Meta do Olho de Falcão revista na T-433: o dano em 20 s com ele no máximo 1,5× o dano sem ele. A proposta era 2,5×,
    /// que nunca é alcançada: com todo golpe crítico o teto é 1,65× (o multiplicador da Mira Firme NH5), e o perfil já tem
    /// 40% de crítico contra alvos a 6+ tiles.
    /// </summary>
    public const double HawkEyeLimit = 1.5;

    /// <summary>O catálogo dos Três Lobos de cada classe mais a elite e o alvo de 20 s com o Arqueiro.</summary>
    internal static ContentCatalog Variants { get; } = BuildVariants(SoloContent);

    private void Log(string label, BatchSummary s) => output.WriteLine(Fmt(label, s));

    public static TheoryData<int> Levels => SecondClassDesignTests.Levels;

    private static BatchSummary BaseIn(string encounter, int level) =>
        RunWith(encounter, SoloStrategy(Archer), CharacterProfile.Expected(level), level, content: Variants);

    private static BatchSummary SecondIn(string encounter, int level, string second, IReadOnlyDictionary<string, string>? options = null) =>
        RunWith(encounter, SoloStrategy(Archer), WithSecondClass(level, second), level, options ?? Specs[second].SoloOptions, Variants);

    // ---------- Atirador de Elite ----------

    /// <summary>Atirador × Arqueiro só com a base, contra a elite do nível: vence ≥ 85% em no máximo 85% do tempo (25: 100%).</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Marksman_beats_the_elite_in_at_most_85_percent_of_the_base_time(int level)
    {
        var baseOnly = BaseIn(Elite, level);
        var marksman = SecondIn(Elite, level, Marksman);
        Log($"nv{level} elite só a base", baseOnly);
        Log($"nv{level} elite Atirador", marksman);

        Assert.True(marksman.WinRate >= 0.85, Fmt("Atirador", marksman));
        Assert.True(marksman.TimeToWinMean <= TimeLimit(level) * baseOnly.TimeToWinMean,
            $"{Fmt("Atirador", marksman)} | {Fmt("base", baseOnly)} | razão {marksman.TimeToWinMean / baseOnly.TimeToWinMean:P0}");
    }

    /// <summary>No grupo típico o Atirador pode ser mais lento, mas vence.</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Marksman_wins_the_pack_at_least_85_percent(int level)
    {
        var marksman = SecondIn(Pack, level, Marksman);
        Log($"nv{level} lobos Atirador", marksman);
        Log($"nv{level} lobos só a base", BaseIn(Pack, level));
        Assert.True(marksman.WinRate >= 0.85, Fmt("Atirador", marksman));
    }

    /// <summary>
    /// Olho de Falcão (nível 37 = NH2, 17 s; nível 40 = NH3, 20 s; no 35 o perfil ainda não tem ponto nele): o dano do
    /// Atirador nos 20 s com ele não passa de <see cref="HawkEyeLimit"/> × o dano nos mesmos 20 s sem ele (o bot não o
    /// usa: <c>skip</c>).
    /// </summary>
    [Theory]
    [InlineData(37)]
    [InlineData(40)]
    public void Hawk_eye_burst_stays_under_the_limit(int level)
    {
        var with = SecondIn(Dummy, level, Marksman);
        var without = SecondIn(Dummy, level, Marksman, new Dictionary<string, string>(Specs[Marksman].SoloOptions!)
        {
            [$"{Archer}:skip"] = "marksman.crippling_shot,marksman.hawk_eye",
        });
        Log($"nv{level} 20 s com o Olho de Falcão", with);
        Log($"nv{level} 20 s sem", without);

        Assert.True(with.AbilityUses.GetValueOrDefault("marksman.hawk_eye") >= 0.95, Fmt("com", with));
        Assert.True(with.DamageDealt <= HawkEyeLimit * without.DamageDealt,
            $"{Fmt("com", with)} | {Fmt("sem", without)} | razão {with.DamageDealt / without.DamageDealt:F2}×");
    }

    // ---------- Caçador ----------

    /// <summary>Caçador × Arqueiro só com a base, grupo típico: vence ≥ 85% em no máximo 85% do tempo (25: 100%).</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Hunter_beats_three_wolves_in_at_most_85_percent_of_the_base_time(int level)
    {
        var baseOnly = BaseIn(Pack, level);
        var hunter = SecondIn(Pack, level, Hunter);
        Log($"nv{level} lobos só a base", baseOnly);
        Log($"nv{level} lobos Caçador", hunter);

        Assert.True(hunter.WinRate >= 0.85, Fmt("Caçador", hunter));
        Assert.True(hunter.TimeToWinMean <= TimeLimit(level) * baseOnly.TimeToWinMean,
            $"{Fmt("Caçador", hunter)} | {Fmt("base", baseOnly)} | razão {hunter.TimeToWinMean / baseOnly.TimeToWinMean:P0}");
    }

    /// <summary>Contra alvo único o Caçador pode ser mais lento, mas vence.</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Hunter_wins_against_the_elite_at_least_85_percent(int level)
    {
        var hunter = SecondIn(Elite, level, Hunter);
        Log($"nv{level} elite Caçador", hunter);
        Log($"nv{level} elite só a base", BaseIn(Elite, level));
        Assert.True(hunter.WinRate >= 0.85, Fmt("Caçador", hunter));
    }

    // ---------- encontros ----------

    private static ContentCatalog BuildVariants(ContentCatalog c)
    {
        var wolves = c.Encounters[Pack];
        var spider = c.Monsters["monster.spider_big"];
        var dummy = spider with { Id = DummyMonster, Name = "Alvo de teste", Health = 1_000_000 };
        var encounters = new[]
        {
            wolves with
            {
                Id = Elite,
                Name = "Aranha Grande (elite)",
                Enemies = [new EnemySpawnDefinition { Monster = spider.Id, Position = new PositionDefinition(17.5, 10.5) }],
            },
            wolves with
            {
                Id = Dummy,
                Name = "Alvo de 20 s",
                Enemies = [new EnemySpawnDefinition { Monster = DummyMonster, Position = new PositionDefinition(14.5, 10.5) }],
                Defeat = wolves.Defeat with { TimeLimitTicks = 20 * Game.Core.Simulation.World.TickRate },
            },
        };

        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values.Append(dummy), c.Arenas.Values, c.Encounters.Values.Concat(encounters), c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
            Shops = c.Shops,
        };
    }
}
