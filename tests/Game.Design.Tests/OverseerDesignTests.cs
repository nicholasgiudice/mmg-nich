using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Sim;
using Xunit.Abstractions;
using static Game.Design.Tests.DesignHarness;
using static Game.Design.Tests.SecondClassDesignTests;

namespace Game.Design.Tests;

/// <summary>
/// T-512: o Capataz Forjado (nível 27, chefe das Minas Fundas; <c>design/combat-encounters-v0.1.md</c> § Minas Fundas) contra
/// um grupo de 4 do nível 27 com segunda classe (<see cref="SecondClassDesignTests.WithSecondClass"/>, perfil de grupo) e raros,
/// como o Vigia. A masmorra é a primeira com papel de grupo: o Martelo e o Punho de Ferro pedem tanque (Guardião), a Fornalha
/// pede cura em grupo (Sacerdote). Metas: com Guardião e Sacerdote vence ≥ 75%; sem os dois (Berserker e Inquisidor no lugar)
/// vence ≤ 40%; a luta dura 3–6 min. Relatório: <c>tasks/reports/balance-v0.26.md</c>.
/// </summary>
public class OverseerDesignTests(ITestOutputHelper output)
{
    public const string Encounter = "encounter.forged_overseer";

    public const int Level = 27;

    /// <summary>O grupo com os papéis: Guardião, Atirador de Elite, Piromante e Sacerdote (nas posições do encontro).</summary>
    public static readonly string[] WithRoles = [Guardian, Marksman, Pyromancer, Priest];

    /// <summary>O mesmo grupo sem tanque e sem cura de grupo: o Berserker e o Inquisidor no lugar do Guardião e do Sacerdote.</summary>
    public static readonly string[] WithoutRoles = [Berserker, Marksman, Pyromancer, Inquisitor];

    /// <summary>Só medidos (relatório): só o tanque e só a cura.</summary>
    public static readonly string[] OnlyTank = [Guardian, Marksman, Pyromancer, Inquisitor];

    public static readonly string[] OnlyHealer = [Berserker, Marksman, Pyromancer, Priest];

    /// <summary>Só medido: os papéis com as outras segundas classes do Arqueiro e do Mago (o Caçador e o Arcanista).</summary>
    public static readonly string[] WithRolesOtherDamage = [Guardian, Hunter, Arcanist, Priest];

    private static string Fmt(string label, BatchSummary s) =>
        $"{label}: vitória {s.WinRate:P0}, {s.TimeToWinMean:F0} s (p90 {s.TimeToWinP90:F0}), mortes {s.PlayerDeaths:F2}, dano recebido "
        + string.Join(" ", s.DamageTakenByClass.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key[(kv.Key.IndexOf('.') + 1)..]}={kv.Value:F0}"))
        + ", vivo " + string.Join(" ", s.PerRun!.SelectMany(r => r.AliveSecondsByClass).GroupBy(kv => kv.Key).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"{g.Key[(g.Key.IndexOf('.') + 1)..]}={g.Sum(kv => kv.Value) / s.PerRun!.Sum(r => r.Seconds):P0}"))
        + ", dano/s causado " + string.Join(" ", s.PerRun!.SelectMany(r => r.DamageDealtByClass).GroupBy(kv => kv.Key).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"{g.Key[(g.Key.IndexOf('.') + 1)..]}={g.Sum(kv => kv.Value) / s.PerRun!.Sum(r => r.Seconds):F0}"))
        + $", cura {s.Healing:F0}, derrotas {s.Losses}, tempo esgotado {s.Timeouts}, dano por fonte "
        + string.Join(" ", s.DamageTakenBySource.Where(u => u.Value >= 50).Select(u => $"{u.Key}={u.Value:F0}"));

    [Fact]
    public void A_group_of_four_with_guardian_and_priest_wins_at_least_75_percent_in_3_to_6_minutes()
    {
        var s = Group(WithRoles);
        output.WriteLine(Fmt("Guardião + Sacerdote", s));

        Assert.True(s.WinRate >= 0.75, Fmt("Guardião + Sacerdote", s));
        Assert.InRange(s.TimeToWinMean, 180, 360);
    }

    [Fact]
    public void Without_guardian_and_priest_the_group_wins_at_most_40_percent()
    {
        var with = Group(WithRoles);
        var without = Group(WithoutRoles);
        output.WriteLine(Fmt("Guardião + Sacerdote", with));
        output.WriteLine(Fmt("Berserker + Inquisidor", without));

        Assert.True(without.WinRate <= 0.40, Fmt("Berserker + Inquisidor", without));
    }

    /// <summary>Só mede e registra no relatório: o grupo só com o tanque e só com a cura de grupo.</summary>
    [Fact]
    public void Only_the_tank_or_only_the_healer_is_measured()
    {
        output.WriteLine(Fmt("só Guardião (com Inquisidor)", Group(OnlyTank)));
        output.WriteLine(Fmt("só Sacerdote (com Berserker)", Group(OnlyHealer)));
        output.WriteLine(Fmt("Guardião + Sacerdote com Caçador e Arcanista", Group(WithRolesOtherDamage)));
    }

    // ---- lotes ----

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    /// <summary>
    /// Perfil de grupo do nível 27 com as segundas classes dadas (uma por classe base) e raros: o perfil esperado com segunda
    /// classe de cada membro (<see cref="SecondClassDesignTests.WithSecondClass"/>, sem o par solo do Clérigo), juntado como na dupla
    /// da Serra (<see cref="RidgeDesignTests"/>).
    /// </summary>
    public static CharacterProfile GroupProfile(IEnumerable<string> seconds)
    {
        var profiles = seconds.Select(s => WithSecondClass(Level, s, solo: false)).ToList();
        return profiles[0] with
        {
            Gear = ItemRarity.Rare,
            Ranks = profiles.SelectMany(p => p.Ranks).ToDictionary(kv => kv.Key, kv => kv.Value),
            SecondClass = profiles.SelectMany(p => p.SecondClass).ToDictionary(kv => kv.Key, kv => kv.Value),
            SecondClassRanks = profiles.SelectMany(p => p.SecondClassRanks).ToDictionary(kv => kv.Key, kv => kv.Value),
        };
    }

    /// <summary>
    /// O Capataz contra o grupo (cada segunda classe na posição da classe base dela). Estratégia de cada classe como na dupla da
    /// Serra (a solo dela, com <c>+dodge</c>), as opções de grupo de <see cref="SecondClassDesignTests.Specs"/> e o Clérigo indo
    /// até o aliado ferido (<c>followInjured</c>), como no Vigia.
    /// </summary>
    internal static BatchSummary Group(string[] seconds) =>
        Cache.GetOrAdd(string.Join("|", seconds), _ => new Lazy<BatchSummary>(() =>
        {
            var options = new Dictionary<string, string>(Options) { [$"{Cleric}:followInjured"] = "1" };
            foreach (var second in seconds)
            {
                foreach (var (k, v) in Specs[second].GroupOptions ?? new Dictionary<string, string>())
                {
                    options[k] = v;
                }
            }

            return Batch.Run(Content, new RunSpec
            {
                EncounterId = Encounter,
                Strategy = "rotation+dodge",
                Roles = seconds.Select(BaseOf).ToDictionary(c => c, c => SoloStrategy(c) + "+dodge"),
                Options = new BotOptions(options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = GroupProfile(seconds),
            }, keepPerRun: true);
        })).Value;
}
