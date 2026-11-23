using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Data.Tests;

/// <summary>
/// T-433: as 10 habilidades do Atirador de Elite e do Caçador nos dados reais. Cada uma mostra o efeito principal no NH1 e
/// no NH5 (assinaturas: NH1 e NH3). Unidades de teste sem atributos nem armadura: o dano é o <c>base</c> do NH e a escala
/// por atributo fica de fora (as passivas usam um personagem de verdade, porque elas valem pela progressão). Os números de
/// tabela estão em <c>design/segunda-classe-v0.1.md</c> §6.
/// </summary>
public class ArcherSecondClassContentTests
{
    private const int Tps = World.TickRate;

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

    private static ClassDefinition Archer => Catalog.Value.Classes["class.archer"];

    private static World NewWorld() => WorldBuilder.CreateDefault(7, null, Catalog.Value);

    private static Entity Unit(World w, Vector2 pos, Faction f, string name, float hp = 1000, params (string Id, int Rank)[] known)
    {
        var st = new AbilityState();
        foreach (var (id, rank) in known)
        {
            st.Known.Add(id);
            st.Ranks[id] = rank;
        }

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

    /// <summary>Arqueiro de verdade no nível 40 com a segunda classe e os NH dados (as passivas entram pela progressão).</summary>
    private static Entity Hero(World w, Vector2 pos, string second, Dictionary<string, int> ranks) =>
        Leveling.SpawnCharacter(w, Archer, pos, new CharacterBuild
        {
            Level = 40,
            SecondClass = second,
            Allocated = new AttributeSet { Dex = 150, Agi = 50 },
            SkillRanks = ranks,
        });

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

    private static List<GameEvent> Use(World w, Entity caster, string ability, Entity? target = null, Vector2? aim = null)
    {
        w.Enqueue(new UseAbilityIntent(caster.Id, ability, target?.Id, aim));
        var events = Run(w);
        Assert.DoesNotContain(events.OfType<AbilityRejected>(), r => r.Source == caster.Id);
        return events;
    }

    private static StatusDefinition Def(World w, StatusInstance s) => w.Content.Statuses[s.StatusId];

    /// <summary>Ticks iguais com tolerância de 1 (arredondamento de segundos para ticks).</summary>
    private static void Ticks(double expected, long actual) => Assert.InRange(actual, expected - 1, expected + 1);

    private static long CooldownLeft(World w, Entity e, string ability) => e.Get<AbilityState>().ReadyAtTick[ability] - w.Tick;

    private static StatusInstance? Tagged(World w, Entity e, string tag) => StatusManager.Active(w, e).SingleOrDefault(s => Def(w, s).Tags.Contains(tag));

    private static float Hits(IEnumerable<GameEvent> events, Entity target, string sourceId) =>
        events.OfType<DamageDealt>().Where(d => d.Target == target.Id && d.SourceId == sourceId).Sum(d => d.Amount);

    // ---------- dados e classe ----------

    [Fact]
    public void Archer_second_classes_list_their_five_abilities_in_the_design_order()
    {
        Assert.Equal(
            ["marksman.piercing_arrow", "marksman.crippling_shot", "marksman.steady_aim", "marksman.true_shot", "marksman.hawk_eye"],
            Archer.SecondClasses.Single(s => s.Id == "second_class.marksman").Abilities);
        Assert.Equal(
            ["hunter.poison_arrow", "hunter.open_wound", "hunter.toxic_cloud", "hunter.scatter_arrow", "hunter.plague"],
            Archer.SecondClasses.Single(s => s.Id == "second_class.hunter").Abilities);

        var abilities = Catalog.Value.Abilities;
        Assert.True(abilities["marksman.hawk_eye"].Signature);
        Assert.True(abilities["hunter.plague"].Signature);
        Assert.True(abilities["marksman.steady_aim"].Passive);
        Assert.True(abilities["hunter.poison_arrow"].Passive);
        foreach (var id in Archer.SecondClasses.SelectMany(s => s.Abilities))
        {
            Assert.Equal(abilities[id].Signature ? 3 : 5, abilities[id].HighestRank);
        }
    }

    /// <summary>Com DES e AGI (o Arqueiro não põe INT), toda habilidade cabe na Mana do nível 40 (Praga 80, Olho de Falcão 70).</summary>
    [Fact]
    public void Every_second_class_ability_fits_the_archer_mana_at_level_40()
    {
        var w = NewWorld();
        var hero = Hero(w, Vector2.Zero, "second_class.hunter", []);
        var mana = hero.Get<ResourcePool>().Max;
        Assert.InRange(mana, 80, 120);
        foreach (var id in Archer.SecondClasses.SelectMany(s => s.Abilities))
        {
            for (var rank = 1; rank <= Catalog.Value.Abilities[id].HighestRank; rank++)
            {
                Assert.True((Catalog.Value.AbilityAt(id, rank)!.Cost?.Amount ?? 0) <= mana, $"{id} NH{rank} custa mais que {mana} de Mana");
            }
        }
    }

    [Theory]
    [InlineData("second_class.marksman", "marksman.piercing_arrow", "marksman.hawk_eye", "marksman.steady_aim")]
    [InlineData("second_class.hunter", "hunter.open_wound", "hunter.plague", "hunter.poison_arrow")]
    public void A_level_40_character_with_every_second_class_rank_spawns_with_them_on_the_bar(
        string second, string first, string signature, string passive)
    {
        var w = NewWorld();
        var abilities = Archer.SecondClasses.Single(s => s.Id == second).Abilities;
        var ranks = abilities.ToDictionary(id => id, id => Catalog.Value.Abilities[id].HighestRank);
        ranks["archer.power_shot"] = 5;
        ranks["archer.rain_of_arrows"] = 5;
        ranks["archer.devastating_arrow"] = 3;
        ranks["archer.hunters_mark"] = 3;
        ranks["archer.quick_retreat"] = 1;
        Assert.Equal(40, ranks.Values.Sum());

        var hero = Hero(w, Vector2.Zero, second, ranks);

        var known = hero.Get<AbilityState>().Known;
        Assert.Equal(Archer.Abilities.Count + abilities.Count, known.Count); // + o Disparo − a passiva
        Assert.Equal(first, known[Archer.Abilities.Count + 1]);
        Assert.Equal(signature, known[^1]);
        Assert.DoesNotContain(passive, known);
        Assert.True(StatusManager.IsPermanent(StatusManager.Get(w, hero, Catalog.Value.AbilityAt(passive, 5)!.PassiveStatus!)!));
    }

    // ---------- Atirador de Elite ----------

    [Theory]
    [InlineData(1, 14, 0.6, 8, false)]
    [InlineData(5, 25.2, 0.8, 6, true)]
    public void Piercing_arrow_deals_less_to_who_is_behind_in_the_line(int rank, float damage, double behind, int cooldown, bool reaches13)
    {
        var w = NewWorld();
        var archer = Unit(w, new Vector2(2, 10), Faction.Player, "Atirador", known: [("marksman.piercing_arrow", rank)]);
        var first = Unit(w, new Vector2(5, 10), Faction.Monster, "Primeiro");
        var second = Unit(w, new Vector2(8, 10), Faction.Monster, "Atrás");
        var far = Unit(w, new Vector2(15, 10), Faction.Monster, "A 13 tiles");

        var events = Use(w, archer, "marksman.piercing_arrow", first, new Vector2(5, 10));

        Assert.Equal(damage, Hits(events, first, "marksman.piercing_arrow"), 0.5);
        Assert.Equal(CoreEffects.Round(damage * behind), Hits(events, second, "marksman.piercing_arrow"), 0.5);
        Assert.Equal(reaches13, Hits(events, far, "marksman.piercing_arrow") > 0); // linha de 10 tiles, 14 no NH5
        Ticks(cooldown * Tps, CooldownLeft(w, archer, "marksman.piercing_arrow"));
    }

    [Theory]
    [InlineData(1, 6, 0.7, 0.85, 4)]
    [InlineData(5, 10.8, 0.5, 0.75, 6)]
    public void Crippling_shot_slows_and_weakens_the_target(int rank, float damage, double speed, double dealt, int seconds)
    {
        var w = NewWorld();
        var archer = Unit(w, new Vector2(2, 10), Faction.Player, "Atirador", known: [("marksman.crippling_shot", rank)]);
        var wolf = Unit(w, new Vector2(10, 10), Faction.Monster, "Lobo");

        var events = Use(w, archer, "marksman.crippling_shot", wolf);

        Assert.Equal(damage, Hits(events, wolf, "marksman.crippling_shot"), 0.5);
        var s = Tagged(w, wolf, "weaken")!;
        Assert.Equal(speed, Def(w, s).Modifiers.SpeedMultiplier);
        Ticks(seconds * Tps, s.ExpiresAtTick - w.Tick);
        Assert.Equal(CoreEffects.Round(100 * dealt), DamageResolver.Deal(w, wolf, archer, "test.bite", 100, DamageSchool.Physical));
    }

    [Theory]
    [InlineData(1, 0.06, 0, 1.5)]
    [InlineData(5, 0.12, 1, 1.65)]
    public void Steady_aim_passive_adds_crit_against_targets_six_tiles_away(int rank, double bonus, double range, double critFactor)
    {
        var w = NewWorld();
        var hero = Hero(w, new Vector2(2, 10), "second_class.marksman", new() { ["marksman.steady_aim"] = rank });
        var near = Unit(w, new Vector2(7, 10), Faction.Monster, "A 4,6 tiles");
        var far = Unit(w, new Vector2(10, 10), Faction.Monster, "A 7,6 tiles");
        var shot = Catalog.Value.Abilities["archer.shot"];
        Assert.Equal(AbilityRejectReason.Passive, Assert.Single(UseRejected(w, hero, "marksman.steady_aim")).Reason);

        var baseChance = StatusCrit.Chance(w, hero, near, shot);
        Assert.Equal(baseChance + bonus, StatusCrit.Chance(w, hero, far, shot), 3);
        Assert.Equal(range, RangeBonus.For(w, hero, shot));
        Assert.Equal(critFactor, StatusCrit.For(w, hero, far).Multiplier ?? 1.5, 3);
    }

    [Theory]
    [InlineData(1, 25, 0.3, 10)]
    [InlineData(5, 45, 0.5, 8)]
    public void True_shot_fires_without_a_cast_and_ignores_part_of_the_armor(int rank, float damage, double ignore, int cooldown)
    {
        var w = NewWorld();
        var archer = Unit(w, new Vector2(2, 10), Faction.Player, "Atirador", known: [("marksman.true_shot", rank)]);
        var wolf = Unit(w, new Vector2(13, 10), Faction.Monster, "Lobo a 11 tiles");
        var def = Catalog.Value.AbilityAt("marksman.true_shot", rank)!;
        Assert.Equal(0, def.CastTimeTicks); // sem conjuração (decisão do designer, 2026-11-22)

        var events = Use(w, archer, "marksman.true_shot", wolf);

        Assert.Equal(damage, Hits(events, wolf, "marksman.true_shot"), 0.5); // sai no mesmo tick
        Assert.Equal(ignore, def.Effects.OfType<DamageEffect>().Single().IgnoreArmor);
        Ticks(cooldown * Tps, CooldownLeft(w, archer, "marksman.true_shot"));
    }

    [Theory]
    [InlineData(1, 15, 0)]
    [InlineData(3, 20, 2)]
    public void Hawk_eye_makes_every_hit_critical(int rank, int seconds, double range)
    {
        var w = NewWorld();
        // crítico só para quem tem estatísticas de personagem: um Arqueiro de verdade
        var archer = Hero(w, new Vector2(2, 10), "second_class.marksman", new() { ["marksman.hawk_eye"] = rank, ["marksman.true_shot"] = 1 });
        var wolf = Unit(w, new Vector2(5, 10), Faction.Monster, "Lobo", hp: 100000);
        var shot = Catalog.Value.Abilities["archer.shot"];
        Assert.InRange(StatusCrit.Chance(w, archer, wolf, shot), 0.05, 0.4);
        var mana = archer.Get<ResourcePool>().Current;

        Use(w, archer, "marksman.hawk_eye");

        Assert.Equal(mana - 70, archer.Get<ResourcePool>().Current);
        var s = Tagged(w, archer, "hawk_eye")!;
        Ticks(seconds * Tps, s.ExpiresAtTick - w.Tick);
        Assert.Equal(range, RangeBonus.For(w, archer, shot));
        Assert.Equal(1, StatusCrit.Chance(w, archer, wolf, shot));
        for (var i = 0; i < 5; i++)
        {
            var hit = Assert.Single(Use(w, archer, "archer.shot", wolf).OfType<DamageDealt>());
            Assert.True(hit.Critical);
            Run(w, Tps);
        }
    }

    // ---------- Caçador ----------

    [Theory]
    [InlineData(1, "status.hunter_venom_1", 3, 4)]
    [InlineData(5, "status.hunter_venom_5", 5, 6)]
    public void Poison_arrow_passive_stacks_the_hunter_venom_on_every_hit(int rank, string venom, int maxStacks, int seconds)
    {
        var w = NewWorld();
        var hero = Hero(w, new Vector2(2, 10), "second_class.hunter", new() { ["hunter.poison_arrow"] = rank });
        var wolf = Unit(w, new Vector2(8, 10), Faction.Monster, "Lobo", hp: 100000);

        for (var i = 1; i <= 6; i++)
        {
            DamageResolver.Deal(w, hero, wolf, "archer.shot", 10, DamageSchool.Physical);
            var s = StatusManager.Get(w, wolf, venom)!;
            Assert.Equal(Math.Min(i, maxStacks), s.Stacks);
            Ticks(seconds * Tps, s.ExpiresAtTick - w.Tick);
            Assert.Equal(hero.Id, s.Source);
        }

        Assert.Null(StatusManager.Get(w, wolf, "status.hunter_venom")); // o apelido nunca fica no monstro
    }

    [Theory]
    [InlineData(1, 6, 0.5, 6)]
    [InlineData(5, 10.8, 1.0, 8)]
    public void Open_wound_bleeds_more_while_the_target_moves(int rank, float perSecond, double movingBonus, int seconds)
    {
        var w = NewWorld();
        var hunter = Unit(w, new Vector2(2, 10), Faction.Player, "Caçador", known: [("hunter.open_wound", rank)]);
        var wolf = Unit(w, new Vector2(8, 10), Faction.Monster, "Lobo", hp: 100000);

        Use(w, hunter, "hunter.open_wound", wolf);
        var s = Tagged(w, wolf, "bleed")!;
        Ticks(seconds * Tps, s.ExpiresAtTick - w.Tick);

        var still = Run(w, Tps);
        Assert.Equal(CoreEffects.Round(perSecond), Hits(still, wolf, s.StatusId));

        w.Enqueue(new MoveIntent(wolf.Id, new Vector2(0, 1)));
        var moving = Run(w, Tps);
        Assert.Equal(CoreEffects.Round(perSecond * (1 + movingBonus)), Hits(moving, wolf, s.StatusId));
    }

    [Theory]
    [InlineData(1, 3, 6, 4, false)]
    [InlineData(5, 4, 8, 7.2, true)]
    public void Toxic_cloud_damages_and_poisons_who_stays_in_it(int rank, float radius, int seconds, float perSecond, bool slows)
    {
        var w = NewWorld();
        var hunter = Unit(w, new Vector2(2, 10), Faction.Player, "Caçador", known: [("hunter.toxic_cloud", rank)]);
        var wolf = Unit(w, new Vector2(10, 10), Faction.Monster, "Lobo", hp: 100000);

        var events = Use(w, hunter, "hunter.toxic_cloud", aim: new Vector2(10, 10));
        var areas = events.OfType<AreaSpawned>().ToList();
        Assert.Equal(2, areas.Count); // dano a cada 1 s e veneno a cada 2 s
        Assert.All(areas, a => Assert.Equal(radius, a.Radius));
        Assert.All(areas, a => Ticks(seconds * Tps, a.ExpiresAtTick - w.Tick));

        events.AddRange(Run(w, (4 * Tps) + 2)); // veneno aos 2 s e aos 4 s
        var cloud = areas.Single(a => !a.AreaId.Contains("venom", StringComparison.Ordinal)).AreaId;
        Assert.Equal(CoreEffects.Round(perSecond), events.OfType<DamageDealt>().First(d => d.SourceId == cloud).Amount);
        Assert.Equal(2, StatusManager.Get(w, wolf, "status.hunter_venom_1")!.Stacks); // sem a passiva: o veneno do NH1
        Assert.Equal(slows, StatusManager.HasTag(w, wolf, "slow"));
    }

    [Theory]
    [InlineData(1, 8, 2)]
    [InlineData(5, 14.4, 4)]
    public void Scatter_arrow_copies_venom_and_bleed_to_nearby_monsters(int rank, float damage, int copies)
    {
        var w = NewWorld();
        var hunter = Unit(w, new Vector2(2, 10), Faction.Player, "Caçador", known: [("hunter.scatter_arrow", rank)]);
        var target = Unit(w, new Vector2(10, 10), Faction.Monster, "Alvo", hp: 100000);
        var others = Enumerable.Range(0, 4)
            .Select(i => Unit(w, new Vector2(10 + MathF.Cos(i * MathF.PI / 2) * (1.5f + (0.5f * i)), 10 + MathF.Sin(i * MathF.PI / 2) * (1.5f + (0.5f * i))), Faction.Monster, $"Perto {i}", hp: 100000))
            .ToList();
        var outside = Unit(w, new Vector2(10, 16), Faction.Monster, "A 6 tiles", hp: 100000);
        StatusManager.Apply(w, hunter, target, "status.hunter_venom");
        StatusManager.Apply(w, hunter, target, "status.hunter_venom");
        StatusManager.Apply(w, hunter, target, "status.open_wound");

        var events = Use(w, hunter, "hunter.scatter_arrow", target);

        Assert.Equal(damage, Hits(events, target, "hunter.scatter_arrow"), 0.5);
        var poisoned = others.Where(o => StatusManager.Get(w, o, "status.hunter_venom_1") is { Stacks: 2 }).ToList();
        Assert.Equal(copies, poisoned.Count);
        Assert.Equal(poisoned, others.Take(copies)); // os mais perto primeiro
        Assert.All(poisoned, o => Assert.True(StatusManager.Has(w, o, "status.open_wound")));
        Assert.False(StatusManager.HasTag(w, outside, "hunter_venom"));
    }

    [Theory]
    [InlineData(1, 3, 8)]
    [InlineData(3, 5, 10)]
    public void Plague_poisons_everyone_in_the_radius_and_jumps_when_it_ends(int rank, int stacks, int seconds)
    {
        var w = NewWorld();
        var hunter = Unit(w, new Vector2(2, 10), Faction.Player, "Caçador", known: [("hunter.plague", rank), ("hunter.poison_arrow", 5)]);
        var a = Unit(w, new Vector2(10, 10), Faction.Monster, "A", hp: 100000);
        var b = Unit(w, new Vector2(10, 15), Faction.Monster, "B (a 5 tiles)", hp: 100000);
        var far = Unit(w, new Vector2(10, 19), Faction.Monster, "Fora do raio", hp: 100000);

        Use(w, hunter, "hunter.plague", aim: new Vector2(10, 10));

        Assert.Equal(stacks, StatusManager.Get(w, a, "status.hunter_venom_5")!.Stacks);
        Assert.Equal(stacks, StatusManager.Get(w, b, "status.hunter_venom_5")!.Stacks);
        Assert.False(StatusManager.HasTag(w, far, "hunter_venom"));
        var plague = Tagged(w, hunter, "plague")!;
        Ticks(seconds * Tps, plague.ExpiresAtTick - w.Tick);
        Assert.Equal(80, 1000 - hunter.Get<ResourcePool>().Current);

        // o veneno de B acaba (6 s) com a Praga ainda ativa: pula com as pilhas para o mais perto sem veneno (a 4 tiles)
        StatusManager.Remove(w, a, "status.hunter_venom_5", hunter.Id);
        Run(w, (6 * Tps) + 1);
        Assert.Equal(stacks, StatusManager.Get(w, far, "status.hunter_venom_5")?.Stacks ?? 0);
    }

    private static List<AbilityRejected> UseRejected(World w, Entity caster, string ability)
    {
        w.Enqueue(new UseAbilityIntent(caster.Id, ability, null));
        return Run(w).OfType<AbilityRejected>().ToList();
    }
}
