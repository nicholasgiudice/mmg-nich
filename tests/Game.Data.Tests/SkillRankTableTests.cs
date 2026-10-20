using Game.Core.Combat;
using Game.Core.Definitions;

namespace Game.Data.Tests;

/// <summary>
/// T-062: cada NH dos dados reais bate com a coluna "Perfil nível 1" de <c>design/skills-v0.1.md</c>.
/// Valor = dano/cura/escudo por acerto × acertos + saltos (Corrente) + pulsos da área (só Campo de Relâmpagos, como
/// no documento). Alvos extras e o chão em chamas do Cataclismo não entram (o documento os descreve à parte).
/// Tolerância: 0,5 por número arredondado no jogo + 0,5 do total arredondado no documento.
/// </summary>
public class SkillRankTableTests
{
    private static readonly Lazy<GameDatabase> Db = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        var r = GameDataLoader.LoadDirectory(Path.Combine(dir!.FullName, "data"));
        Assert.True(r.Success, string.Join("\n", r.Errors));
        return r.Database!;
    });

    /// <summary>(habilidade, acertos por NH, perfil nível 1 por NH) copiados das tabelas do documento.</summary>
    public static TheoryData<string, int[], double[]> Tables => new()
    {
        { "warrior.heavy_strike", [1, 1, 1, 1, 1], [51, 61, 71, 82, 92] },
        { "warrior.whirlwind", [1, 1, 2, 2, 3], [27, 33, 46, 54, 69] },
        { "warrior.living_wall", [1, 1, 1], [60, 78, 96] },
        { "archer.power_shot", [1, 1, 2, 2, 3], [40, 48, 67, 80, 100] },
        { "archer.rain_of_arrows", [3, 3, 3, 3, 4], [80, 96, 113, 129, 193] },
        { "archer.devastating_arrow", [1, 1, 1], [106, 138, 170] },
        { "mage.elemental_blast", [1, 1, 2, 2, 3], [44, 53, 74, 88, 111] },
        { "mage.ice_prison", [1, 1, 1, 1, 1], [9, 11, 12, 14, 16] },
        { "mage.arcane_barrier", [1, 1, 1, 1, 1], [45, 54, 63, 72, 81] },
        { "mage.lightning_field", [1, 1, 1, 1, 1], [51, 59, 95, 105, 123] },
        { "mage.arcane_cataclysm", [1, 1, 1], [138, 179, 221] },
        { "cleric.direct_heal", [1, 1, 1, 1, 1], [50, 60, 70, 80, 90] },
        { "cleric.holy_chain", [1, 1, 1, 1, 1], [39, 47, 93, 106, 168] },
        { "cleric.sacred_shield", [1, 1, 1, 1, 1], [45, 54, 63, 72, 81] },
        { "cleric.miracle", [1, 1, 1], [108, 140, 173] },
    };

    [Theory]
    [MemberData(nameof(Tables))]
    public void Profile_value_of_each_rank_matches_the_design_table(string id, int[] hits, double[] expected)
    {
        var ability = Db.Value.Abilities[id];
        Assert.Equal(expected.Length, ability.HighestRank);
        for (var rank = 1; rank <= expected.Length; rank++)
        {
            var r = AbilityRanks.Resolve(ability, rank);
            Assert.True(hits[rank - 1] == r.HitCount, $"{id} NH{rank}: {r.HitCount} acertos, esperado {hits[rank - 1]}");
            var (value, rounded) = Value(r, id == "mage.lightning_field");
            Assert.True(
                Math.Abs(value - expected[rank - 1]) <= 0.5 * (rounded + 1),
                $"{id} NH{rank}: {value}, esperado {expected[rank - 1]} (±{0.5 * (rounded + 1)})");
        }
    }

    private static (double Value, int Rounded) Value(AbilityDefinition a, bool countAreas)
    {
        double perHit = 0;
        var rounded = 0;
        foreach (var e in a.Effects.Where(e => e.Target == EffectTarget.Targets))
        {
            switch (e)
            {
                case DamageEffect d:
                    perHit += d.Base;
                    rounded++;
                    break;
                case HealEffect h:
                    perHit += h.Base;
                    rounded++;
                    break;
                case ShieldEffect s:
                    perHit += s.Amount;
                    rounded++;
                    break;
                case ChainEffect c:
                    var jump = c.Effects.OfType<DamageEffect>().Sum(d => Math.Round(d.Base * c.Multiplier, MidpointRounding.AwayFromZero));
                    perHit += c.Jumps * jump;
                    rounded += c.Jumps;
                    break;
                case SpawnAreaEffect sa when countAreas:
                    var area = Db.Value.Areas[sa.Area];
                    var pulses = area.DurationTicks / area.IntervalTicks;
                    perHit += pulses * area.Effects.OfType<DamageEffect>().Sum(d => d.Base);
                    rounded += pulses;
                    break;
            }
        }

        return (perHit * a.HitCount, Math.Max(1, rounded * a.HitCount));
    }

    [Fact]
    public void Hunters_mark_bonus_and_extra_target_follow_the_table()
    {
        var mark = Db.Value.Abilities["archer.hunters_mark"];
        double[] bonus = [1.25, 1.25, 1.25, 1.30, 1.30];
        for (var rank = 1; rank <= 5; rank++)
        {
            var r = AbilityRanks.Resolve(mark, rank);
            var status = Db.Value.Statuses[r.Effects.OfType<ApplyStatusEffect>().First().Status];
            Assert.Equal(bonus[rank - 1], status.Modifiers.DamageTakenMultiplier, 3);
            Assert.Equal(rank >= 3, r.Effects.OfType<ExtraTargetsEffect>().Any());
        }

        var slowing = Db.Value.Statuses[AbilityRanks.Resolve(mark, 5).Effects.OfType<ApplyStatusEffect>().First().Status];
        Assert.Equal(0.8, slowing.Modifiers.SpeedMultiplier, 3);
    }

    [Fact]
    public void Signatures_have_three_ranks_and_normal_skills_five()
    {
        foreach (var cls in Db.Value.Classes.Values)
        {
            foreach (var id in cls.Abilities)
            {
                var a = Db.Value.Abilities[id];
                Assert.Equal(a.Signature ? 3 : 5, a.HighestRank);
            }
        }
    }
}
