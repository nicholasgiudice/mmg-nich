using Game.Core.Combat;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;
using static Game.Design.Tests.SecondClassDesignTests;

namespace Game.Design.Tests;

/// <summary>
/// T-443: metas da segunda classe do Mago (<c>design/progression-v0.1.md</c> §8, "Metas da segunda classe") com as
/// habilidades reais do Piromante e do Arcanista. Relatório: <c>tasks/reports/balance-v0.22.md</c>. As metas gerais (nível
/// 21, distribuição de pontos, diferença entre segundas classes) estão em <see cref="SecondClassDesignTests"/>.
/// </summary>
public class MageSecondClassDesignTests(ITestOutputHelper output)
{
    /// <summary>As duas principais do Piromante no perfil focado.</summary>
    public static readonly string[] PyromancerPair = ["pyromancer.fireball", "pyromancer.combustion"];

    /// <summary>As duas principais do Arcanista no perfil focado.</summary>
    public static readonly string[] ArcanistPair = ["arcanist.arcane_mark", "arcanist.arcane_orbs"];

    /// <summary>Estratégia do Piromante: o Meteoro só com 3 monstros ou mais no raio.</summary>
    public static readonly IReadOnlyDictionary<string, string> PyromancerOptions = new Dictionary<string, string>
    {
        [$"{Mage}:minAoeOf"] = "pyromancer.meteor=3",
    };

    /// <summary>Estratégia do Arcanista: a rotação da barra (o bot decide Marca, Orbes e Sifão pelos efeitos).</summary>
    public static readonly IReadOnlyDictionary<string, string> ArcanistOptions = new Dictionary<string, string>();

    /// <summary>Alvo único forte: a Aranha Grande (elite) do nível, sozinha contra o Mago, na arena dos Três Lobos.</summary>
    public const string Elite = "monster.spider_big";

    /// <summary>Id do duelo contra a elite (<see cref="LevelCurve.WithDuel"/>).</summary>
    public static readonly string EliteEncounter = $"encounter.t402_duel.{Mage}.{Elite}";

    /// <summary>O catálogo dos testes de segunda classe com o duelo contra a elite (preguiçoso: <see cref="Specs"/> usa esta classe).</summary>
    private static readonly Lazy<ContentCatalog> EliteContentLazy = new(() => LevelCurve.WithDuel(SoloContent, Mage, Elite, out _));

    private static ContentCatalog EliteContent => EliteContentLazy.Value;

    private void Log(string label, BatchSummary s) => output.WriteLine(Fmt(label, s) + $" | sem Mana {DryShare(s):P0}");

    public static TheoryData<int> Levels => SecondClassDesignTests.Levels;

    /// <summary>Assinaturas (recarga de 90–120 s) fora da luta, como no tempo para matar da T-402.</summary>
    public const string NoSignatures = "mage.arcane_cataclysm,pyromancer.meteor,arcanist.void_beam";

    private static Dictionary<string, string> With(IReadOnlyDictionary<string, string>? options, bool signatures)
    {
        var all = new Dictionary<string, string>(options ?? new Dictionary<string, string>());
        if (!signatures)
        {
            all[$"{Mage}:skip"] = NoSignatures;
        }

        return all;
    }

    /// <summary>Só a base num encontro (null = os Três Lobos do nível).</summary>
    public static BatchSummary BaseRun(int level, string? encounter, bool signatures, ContentCatalog? content = null) =>
        RunWith(encounter ?? SoloOf(Mage), SoloStrategy(Mage), CharacterProfile.Expected(level), level, With(null, signatures), content);

    /// <summary>Com a segunda classe num encontro (null = os Três Lobos do nível).</summary>
    public static BatchSummary SecondRun(int level, string second, string? encounter, bool signatures, ContentCatalog? content = null) =>
        RunWith(encounter ?? SoloOf(Mage), SoloStrategy(Mage), WithSecondClass(level, second), level, With(Specs[second].SoloOptions, signatures), content);

    /// <summary>
    /// Parcela das lutas em que o Mago fica "sem Mana": mais da metade da luta com a Mana abaixo do custo da habilidade mais
    /// barata da segunda classe (Piromante: Rajada de Chamas; Arcanista: Marca Arcana).
    /// </summary>
    public static double DryShare(BatchSummary s) =>
        s.PerRun is null || s.PerRun.Count == 0 ? 0 : s.PerRun.Count(r => r.Ticks > 0 && r.TicksBelowSecondClassCost > 0.5 * r.Ticks) / (double)s.PerRun.Count;

    // ---------- Piromante ----------

