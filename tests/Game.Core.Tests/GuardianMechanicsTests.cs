using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Ai;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Status;
using Game.Core.Combat.Threat;
using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-406: mecânicas novas do Guardião — desvio de dano (Interceptação, Juramento), avanço até um aliado, imunidade a
/// empurrão (Bastião NH5) e ameaça multiplicada pelo dano (Golpe de Escudo). Dados de teste, não as habilidades reais (T-408).
/// </summary>
public class GuardianMechanicsTests
{
    // ---------- dados de teste ----------

    private static readonly StatusDefinition Intercept = new()
    {
        Id = "status.test_intercept",
        Name = "Interceptado",
        DefaultDurationTicks = 80,
        Redirect = new RedirectDefinition { Fraction = 0.3, MaxDistance = 10 },
    };

    /// <summary>Como o Juramento: desvia 50% e o protetor recebe a parte desviada 40% menor.</summary>
    private static readonly StatusDefinition Oath = new()
    {
        Id = "status.test_oath",
        Name = "Juramento",
        DefaultDurationTicks = 160,
        Redirect = new RedirectDefinition { Fraction = 0.5, Reduction = 0.4, MaxDistance = 6 },
    };

    private static readonly StatusDefinition Steadfast = new()
    {
        Id = "status.test_steadfast",
        Name = "Inamovível",
        DefaultDurationTicks = 160,
        Flags = new StatusFlags { KnockbackImmune = true },
    };

    private static readonly AbilityDefinition Interception = new()
    {
        Id = "guardian.test_interception",
        Name = "Interceptação",
        Targeting = new UnitTargeting { Range = 8, Faction = FactionFilter.Ally },
        Effects =
        [
            new DisplacementEffect { Kind = DisplacementKind.Dash, Distance = 8, Direction = DisplacementDirection.TowardTarget },
            new ApplyStatusEffect { Status = Intercept.Id },
        ],
    };

    private static readonly AbilityDefinition Shove = new()
    {
        Id = "test.shove",
        Name = "Empurrão",
        Targeting = new UnitTargeting { Range = 5, Faction = FactionFilter.Any },
        Effects = [new DisplacementEffect { Kind = DisplacementKind.Knockback, Distance = 3, Direction = DisplacementDirection.AwayFromSource }],
    };

    private static readonly AbilityDefinition ShieldBash = new()
    {
        Id = "guardian.test_shield_bash",
        Name = "Golpe de Escudo",
        Targeting = new UnitTargeting { Range = 30 },
        Effects = [new DamageEffect { Base = 50, ThreatMultiplier = 3 }],
    };

    private static readonly AbilityDefinition Strike = new()
    {
        Id = "test.strike",
        Name = "Golpe",
        Targeting = new UnitTargeting { Range = 30 },
        Effects = [new DamageEffect { Base = 50 }],
    };

    private static readonly MonsterDefinition Dummy = new()
    {
        Id = "monster.test_dummy",
        Name = "Boneco",
        Health = 100000,
        Speed = 0,
        Radius = 0.4,
        Ai = new AiProfileDefinition { Id = "ai.melee_chaser" },
    };

    private static World NewWorld() => WorldBuilder.CreateDefault(
        7,
        null,
        new ContentCatalog(
            abilities: [Interception, Shove, ShieldBash, Strike],
            statuses: [Intercept, Oath, Steadfast],
            monsters: [Dummy]));

    private static Entity Unit(World w, Vector2 pos, Faction f, string name, float hp = 1000, double armor = 0, params string[] known)
    {
        var st = new AbilityState();
        st.Known.AddRange(known);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(hp),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = name, Armor = armor },
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

    private static ulong Hash(IComponent c)
    {
        var h = new StateHasher();
        c.WriteHash(h);
        return h.Value;
    }

    private static float Lost(Entity e) => e.Get<Health>().Max - e.Get<Health>().Current;

