using System.Numerics;
using System.Text.Json;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Data.Tests;

/// <summary>
/// T-205: conteúdo de progressão de <c>data/</c> contra <c>design/progression-v0.1.md</c> — equipamento inicial, itens,
/// afixos, raridades, quedas e níveis dos monstros.
/// </summary>
public class ProgressionContentTests
{
    private static readonly Lazy<GameDatabase> Loaded = new(() => GameDataLoader.LoadDirectory(DataDir()).GetDatabaseOrThrow());

    private static GameDatabase Db => Loaded.Value;

    private static string DataDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir.FullName, "data");
    }

    private static ContentCatalog Catalog() =>
        new(Db.Classes.Values, Db.Abilities.Values, Db.Statuses.Values, Db.Areas.Values, Db.Monsters.Values, Db.Arenas.Values,
            Db.Encounters.Values, Db.Items.Values)
        {
            Rules = Db.Rules,
            Threat = Db.Threat,
        };

    // ---------- equipamento inicial ----------

    /// <summary>§1/§6: perfil nível 1 com a arma inicial (nível do item 2) — os números do protótipo.</summary>
    [Theory]
    [InlineData("class.warrior", 6, 12, 3, 6, 4, 10, 240, 88)]
    [InlineData("class.archer", 6, 4, 12, 6, 5, 8, 130, 90)]
    [InlineData("class.mage", 3, 3, 3, 5, 13, 6, 110, 106)]
    [InlineData("class.cleric", 5, 6, 2, 4, 12, 8, 150, 104)]
    public void Starting_equipment_reproduces_the_level_1_profile(
        string cls, float weapon, int str, int dex, int agi, int intel, int vit, float health, float mana)
    {
        var content = Catalog();
        var world = WorldBuilder.CreateDefault(1, null, content);
        var e = EntityFactory.SpawnPlayer(world, content.Classes[cls], Vector2.Zero);
        var s = e.Get<CharacterStats>();

        Assert.Equal((weapon, str, dex, agi, intel, vit), (s.WeaponDamage, s.Str, s.Dex, s.Agi, s.Int, s.Vit));
        Assert.Equal((0f, 0f), (s.Armor, s.Resistance));
        Assert.Equal(health, e.Get<Game.Core.Simulation.Components.Health>().Max, 3);
        Assert.Equal(mana, e.Get<Game.Core.Simulation.Components.ResourcePool>().Max, 3);

        var start = Assert.Single(content.Classes[cls].StartingEquipment);
        Assert.Equal(2, start.Level);
        Assert.Equal(EquipSlot.Weapon, content.Items[start.Item].Slot);
    }

    // ---------- itens ----------

    [Fact]
    public void Every_class_has_a_weapon_and_an_item_for_every_slot()
    {
        var rules = Db.Rules.Items;
        foreach (var cls in Db.Classes.Keys)
        {
            foreach (var slot in Enum.GetValues<EquipSlot>())
            {
                Assert.True(
                    Db.Items.Values.Any(i => i.Slot == slot && ItemMath.CanUse(rules, i, cls)),
                    $"{cls} não tem item para {slot}");
            }
        }

        foreach (var slot in new[] { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Hands, EquipSlot.Feet })
        {
            foreach (var type in Enum.GetValues<ArmorType>())
            {
                Assert.Single(Db.Items.Values, i => i.Slot == slot && i.ArmorType == type);
            }
        }
    }

    [Theory]
    [InlineData("weapon.sword", 10, ItemStat.WeaponDamage, 14)]
    [InlineData("weapon.bow", 10, ItemStat.WeaponDamage, 14)]
    [InlineData("weapon.mace", 10, ItemStat.WeaponDamage, 12)]
    [InlineData("weapon.staff", 10, ItemStat.WeaponDamage, 8)]
    [InlineData("weapon.staff", 10, ItemStat.Int, 6)]
    [InlineData("weapon.scepter", 10, ItemStat.Int, 6)]
    [InlineData("armor.medium_chest", 10, ItemStat.Armor, 26)]
    [InlineData("armor.heavy_chest", 10, ItemStat.Armor, 39)]
    [InlineData("armor.light_chest", 10, ItemStat.Armor, 16)]
    [InlineData("armor.light_chest", 10, ItemStat.Resistance, 8)]
    [InlineData("armor.medium_head", 10, ItemStat.Armor, 13)]
    [InlineData("accessory.ring", 10, ItemStat.Resistance, 19)]
    [InlineData("accessory.amulet", 10, ItemStat.Resistance, 19)]
    public void Item_bases_follow_the_design_formulas(string item, int level, ItemStat stat, double expected) =>
        Assert.Equal(expected, ItemMath.StatsOf(Catalog(), ItemInstance.Common(item, level))[(int)stat], 6);

    [Fact]
    public void Affixes_and_rarities_follow_the_design()
    {
        var rules = Db.Rules.Items;
        Assert.Equal((0.0, 1.0, 2.0, 2.0), (rules.AffixCount.Common, rules.AffixCount.Uncommon, rules.AffixCount.Rare, rules.AffixCount.Epic));

        var at10 = rules.Affixes.ToDictionary(a => a.Value.Stat, a => ItemMath.ValueAt(a.Value, 10));
        Assert.Equal(10, rules.Affixes.Count);
        Assert.Equal(Enum.GetValues<ItemStat>().Where(st => st != ItemStat.WeaponDamage).Order(), at10.Keys.Order()); // uma por estatística
        foreach (var attr in new[] { ItemStat.Str, ItemStat.Dex, ItemStat.Agi, ItemStat.Int, ItemStat.Vit })
        {
            Assert.Equal(5, at10[attr]); // 1 + 0,4 × 10
        }

        Assert.Equal(50, at10[ItemStat.Health]);
        Assert.Equal(0.015, at10[ItemStat.Crit], 6);
        Assert.Equal(0.5, at10[ItemStat.ManaRegen], 6);
        Assert.Equal(20, at10[ItemStat.Armor]);
        Assert.Equal(20, at10[ItemStat.Resistance]);
    }

    // ---------- quedas ----------

    [Fact]
    public void Drop_tables_follow_the_design_and_reference_existing_items()
    {
        var d = Db.Rules.Items.Drops;
        Assert.Equal((0.08, 1), (d.Normal.Chance, d.Normal.Count));
        Assert.Equal((70.0, 25.0, 5.0, 0.0), (d.Normal.Rarity.Common, d.Normal.Rarity.Uncommon, d.Normal.Rarity.Rare, d.Normal.Rarity.Epic));
        Assert.Equal((0.4, 1), (d.Elite.Chance, d.Elite.Count));
        Assert.Equal((40.0, 45.0, 15.0, 0.0), (d.Elite.Rarity.Common, d.Elite.Rarity.Uncommon, d.Elite.Rarity.Rare, d.Elite.Rarity.Epic));
        Assert.Equal((1.0, 2, ItemRarity.Uncommon), (d.Boss.Chance, d.Boss.Count, d.Boss.GuaranteedRarity));
        Assert.Equal((0.0, 60.0, 35.0, 5.0), (d.Boss.Rarity.Common, d.Boss.Rarity.Uncommon, d.Boss.Rarity.Rare, d.Boss.Rarity.Epic));

        var tables = new[] { d.Normal, d.Elite, d.Boss };
        Assert.All(tables.SelectMany(t => t.Items), id => Assert.True(Db.Items.ContainsKey(id), id));
        Assert.All(tables, t => Assert.Equal(t.Items.Count, t.Items.Distinct().Count()));
        Assert.All(Db.Items.Keys, id => Assert.Contains(tables, t => t.Items.Contains(id))); // todo item cai de algum lugar
    }

    // ---------- monstros ----------

    [Fact]
    public void No_monster_without_an_explicit_level()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(DataDir(), "monsters.json")));
        foreach (var m in doc.RootElement.GetProperty("monsters").EnumerateArray())
        {
            Assert.True(m.TryGetProperty("level", out var level) && level.GetInt32() >= 1, m.GetProperty("id").GetString());
        }
    }

    /// <summary>§2 "Nível por área": Campos 1–4, Floresta 5–8 (chefe 8), Ruínas 9–11.</summary>
    [Theory]
    [InlineData("monster.field_rat", 1, 1, MonsterRank.Normal)]
    [InlineData("monster.grey_wolf", 2, 3, MonsterRank.Normal)]
    [InlineData("monster.road_bandit", 3, 4, MonsterRank.Normal)]
    [InlineData("monster.spider_small", 5, 5, MonsterRank.Normal)]
    [InlineData("monster.spider_big", 5, 8, MonsterRank.Elite)]
    [InlineData("monster.forest_matriarch", 8, 8, MonsterRank.Boss)]
    [InlineData("monster.skeleton_guard", 9, 11, MonsterRank.Normal)]
    public void Monster_levels_and_ranks_follow_the_area_table(string id, int min, int max, MonsterRank rank)
    {
        var m = Db.Monsters[id];
        Assert.InRange(m.Level, min, max);
        Assert.Equal(rank, m.Rank);
    }

    [Fact]
    public void Every_monster_is_covered_by_the_area_table() =>
        Assert.Equal(
            new[] { "monster.field_rat", "monster.forest_matriarch", "monster.grey_wolf", "monster.road_bandit", "monster.skeleton_guard", "monster.spider_big", "monster.spider_small" },
            Db.Monsters.Keys.Order(StringComparer.Ordinal));
}
