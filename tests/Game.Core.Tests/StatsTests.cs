using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-201: atributos, escala dos efeitos, defesa, crítico, velocidade de ataque e derivados (vida, Mana, regeneração).</summary>
public class StatsTests
{
    private static readonly StatRules NoCrit = new() { CritBase = 0, CritPerAgi = 0 };
    private static readonly UnitTargeting Enemy = new() { Range = 20, Faction = FactionFilter.Enemy };

    private static AbilityDefinition Ab(string id, params EffectDefinition[] effects) =>
        new() { Id = id, Name = id, Targeting = Enemy, Effects = effects };

    private static World NewWorld(StatRules rules, params AbilityDefinition[] abilities) =>
        WorldBuilder.CreateDefault(7, null, new ContentCatalog(abilities: abilities) { Rules = new RulesDefinition { Stats = rules } });

    private static Entity Unit(World w, Vector2 pos, Faction f, float hp = 1000, CharacterStats? stats = null, UnitInfo? info = null, params string[] known)
    {
        var st = new AbilityState();
        st.Known.AddRange(known);
        var e = w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            info ?? new UnitInfo { DefinitionId = "test", Name = f == Faction.Player ? "Heroi" : "Inimigo" },
            st);
        if (stats is not null)
        {
            e.Add(stats);
        }