    // ---------- desvio de dano ----------

    [Fact]
    public void Ally_with_30_percent_redirect_takes_70_and_protector_takes_30_after_defense_and_reduction()
    {
        var reduced = Intercept with { Redirect = Intercept.Redirect! with { Reduction = 0.2 } };
        var w = WorldBuilder.CreateDefault(7, null, new ContentCatalog(statuses: [reduced]));
        var guardian = Unit(w, new Vector2(2, 1), Faction.Player, "Guardião", armor: 40);
        var ally = Unit(w, new Vector2(1, 1), Faction.Player, "Maga");
        var wolf = Unit(w, new Vector2(1, 2), Faction.Monster, "Lobo");
        StatusManager.Apply(w, guardian, ally, reduced.Id);

        var allyAmount = DamageResolver.Deal(w, wolf, ally, "test.bite", 100, DamageSchool.Physical);

        // aliado: 70% do dano, sem a defesa do Guardião
        Assert.Equal(70, allyAmount);
        Assert.Equal(70, Lost(ally));

        // Guardião: 30% × (1 − 0,2) × (1 − defesa dele)
        var defense = Combat.Stats.StatMath.DefenseReduction(w, guardian, wolf, DamageSchool.Physical);
        Assert.True(defense > 0.4);
        var expected = CoreEffects.Round(100 * 0.3 * 0.8 * (1 - defense));
        Assert.Equal(expected, Lost(guardian));
        Assert.Equal(70 + expected, Lost(ally) + Lost(guardian)); // o total bate com a conta

        var events = w.Events.Current;
        var redirected = Assert.Single(events.OfType<DamageRedirected>());
        Assert.Equal((wolf.Id, ally.Id, guardian.Id, "test.bite", reduced.Id, expected),
            (redirected.Attacker, redirected.Target, redirected.Protector, redirected.SourceId, redirected.StatusId, redirected.Amount));
        var onGuardian = Assert.Single(events.OfType<DamageDealt>(), d => d.Target == guardian.Id);
        Assert.Equal(wolf.Id, onGuardian.Source);
        Assert.Equal(expected, onGuardian.Amount);
    }

    [Fact]
    public void Redirected_part_skips_the_allys_modifiers_and_uses_the_protectors()
    {
        var vulnerable = new StatusDefinition
        {
            Id = "status.test_vulnerable",
            Name = "Vulnerável",
            DefaultDurationTicks = 100,
            Modifiers = new StatModifiers { DamageTakenMultiplier = 2 },
        };
        var bastion = new StatusDefinition
        {
            Id = "status.test_bastion",
            Name = "Bastião",
            DefaultDurationTicks = 100,
            Modifiers = new StatModifiers { DamageTakenMultiplier = 0.5 },
        };
        var w = WorldBuilder.CreateDefault(7, null, new ContentCatalog(statuses: [Intercept, vulnerable, bastion]));
        var guardian = Unit(w, new Vector2(2, 1), Faction.Player, "Guardião");
        var ally = Unit(w, new Vector2(1, 1), Faction.Player, "Maga");
        StatusManager.Apply(w, guardian, ally, Intercept.Id);
        StatusManager.Apply(w, null, ally, vulnerable.Id);
        StatusManager.Apply(w, null, guardian, bastion.Id);

        DamageResolver.Deal(w, null, ally, "test.trap", 100, DamageSchool.Physical);

        Assert.Equal(140, Lost(ally)); // 70 × 2
        Assert.Equal(15, Lost(guardian)); // 30 × 0,5
    }

