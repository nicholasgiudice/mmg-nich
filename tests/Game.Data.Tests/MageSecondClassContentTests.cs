using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Combat.Status;
using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Data.Tests;

/// <summary>
/// T-443: as 10 habilidades do Piromante e do Arcanista nos dados reais. Cada uma mostra o efeito principal no NH1 e no NH5
/// (assinaturas: NH1 e NH3). Quem lança é uma unidade sem atributos (o valor é o <c>base</c> do NH), menos no Sifão Arcano,
/// que precisa de um Mago de verdade (a Mana por segundo vale só em personagem), com a INT 122 do perfil do nível 21 de
/// <c>design/segunda-classe-v0.1.md</c> §7.
/// </summary>
public class MageSecondClassContentTests
{
    private const int Tps = World.TickRate;
    private const int ProfileInt = 122;

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

    private static ClassDefinition Mage => Catalog.Value.Classes["class.mage"];

    private static World NewWorld() => WorldBuilder.CreateDefault(7, null, Catalog.Value);

    /// <summary>Unidade sem atributos: o valor de cada habilidade é o <c>base</c> do NH.</summary>
    private static Entity Unit(World w, Vector2 pos, Faction f, string name, float hp = 100000, params (string Id, int Rank)[] known)
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

    private static Entity Monster(World w, float x, float y, string name) => Unit(w, new Vector2(x, y), Faction.Monster, name);

    /// <summary>Mago de verdade (atributos e ataque básico da classe) com a INT dada e Mana de sobra.</summary>
    private static Entity MageAt(World w, Vector2 pos, int intel = ProfileInt, params (string Id, int Rank)[] known)
    {
        var p = EntityFactory.SpawnPlayer(w, Mage, pos);
        p.Get<CharacterStats>().Int = intel;
        p.Get<ResourcePool>().Max = 1000;
        p.Get<ResourcePool>().Current = 1000;
        Learn(p.Get<AbilityState>(), known);
        return p;
    }

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