        return e;
    }

    private static List<GameEvent> Use(World w, Entity e, string ability, Entity target)
    {
        w.Enqueue(new UseAbilityIntent(e.Id, ability, target.Id, null));
        w.Step();
        return w.LastTickEvents.ToList();
    }

    // ---------- escala ----------

    [Fact]
    public void Damage_is_base_plus_coefficients_times_source_stats()
    {
        var hit = Ab("hero.hit", new DamageEffect { Base = 10, Scaling = new Dictionary<string, double> { ["weapon"] = 1.0, ["str"] = 2.0 } });
        var w = NewWorld(NoCrit, hit);
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, stats: new CharacterStats { WeaponDamage = 6, Str = 5 }, known: "hero.hit");
        var foe = Unit(w, new Vector2(3, 1), Faction.Monster);

        var dmg = Assert.Single(Use(w, hero, "hero.hit", foe).OfType<DamageDealt>());

        Assert.Equal(26, dmg.Amount, 3); // 10 + 6 × 1 + 5 × 2
        Assert.False(dmg.Critical);
    }

    [Fact]
    public void Source_without_stats_uses_only_the_base()
    {
        var hit = Ab("mob.hit", new DamageEffect { Base = 10, Scaling = new Dictionary<string, double> { ["str"] = 3.0 } });
        var w = NewWorld(new StatRules(), hit);
        var mob = Unit(w, new Vector2(1, 1), Faction.Monster, known: "mob.hit");
        var hero = Unit(w, new Vector2(3, 1), Faction.Player);

        var dmg = Assert.Single(Use(w, mob, "mob.hit", hero).OfType<DamageDealt>());

        Assert.Equal(10, dmg.Amount, 3);
        Assert.False(dmg.Critical);
    }

    [Fact]
    public void Heal_and_shield_scale_with_int()
    {
        var mend = new AbilityDefinition
        {
            Id = "hero.mend",
            Name = "mend",
            Targeting = new UnitTargeting { Range = 20, Faction = FactionFilter.Ally },
            Effects =
            [
                new HealEffect { Base = 10, Scaling = new Dictionary<string, double> { ["int"] = 1.5 } },
                new ShieldEffect { Amount = 20, DurationTicks = 100, Scaling = new Dictionary<string, double> { ["int"] = 2.0 } },
            ],
        };
        var w = NewWorld(NoCrit, mend);
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, stats: new CharacterStats { Int = 10 }, known: "hero.mend");
        var ally = Unit(w, new Vector2(2, 1), Faction.Player);
        ally.Get<Health>().Current = 500;

        var ev = Use(w, hero, "hero.mend", ally);

        Assert.Equal(25, Assert.Single(ev.OfType<HealApplied>()).Amount, 3);
        Assert.Equal(40, Assert.Single(ev.OfType<ShieldApplied>()).Amount, 3);
    }

    // ---------- defesa ----------

    [Fact]
    public void Armor_reduces_physical_damage_by_attacker_level()
    {
        var hit = Ab("hero.hit", new DamageEffect { Base = 100 });
        var w = NewWorld(NoCrit, hit);
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, stats: new CharacterStats(), known: "hero.hit");
        var foe = Unit(w, new Vector2(3, 1), Faction.Monster, info: new UnitInfo { DefinitionId = "m", Name = "m", Armor = 48 });

        var dmg = Assert.Single(Use(w, hero, "hero.hit", foe).OfType<DamageDealt>());

        Assert.Equal(50, dmg.Amount, 3); // 48 / (48 + 40 + 8 × 1) = 50%

        hero.Get<CharacterStats>().Level = 7; // 48 / (48 + 40 + 56) = 1/3
        Assert.Equal(1.0 / 3, StatMath.DefenseReduction(w, foe, hero, DamageSchool.Physical), 6);
    }

    [Fact]
    public void Resistance_reduces_other_schools_and_armor_does_not()
    {
        var w = NewWorld(NoCrit);
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, stats: new CharacterStats());
        var armored = Unit(w, new Vector2(3, 1), Faction.Monster, info: new UnitInfo { DefinitionId = "m", Name = "m", Armor = 48 });
        var warded = Unit(w, new Vector2(3, 3), Faction.Monster, info: new UnitInfo { DefinitionId = "m", Name = "m", Resistance = 48 });

        Assert.Equal(0, StatMath.DefenseReduction(w, armored, hero, DamageSchool.Fire));
        Assert.Equal(0.5, StatMath.DefenseReduction(w, warded, hero, DamageSchool.Fire), 6);
        Assert.Equal(0, StatMath.DefenseReduction(w, warded, hero, DamageSchool.Physical));
    }

    [Fact]
    public void Defense_is_capped()
    {
        var w = NewWorld(NoCrit);
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, stats: new CharacterStats());
        var wall = Unit(w, new Vector2(3, 1), Faction.Monster, stats: new CharacterStats { Armor = 10_000 });

        Assert.Equal(0.6, StatMath.DefenseReduction(w, wall, hero, DamageSchool.Physical), 6);
    }

    // ---------- crítico ----------

    [Fact]
    public void Critical_multiplies_damage_and_is_flagged()
    {
        var hit = Ab("hero.hit", new DamageEffect { Base = 10 });
        var w = NewWorld(new StatRules { CritBase = 1, CritMax = 1 }, hit);
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, stats: new CharacterStats(), known: "hero.hit");
        var foe = Unit(w, new Vector2(3, 1), Faction.Monster);

        var dmg = Assert.Single(Use(w, hero, "hero.hit", foe).OfType<DamageDealt>());

        Assert.True(dmg.Critical);
        Assert.Equal(15, dmg.Amount, 3);
    }

    [Fact]
    public void Critical_rolls_are_deterministic_by_seed()
    {
        static List<bool> Rolls(ulong seed)
        {
            var hit = Ab("hero.hit", new DamageEffect { Base = 1 });
            var w = WorldBuilder.CreateDefault(seed, null, new ContentCatalog(abilities: [hit])
            {
                Rules = new RulesDefinition { Stats = new StatRules { CritBase = 0.5 } },
            });
            var hero = Unit(w, new Vector2(1, 1), Faction.Player, stats: new CharacterStats(), known: "hero.hit");
            var foe = Unit(w, new Vector2(3, 1), Faction.Monster);
            var rolls = new List<bool>();
            for (var i = 0; i < 40; i++)
            {
                rolls.AddRange(Use(w, hero, "hero.hit", foe).OfType<DamageDealt>().Select(d => d.Critical));
            }

            return rolls;
        }

        var a = Rolls(11);
        Assert.Equal(a, Rolls(11));
        Assert.Contains(true, a);
        Assert.Contains(false, a);
    }

    [Fact]
    public void Crit_chance_grows_with_agi_up_to_the_cap()
    {
        var r = new StatRules();
        Assert.Equal(0.065, StatMath.CritChance(new CharacterStats { Agi = 6 }, r), 6);
        Assert.Equal(0.40, StatMath.CritChance(new CharacterStats { Agi = 1000 }, r), 6);
    }

    // ---------- AGI: velocidade do ataque básico ----------

    [Fact]
    public void Agi_shortens_only_the_basic_attack_cooldown()
    {
        var basic = Ab("hero.basic", new DamageEffect { Base = 1 }) with { CooldownTicks = 30 };
        var skill = Ab("hero.skill", new DamageEffect { Base = 1 }) with { CooldownTicks = 30 };
        var w = NewWorld(NoCrit, basic, skill);
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, stats: new CharacterStats { Agi = 100 }, known: ["hero.basic", "hero.skill"]);
        hero.Get<AbilityState>().BasicAttackId = "hero.basic";
        var foe = Unit(w, new Vector2(3, 1), Faction.Monster);

        var b = Assert.Single(Use(w, hero, "hero.basic", foe).OfType<AbilityExecuted>());
        var s = Assert.Single(Use(w, hero, "hero.skill", foe).OfType<AbilityExecuted>());

        // usados em ticks seguidos: básico pronto em t + 20 (30 ÷ (1 + 100 × 0,005)), habilidade em t + 1 + 30
        Assert.Equal(11, s.CooldownReadyTick - b.CooldownReadyTick);
    }

    [Fact]
    public void Attack_speed_bonus_is_capped()
    {
        var r = new StatRules();
        Assert.Equal(1.03, StatMath.AttackSpeedFactor(new CharacterStats { Agi = 6 }, r), 6);
        Assert.Equal(1.5, StatMath.AttackSpeedFactor(new CharacterStats { Agi = 1000 }, r), 6);
    }

    // ---------- derivados com o perfil real (data/classes.json) ----------

    [Theory]
    [InlineData("class.warrior", 240, 88)]
    [InlineData("class.archer", 130, 90)]
    [InlineData("class.mage", 110, 106)]
    [InlineData("class.cleric", 150, 104)]
    public void Level_one_profile_gives_prototype_health_and_int_mana(string classId, float health, float mana)
    {
        var w = WorldBuilder.CreateDefault(1, null, RealContent.Value);
        var p = EntityFactory.SpawnPlayer(w, RealContent.Value.Classes[classId], new Vector2(2, 2));

        Assert.Equal(health, p.Get<Health>().Max);
        Assert.Equal(mana, p.Get<ResourcePool>().Max);
        Assert.Equal(mana, p.Get<ResourcePool>().Current);
        Assert.Equal(1, p.Get<CharacterStats>().Level);
    }

    [Fact]
    public void Mage_starts_with_staff_int_and_regenerates_mana_with_int()
    {
        var w = WorldBuilder.CreateDefault(1, null, RealContent.Value);
        var mage = EntityFactory.SpawnPlayer(w, RealContent.Value.Classes["class.mage"], new Vector2(2, 2));
        Assert.Equal(13, mage.Get<CharacterStats>().Int); // 11 + 2 do cajado

        mage.Get<ResourcePool>().Current = 0;
        for (var i = 0; i < 10 * World.TickRate; i++)
        {
            w.Step();
        }

        Assert.Equal(56, mage.Get<ResourcePool>().Current); // 10 s × (5 + 13 × 0,05)
    }

    [Fact]
    public void Max_health_grows_with_level()
    {
        var mage = RealContent.Value.Classes["class.mage"];
        var stats = StatMath.FromClass(mage)!;
        stats.Level = 3;

        Assert.Equal(110 + (2 * mage.HealthGrowth), StatMath.MaxHealth(mage, stats, new StatRules()), 6);
    }
}