    [Fact]
    public void Allys_shield_does_not_absorb_the_redirected_part()
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(2, 1), Faction.Player, "Guardião");
        var ally = Unit(w, new Vector2(1, 1), Faction.Player, "Maga");
        var shields = new ShieldState();
        shields.Shields.Add(new ShieldInstance { Id = 1, Remaining = 1000, ExpiresAtTick = 10_000 });
        ally.Add(shields);
        StatusManager.Apply(w, guardian, ally, Intercept.Id);

        DamageResolver.Deal(w, null, ally, "test.trap", 100, DamageSchool.Physical);

        Assert.Equal(0, Lost(ally));
        Assert.Equal(930, shields.Total); // só os 70 do aliado
        Assert.Equal(30, Lost(guardian));
    }

    [Fact]
    public void Protectors_in_a_chain_or_loop_get_only_one_hop()
    {
        var w = NewWorld();
        var a = Unit(w, new Vector2(1, 1), Faction.Player, "A");
        var b = Unit(w, new Vector2(2, 1), Faction.Player, "B");
        var c = Unit(w, new Vector2(3, 1), Faction.Player, "C");
        StatusManager.Apply(w, b, a, Intercept.Id); // A → B
        StatusManager.Apply(w, c, b, Intercept.Id); // B → C
        StatusManager.Apply(w, a, c, Intercept.Id); // C → A (laço)

        DamageResolver.Deal(w, null, a, "test.trap", 100, DamageSchool.Physical);

        Assert.Equal(70, Lost(a));
        Assert.Equal(30, Lost(b)); // não passa adiante para C
        Assert.Equal(0, Lost(c));
        Assert.Single(w.Events.Current.OfType<DamageRedirected>());
    }

    [Fact]
    public void Dead_or_distant_protector_gets_nothing()
    {
        var w = NewWorld();
        var dead = Unit(w, new Vector2(2, 1), Faction.Player, "Caído");
        var far = Unit(w, new Vector2(30, 1), Faction.Player, "Longe");
        var a = Unit(w, new Vector2(1, 1), Faction.Player, "A");
        var b = Unit(w, new Vector2(1, 3), Faction.Player, "B");
        StatusManager.Apply(w, dead, a, Intercept.Id);
        StatusManager.Apply(w, far, b, Intercept.Id);
        w.Kill(dead);

        DamageResolver.Deal(w, null, a, "test.trap", 100, DamageSchool.Physical);
        DamageResolver.Deal(w, null, b, "test.trap", 100, DamageSchool.Physical);

        Assert.Equal(100, Lost(a));
        Assert.Equal(100, Lost(b));
        Assert.Equal(0, Lost(far));
        Assert.Empty(w.Events.Current.OfType<DamageRedirected>());
    }

    [Fact]
    public void Several_redirects_add_up_to_at_most_all_the_damage()
    {
        var big = new StatusDefinition
        {
            Id = "status.test_big",
            Name = "Grande",
            DefaultDurationTicks = 100,
            Redirect = new RedirectDefinition { Fraction = 0.9, MaxDistance = 10 },
        };
        var w = WorldBuilder.CreateDefault(7, null, new ContentCatalog(statuses: [Intercept, big]));
        var g1 = Unit(w, new Vector2(2, 1), Faction.Player, "G1");
        var g2 = Unit(w, new Vector2(1, 2), Faction.Player, "G2");
        var ally = Unit(w, new Vector2(1, 1), Faction.Player, "Maga");
        StatusManager.Apply(w, g1, ally, big.Id);
        StatusManager.Apply(w, g2, ally, Intercept.Id);

        DamageResolver.Deal(w, null, ally, "test.trap", 100, DamageSchool.Physical);

        Assert.Equal(0, Lost(ally));
        Assert.Equal(90, Lost(g1));
        Assert.Equal(10, Lost(g2)); // só o que sobrou
    }

    [Fact]
    public void Redirect_can_point_to_another_entity_chosen_by_the_effect()
    {
        var w = NewWorld();
        var priest = Unit(w, new Vector2(3, 1), Faction.Player, "Sacerdote");
        var tank = Unit(w, new Vector2(2, 1), Faction.Player, "Tanque");
        var ally = Unit(w, new Vector2(1, 1), Faction.Player, "Maga");
        var inst = StatusManager.Apply(w, priest, ally, Intercept.Id, redirectTo: tank.Id)!;
        Assert.Equal(tank.Id, inst.Protector);

        DamageResolver.Deal(w, null, ally, "test.trap", 100, DamageSchool.Physical);

        Assert.Equal(70, Lost(ally));
        Assert.Equal(30, Lost(tank));
        Assert.Equal(0, Lost(priest));
    }

    [Fact]
    public void Oath_reduction_applies_to_redirected_damage_and_snapshot_keeps_the_link()
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(2, 1), Faction.Player, "Guardião");
        var priest = Unit(w, new Vector2(3, 1), Faction.Player, "Sacerdote");
        var ally = Unit(w, new Vector2(1, 1), Faction.Player, "Maga");
        StatusManager.Apply(w, guardian, ally, Oath.Id, redirectTo: priest.Id);
        var copy = (StatusEffects)ally.Get<StatusEffects>().Clone();
        Assert.Equal(priest.Id, copy.Active[0].RedirectTo); // o vínculo vai na cópia (e no hash)
        var hash = Hash(ally.Get<StatusEffects>());
        StatusManager.Apply(w, guardian, ally, Oath.Id); // renovar sem redirectTo volta para quem aplicou
        Assert.NotEqual(hash, Hash(ally.Get<StatusEffects>()));
        Assert.Equal(guardian.Id, StatusManager.Get(w, ally, Oath.Id)!.Protector);

        DamageResolver.Deal(w, null, ally, "test.trap", 100, DamageSchool.Physical);

        Assert.Equal(50, Lost(ally));
        Assert.Equal(30, Lost(guardian)); // 50 × (1 − 0,4)
        Assert.Equal(0, Lost(priest));
    }

    [Fact]
    public void Redirect_is_shown_in_the_combat_log()
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(2, 1), Faction.Player, "Guardião");
        var ally = Unit(w, new Vector2(1, 1), Faction.Player, "Maga");
        var wolf = Unit(w, new Vector2(1, 2), Faction.Monster, "Lobo", known: [Strike.Id]);
        var log = new CombatLogRecorder(viewer: EntityId.None);
        StatusManager.Apply(w, guardian, ally, Intercept.Id);
        Run(w);
        log.Record(w);

        w.Enqueue(new UseAbilityIntent(wolf.Id, Strike.Id, ally.Id, null));
        Run(w);
        log.Record(w);

        var texts = log.Entries.Select(e => e.Text).ToList();
        Assert.Contains("Lobo usou Golpe em Maga: 35 de dano", texts);
        Assert.Contains("Lobo usou Golpe em Guardião: 15 de dano", texts);
        Assert.Contains("Guardião protegeu Maga com Interceptado: 15 de dano desviado", texts);
    }

    // ---------- avançar até um aliado ----------

    [Fact]
    public void Test_interception_dashes_the_guardian_to_the_ally_and_protects_them()
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(1, 1), Faction.Player, "Guardião", known: [Interception.Id]);
        var ally = Unit(w, new Vector2(6, 1), Faction.Player, "Maga");

        w.Enqueue(new UseAbilityIntent(guardian.Id, Interception.Id, ally.Id, null));
        var ev = Run(w);

        // para no contato (raios 0,4 + 0,4), sem passar do aliado mesmo com distância 8
        var pos = guardian.Get<Transform>().Position;
        Assert.Equal(5.2f, pos.X, 3);
        Assert.Equal(1f, pos.Y, 3);
        var dash = Assert.Single(ev.OfType<Displaced>());
        Assert.Equal(guardian.Id, dash.Entity);
        Assert.Equal(4.2f, dash.Distance, 3);
        Assert.Equal(guardian.Id, StatusManager.Get(w, ally, Intercept.Id)!.Protector);

        DamageResolver.Deal(w, null, ally, "test.trap", 100, DamageSchool.Physical);
        Assert.Equal(30, Lost(guardian));
    }

    [Fact]
    public void Dash_toward_a_far_target_still_goes_only_the_dash_distance()
    {
        var far = Interception with
        {
            Effects = [new DisplacementEffect { Kind = DisplacementKind.Dash, Distance = 3, Direction = DisplacementDirection.TowardTarget }],
        };
        var w = WorldBuilder.CreateDefault(7, null, new ContentCatalog(abilities: [far]));
        var guardian = Unit(w, new Vector2(1, 1), Faction.Player, "Guardião", known: [far.Id]);
        var ally = Unit(w, new Vector2(1, 8.5f), Faction.Player, "Maga");

        w.Enqueue(new UseAbilityIntent(guardian.Id, far.Id, ally.Id, null));
        Run(w);

        Assert.Equal(4f, guardian.Get<Transform>().Position.Y, 3);
    }

    // ---------- imunidade a empurrão ----------

    [Theory]
    [InlineData(false, 3f)]
    [InlineData(true, 0f)]
    public void Knockback_from_a_monster_moves_the_target_only_without_immunity(bool immune, float expectedMove)
    {
        var w = NewWorld();
        var hero = Unit(w, new Vector2(3, 1), Faction.Player, "Guardião");
        var ogre = Unit(w, new Vector2(1, 1), Faction.Monster, "Ogro", known: [Shove.Id]);
        if (immune)
        {
            StatusManager.Apply(w, hero, hero, Steadfast.Id);
        }

        w.Enqueue(new UseAbilityIntent(ogre.Id, Shove.Id, hero.Id, null));
        var ev = Run(w);

        Assert.Equal(3f + expectedMove, hero.Get<Transform>().Position.X, 3);
        Assert.Equal(immune ? 0 : 1, ev.OfType<Displaced>().Count());
    }

    [Fact]
    public void Knockback_immunity_does_not_block_an_allys_knockback()
    {
        var w = NewWorld();
        var hero = Unit(w, new Vector2(3, 1), Faction.Player, "Guardião");
        var friend = Unit(w, new Vector2(1, 1), Faction.Player, "Amigo", known: [Shove.Id]);
        StatusManager.Apply(w, hero, hero, Steadfast.Id);

        w.Enqueue(new UseAbilityIntent(friend.Id, Shove.Id, hero.Id, null));
        Run(w);

        Assert.Equal(6f, hero.Get<Transform>().Position.X, 3);
    }

    // ---------- ameaça multiplicada pelo dano ----------

    [Fact]
    public void Shield_bash_with_threat_x3_generates_three_times_the_damage_threat()
    {
        var w = NewWorld();
        var guardian = Unit(w, new Vector2(10, 10), Faction.Player, "Guardião", known: [ShieldBash.Id]);
        var dps = Unit(w, new Vector2(10, 12), Faction.Player, "Maga", known: [Strike.Id]);
        var dummy = EntityFactory.SpawnMonster(w, Dummy, new Vector2(30, 30));

        w.Enqueue(new UseAbilityIntent(guardian.Id, ShieldBash.Id, dummy.Id, null));
        w.Enqueue(new UseAbilityIntent(dps.Id, Strike.Id, dummy.Id, null));
        var ev = Run(w);

        var table = dummy.Get<ThreatTable>();
        var bash = Assert.Single(ev.OfType<DamageDealt>(), d => d.Source == guardian.Id);
        var strike = Assert.Single(ev.OfType<DamageDealt>(), d => d.Source == dps.Id);
        Assert.Equal(bash.Amount, strike.Amount);
        Assert.Equal(bash.Amount * 3, table.Entries[guardian.Id.Value]);
        Assert.Equal(strike.Amount * w.Content.Threat.DamageMultiplier, table.Entries[dps.Id.Value]);
        Assert.Equal(guardian.Id, dummy.Get<AiState>().Target);
    }
}
