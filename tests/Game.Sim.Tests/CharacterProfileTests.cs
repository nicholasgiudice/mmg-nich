using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Sim.Tests;

/// <summary>T-206: perfis de personagem por nível (pontos, NH, equipamento) e monstros no nível pedido.</summary>
public class CharacterProfileTests
{
    private static readonly Lazy<ContentCatalog> Real = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        return ContentLoading.Load(Path.Combine(dir.FullName, "data"));
    });

    private static ContentCatalog Content => Real.Value;

    public static TheoryData<string> Classes() => new() { "class.warrior", "class.archer", "class.mage", "class.cleric" };

    [Theory]
    [MemberData(nameof(Classes))]
    public void Every_level_gives_a_valid_build_that_spends_all_points(string cls)
    {
        var c = Content.Classes[cls];
        var rules = Content.Rules.Progression;
        // T-402: do 1 ao nível máximo. Sem segunda classe (T-403), a classe base tem 23 NH: do nível 24 em diante sobram pontos
        // de habilidade, e os pontos de atributo não contam os +5 da segunda classe.
        var baseRanks = c.Abilities.Where(id => Leveling.IsLearnable(c, id)).Sum(id => Content.Abilities[id].HighestRank);
        for (var level = 1; level <= rules.MaxLevel; level++)
        {
            foreach (var plan in new[] { RankProfiles.Focused, RankProfiles.Spread })
            {
                foreach (var attrs in Enum.GetValues<AttributePlan>())
                {
                    var build = CharacterProfiles.BuildFor(Content, c, new CharacterProfile { Level = level, Skills = plan, Attributes = attrs });
                    Assert.Null(Leveling.Validate(Content, c, build));
                    Assert.Equal(Math.Min(rules.SkillPointsAt(level), baseRanks), build.SkillRanks.Values.Sum());
                    var a = build.Allocated;
                    Assert.Equal(rules.AttributePointsAt(level), a.Str + a.Dex + a.Agi + a.Int + a.Vit);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Classes))]
    public void At_level_15_the_skill_plans_match_the_T063_profiles(string cls)
    {
        var c = Content.Classes[cls];
        Assert.Equal(RankProfiles.For(RankProfiles.Focused, Content)[cls], CharacterProfiles.SkillRanks(Content, c, 15, RankProfiles.Focused));
        Assert.Equal(RankProfiles.For(RankProfiles.Spread, Content)[cls], CharacterProfiles.SkillRanks(Content, c, 15, RankProfiles.Spread));
    }

    [Fact]
    public void Focused_learns_every_normal_skill_first_then_raises_the_pair()
    {
        var c = Content.Classes["class.warrior"];
        Assert.Equal(new Dictionary<string, int> { ["warrior.heavy_strike"] = 1 }, CharacterProfiles.SkillRanks(Content, c, 1, RankProfiles.Focused));
        var nine = CharacterProfiles.SkillRanks(Content, c, 9, RankProfiles.Focused);
        Assert.Equal((4, 3, 1, 1), (nine["warrior.heavy_strike"], nine["warrior.whirlwind"], nine["warrior.firm_guard"], nine["warrior.taunt"]));
        Assert.False(nine.ContainsKey("warrior.living_wall")); // assinatura só no nível 12
    }

    [Fact]
    public void Suggested_puts_every_point_in_the_suggested_attributes_and_spread_only_half()
    {
        var c = Content.Classes["class.warrior"];
        var suggested = CharacterProfiles.BuildFor(Content, c, CharacterProfile.Expected(9)).Allocated;
        Assert.Equal((20, 0, 0, 0, 20), (suggested.Str, suggested.Dex, suggested.Agi, suggested.Int, suggested.Vit));

        var spread = CharacterProfiles.BuildFor(Content, c, CharacterProfile.Expected(9) with { Attributes = AttributePlan.Spread }).Allocated;
        Assert.Equal((10, 7, 7, 6, 10), (spread.Str, spread.Dex, spread.Agi, spread.Int, spread.Vit));
    }

    [Theory]
    [InlineData("class.warrior", "weapon.sword", "armor.heavy_chest")]
    [InlineData("class.archer", "weapon.bow", "armor.medium_chest")]
    [InlineData("class.mage", "weapon.staff", "armor.light_chest")]
    [InlineData("class.cleric", "weapon.mace", "armor.medium_chest")]
    public void Gear_fills_every_slot_at_the_profile_level_and_rarity(string cls, string weapon, string chest)
    {
        var gear = CharacterProfiles.GearFor(Content, Content.Classes[cls], new CharacterProfile { Level = 7, Gear = ItemRarity.Rare }).ToDictionary(g => g.Slot, g => g.Item);

        Assert.Equal(Enum.GetValues<EquipSlot>().Length, gear.Count);
        Assert.Equal((weapon, chest), (gear[EquipSlot.Weapon].BaseId, gear[EquipSlot.Chest].BaseId));
        Assert.All(gear.Values, i => Assert.Equal((7, ItemRarity.Rare, 2), (i.Level, i.Rarity, i.Affixes.Count)));
        Assert.All(gear.Values, i => Assert.True(ItemMath.CanUse(Content.Rules.Items, Content.Items[i.BaseId], cls), i.BaseId));
    }

    [Fact]
    public void Spawned_profile_character_has_level_gear_and_full_health()
    {
        var w = WorldBuilder.CreateDefault(1, null, Content);
        var e = CharacterProfiles.Spawn(w, Content.Classes["class.warrior"], Vector2.Zero, CharacterProfile.Expected(9) with { Gear = ItemRarity.Uncommon });
        var s = e.Get<CharacterStats>();

        Assert.Equal(9, s.Level);
        Assert.Equal(12 + 20 + (6 * 5), s.Str); // perfil + pontos + afixo de FOR (1 + 0,4 × 9 = 4,6 → 5) nas 6 peças
        Assert.Equal(13, s.WeaponDamage); // espada 4 + 9
        Assert.True(s.Armor > 0);
        Assert.Equal(e.Get<Health>().Max, e.Get<Health>().Current);
        Assert.Equal(e.Get<ResourcePool>().Max, e.Get<ResourcePool>().Current);
    }

    [Theory]
    [MemberData(nameof(Classes))]
    public void Profiles_from_level_21_to_40_spawn_with_the_level_numbers_and_win_a_duel_of_their_level(string cls)
    {
        var rules = Content.Rules.Progression;
        var previous = LevelCurve.Measure(Content, cls, 20);
        for (var level = 21; level <= rules.MaxLevel; level++)
        {
            var stats = LevelCurve.Measure(Content, cls, level);
            Assert.Equal((level, rules.AttributePointsAt(level)), (stats.Level, stats.AttributePoints));
            Assert.True(stats.Health > previous.Health, $"vida não cresce no nível {level}");
            Assert.True(stats.Stats.WeaponDamage >= previous.Stats.WeaponDamage, $"arma diminui no nível {level}"); // cajado e maça arredondam (0,6 e 0,9 por nível)
            Assert.True(stats.Abilities.Sum(a => a.Total) > previous.Abilities.Sum(a => a.Total), $"habilidades não crescem no nível {level}");
            previous = stats;
        }

        foreach (var level in new[] { 21, 30, rules.MaxLevel })
        {
            var kill = LevelCurve.Kill(Content, cls, level, "monster.grey_wolf", runs: 2, parallelism: 1);
            Assert.Equal(1.0, kill.Batch.WinRate);
            Assert.True(kill.XpPerMinute > 0);
        }
    }

    [Fact]
    public void Expected_archer_stays_below_the_attack_speed_cap_up_to_level_40()
    {
        // T-402 (balance-v0.18 §3): com o teto antigo (+50%, AGI 100) o Arqueiro esperado batia no máximo no nível 39.
        var r = Content.Rules.Stats;
        var top = LevelCurve.Measure(Content, "class.archer", Content.Rules.Progression.MaxLevel);
        Assert.True(top.AttackSpeedBonus < r.AttackSpeedMax, $"AGI {top.Stats.Agi}: +{top.AttackSpeedBonus:P0} de {r.AttackSpeedMax:P0}");
        Assert.True(top.Crit < r.CritMax);
    }

    [Fact]
    public void Runner_uses_the_profile_and_the_monster_level()
    {
        var spec = new RunSpec { EncounterId = "encounter.three_wolves", Runs = 1, Profile = CharacterProfile.Expected(5), MonsterLevel = 5 };
        Assert.Contains("[nv5 sugerido comum NH:focado] monstros nv5", spec.Label, StringComparison.Ordinal);

        var protoWin = EncounterRunner.Run(Content, new RunSpec { EncounterId = "encounter.three_wolves", Runs = 1 }, 1);
        var leveled = EncounterRunner.Run(Content, spec, 1);
        Assert.NotEqual(protoWin.StateHash, leveled.StateHash);
        Assert.Equal(RunOutcome.Win, leveled.Outcome);
    }
}
