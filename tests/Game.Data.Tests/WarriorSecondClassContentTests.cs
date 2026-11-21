using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Status;
using Game.Core.Combat.Threat;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Data.Tests;

/// <summary>
/// T-408: as 10 habilidades do Guardião e do Berserker nos dados reais. Cada uma mostra o efeito principal no NH1 e no
/// NH5 (assinaturas: NH1 e NH3). Unidades de teste sem atributos nem armadura: o dano é o <c>base</c> do NH e a escala por
/// atributo fica de fora; os números de tabela estão em <c>design/segunda-classe-v0.1.md</c> §5.
/// </summary>
public class WarriorSecondClassContentTests
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

    /// <summary>Ticks que faltam para o status acabar.</summary>
    private static long Left(World w, Entity e, string statusId) => StatusManager.Get(w, e, statusId)!.ExpiresAtTick - w.Tick;

    private static long CooldownLeft(World w, Entity e, string ability) => e.Get<AbilityState>().ReadyAtTick[ability] - w.Tick;

    private static StatusInstance? WithRedirect(World w, Entity e) => StatusManager.Active(w, e).SingleOrDefault(s => Def(w, s).Redirect is not null);

    private static StatusInstance? Tagged(World w, Entity e, string tag) => StatusManager.Active(w, e).SingleOrDefault(s => Def(w, s).Tags.Contains(tag));

    // ---------- dados e classe ----------

    [Fact]
    public void Warrior_second_classes_list_their_five_abilities_in_the_design_order()
    {
        var warrior = Catalog.Value.Classes["class.warrior"];
        Assert.Equal(
            ["guardian.interception", "guardian.shield_bash", "guardian.bastion", "guardian.steadfast", "guardian.oath"],
            warrior.SecondClasses.Single(s => s.Id == "second_class.guardian").Abilities);
        Assert.Equal(
            ["berserker.charge", "berserker.fury", "berserker.execution", "berserker.blood_thirst", "berserker.steel_storm"],
            warrior.SecondClasses.Single(s => s.Id == "second_class.berserker").Abilities);

        var abilities = Catalog.Value.Abilities;
        Assert.True(abilities["guardian.oath"].Signature);
        Assert.True(abilities["berserker.steel_storm"].Signature);
        Assert.True(abilities["berserker.blood_thirst"].Passive);
        foreach (var id in warrior.SecondClasses.SelectMany(s => s.Abilities))
        {
            Assert.Equal(abilities[id].Signature ? 3 : 5, abilities[id].HighestRank);
        }
    }

    /// <summary>A pendência da T-402: com 88 de Mana no nível 40 (o Guerreiro não põe INT), toda habilidade cabe na barra.</summary>
    [Fact]
    public void Every_second_class_ability_fits_the_warrior_mana_at_level_40()
    {
        var w = NewWorld();
        var hero = Leveling.SpawnCharacter(w, Catalog.Value.Classes["class.warrior"], Vector2.Zero, new CharacterBuild
        {
            Level = 40,
            SecondClass = "second_class.berserker",
            Allocated = new AttributeSet { Str = 100, Vit = 100 },
        });
        var mana = hero.Get<ResourcePool>().Max;
        Assert.InRange(mana, 80, 100);
        foreach (var id in Catalog.Value.Classes["class.warrior"].SecondClasses.SelectMany(s => s.Abilities))
        {
            for (var rank = 1; rank <= Catalog.Value.Abilities[id].HighestRank; rank++)
            {
                Assert.True((Catalog.Value.AbilityAt(id, rank)!.Cost?.Amount ?? 0) <= mana, $"{id} NH{rank} custa mais que {mana} de Mana");
            }
        }
    }

    [Theory]
    [InlineData("second_class.guardian", "guardian.interception", "guardian.oath", null)]
    [InlineData("second_class.berserker", "berserker.charge", "berserker.steel_storm", "berserker.blood_thirst")]
    public void A_level_40_character_with_every_second_class_rank_spawns_with_them_on_the_bar(
        string second, string first, string signature, string? passive)
    {
        var w = NewWorld();
        var cls = Catalog.Value.Classes["class.warrior"];
        var abilities = cls.SecondClasses.Single(s => s.Id == second).Abilities;
        var ranks = abilities.ToDictionary(id => id, id => Catalog.Value.Abilities[id].HighestRank);
        ranks["warrior.heavy_strike"] = 5;
        ranks["warrior.whirlwind"] = 5;
        ranks["warrior.living_wall"] = 3;
        ranks["warrior.firm_guard"] = 3;
        ranks["warrior.taunt"] = 1;
        Assert.Equal(40, ranks.Values.Sum());

        var hero = Leveling.SpawnCharacter(w, cls, Vector2.Zero, new CharacterBuild
        {
            Level = 40,
            SecondClass = second,
            Allocated = new AttributeSet { Str = 100, Vit = 100 },
            SkillRanks = ranks,
        });

        var known = hero.Get<AbilityState>().Known;
        Assert.Equal(cls.Abilities.Count + abilities.Count + 1 - (passive is null ? 0 : 1), known.Count);
        Assert.Equal(first, known[cls.Abilities.Count + 1]);
        Assert.Equal(signature, known[^1]);
        if (passive is not null)
        {
            Assert.DoesNotContain(passive, known);
            Assert.True(StatusManager.IsPermanent(StatusManager.Get(w, hero, Catalog.Value.AbilityAt(passive, 5)!.PassiveStatus!)!));
        }
    }

    // ---------- Guardião ----------

    [Theory]
    [InlineData(1, 0.30, 4, 12, 0)]
    [InlineData(5, 0.40, 6, 9, 40)]
    public void Interception_dashes_to_the_ally_and_takes_part_of_its_damage(int rank, double fraction, int seconds, int cooldown, float shield)
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(1, 1), Faction.Player, "Guardião", known: [("guardian.interception", rank)]);
        var ally = Unit(w, new Vector2(7, 1), Faction.Player, "Maga");
        var wolf = Unit(w, new Vector2(7, 2.5f), Faction.Monster, "Lobo");

        var events = Use(w, guardian, "guardian.interception", ally);

        Assert.True(Vector2.Distance(guardian.Get<Transform>().Position, ally.Get<Transform>().Position) <= 0.81f, "avança até o aliado");
        var link = WithRedirect(w, ally)!;
        Assert.Equal(fraction, Def(w, link).Redirect!.Fraction);
        Ticks(seconds * Tps, link.ExpiresAtTick - w.Tick);
        Ticks(cooldown * Tps, CooldownLeft(w, guardian, "guardian.interception"));
        Assert.Equal(shield, events.OfType<ShieldApplied>().Where(s => s.Target == ally.Id).Sum(s => s.Amount)); // escudo no NH3+: 40 + 2 × VIT no NH5

        var allyPart = DamageResolver.Deal(w, wolf, ally, "test.bite", 100, DamageSchool.Physical);
        Assert.Equal(CoreEffects.Round(100 * (1 - fraction)), allyPart);
        Assert.Equal(CoreEffects.Round(100 * fraction), Lost(guardian));
    }

    [Theory]
    [InlineData(1, 10, 1.0, false)]
    [InlineData(5, 18, 1.5, true)]
    public void Shield_bash_stuns_with_triple_threat_and_the_rank_5_arc_hits_the_side(int rank, float damage, double stun, bool hitsSide)
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(10, 10), Faction.Player, "Guardião", known: [("guardian.shield_bash", rank)]);
        var front = EntityFactory.SpawnMonster(w, Catalog.Value.Monsters["monster.grey_wolf"], new Vector2(11.4f, 10));
        var side = Unit(w, new Vector2(10 + (1.4f * MathF.Cos(50 * MathF.PI / 180)), 10 + (1.4f * MathF.Sin(50 * MathF.PI / 180))), Faction.Monster, "Lado");

        var events = Use(w, guardian, "guardian.shield_bash", front);

        var hit = Assert.Single(events.OfType<DamageDealt>(), d => d.Target == front.Id);
        Assert.InRange(hit.Amount, damage * 0.5f, damage); // base do NH menos a armadura do Lobo
        Assert.Equal(hit.Amount * 3, front.Get<ThreatTable>().Entries[guardian.Id.Value], 0.01);
        Ticks(stun * Tps, Left(w, front, "status.stunned"));
        Assert.Equal(hitsSide, Lost(side) > 0);
    }

    [Theory]
    [InlineData(1, 0.85, 6, false, false)]
    [InlineData(5, 0.75, 8, true, true)]
    public void Bastion_cuts_damage_for_allies_around_the_guardian(int rank, double taken, int seconds, bool reachesSix, bool knockbackImmune)
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(5, 5), Faction.Player, "Guardião", known: [("guardian.bastion", rank)]);
        var near = Unit(w, new Vector2(8, 5), Faction.Player, "Perto");
        var six = Unit(w, new Vector2(11, 5), Faction.Player, "A 6 tiles");
        var wolf = Unit(w, new Vector2(5, 6), Faction.Monster, "Lobo");

        Use(w, guardian, "guardian.bastion");

        foreach (var e in new[] { guardian, near })
        {
            var s = Tagged(w, e, "bastion")!;
            Assert.Equal(taken, Def(w, s).Modifiers.DamageTakenMultiplier);
            Ticks(seconds * Tps, s.ExpiresAtTick - w.Tick);
            Assert.Equal(knockbackImmune, StatusManager.IsKnockbackImmune(w, e));
        }

        Assert.Equal(reachesSix, Tagged(w, six, "bastion") is not null); // raio 5, e 7 do NH3 em diante
        Assert.Null(Tagged(w, wolf, "bastion"));
        Assert.Equal(CoreEffects.Round(100 * taken), DamageResolver.Deal(w, wolf, near, "test.bite", 100, DamageSchool.Physical));
    }

    [Theory]
    [InlineData(1, 3, 0.9, false)]
    [InlineData(5, 5, 0.8, true)]
    public void Steadfast_cleanses_control_and_grants_immunity(int rank, int seconds, double taken, bool cleansesAllies)
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(5, 5), Faction.Player, "Guardião", known: [("guardian.steadfast", rank)]);
        var ally = Unit(w, new Vector2(8, 5), Faction.Player, "Aliado");
        var wolf = Unit(w, new Vector2(5, 6), Faction.Monster, "Lobo");
        StatusManager.Apply(w, wolf, guardian, "status.slow");
        StatusManager.Apply(w, wolf, guardian, "status.snared");
        StatusManager.Apply(w, wolf, ally, "status.stunned", 3 * Tps);
        var mana = guardian.Get<ResourcePool>().Current;

        Use(w, guardian, "guardian.steadfast");

        Assert.False(StatusManager.HasTag(w, guardian, "slow"));
        Assert.False(StatusManager.IsRooted(w, guardian));
        Assert.Equal(mana, guardian.Get<ResourcePool>().Current); // sem custo
        var s = Tagged(w, guardian, "steadfast")!;
        Ticks(seconds * Tps, s.ExpiresAtTick - w.Tick);
        Assert.Null(StatusManager.Apply(w, wolf, guardian, "status.stunned")); // imune pela duração
        Assert.Equal(CoreEffects.Round(100 * taken), DamageResolver.Deal(w, wolf, guardian, "test.bite", 100, DamageSchool.Physical));

        Assert.Equal(!cleansesAllies, StatusManager.IsStunned(w, ally)); // NH5: tira dos aliados num raio de 4
        Assert.Null(Tagged(w, ally, "steadfast")); // sem imunidade para eles
    }

    [Theory]
    [InlineData(1, 0.5, 0.4, 8, false)]
    [InlineData(3, 0.7, 0.5, 10, true)]
    public void Oath_redirects_the_group_damage_to_the_guardian_reduced(int rank, double fraction, double reduction, int seconds, bool reachesSeven)
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(5, 5), Faction.Player, "Guardião", known: [("guardian.oath", rank)]);
        var near = Unit(w, new Vector2(9, 5), Faction.Player, "Perto");
        var seven = Unit(w, new Vector2(5, 12), Faction.Player, "A 7 tiles");
        var wolf = Unit(w, new Vector2(10, 5), Faction.Monster, "Lobo");

        Use(w, guardian, "guardian.oath");

        var link = WithRedirect(w, near)!;
        Assert.Equal((fraction, reduction), (Def(w, link).Redirect!.Fraction, Def(w, link).Redirect!.Reduction));
        Ticks(seconds * Tps, link.ExpiresAtTick - w.Tick);
        Assert.Equal(reachesSeven, WithRedirect(w, seven) is not null); // raio 6, e 8 no NH3
        Assert.Null(WithRedirect(w, guardian));

        Assert.Equal(CoreEffects.Round(100 * (1 - fraction)), DamageResolver.Deal(w, wolf, near, "test.bite", 100, DamageSchool.Physical));
        Assert.Equal(CoreEffects.Round(100 * fraction * (1 - reduction)), Lost(guardian));
    }

    // ---------- Berserker ----------

    [Theory]
    [InlineData(1, 8, 0.5, 10, false)]
    [InlineData(5, 14, 1.0, 7, true)]
    public void Charge_dashes_to_the_monster_and_stuns_it(int rank, float damage, double stun, int cooldown, bool empowersNextHit)
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(1, 1), Faction.Player, "Berserker", known: [("berserker.charge", rank), ("berserker.execution", 1)]);
        var wolf = Unit(w, new Vector2(7, 1), Faction.Monster, "Lobo");

        var events = Use(w, berserker, "berserker.charge", wolf);

        Assert.True(Vector2.Distance(berserker.Get<Transform>().Position, wolf.Get<Transform>().Position) <= 0.81f, "avança até o alvo");
        Assert.Equal(damage, Assert.Single(events.OfType<DamageDealt>()).Amount);
        Ticks(stun * Tps, Left(w, wolf, "status.stunned"));
        Ticks(cooldown * Tps, CooldownLeft(w, berserker, "berserker.charge"));

        // NH5: o próximo golpe em até 3 s causa +30% (a Execução NH1 dá 20)
        Assert.Equal(empowersNextHit, StatusManager.Has(w, berserker, "status.charge_momentum"));
        var next = Use(w, berserker, "berserker.execution", wolf);
        Assert.Equal(empowersNextHit ? 26 : 20, Assert.Single(next.OfType<DamageDealt>()).Amount);
        Assert.False(StatusManager.Has(w, berserker, "status.charge_momentum"));
    }

    [Theory]
    [InlineData(1, 1.20, 1.10, 8, 1.0)]
    [InlineData(5, 1.36, 1.15, 10, 0.5)]
    public void Fury_raises_damage_dealt_and_taken_and_speed(int rank, double dealt, double speed, int seconds, double controlFactor)
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(1, 1), Faction.Player, "Berserker", known: [("berserker.fury", rank)]);
        var wolf = Unit(w, new Vector2(2, 1), Faction.Monster, "Lobo");
        var mana = berserker.Get<ResourcePool>().Current;

        Use(w, berserker, "berserker.fury");

        Assert.Equal(mana, berserker.Get<ResourcePool>().Current); // sem custo (T-408)
        var s = Tagged(w, berserker, "fury")!;
        Ticks(seconds * Tps, s.ExpiresAtTick - w.Tick);
        Assert.Equal(speed, Def(w, s).Modifiers.SpeedMultiplier);
        Assert.Equal(CoreEffects.Round(100 * dealt), DamageResolver.Deal(w, berserker, wolf, "test.strike", 100, DamageSchool.Physical));
        Assert.Equal(120, DamageResolver.Deal(w, wolf, berserker, "test.bite", 100, DamageSchool.Physical)); // recebe 20% mais

        StatusManager.Apply(w, wolf, berserker, "status.stunned", Tps);
        Ticks(Tps * controlFactor, Left(w, berserker, "status.stunned")); // NH5: controle dura metade
    }

    [Theory]
    [InlineData(1, 500, 20)] // 50% de vida: dano normal
    [InlineData(1, 300, 20)] // 30% exatos: não está abaixo
    [InlineData(1, 200, 30)] // 20%: +50%
    [InlineData(5, 400, 36)] // 40%: dano normal do NH5
    [InlineData(5, 340, 63)] // 34%: abaixo de 35%, +75%
    public void Execution_deals_more_to_low_health_targets(int rank, float health, float expected)
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(1, 1), Faction.Player, "Berserker", known: [("berserker.execution", rank)]);
        var wolf = Unit(w, new Vector2(2, 1), Faction.Monster, "Lobo");
        wolf.Get<Health>().Current = health;

        var events = Use(w, berserker, "berserker.execution", wolf);

        Assert.Equal(expected, Assert.Single(events.OfType<DamageDealt>()).Amount);
        Assert.Equal(1000 - 20, berserker.Get<ResourcePool>().Current); // custo 20 (T-408)
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(5, true)]
    public void Execution_resets_its_cooldown_on_a_kill_only_at_rank_5(int rank, bool resets)
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(1, 1), Faction.Player, "Berserker", known: [("berserker.execution", rank)]);
        var rat = Unit(w, new Vector2(2, 1), Faction.Monster, "Rato", hp: 10);

        Use(w, berserker, "berserker.execution", rat);

        Assert.False(rat.IsAlive);
        Assert.Equal(!resets, berserker.Get<AbilityState>().IsOnCooldown(w.Content.AbilityAt("berserker.execution", rank)!, w.Tick));
    }

    [Theory]
    [InlineData(1, 0.05, 1.0, 0.0)]
    [InlineData(5, 0.10, 2.0, 0.05)]
    public void Blood_thirst_passive_heals_from_the_damage_dealt(int rank, double fraction, double lowHealthFactor, double killHeal)
    {
        var w = NewWorld();
        var cls = Catalog.Value.Classes["class.warrior"];
        var berserker = Leveling.SpawnCharacter(w, cls, new Vector2(1, 1), new CharacterBuild
        {
            Level = 40,
            SecondClass = "second_class.berserker",
            Allocated = new AttributeSet { Str = 100, Vit = 100 },
            SkillRanks = new Dictionary<string, int> { ["berserker.blood_thirst"] = rank },
        });
        var wolf = Unit(w, new Vector2(2, 1), Faction.Monster, "Lobo", hp: 100000);
        var rat = Unit(w, new Vector2(1, 2), Faction.Monster, "Rato", hp: 50);
        var health = berserker.Get<Health>();
        Assert.Equal(AbilityRejectReason.Passive, Assert.Single(UseRejected(w, berserker, "berserker.blood_thirst")).Reason);

        // acima de 30% de vida: a fração do NH
        health.Current = health.Max / 2;
        var dealt = DamageResolver.Deal(w, berserker, wolf, "test.strike", 1000, DamageSchool.Physical);
        Assert.Equal(health.Max / 2 + CoreEffects.Round(dealt * fraction), health.Current, 1);

        // abaixo de 30%: dobra do NH3 em diante
        health.Current = health.Max / 5;
        dealt = DamageResolver.Deal(w, berserker, wolf, "test.strike", 1000, DamageSchool.Physical);
        Assert.Equal(health.Max / 5 + CoreEffects.Round(dealt * fraction * lowHealthFactor), health.Current, 1);

        // NH5: matar cura 5% da vida máxima a mais
        health.Current = health.Max / 2;
        dealt = DamageResolver.Deal(w, berserker, rat, "test.strike", 1000, DamageSchool.Physical);
        Assert.False(rat.IsAlive);
        Assert.Equal(health.Max / 2 + CoreEffects.Round(dealt * fraction) + CoreEffects.Round(health.Max * killHeal), health.Current, 2);
    }

    private static List<AbilityRejected> UseRejected(World w, Entity caster, string ability)
    {
        w.Enqueue(new UseAbilityIntent(caster.Id, ability, null));
        return Run(w).OfType<AbilityRejected>().ToList();
    }

    [Theory]
    [InlineData(1, 6, false)]
    [InlineData(3, 10, true)]
    public void Steel_storm_spins_eight_times_around_the_moving_berserker(int rank, float perSpin, bool bleeds)
    {
        var w = NewWorld();
        var berserker = Unit(w, new Vector2(5, 5), Faction.Player, "Berserker", known: [("berserker.steel_storm", rank)]);
        var near = Unit(w, new Vector2(5, 7), Faction.Monster, "Perto", hp: 100000);
        var far = Unit(w, new Vector2(5, 9), Faction.Monster, "Longe", hp: 100000);
        var mana = berserker.Get<ResourcePool>().Current;

        var events = Use(w, berserker, "berserker.steel_storm");
        Assert.Equal(mana - 80, berserker.Get<ResourcePool>().Current); // custo 80 (T-408; era 90)
        var area = w.Find(Assert.Single(events.OfType<AreaSpawned>()).Area)!;
        Assert.True(w.Content.Areas[area.Get<AreaState>().AreaId].FollowOwner);
        Assert.Equal(0.8, Def(w, Tagged(w, berserker, "steel_storm")!).Modifiers.SpeedMultiplier); // 20% mais devagar

        events.AddRange(Run(w, 4 * Tps));

        var spins = events.OfType<DamageDealt>().Where(d => d.Target == near.Id && d.SourceId != "status.bleed").ToList();
        Assert.Equal(8, spins.Count);
        Assert.All(spins, d => Assert.Equal(perSpin, d.Amount));
        Assert.Equal(bleeds, StatusManager.Has(w, near, "status.bleed") || events.OfType<DamageDealt>().Any(d => d.SourceId == "status.bleed"));
        Assert.Equal(0, Lost(far)); // raio 3 em volta de onde ele está
    }
}