    private static List<GameEvent> Use(World w, Entity caster, string ability, Entity? target = null, Vector2? aim = null)
    {
        w.Enqueue(new UseAbilityIntent(caster.Id, ability, target?.Id, aim));
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

    private static IEnumerable<DamageDealt> Hits(IEnumerable<GameEvent> events, string sourceId, Entity? target = null) =>
        events.OfType<DamageDealt>().Where(d => d.SourceId == sourceId && (target is null || d.Target == target.Id));

    private static Vector2 At(Vector2 from, float distance, float degrees) =>
        from + (distance * new Vector2(MathF.Cos(degrees * MathF.PI / 180), MathF.Sin(degrees * MathF.PI / 180)));

    // ---------- dados e classe ----------

    [Fact]
    public void Mage_second_classes_list_their_five_abilities_in_the_design_order()
    {
        Assert.Equal(
            ["pyromancer.combustion", "pyromancer.flame_burst", "pyromancer.burning_ground", "pyromancer.fireball", "pyromancer.meteor"],
            Mage.SecondClasses.Single(s => s.Id == "second_class.pyromancer").Abilities);
        Assert.Equal(
            ["arcanist.arcane_ray", "arcanist.arcane_mark", "arcanist.arcane_orbs", "arcanist.arcane_siphon", "arcanist.void_beam"],
            Mage.SecondClasses.Single(s => s.Id == "second_class.arcanist").Abilities);

        var abilities = Catalog.Value.Abilities;
        Assert.True(abilities["pyromancer.meteor"].Signature);
        Assert.True(abilities["arcanist.void_beam"].Signature);
        foreach (var id in Mage.SecondClasses.SelectMany(s => s.Abilities))
        {
            Assert.False(abilities[id].Passive); // nenhuma das duas tem passiva
            Assert.Equal(abilities[id].Signature ? 3 : 5, abilities[id].HighestRank);
        }
    }

    /// <summary>Todo custo cabe na Mana do Mago no nível 40 (perfil com INT).</summary>
    [Fact]
    public void Every_second_class_ability_fits_the_mage_mana_at_level_40()
    {
        var w = NewWorld();
        var hero = Leveling.SpawnCharacter(w, Mage, Vector2.Zero, new CharacterBuild
        {
            Level = 40,
            SecondClass = "second_class.pyromancer",
            Allocated = new AttributeSet { Int = 200 },
        });
        var mana = hero.Get<ResourcePool>().Max;
        foreach (var id in Mage.SecondClasses.SelectMany(s => s.Abilities))
        {
            for (var rank = 1; rank <= Catalog.Value.Abilities[id].HighestRank; rank++)
            {
                Assert.True((Catalog.Value.AbilityAt(id, rank)!.Cost?.Amount ?? 0) <= mana, $"{id} NH{rank} custa mais que {mana} de Mana");
            }
        }
    }

    [Theory]
    [InlineData("second_class.pyromancer", "pyromancer.combustion", "pyromancer.meteor")]
    [InlineData("second_class.arcanist", "arcanist.arcane_ray", "arcanist.void_beam")]
    public void A_level_40_character_with_every_second_class_rank_spawns_with_them_on_the_bar(string second, string first, string signature)
    {
        var w = NewWorld();
        var abilities = Mage.SecondClasses.Single(s => s.Id == second).Abilities;
        var ranks = abilities.ToDictionary(id => id, id => Catalog.Value.Abilities[id].HighestRank);
        ranks["mage.elemental_blast"] = 5;
        ranks["mage.lightning_field"] = 5;
        ranks["mage.ice_prison"] = 3;
        ranks["mage.arcane_cataclysm"] = 3;
        ranks["mage.arcane_barrier"] = 1;
        Assert.Equal(40, ranks.Values.Sum());

        var hero = Leveling.SpawnCharacter(w, Mage, Vector2.Zero, new CharacterBuild
        {
            Level = 40,
            SecondClass = second,
            Allocated = new AttributeSet { Int = 200 },
            SkillRanks = ranks,
        });

        var known = hero.Get<AbilityState>().Known;
        Assert.Equal(Mage.Abilities.Count + abilities.Count + 1, known.Count);
        Assert.Equal(first, known[Mage.Abilities.Count + 1]);
        Assert.Equal(signature, known[^1]);
    }

    // ---------- Piromante ----------

    /// <summary>Queimadura por NH: 4 s, um pulso por segundo, do mesmo grupo (fica a de NH mais alto).</summary>
    [Theory]
    [InlineData(1, 3, 0.3)]
    [InlineData(5, 5.4, 0.54)]
    public void Burn_of_each_rank_deals_fire_damage_every_second_for_4_seconds(int rank, double perSecond, double intScaling)
    {
        var burn = Catalog.Value.Statuses[$"status.burn_{rank}"];
        Assert.Equal(4 * Tps, burn.DefaultDurationTicks);
        Assert.Equal(("pyromancer_burn", rank), (burn.Exclusive!.Group, burn.Exclusive.Tier));
        Assert.Contains("burn", burn.Tags);
        var pulse = Assert.Single(burn.Periodic);
        Assert.Equal(Tps, pulse.IntervalTicks);
        var damage = Assert.IsType<DamageEffect>(Assert.Single(pulse.Effects));
        Assert.Equal((perSecond, intScaling, DamageSchool.Fire), (damage.Base, damage.Scaling["int"], damage.School));
    }

    [Theory]
    [InlineData(1, 20, false, false)]
    [InlineData(5, 36, true, true)]
    public void Combustion_only_hurts_who_burns_and_from_rank_3_keeps_the_burn(int rank, float damage, bool keepsBurn, bool spreads)
    {
        var w = NewWorld();
        var pyro = Unit(w, new Vector2(1, 10), Faction.Player, "Piromante", known: [("pyromancer.combustion", rank)]);
        var a = Monster(w, 5, 10, "Queimando A");
        var b = Monster(w, 5, 12, "Queimando B");
        var near = Monster(w, 5, 14, "A 2 tiles de B");
        StatusManager.Apply(w, pyro, a, "status.burn_1");
        StatusManager.Apply(w, pyro, b, "status.burn_1");

        var events = Use(w, pyro, "pyromancer.combustion");

        Assert.Equal(damage, Assert.Single(Hits(events, "pyromancer.combustion", a)).Amount);
        Assert.Equal(damage, Assert.Single(Hits(events, "pyromancer.combustion", b)).Amount);
        Assert.Empty(Hits(events, "pyromancer.combustion", near)); // não queimava: não leva dano
        Assert.Equal(keepsBurn, Tagged(w, a, "burn") is not null);
        Assert.Equal(keepsBurn, Tagged(w, b, "burn") is not null);
        Assert.Equal(spreads, Tagged(w, near, "burn") is not null); // NH5: pula para quem está a até 2 tiles
    }

    [Theory]
    [InlineData(1, 14, false, 4)]
    [InlineData(5, 25, true, 6)]
    public void Flame_burst_burns_a_short_cone_that_widens_at_rank_3(int rank, float damage, bool hitsSide, int burnSeconds)
    {
        var w = NewWorld();
        var pyro = Unit(w, new Vector2(5, 5), Faction.Player, "Piromante", known: [("pyromancer.flame_burst", rank)]);
        var front = Monster(w, 8, 5, "À frente, a 3 tiles");
        var sideAt = At(new Vector2(5, 5), 3, 55);
        var side = Monster(w, sideAt.X, sideAt.Y, "A 55°");
        var far = Monster(w, 10, 5, "A 5 tiles");

        var events = Use(w, pyro, "pyromancer.flame_burst", front);

        Assert.Equal(damage, Assert.Single(Hits(events, "pyromancer.flame_burst", front)).Amount);
        Assert.Equal(hitsSide, Lost(side) > 0); // 90° no NH1, 120° do NH3 em diante
        Assert.Equal(0, Lost(far)); // 4 tiles à frente
        var burn = Tagged(w, front, "burn")!;
        Assert.Equal($"status.burn_{rank}", burn.StatusId);
        Ticks(burnSeconds * Tps, Left(w, burn));
    }

    [Theory]
    [InlineData(1, 5, 3, 6, false)]
    [InlineData(5, 9, 4, 8, true)]
    public void Burning_ground_pulses_every_second_and_burns_who_stands_in_it(int rank, float perPulse, float radius, int seconds, bool reachesThreeAndHalf)
    {
        var w = NewWorld();
        var pyro = Unit(w, new Vector2(2, 5), Faction.Player, "Piromante", known: [("pyromancer.burning_ground", rank)]);
        var center = Monster(w, 10, 5, "No centro");
        var edge = Monster(w, 13.5f, 5, "A 3,5 tiles");

        var events = Use(w, pyro, "pyromancer.burning_ground", aim: new Vector2(10, 5));
        var spawned = Assert.Single(events.OfType<AreaSpawned>());
        Assert.Equal(($"area.burning_ground_{rank}", radius), (spawned.AreaId, spawned.Radius));
        Ticks(seconds * Tps, spawned.ExpiresAtTick - w.Tick + 1);

        events.AddRange(Run(w, (seconds + 2) * Tps));

        var pulses = Hits(events, spawned.AreaId, center).ToList();
        Assert.Equal(seconds, pulses.Count);
        Assert.All(pulses, p => Assert.Equal(perPulse, p.Amount));
        Assert.Contains(events.OfType<StatusApplied>(), s => s.Target == center.Id && s.StatusId == $"status.burn_{rank}");
        Assert.Equal(reachesThreeAndHalf, Hits(events, spawned.AreaId, edge).Any()); // raio 3, e 4 do NH3 em diante
    }

    [Theory]
    [InlineData(1, 38, false)]
    [InlineData(5, 68, true)]
    public void Fireball_explodes_on_the_first_monster_and_burns_everyone_in_the_blast(int rank, float damage, bool hitsTwoAndHalf)
    {
        var w = NewWorld();
        var pyro = Unit(w, new Vector2(1, 5), Faction.Player, "Piromante", known: [("pyromancer.fireball", rank)]);
        var first = Monster(w, 5, 5, "Primeiro");
        var behind = Monster(w, 6.5f, 5, "1,5 atrás");
        var side = Monster(w, 5, 7.5f, "2,5 ao lado");
        var far = Monster(w, 9.5f, 5, "4,5 atrás");

        var events = Use(w, pyro, "pyromancer.fireball", first);

        Assert.Equal(damage, Assert.Single(Hits(events, "pyromancer.fireball", first)).Amount);
        Assert.Equal(damage, Assert.Single(Hits(events, "pyromancer.fireball", behind)).Amount);
        Assert.Equal(hitsTwoAndHalf, Lost(side) > 0); // raio 2 no NH1, 3 no NH5
        Assert.Equal(0, Lost(far)); // na linha, mas longe da explosão
        Assert.Equal($"status.burn_{rank}", Tagged(w, first, "burn")!.StatusId);
        Assert.Equal($"status.burn_{rank}", Tagged(w, behind, "burn")!.StatusId);
    }

    [Theory]
    [InlineData(1, 90, false, false)]
    [InlineData(3, 144, true, true)]
    public void Meteor_lands_after_a_2_second_warning_and_at_rank_3_leaves_burning_ground(int rank, float damage, bool reachesFiveAndHalf, bool leavesFlames)
    {
        var w = NewWorld();
        var pyro = Unit(w, new Vector2(2, 10), Faction.Player, "Piromante", known: [("pyromancer.meteor", rank)]);
        var center = Monster(w, 10, 10, "No centro");
        var edge = Monster(w, 15.5f, 10, "A 5,5 tiles");

        var events = Use(w, pyro, "pyromancer.meteor", aim: new Vector2(10, 10));
        Assert.Single(events.OfType<TelegraphStarted>());
        events = Run(w, (2 * Tps) - 2);
        Assert.Equal(0, Lost(center)); // ainda no aviso
        events = Run(w, 3);

        Assert.Equal(damage, Assert.Single(Hits(events, "pyromancer.meteor", center)).Amount);
        Assert.Equal($"status.burn_{rank}", Tagged(w, center, "burn")!.StatusId);
        Assert.Equal(reachesFiveAndHalf, Lost(edge) > 0); // raio 5, e 6 do NH2 em diante
        var flames = events.OfType<AreaSpawned>().SingleOrDefault();
        Assert.Equal(leavesFlames, flames is not null);
        if (flames is not null)
        {
            Assert.Equal("area.meteor_flames", flames.AreaId);
            Ticks(5 * Tps, flames.ExpiresAtTick - w.Tick + 1);
            var later = Run(w, 6 * Tps);
            Assert.Equal(5, Hits(later, "area.meteor_flames", center).Count()); // o Chão em Chamas do NH1 por 5 s
            Assert.All(Hits(later, "area.meteor_flames", center), h => Assert.Equal(5, h.Amount));
        }
    }

    // ---------- Arcanista ----------

    [Theory]
    [InlineData(1, 6, 6, false)]
    [InlineData(5, 10.8, 8, true)]
    public void Arcane_ray_grows_15_percent_each_pulse_and_at_rank_5_lets_the_arcanist_walk(int rank, double firstPulse, int pulses, bool walks)
    {
        var w = NewWorld();
        var arcanist = Unit(w, new Vector2(1, 5), Faction.Player, "Arcanista", known: [("arcanist.arcane_ray", rank)]);
        var target = Monster(w, 7, 5, "Alvo");

        // parado: todos os pulsos, cada um 15% acima do anterior
        var events = Use(w, arcanist, "arcanist.arcane_ray", target);
        events.AddRange(Run(w, 5 * Tps));
        var hits = Hits(events, "arcanist.arcane_ray", target).Select(h => h.Amount).ToList();
        Assert.Equal(pulses, hits.Count);
        for (var i = 0; i < hits.Count; i++)
        {
            Assert.Equal(CoreEffects.Round(firstPulse * Math.Pow(1.15, i)), hits[i], 1.0);
        }

        // andando depois do segundo pulso: corta no NH1, segue (devagar) no NH5
        arcanist.Get<AbilityState>().ReadyAtTick.Clear();
        events = Use(w, arcanist, "arcanist.arcane_ray", target);
        events.AddRange(Run(w, (Tps / 2 * 2) + 1));
        w.Enqueue(new MoveIntent(arcanist.Id, new Vector2(0, 1)));
        events.AddRange(Run(w, 5 * Tps));
        Assert.Equal(walks ? pulses : 2, Hits(events, "arcanist.arcane_ray", target).Count());
        Assert.Equal(!walks, events.OfType<CastInterrupted>().Any(c => c.Entity == arcanist.Id));
    }

    [Theory]
    [InlineData(1, 3, 1)]
    [InlineData(5, 5, 1.8)]
    public void Arcane_mark_stacks_up_to_its_rank_maximum_and_each_stack_deals_damage_every_second(int rank, int maxStacks, double perStack)
    {
        var w = NewWorld();
        var arcanist = Unit(w, new Vector2(1, 5), Faction.Player, "Arcanista", known: [("arcanist.arcane_mark", rank)]);
        var target = Monster(w, 6, 5, "Alvo");

        for (var i = 0; i < maxStacks + 2; i++)
        {
            arcanist.Get<AbilityState>().ReadyAtTick.Clear();
            Use(w, arcanist, "arcanist.arcane_mark", target);
        }

        var mark = Tagged(w, target, "arcane_mark")!;
        Assert.Equal(($"status.arcane_mark_{rank}", maxStacks), (mark.StatusId, mark.Stacks));
        Ticks(10 * Tps, Left(w, mark)); // um uso novo renova todas as pilhas
        var pulse = new List<DamageDealt>();
        for (var i = 0; i <= Tps && pulse.Count == 0; i++)
        {
            pulse = Hits(Run(w), mark.StatusId, target).ToList();
        }

        Assert.Equal(maxStacks, pulse.Count); // um acerto por pilha a cada segundo
        Assert.All(pulse, p => Assert.Equal(CoreEffects.Round(perStack), p.Amount));
    }

    [Theory]
    [InlineData(1, 2, 2)]
    [InlineData(5, 4, 4)]
    public void Arcane_orbs_shoot_the_current_target_every_one_and_a_half_seconds_for_10_seconds(int rank, int orbs, float perShot)
    {
        var w = NewWorld();
        var arcanist = Unit(w, new Vector2(1, 5), Faction.Player, "Arcanista",
            known: [("arcanist.arcane_mark", 1), ("arcanist.arcane_orbs", rank)]);
        var target = Monster(w, 6, 5, "Alvo atual");
        var other = Monster(w, 4, 8, "Outro");

        Use(w, arcanist, "arcanist.arcane_mark", target); // o alvo atual é o da última habilidade em unidade
        Use(w, arcanist, "arcanist.arcane_orbs");
        var events = Run(w, 11 * Tps);

        var shots = Hits(events, $"status.arcane_orbs_{rank}").ToList();
        Assert.Equal(6 * orbs, shots.Count); // disparos aos 1,5 s, 3 s, ..., 9 s
        Assert.All(shots, s => Assert.Equal((target.Id, arcanist.Id, perShot), (s.Target, s.Source, s.Amount)));
        Assert.Equal(0, Lost(other));
    }

    [Theory]
    [InlineData(1, 20, 1.5, 6, 1.0, false)]
    [InlineData(5, 36, 2.7, 10, 0.5, true)]
    public void Arcane_siphon_raises_the_mana_regeneration_for_5_seconds_and_from_rank_3_adds_a_mark(
        int rank, double baseDamage, double intScaling, double regen, double castSeconds, bool marks)
    {
        var w = NewWorld();
        var arcanist = MageAt(w, new Vector2(1, 5), known: [("arcanist.arcane_siphon", rank)]);
        var target = Monster(w, 6, 5, "Alvo");
        var before = arcanist.Get<CharacterStats>().ManaRegen;

        var events = Use(w, arcanist, "arcanist.arcane_siphon", target);
        var start = w.Tick;
        while (!Hits(events, "arcanist.arcane_siphon", target).Any() && w.Tick < start + (2 * Tps))
        {
            events.AddRange(Run(w));
        }

        var hit = Assert.Single(Hits(events, "arcanist.arcane_siphon", target));
        Ticks(castSeconds * Tps, w.Tick - start + 1); // conjuração de 1 s, 0,5 s no NH5
        var expected = CoreEffects.Round(baseDamage + (intScaling * ProfileInt));
        Assert.Equal(expected * (hit.Critical ? w.Content.Rules.Stats.CritMultiplier : 1), hit.Amount, 1.0);

        Assert.Equal(regen, arcanist.Get<CharacterStats>().ManaRegen - before, 0.05); // Mana por segundo, não Mana na hora
        var siphon = StatusManager.Get(w, arcanist, $"status.arcane_siphon_{rank}")!;
        Ticks(5 * Tps, Left(w, siphon));
        Assert.Equal(marks, Tagged(w, target, "arcane_mark") is not null);

        Run(w, (5 * Tps) + 1);
        Assert.Equal(before, arcanist.Get<CharacterStats>().ManaRegen, 0.05);
    }

    [Theory]
    [InlineData(1, 20, false)]
    [InlineData(3, 32, true)]
    public void Void_beam_channels_8_pulses_while_walking_and_at_rank_3_exposes_the_target(int rank, float perPulse, bool exposes)
    {
        var w = NewWorld();
        var arcanist = Unit(w, new Vector2(1, 5), Faction.Player, "Arcanista", known: [("arcanist.void_beam", rank)]);
        var target = Monster(w, 7, 5, "Alvo");

        var events = Use(w, arcanist, "arcanist.void_beam", target);
        events.AddRange(Run(w, Tps));
        w.Enqueue(new MoveIntent(arcanist.Id, new Vector2(0, 1))); // anda 50% mais devagar sem cortar
        events.AddRange(Run(w, 4 * Tps));

        var pulses = Hits(events, "arcanist.void_beam", target).ToList();
        Assert.Equal(8, pulses.Count);
        Assert.All(pulses, p => Assert.Equal(perPulse, p.Amount));
        Assert.DoesNotContain(events.OfType<CastInterrupted>(), c => c.Entity == arcanist.Id);
        var exposed = StatusManager.Get(w, target, "status.void_exposed");
        Assert.Equal(exposes, exposed is not null);
        if (exposed is not null)
        {
            Assert.Equal(1.15, Def(w, exposed).Modifiers.DamageTakenMultiplier, 3);
            Assert.InRange(Left(w, exposed), (5 * Tps) - 2, 6 * Tps);
        }
    }
}
