using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Status;
using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>
/// T-341: mecânicas dos monstros das Ruínas. Cura recebida reduzida (<c>modifiers.healingTakenMultiplier</c>, maldição do
/// Sacerdote Morto), reflexo e quebra de status (<c>reflect</c>, <c>breaks</c>, Escudo Espectral do Cavaleiro), o perfil
/// <c>ai.support</c> (Sacerdote) e o reforço de <c>ai.brute</c> com a tag <c>ai.buff</c> (Cavaleiro).
/// </summary>
public class RuinsMonsterTests
{
    private static readonly StatusDefinition Curse = new()
    {
        Id = "s.curse",
        Name = "Maldição",
        Tags = ["debuff", "curse"],
        DefaultDurationTicks = 100,
        Modifiers = new StatModifiers { HealingTakenMultiplier = 0.5 },
    };

    private static readonly StatusDefinition Mirror = new()
    {
        Id = "s.mirror",
        Name = "Espelho",
        DefaultDurationTicks = 200,
        Reflect = new ReflectDefinition { Fraction = 0.3 },
        Breaks = new StatusBreakDefinition { DamageFraction = 0.1, Interrupt = true },
    };

    private static Entity Unit(World w, Vector2 pos, Faction f, float health = 1000, string[]? known = null)
    {
        var st = new AbilityState();
        st.Known.AddRange(known ?? []);
        return w.Spawn(
            new Transform { Position = pos },
            new Health(health),
            new FactionTag(f),
            new Collider(0.4f),
            new Movement(4),
            new UnitInfo { DefinitionId = "test", Name = f.ToString() },
            st);
    }

    private static List<GameEvent> Run(World w, int ticks)
    {
        var all = new List<GameEvent>();
        for (var i = 0; i < ticks; i++)
        {
            w.Step();
            all.AddRange(w.LastTickEvents);
        }

        return all;
    }

    private static List<GameEvent> Use(World w, Entity e, string ability, Entity? target = null)
    {
        w.Enqueue(new UseAbilityIntent(e.Id, ability, target?.Id, null));
        return Run(w, 1);
    }

    // ---------- cura recebida ----------