    /// <summary>Piromante × Mago só com a base, Três Lobos do nível, sem as assinaturas: vence ≥ 85% em no máximo 85% do tempo.</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Pyromancer_beats_three_wolves_in_at_most_85_percent_of_the_base_time(int level)
    {
        var baseOnly = BaseRun(level, null, signatures: false);
        var pyromancer = SecondRun(level, Pyromancer, null, signatures: false);
        Log($"nv{level} Mago só a base, Três Lobos", baseOnly);
        Log($"nv{level} Piromante, Três Lobos", pyromancer);

        Assert.True(pyromancer.WinRate >= 0.85, Fmt("Piromante", pyromancer));
        Assert.True(pyromancer.TimeToWinMean <= 0.85 * baseOnly.TimeToWinMean,
            $"{Fmt("Piromante", pyromancer)} | {Fmt("base", baseOnly)} | razão {pyromancer.TimeToWinMean / baseOnly.TimeToWinMean:P0}");
    }

    /// <summary>Piromante contra a elite do nível: vence ≥ 85% (pode ser mais lento que no grupo).</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Pyromancer_wins_against_the_elite_at_least_85_percent(int level)
    {
        var pyromancer = SecondRun(level, Pyromancer, EliteEncounter, signatures: false, EliteContent);
        Log($"nv{level} Piromante, elite", pyromancer);
        Assert.True(pyromancer.WinRate >= 0.85, Fmt("Piromante", pyromancer));
    }

    // ---------- Arcanista ----------

    /// <summary>
    /// Arcanista × Mago só com a base contra um alvo único forte (a Aranha Grande, elite, do nível), sem as assinaturas:
    /// vence ≥ 85% em no máximo 85% do tempo.
    /// </summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Arcanist_beats_the_elite_in_at_most_85_percent_of_the_base_time(int level)
    {
        var baseOnly = BaseRun(level, EliteEncounter, signatures: false, EliteContent);
        var arcanist = SecondRun(level, Arcanist, EliteEncounter, signatures: false, EliteContent);
        Log($"nv{level} Mago só a base, elite", baseOnly);
        Log($"nv{level} Arcanista, elite", arcanist);

        Assert.True(arcanist.WinRate >= 0.85, Fmt("Arcanista", arcanist));
        Assert.True(arcanist.TimeToWinMean <= 0.85 * baseOnly.TimeToWinMean,
            $"{Fmt("Arcanista", arcanist)} | {Fmt("base", baseOnly)} | razão {arcanist.TimeToWinMean / baseOnly.TimeToWinMean:P0}");
    }

    /// <summary>Arcanista nos Três Lobos do nível: vence ≥ 85% (pode ser mais lento que só a base).</summary>
    [Theory]
    [MemberData(nameof(Levels))]
    public void Arcanist_wins_three_wolves_at_least_85_percent(int level)
    {
        var arcanist = SecondRun(level, Arcanist, null, signatures: false);
        Log($"nv{level} Arcanista, Três Lobos", arcanist);
        Assert.True(arcanist.WinRate >= 0.85, Fmt("Arcanista", arcanist));
    }

    // ---------- os dois ----------

    /// <summary>
    /// Mana: nenhum dos dois fica "sem Mana" (<see cref="DryShare"/>: mais da metade da luta abaixo do custo da habilidade mais
    /// barata da segunda classe) em mais da metade das lutas dos Três Lobos do nível, com e sem as assinaturas.
    /// </summary>
    [Theory]
    [InlineData(Pyromancer, 25)]
    [InlineData(Pyromancer, 30)]
    [InlineData(Pyromancer, 40)]
    [InlineData(Arcanist, 25)]
    [InlineData(Arcanist, 30)]
    [InlineData(Arcanist, 40)]
    public void Mage_second_classes_do_not_run_out_of_mana_in_most_fights(string second, int level)
    {
        foreach (var signatures in new[] { false, true })
        {
            var run = SecondRun(level, second, null, signatures);
            Log($"nv{level} {second}{(signatures ? " com assinatura" : "")}, Três Lobos", run);
            Assert.True(DryShare(run) <= 0.5, $"{second}: sem Mana em {DryShare(run):P0} das lutas");
        }
    }

    /// <summary>
    /// Com as assinaturas, no 40: o Piromante usa o Meteoro contra os Três Lobos (3 no raio) e o Arcanista usa o Feixe do Vazio
    /// na elite; os dois vencem ≥ 85%.
    /// </summary>
    [Fact]
    public void At_level_40_the_bot_uses_the_signatures()
    {
        var pyromancer = SecondRun(40, Pyromancer, null, signatures: true);
        var arcanist = SecondRun(40, Arcanist, EliteEncounter, signatures: true, EliteContent);
        Log("nv40 Piromante com assinatura, Três Lobos", pyromancer);
        Log("nv40 Arcanista com assinatura, elite", arcanist);

        Assert.True(pyromancer.WinRate >= 0.85 && pyromancer.AbilityUses.GetValueOrDefault("pyromancer.meteor") >= 0.5, Fmt("Piromante", pyromancer));
        Assert.True(arcanist.WinRate >= 0.85 && arcanist.AbilityUses.GetValueOrDefault("arcanist.void_beam") >= 0.5, Fmt("Arcanista", arcanist));
    }
}
