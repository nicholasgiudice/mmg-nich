using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Data.Tests;

/// <summary>
/// T-423: as 10 habilidades do Sacerdote e do Inquisidor nos dados reais. Cada uma mostra o efeito principal no NH1 e no
/// NH5 (assinaturas: NH1 e NH3). Quem escala pela INT nasce como Clérigo com INT 62 (o perfil do nível 21 de
/// <c>design/segunda-classe-v0.1.md</c> §8); nas outras, unidades sem atributos nem armadura: o valor é o <c>base</c> do NH.
/// </summary>
public class ClericSecondClassContentTests
{
    private const int Tps = World.TickRate;
    private const int ProfileInt = 62;

    private static readonly Lazy<ContentCatalog> Catalog = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        var r = GameDataLoader.LoadDirectory(Path.Combine(dir!.FullName, "data"));
        Assert.True(r.Success, string.Join("\n", r.Errors));
        var db = r.Database!;
        return new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values, db.Items.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
        };
    });

    private static ClassDefinition Cleric => Catalog.Value.Classes["class.cleric"];

    private static World NewWorld() => WorldBuilder.CreateDefault(7, null, Catalog.Value);

    /// <summary>Unidade sem atributos: o valor de cada habilidade é o <c>base</c> do NH.</summary>
    private static Entity Unit(World w, Vector2 pos, Faction f, string name, float hp = 1000, params (string Id, int Rank)[] known)
    {
        var st = new AbilityState();
        Learn(st, known);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = name },
            new ResourcePool("mana", 1000, 1000),
            st);
    }

    /// <summary>Clérigo de verdade (atributos e ataque básico da classe) com a INT dada e Mana de sobra.</summary>
    private static Entity ClericAt(World w, Vector2 pos, int intel = ProfileInt, params (string Id, int Rank)[] known)
    {
        var p = EntityFactory.SpawnPlayer(w, Cleric, pos);
        p.Get<CharacterStats>().Int = intel;
        p.Get<ResourcePool>().Max = 1000;
        p.Get<ResourcePool>().Current = 1000;
        Learn(p.Get<AbilityState>(), known);
        return p;
    }

    private static Entity Warrior(World w, Vector2 pos) => EntityFactory.SpawnPlayer(w, Catalog.Value.Classes["class.warrior"], pos);

    private static void Learn(AbilityState st, (string Id, int Rank)[] known)
    {
        foreach (var (id, rank) in known)
        {
            st.Known.Add(id);
            st.Ranks[id] = rank;
        }
    }

    private static List<GameEvent> Run(World w, int ticks = 1)
    {
        var all = new List<GameEvent>();
        for (var i = 0; i < ticks; i++)
        {
            w.Step();
            all.AddRange(w.LastTickEvents);
        }

        return all;
    }

    private static List<GameEvent> Use(World w, Entity caster, string ability, Entity? target = null)
    {
        w.Enqueue(new UseAbilityIntent(caster.Id, ability, target?.Id));
        var events = Run(w);
        Assert.DoesNotContain(events.OfType<AbilityRejected>(), r => r.Source == caster.Id);
        return events;
    }

    private static float Lost(Entity e) => e.Get<Health>().Max - e.Get<Health>().Current;

    private static StatusDefinition Def(World w, StatusInstance s) => w.Content.Statuses[s.StatusId];

    /// <summary>Ticks iguais com tolerância de 1 (arredondamento de segundos para ticks).</summary>
    private static void Ticks(double expected, long actual) => Assert.InRange(actual, expected - 1, expected + 1);

    private static StatusInstance? Tagged(World w, Entity e, string tag) => StatusManager.Active(w, e).SingleOrDefault(s => Def(w, s).Tags.Contains(tag));

    private static long Left(World w, StatusInstance s) => s.ExpiresAtTick - w.Tick;

    private static (int Str, int Dex, int Agi, int Int, int Vit) Attributes(Entity e)
    {
        var s = e.Get<CharacterStats>();
        return (s.Str, s.Dex, s.Agi, s.Int, s.Vit);
    }

    // ---------- dados e classe ----------

    [Fact]
    public void Cleric_second_classes_list_their_five_abilities_in_the_design_order()
    {
        Assert.Equal(
            ["priest.restoring_prayer", "priest.blessing", "priest.swift_grace", "priest.inspiration", "priest.sanctuary"],
            Cleric.SecondClasses.Single(s => s.Id == "second_class.priest").Abilities);
        Assert.Equal(
            ["inquisitor.hammer_of_justice", "inquisitor.consecration", "inquisitor.seal_of_light", "inquisitor.judgment", "inquisitor.divine_wrath"],
            Cleric.SecondClasses.Single(s => s.Id == "second_class.inquisitor").Abilities);

        var abilities = Catalog.Value.Abilities;
        Assert.True(abilities["priest.sanctuary"].Signature);
        Assert.True(abilities["inquisitor.divine_wrath"].Signature);
        foreach (var id in Cleric.SecondClasses.SelectMany(s => s.Abilities))
        {
            Assert.False(abilities[id].Passive); // nenhuma das duas tem passiva
            Assert.Equal(abilities[id].Signature ? 3 : 5, abilities[id].HighestRank);
        }
    }

    /// <summary>Todo custo cabe na Mana do Clérigo no nível 40 (perfil com INT e VIT).</summary>
    [Fact]
    public void Every_second_class_ability_fits_the_cleric_mana_at_level_40()
    {
        var w = NewWorld();
        var hero = Leveling.SpawnCharacter(w, Cleric, Vector2.Zero, new CharacterBuild
        {
            Level = 40,
            SecondClass = "second_class.priest",
            Allocated = new AttributeSet { Int = 100, Vit = 100 },
        });
        var mana = hero.Get<ResourcePool>().Max;
        foreach (var id in Cleric.SecondClasses.SelectMany(s => s.Abilities))
        {
            for (var rank = 1; rank <= Catalog.Value.Abilities[id].HighestRank; rank++)
            {
                Assert.True((Catalog.Value.AbilityAt(id, rank)!.Cost?.Amount ?? 0) <= mana, $"{id} NH{rank} custa mais que {mana} de Mana");
            }
        }
    }

    [Theory]
    [InlineData("second_class.priest", "priest.restoring_prayer", "priest.sanctuary")]
    [InlineData("second_class.inquisitor", "inquisitor.hammer_of_justice", "inquisitor.divine_wrath")]
    public void A_level_40_character_with_every_second_class_rank_spawns_with_them_on_the_bar(string second, string first, string signature)
    {
        var w = NewWorld();
        var abilities = Cleric.SecondClasses.Single(s => s.Id == second).Abilities;
        var ranks = abilities.ToDictionary(id => id, id => Catalog.Value.Abilities[id].HighestRank);
        ranks["cleric.direct_heal"] = 5;
        ranks["cleric.holy_chain"] = 5;
        ranks["cleric.sacred_shield"] = 3;
        ranks["cleric.miracle"] = 3;
        ranks["cleric.purify"] = 1;
        Assert.Equal(40, ranks.Values.Sum());

        var hero = Leveling.SpawnCharacter(w, Cleric, Vector2.Zero, new CharacterBuild
        {
            Level = 40,
            SecondClass = second,
            Allocated = new AttributeSet { Int = 100, Vit = 100 },
            SkillRanks = ranks,
        });

        var known = hero.Get<AbilityState>().Known;
        Assert.Equal(Cleric.Abilities.Count + abilities.Count + 1, known.Count);
        Assert.Equal(first, known[Cleric.Abilities.Count + 1]);
        Assert.Equal(signature, known[^1]);
    }

    // ---------- Sacerdote ----------

    [Theory]
    [InlineData(1, 29, 8, false)] // 4 + 0,4 × 62 = 28,8
    [InlineData(5, 52, 10, true)] // 7,2 + 0,72 × 62 = 51,8; raio 8
    public void Restoring_prayer_heals_every_second_the_allies_around_by_the_priests_int(int rank, float perSecond, int seconds, bool reachesSeven)
    {
        var w = NewWorld();
        var priest = ClericAt(w, new Vector2(5, 5), known: [("priest.restoring_prayer", rank)]);
        var near = Unit(w, new Vector2(8, 5), Faction.Player, "Perto", hp: 2000);
        var seven = Unit(w, new Vector2(12.5f, 5), Faction.Player, "A 7 tiles", hp: 2000);
        near.Get<Health>().Current = 100;
        seven.Get<Health>().Current = 100;

        Use(w, priest, "priest.restoring_prayer");

        var hot = Tagged(w, near, "hot")!;
        Ticks(seconds * Tps, Left(w, hot));
        Assert.NotNull(Tagged(w, priest, "hot")); // o Sacerdote também
        Assert.Equal(reachesSeven, Tagged(w, seven, "hot") is not null);

        var heals = Run(w, (seconds * Tps) + 2).OfType<HealApplied>().Where(h => h.Target == near.Id).ToList();
        Assert.Equal(seconds, heals.Count);
        Assert.All(heals.Where(h => !h.Critical), h => Assert.Equal(perSecond, h.Amount)); // a cura pode ser crítica
    }

    [Theory]
    [InlineData(1, 7, 15, false)] // 2 + 0,08 × 62 = 6,96
    [InlineData(5, 13, 20, true)] // 3,6 + 0,144 × 62 = 12,5; raio 8
    public void Blessing_adds_every_attribute_to_the_priest_and_the_allies_around(int rank, int bonus, int seconds, bool reachesSeven)
    {
        var w = NewWorld();
        var priest = ClericAt(w, new Vector2(5, 5), known: [("priest.blessing", rank)]);
        var near = Warrior(w, new Vector2(8, 5));
        var seven = Warrior(w, new Vector2(12.5f, 5));
        var (before, priestInt) = (Attributes(near), priest.Get<CharacterStats>().Int);

        Use(w, priest, "priest.blessing");

        Assert.Equal((before.Str + bonus, before.Dex + bonus, before.Agi + bonus, before.Int + bonus, before.Vit + bonus), Attributes(near));
        Assert.Equal(priestInt + bonus, priest.Get<CharacterStats>().Int);
        Ticks(seconds * Tps, Left(w, Tagged(w, near, "blessing")!));
        Assert.Equal(reachesSeven, Tagged(w, seven, "blessing") is not null);
    }

    [Theory]
    [InlineData(1, 1.15, false, false)]
    [InlineData(5, 1.25, true, true)]
    public void Swift_grace_speeds_up_everyone_around_and_from_rank_3_removes_slows(int rank, double speed, bool cleanses, bool ward)
    {
        var w = NewWorld();
        var priest = ClericAt(w, new Vector2(5, 5), known: [("priest.swift_grace", rank)]);
        var ally = Unit(w, new Vector2(8, 5), Faction.Player, "Aliado");
        var wolf = Unit(w, new Vector2(5, 6), Faction.Monster, "Lobo");
        StatusManager.Apply(w, wolf, ally, "status.slow");

        Use(w, priest, "priest.swift_grace");

        foreach (var e in new[] { priest, ally })
        {
            var haste = StatusManager.Active(w, e).Single(s => s.StatusId.StartsWith("status.swift_grace_", StringComparison.Ordinal) && Def(w, s).Modifiers.SpeedMultiplier > 1);
            Assert.Equal(speed, Def(w, haste).Modifiers.SpeedMultiplier);
            Ticks(30 * Tps, Left(w, haste)); // 30 s em todos os NH
        }

        Assert.Equal(!cleanses, StatusManager.HasTag(w, ally, "slow"));
        Assert.Equal(ward, StatusManager.Apply(w, wolf, ally, "status.slow") is null); // NH5: imune a lentidão por 10 s
        if (ward)
        {
            Ticks(10 * Tps, Left(w, StatusManager.Get(w, ally, "status.swift_grace_ward")!));
        }
    }

    [Theory]
    [InlineData(1, 4.0, 12, false)] // 1,5 + 0,04 × 62 = 3,98
    [InlineData(5, 7.2, 15, true)] // 2,7 + 0,072 × 62 = 7,16; raio 8
    public void Inspiration_raises_the_mana_regen_of_the_allies_but_not_of_the_priest(int rank, double regen, int seconds, bool reachesSeven)
    {
        var w = NewWorld();
        var priest = ClericAt(w, new Vector2(5, 5), known: [("priest.inspiration", rank)]);
        var near = Warrior(w, new Vector2(8, 5));
        var seven = Warrior(w, new Vector2(12.5f, 5));
        var (nearBefore, priestBefore) = (near.Get<CharacterStats>().ManaRegen, priest.Get<CharacterStats>().ManaRegen);

        Use(w, priest, "priest.inspiration");

        Assert.Equal(regen, near.Get<CharacterStats>().ManaRegen - nearBefore, 0.05);
        Assert.Equal(priestBefore, priest.Get<CharacterStats>().ManaRegen);
        Assert.Null(Tagged(w, priest, "inspiration"));
        Ticks(seconds * Tps, Left(w, Tagged(w, near, "inspiration")!));
        Assert.Equal(reachesSeven, Tagged(w, seven, "inspiration") is not null);
    }

    [Theory]
    [InlineData(1, 4, 0.0)]
    [InlineData(3, 6, 0.2)]
    public void Sanctuary_keeps_everyone_around_at_1_health_and_at_rank_3_heals_when_it_ends(int rank, int seconds, double healAtEnd)
    {
        var w = NewWorld();
        var priest = ClericAt(w, new Vector2(5, 5), known: [("priest.sanctuary", rank)]);
        var ally = Unit(w, new Vector2(8, 5), Faction.Player, "Aliado", hp: 1000);
        var wolf = Unit(w, new Vector2(8, 6), Faction.Monster, "Lobo");

        Use(w, priest, "priest.sanctuary");

        Assert.NotNull(Tagged(w, priest, "sanctuary"));
        Ticks(seconds * Tps, Left(w, Tagged(w, ally, "sanctuary")!));
        DamageResolver.Deal(w, wolf, ally, "test.bite", 100000, DamageSchool.Physical);
        Assert.True(ally.IsAlive);
        Assert.Equal(1, ally.Get<Health>().Current);

        Run(w, (seconds * Tps) + 2);
        Assert.Null(Tagged(w, ally, "sanctuary"));
        Assert.Equal(1 + CoreEffects.Round(1000 * healAtEnd), ally.Get<Health>().Current);
    }

    // ---------- Inquisidor ----------

    [Theory]
    [InlineData(1, 10, 1.0, false)]
    [InlineData(5, 18, 1.5, true)]
    public void Hammer_of_justice_deals_holy_damage_and_stuns_and_the_rank_5_arc_hits_the_side(int rank, float damage, double stun, bool hitsSide)
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(10, 10), Faction.Player, "Inquisidor", known: [("inquisitor.hammer_of_justice", rank)]);
        var front = Unit(w, new Vector2(11.4f, 10), Faction.Monster, "Frente");
        var side = Unit(w, new Vector2(10 + (1.4f * MathF.Cos(50 * MathF.PI / 180)), 10 + (1.4f * MathF.Sin(50 * MathF.PI / 180))), Faction.Monster, "Lado");

        var events = Use(w, inquisitor, "inquisitor.hammer_of_justice", front);

        var hit = Assert.Single(events.OfType<DamageDealt>(), d => d.Target == front.Id);
        Assert.Equal(damage, hit.Amount);
        Assert.Equal(DamageSchool.Holy, hit.School);
        Ticks(stun * Tps, Left(w, StatusManager.Get(w, front, "status.stunned")!));
        Assert.Equal(hitsSide, Lost(side) > 0);
        Assert.Equal(1000 - 25, inquisitor.Get<ResourcePool>().Current);
    }

    [Theory]
    [InlineData(1, 4, 3, 6, false)]
    [InlineData(5, 7, 5, 8, true)] // 7,2 e 5,4; raio 4
    public void Consecration_hurts_monsters_and_heals_allies_every_second(int rank, float damage, float heal, int pulses, bool reachesThreeAndHalf)
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(5, 5), Faction.Player, "Inquisidor", known: [("inquisitor.consecration", rank)]);
        var ally = Unit(w, new Vector2(6, 5), Faction.Player, "Aliado");
        var wolf = Unit(w, new Vector2(5, 7), Faction.Monster, "Lobo", hp: 100000);
        var edge = Unit(w, new Vector2(5, 1.5f), Faction.Monster, "A 3,5 tiles", hp: 100000);
        ally.Get<Health>().Current = 100;

        var events = Use(w, inquisitor, "inquisitor.consecration");
        events.AddRange(Run(w, (pulses + 1) * Tps));

        var hits = events.OfType<DamageDealt>().Where(d => d.Target == wolf.Id).ToList();
        Assert.Equal(pulses, hits.Count);
        Assert.All(hits, d => Assert.Equal(damage, d.Amount));
        Assert.All(hits, d => Assert.Equal(DamageSchool.Holy, d.School));
        var heals = events.OfType<HealApplied>().Where(h => h.Target == ally.Id).ToList();
        Assert.Equal(pulses, heals.Count);
        Assert.All(heals, h => Assert.Equal(heal, h.Amount));
        Assert.DoesNotContain(events.OfType<HealApplied>(), h => h.Target == wolf.Id);
        Assert.Equal(reachesThreeAndHalf, Lost(edge) > 0);
    }

    [Theory]
    [InlineData(1, 0.10, 8)]
    [InlineData(5, 0.20, 10)]
    public void Seal_of_light_heals_the_allies_who_hit_the_marked_monster(int rank, double fraction, int seconds)
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(1, 1), Faction.Player, "Inquisidor", known: [("inquisitor.seal_of_light", rank)]);
        var ally = Unit(w, new Vector2(6, 2), Faction.Player, "Aliado");
        var wolf = Unit(w, new Vector2(6, 1), Faction.Monster, "Lobo", hp: 100000);
        ally.Get<Health>().Current = 500;

        Use(w, inquisitor, "inquisitor.seal_of_light", wolf);

        var seal = Tagged(w, wolf, "seal")!;
        Ticks(seconds * Tps, Left(w, seal));
        DamageResolver.Deal(w, ally, wolf, "test.strike", 100, DamageSchool.Physical);
        Assert.Equal(500 + CoreEffects.Round(100 * fraction), ally.Get<Health>().Current);
        Assert.Equal(1000 - 20, inquisitor.Get<ResourcePool>().Current);
    }

    [Theory]
    [InlineData(1, 20, 0.0)]
    [InlineData(5, 36, 0.2)]
    public void Judgment_deals_strong_holy_damage_up_close_and_at_rank_5_heals_the_inquisitor(int rank, float damage, double selfHeal)
    {
        var w = NewWorld();
        var inquisitor = Unit(w, new Vector2(1, 1), Faction.Player, "Inquisidor", known: [("inquisitor.judgment", rank)]);
        var near = Unit(w, new Vector2(3, 1), Faction.Monster, "Perto", hp: 100000);
        var mid = Unit(w, new Vector2(5.5f, 1), Faction.Monster, "A 4,5 tiles", hp: 100000);
        inquisitor.Get<Health>().Current = 500;

        var events = Use(w, inquisitor, "inquisitor.judgment", near);

        var hit = Assert.Single(events.OfType<DamageDealt>());
        Assert.Equal((damage, DamageSchool.Holy), (hit.Amount, hit.School));
        Assert.Equal(500 + CoreEffects.Round(damage * selfHeal), inquisitor.Get<Health>().Current);

        // alcance 3, e 5 do NH3 em diante
        w.Enqueue(new UseAbilityIntent(inquisitor.Id, "inquisitor.judgment", mid.Id));
        inquisitor.Get<AbilityState>().ReadyAtTick.Clear();
        var second = Run(w);
        Assert.Equal(rank >= 3, Lost(mid) > 0);
        Assert.Equal(rank < 3, second.OfType<AbilityRejected>().Any(r => r.Source == inquisitor.Id));
    }

    [Theory]
    [InlineData(1, 10, 0.10)]
    [InlineData(3, 15, 0.15)]
    public void Divine_wrath_turns_the_basic_attack_into_an_area_that_heals_the_allies_near(int rank, int seconds, double healFraction)
    {
        var w = NewWorld();
        var inquisitor = ClericAt(w, new Vector2(5, 5), known: [("inquisitor.divine_wrath", rank)]);
        var ally = Unit(w, new Vector2(7, 5), Faction.Player, "Aliado", hp: 5000);
        var target = Unit(w, new Vector2(9, 5), Faction.Monster, "Alvo", hp: 100000);
        var beside = Unit(w, new Vector2(9, 6.5f), Faction.Monster, "Ao lado", hp: 100000);
        var away = Unit(w, new Vector2(9, 9), Faction.Monster, "Longe", hp: 100000);
        ally.Get<Health>().Current = 1000;

        Use(w, inquisitor, "inquisitor.divine_wrath");
        var wrath = Tagged(w, inquisitor, "divine_wrath")!;
        Ticks(seconds * Tps, Left(w, wrath));

        var events = Use(w, inquisitor, "cleric.holy_strike", target); // o ataque básico de sempre
        events.AddRange(Run(w, 2));

        var hits = events.OfType<DamageDealt>().Where(d => d.Source == inquisitor.Id).ToList();
        Assert.Equal([target.Id, beside.Id], hits.Select(d => d.Target).Order().ToList());
        Assert.Equal(0, Lost(away)); // raio 2 em volta do alvo
        var allyHeal = events.OfType<HealApplied>().Where(h => h.Target == ally.Id).ToList();
        Assert.All(allyHeal, h => Assert.Equal("inquisitor.divine_wrath", h.SourceId));
        Assert.Equal(hits.Sum(d => CoreEffects.Round(d.Amount * healFraction)), allyHeal.Sum(h => h.Amount));

        // o golpe é o Golpe Sagrado da classe (ARMA + 0,6 × INT) × 1,5 no NH1 (× 1,8 no NH3); o crítico multiplica
        var stats = inquisitor.Get<CharacterStats>();
        var expected = ((stats.WeaponDamage * 1.0) + (0.6 * stats.Int)) * (rank >= 3 ? 1.8 : 1.5);
        Assert.All(hits, d => Assert.Equal(expected * (d.Critical ? w.Content.Rules.Stats.CritMultiplier : 1), d.Amount, 1.0));
    }
}