    [Fact]
    public void Curse_halves_the_healing_received()
    {
        var heal = new AbilityDefinition
        {
            Id = "hero.heal",
            Name = "x",
            Targeting = new UnitTargeting { Range = 10, Faction = FactionFilter.Ally },
            Effects = [new HealEffect { Base = 40 }],
        };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [heal], statuses: [Curse]));
        var healer = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.heal"]);
        var ally = Unit(w, new Vector2(3, 1), Faction.Player);
        ally.Get<Health>().Current = 500;

        StatusManager.Apply(w, null, ally, "s.curse");
        var ev = Use(w, healer, "hero.heal", ally);

        Assert.Equal(520, ally.Get<Health>().Current);
        Assert.Contains(ev, e => e is HealApplied { Amount: 20 });
        Assert.Equal(0.5, StatusManager.HealingTakenMultiplier(w, ally));
    }

    // ---------- reflexo e quebra ----------

    [Fact]
    public void Reflect_returns_a_fraction_of_direct_damage_to_the_attacker()
    {
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(statuses: [Mirror]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player);
        var knight = Unit(w, new Vector2(2, 1), Faction.Monster, health: 10_000);
        StatusManager.Apply(w, knight, knight, "s.mirror");

        DamageResolver.Deal(w, hero, knight, "hero.strike", 100, DamageSchool.Physical);

        Assert.Equal(9_900, knight.Get<Health>().Current);
        Assert.Equal(970, hero.Get<Health>().Current);
    }

    [Fact]
    public void Damage_from_statuses_is_not_reflected()
    {
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(statuses: [Mirror, Curse]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player);
        var knight = Unit(w, new Vector2(2, 1), Faction.Monster, health: 10_000);
        StatusManager.Apply(w, knight, knight, "s.mirror");

        DamageResolver.Deal(w, hero, knight, "s.curse", 100, DamageSchool.Arcane); // como um veneno do herói

        Assert.Equal(1000, hero.Get<Health>().Current);
    }

    [Fact]
    public void Two_reflecting_units_do_not_bounce_damage_forever()
    {
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(statuses: [Mirror]));
        var a = Unit(w, new Vector2(1, 1), Faction.Player, health: 10_000);
        var b = Unit(w, new Vector2(2, 1), Faction.Monster, health: 10_000);
        StatusManager.Apply(w, a, a, "s.mirror");
        StatusManager.Apply(w, b, b, "s.mirror");

        DamageResolver.Deal(w, a, b, "a.strike", 100, DamageSchool.Physical);

        Assert.Equal(9_900, b.Get<Health>().Current);
        Assert.Equal(9_970, a.Get<Health>().Current); // o reflexo de b não volta para b
    }

    [Fact]
    public void Status_breaks_after_the_holder_takes_its_damage_fraction()
    {
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(statuses: [Mirror]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, health: 10_000);
        var knight = Unit(w, new Vector2(2, 1), Faction.Monster, health: 1000); // quebra com 100 de dano

        StatusManager.Apply(w, knight, knight, "s.mirror");
        DamageResolver.Deal(w, hero, knight, "hero.strike", 60, DamageSchool.Physical);
        Assert.True(StatusManager.Has(w, knight, "s.mirror"));

        DamageResolver.Deal(w, hero, knight, "hero.strike", 40, DamageSchool.Physical);
        Assert.False(StatusManager.Has(w, knight, "s.mirror"));
        Assert.Contains(w.Events.Of<StatusRemoved>(), r => r.StatusId == "s.mirror");

        // depois de quebrar, não reflete mais
        var before = hero.Get<Health>().Current;
        DamageResolver.Deal(w, hero, knight, "hero.strike", 50, DamageSchool.Physical);
        Assert.Equal(before, hero.Get<Health>().Current);
    }

    [Fact]
    public void Renewing_the_status_resets_its_damage_count()
    {
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(statuses: [Mirror]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, health: 10_000);
        var knight = Unit(w, new Vector2(2, 1), Faction.Monster, health: 1000);

        StatusManager.Apply(w, knight, knight, "s.mirror");
        DamageResolver.Deal(w, hero, knight, "hero.strike", 60, DamageSchool.Physical);
        StatusManager.Apply(w, knight, knight, "s.mirror");
        DamageResolver.Deal(w, hero, knight, "hero.strike", 60, DamageSchool.Physical);

        Assert.True(StatusManager.Has(w, knight, "s.mirror"));
    }

    [Fact]
    public void Interrupt_breaks_the_status_even_without_a_cast()
    {
        var kick = new AbilityDefinition
        {
            Id = "hero.kick",
            Name = "x",
            Targeting = new UnitTargeting { Range = 3, Faction = FactionFilter.Enemy },
            Effects = [new InterruptEffect()],
        };
        var w = WorldBuilder.CreateDefault(1, null, new ContentCatalog(abilities: [kick], statuses: [Mirror, Curse]));
        var hero = Unit(w, new Vector2(1, 1), Faction.Player, known: ["hero.kick"]);
        var knight = Unit(w, new Vector2(2, 1), Faction.Monster);
        StatusManager.Apply(w, knight, knight, "s.mirror");
        StatusManager.Apply(w, null, knight, "s.curse");

        Use(w, hero, "hero.kick", knight);

        Assert.False(StatusManager.Has(w, knight, "s.mirror"));
        Assert.True(StatusManager.Has(w, knight, "s.curse")); // sem breaks.interrupt: fica
    }

    // ---------- dados reais ----------

    [Fact]
    public void Ruins_monsters_are_undead_with_the_lpc_sprite_radius()
    {
        var c = RealContent.Value;
        var priest = c.Monsters["monster.dead_priest"];
        Assert.Equal(10, priest.Level);
        Assert.Contains("undead", priest.Tags);
        Assert.InRange(priest.Radius, 0.45, 0.50);
        Assert.Equal("ai.support", priest.Ai.Id);

        var knight = c.Monsters["monster.spectral_knight"];
        Assert.Equal(11, knight.Level);
        Assert.Equal(MonsterRank.Elite, knight.Rank);
        Assert.Contains("undead", knight.Tags);
        Assert.InRange(knight.Radius, 0.45, 0.50);

        var shield = c.Statuses["status.spectral_shield"];
        Assert.True(shield.Reflect!.Fraction > 0);
        Assert.True(shield.Breaks is { Interrupt: true, DamageFraction: > 0 });
        Assert.True(c.Statuses["status.withering_curse"].Modifiers.HealingTakenMultiplier < 1);
        Assert.Contains("curse", c.Statuses["status.withering_curse"].Tags); // a Purificação do Clérigo remove
    }

    // ---------- ai.support (Sacerdote Morto) ----------

    private static (World W, Entity Priest, Entity[] Guards, Entity Hero) Courtyard()
    {
        var c = RealContent.Value;
        var w = WorldBuilder.CreateDefault(1, null, c);
        var guards = new[]
        {
            EntityFactory.SpawnMonster(w, c.Monsters["monster.skeleton_guard"], new Vector2(10, 9), 9),
            EntityFactory.SpawnMonster(w, c.Monsters["monster.skeleton_guard"], new Vector2(10, 11), 9),
        };
        var priest = EntityFactory.SpawnMonster(w, c.Monsters["monster.dead_priest"], new Vector2(13, 10), 10);
        var hero = Unit(w, new Vector2(6, 10), Faction.Player, health: 100_000);
        return (w, priest, guards, hero);
    }

    [Fact]
    public void Dead_priest_casts_its_heal_on_the_most_injured_ally_and_not_on_itself()
    {
        var (w, priest, guards, _) = Courtyard();
        priest.Get<Health>().Current = priest.Get<Health>().Max * 0.2f;
        guards[0].Get<Health>().Current = guards[0].Get<Health>().Max * 0.6f;
        guards[1].Get<Health>().Current = guards[1].Get<Health>().Max * 0.3f;

        var ev = Run(w, 3);

        var started = Assert.Single(ev.OfType<AbilityStarted>(), a => a.AbilityId == "dead_priest.mend_dead");
        Assert.Equal(guards[1].Id, started.Target);
        Assert.Equal(40, started.CastTicks); // 2 s: a janela para interromper
    }

    [Fact]
    public void Dead_priest_does_not_heal_allies_above_the_threshold()
    {
        var (w, _, guards, _) = Courtyard();
        guards[0].Get<Health>().Current = guards[0].Get<Health>().Max * 0.9f;

        var ev = Run(w, 20);

        Assert.DoesNotContain(ev, e => e is AbilityStarted { AbilityId: "dead_priest.mend_dead" });
        Assert.Contains(ev, e => e is AbilityStarted { AbilityId: "dead_priest.withering_curse" or "dead_priest.grave_bolt" });
    }

    [Fact]
    public void Dead_priest_heal_is_interrupted_by_the_warrior_heavy_strike()
    {
        var c = RealContent.Value;
        var (w, priest, guards, _) = Courtyard();
        var warrior = EntityFactory.SpawnPlayer(w, c.Classes["class.warrior"], new Vector2(14, 10));
        warrior.Get<Health>().Max = 100_000;
        warrior.Get<Health>().Current = 100_000;
        var hurt = guards[1].Get<Health>();
        hurt.Current = hurt.Max * 0.3f;
        var before = hurt.Current;

        var ev = Run(w, 3);
        Assert.Contains(ev, e => e is AbilityStarted { AbilityId: "dead_priest.mend_dead" });
        warrior.Get<Transform>().Facing = new Vector2(-1, 0);

        ev = Use(w, warrior, "warrior.heavy_strike", priest);
        ev.AddRange(Run(w, 40));

        Assert.Contains(ev, e => e is CastInterrupted { AbilityId: "dead_priest.mend_dead", Reason: CastInterruptReason.Interrupted });
        Assert.DoesNotContain(ev, e => e is HealApplied { SourceId: "dead_priest.mend_dead" });
        Assert.True(hurt.Current <= before);
    }

    [Fact]
    public void Dead_priest_stays_behind_its_guards()
    {
        var (w, priest, guards, hero) = Courtyard();
        hero.Get<Transform>().Position = new Vector2(4, 10);

        for (var t = 0; t < 4; t++)
        {
            Run(w, 20);
            var heroPos = hero.Get<Transform>().Position;
            var priestDistance = Vector2.Distance(priest.Get<Transform>().Position, heroPos);
            var guardDistance = guards.Max(g => Vector2.Distance(g.Get<Transform>().Position, heroPos));
            Assert.True(priestDistance > guardDistance + 1.5f, $"t={w.Tick}: sacerdote a {priestDistance:F1}, guardas a até {guardDistance:F1}");
        }
    }

    // ---------- ai.brute + ai.buff (Cavaleiro Espectral) ----------

    [Fact]
    public void Spectral_knight_raises_its_shield_up_close_and_charges_from_afar()
    {
        var c = RealContent.Value;
        var w = WorldBuilder.CreateDefault(1, null, c);
        var knight = EntityFactory.SpawnMonster(w, c.Monsters["monster.spectral_knight"], new Vector2(10, 10), 11);
        var hero = Unit(w, new Vector2(16, 10), Faction.Player, health: 100_000); // a 6 tiles: investida (8)

        var ev = Run(w, 3);
        var charge = Assert.Single(ev.OfType<TelegraphStarted>(), t => t.AbilityId == "spectral_knight.spectral_charge");
        Assert.Equal(20, charge.ResolveAtTick - charge.Tick); // 1 s de aviso
        Assert.False(StatusManager.Has(w, knight, "status.spectral_shield"));

        Run(w, 25);
        hero.Get<Transform>().Position = knight.Get<Transform>().Position + new Vector2(1.5f, 0);
        ev = Run(w, 3);

        Assert.True(StatusManager.Has(w, knight, "status.spectral_shield"));
    }
}
