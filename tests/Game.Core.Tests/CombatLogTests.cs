using System.Numerics;
using Game.Core.Combat;
using Game.Core.CombatLog;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.Tests;

public class CombatLogTests
{
    private sealed record TestPing(int Value) : GameEvent;

    // ---------- helpers ----------

    private static AbilityDefinition Ab(string id, string name) =>
        new() { Id = id, Name = name, Targeting = new SelfTargeting() };

    private static ContentCatalog Content() => new(
        abilities:
        [
            Ab("wolf.bite", "Mordida"),
            Ab("warrior.heavy_strike", "Golpe Pesado"),
            Ab("warrior.shove", "Empurrão"),
            Ab("mage.bolt", "Raio de Gelo"),
            Ab("priest.ward", "Proteção"),
            Ab("warrior.second_wind", "Fôlego"),
        ],
        statuses:
        [
            new StatusDefinition { Id = "status.bleed", Name = "Sangramento", DefaultDurationTicks = 40 },
        ]);

    private sealed class Scene
    {
        public required World World { get; init; }
        public required CombatLogRecorder Log { get; init; }
        public required Entity Player { get; init; }
        public required Entity Wolf { get; init; }
        public required Entity Priest { get; init; }
        public required Entity Mage { get; init; }
    }

    private static Scene NewScene()
    {
        var world = WorldBuilder.CreateDefault(1, null, Content());
        var player = world.Spawn(new UnitInfo { DefinitionId = "class.warrior", Name = "Guerreiro" });
        var wolf = world.Spawn(new UnitInfo { DefinitionId = "monster.grey_wolf", Name = "Lobo Cinzento" });
        var priest = world.Spawn(new UnitInfo { DefinitionId = "class.cleric", Name = "Clérigo" });
        var mage = world.Spawn(new UnitInfo { DefinitionId = "class.mage", Name = "Mago" });
        return new Scene
        {
            World = world,
            Log = new CombatLogRecorder(viewer: player.Id),
            Player = player,
            Wolf = wolf,
            Priest = priest,
            Mage = mage,
        };
    }

    /// <summary>Publica eventos sintéticos no tick seguinte, avança um tick e registra no log.</summary>
    private static IReadOnlyList<DeathSummary> Tick(Scene s, params GameEvent[] events)
    {
        foreach (var e in events)
        {
            s.World.Events.Publish(e);
        }

        s.World.Step();
        return s.Log.Record(s.World);
    }

    private static string LastText(Scene s) => s.Log.Entries[^1].Text;

    // ---------- formatos ----------

    [Fact]
    public void Damage_with_absorb_names_source_ability_and_target()
    {
        var s = NewScene();
        Tick(s, new DamageDealt(s.Wolf.Id, s.Player.Id, "wolf.bite", DamageSchool.Physical, 12, 4, 8, 0, false));

        Assert.Equal("Lobo Cinzento usou Mordida em Você: 12 de dano (4 absorvidos)", LastText(s));
        var entry = s.Log.Entries[^1];
        Assert.Equal(LogCategory.Dano, entry.Category);
        Assert.Equal(new[] { s.Wolf.Id, s.Player.Id }, entry.Involved);
    }

    [Fact]
    public void Damage_without_absorb_has_no_parenthesis()
    {
        var s = NewScene();
        Tick(s, new DamageDealt(s.Wolf.Id, s.Player.Id, "wolf.bite", DamageSchool.Physical, 7, 0, 7, 0, false));

        Assert.Equal("Lobo Cinzento usou Mordida em Você: 7 de dano", LastText(s));
    }

    [Fact]
    public void Damage_from_non_ability_source_uses_causou_dano()
    {
        var s = NewScene();
        Tick(s, new DamageDealt(s.Wolf.Id, s.Player.Id, "status.bleed", DamageSchool.Physical, 3, 0, 3, 0, false));

        Assert.Equal("Lobo Cinzento causou dano em Você: 3 de dano", LastText(s));
    }

    [Fact]
    public void Damage_without_source_uses_content_name()
    {
        var s = NewScene();
        Tick(s, new DamageDealt(EntityId.None, s.Player.Id, "status.bleed", DamageSchool.Physical, 2, 0, 2, 0, false));

        Assert.Equal("Sangramento causou dano em Você: 2 de dano", LastText(s));
    }

    [Fact]
    public void Heal_shows_effective_and_overheal()
    {
        var s = NewScene();
        Tick(s, new HealApplied(s.Priest.Id, s.Player.Id, "warrior.second_wind", 10, 8, 2));

        Assert.Equal("Clérigo curou Você em 8 de vida (2 excedentes)", LastText(s));
        Assert.Equal(LogCategory.Cura, s.Log.Entries[^1].Category);
    }

    [Fact]
    public void Shield_applied_shows_duration_in_seconds()
    {
        var s = NewScene();
        // ExpiresAtTick é absoluto; o evento é carimbado com world.Tick + 1 ao publicar.
        var expires = s.World.Tick + 1 + 40;
        Tick(s, new ShieldApplied(s.Priest.Id, s.Player.Id, "priest.ward", 1, 6, expires));

        Assert.Equal("Clérigo concedeu escudo de 6 a Você por 2,0 s", LastText(s));
        Assert.Equal(LogCategory.Status, s.Log.Entries[^1].Category);
    }

    [Fact]
    public void Shield_expired_text_depends_on_reason()
    {
        var s = NewScene();
        Tick(s, new ShieldExpired(s.Player.Id, 1, 0, ShieldEndReason.Depleted));
        Assert.Equal("O escudo de Você foi consumido", LastText(s));

        Tick(s, new ShieldExpired(s.Player.Id, 2, 2.5f, ShieldEndReason.Expired));
        Assert.Equal("O escudo de Você expirou com 2,5 de absorção restante", LastText(s));
    }

    [Fact]
    public void Rejected_uses_reason_in_portuguese()
    {
        var s = NewScene();
        Tick(s, new AbilityRejected(s.Player.Id, "warrior.heavy_strike", AbilityRejectReason.OnCooldown));

        Assert.Equal("Você não pode usar Golpe Pesado: em recarga", LastText(s));
        Assert.Equal(LogCategory.Habilidade, s.Log.Entries[^1].Category);
    }

    [Fact]
    public void Cast_start_with_time_is_logged_and_instant_start_is_suppressed()
    {
        var s = NewScene();
        Tick(s, new AbilityStarted(s.Player.Id, "warrior.heavy_strike", 50, 0, s.Wolf.Id, null));
        Assert.Equal("Você começou a conjurar Golpe Pesado (2,5 s)", LastText(s));

        var before = s.Log.Entries.Count;
        Tick(
            s,
            new AbilityStarted(s.Player.Id, "warrior.heavy_strike", 0, 0, s.Wolf.Id, null),
            new AbilityExecuted(s.Player.Id, "warrior.heavy_strike", [s.Wolf.Id], null, 0, false, 0));
        Assert.Equal(before, s.Log.Entries.Count);
    }

    [Fact]
    public void Interrupt_names_interrupter()
    {
        var s = NewScene();
        Tick(s, new CastInterrupted(s.Wolf.Id, "wolf.bite", CastInterruptReason.Interrupted, s.Player.Id));

        Assert.Equal("Lobo Cinzento teve Mordida interrompida por Você", LastText(s));
    }

    [Fact]
    public void Knockback_shows_distance_and_source()
    {
        var s = NewScene();
        Tick(s, new Displaced(s.Player.Id, s.Wolf.Id, "warrior.shove", DisplacementKind.Knockback,
            Vector2.Zero, new Vector2(2.5f, 0), 2.5f));

        Assert.Equal("Lobo Cinzento foi empurrado 2,5 tiles por Empurrão", LastText(s));
    }

    [Fact]
    public void Death_line_is_morte_category()
    {
        var s = NewScene();
        Tick(s, new EntityDied(s.Wolf.Id));

        Assert.Equal("Lobo Cinzento foi derrotado", LastText(s));
        Assert.Equal(LogCategory.Morte, s.Log.Entries[^1].Category);
    }

    [Fact]
    public void Events_without_formatter_are_reported_as_unformatted()
    {
        var s = NewScene();
        Tick(s);

        Assert.Contains(typeof(EntitySpawned), s.Log.UnformattedTypes);
        Assert.Empty(s.Log.Entries);
    }

    [Fact]
    public void Record_is_idempotent_for_the_same_tick()
    {
        var s = NewScene();
        var s1 = Tick(s, new EntityDied(s.Wolf.Id));
        var count = s.Log.Entries.Count;

        s.Log.Record(s.World);

        Assert.Equal(count, s.Log.Entries.Count);
        Assert.Single(s1);
    }

    [Fact]
    public void Capacity_drops_oldest_entries()
    {
        var s = NewScene();
        var log = new CombatLogRecorder(viewer: s.Player.Id, capacity: 2);
        s.World.Events.Publish(new EntityDied(s.Wolf.Id));
        s.World.Step();
        log.Record(s.World);
        s.World.Events.Publish(new EntityDied(s.Priest.Id));
        s.World.Events.Publish(new EntityDied(s.Mage.Id));
        s.World.Step();
        log.Record(s.World);

        Assert.Equal(2, log.Entries.Count);
        Assert.Equal("Clérigo foi derrotado", log.Entries[0].Text);
        Assert.Equal("Mago foi derrotado", log.Entries[1].Text);
    }

    // ---------- resumo de morte ----------

    [Fact]
    public void Death_summary_aggregates_damage_by_source_within_last_entries_and_window()
    {
        var s = NewScene();
        // Dano antigo (fora da janela de 5 s): não pode aparecer no resumo.
        Tick(s, new DamageDealt(s.Wolf.Id, s.Player.Id, "wolf.bite", DamageSchool.Physical, 50, 0, 50, 0, false));
        for (var i = 0; i < 120; i++)
        {
            s.World.Step();
            s.Log.Record(s.World);
        }

        var events = new List<GameEvent>();
        // 6 mordidas de 5 (1 absorvido cada) e 6 raios de 2; só as 9 últimas linhas de dano entram.
        for (var i = 0; i < 6; i++)
        {
            events.Add(new DamageDealt(s.Wolf.Id, s.Player.Id, "wolf.bite", DamageSchool.Physical, 5, 1, 4, 0, false));
        }

        for (var i = 0; i < 6; i++)
        {
            events.Add(new DamageDealt(s.Mage.Id, s.Player.Id, "mage.bolt", DamageSchool.Frost, 2, 0, 2, 0, false));
        }

        events.Add(new EntityDied(s.Player.Id));
        var deaths = Tick(s, events.ToArray());

        var death = Assert.Single(deaths);
        Assert.Equal(s.Player.Id, death.Victim);
        Assert.Equal(10, death.Entries.Count);
        Assert.Equal(LogCategory.Morte, death.Entries[^1].Category);

        Assert.Collection(
            death.DamageBySources,
            wolf =>
            {
                Assert.Equal(s.Wolf.Id, wolf.Source);
                Assert.Equal("Lobo Cinzento", wolf.SourceName);
                Assert.Equal(15, wolf.Amount);
                Assert.Equal(3, wolf.Absorbed);
                Assert.Equal(3, wolf.Hits);
            },
            mage =>
            {
                Assert.Equal(s.Mage.Id, mage.Source);
                Assert.Equal("Mago", mage.SourceName);
                Assert.Equal(12, mage.Amount);
                Assert.Equal(6, mage.Hits);
            });
    }

    [Fact]
    public void Death_summary_is_limited_by_window_even_with_few_entries()
    {
        var s = NewScene();
        Tick(s, new DamageDealt(s.Wolf.Id, s.Player.Id, "wolf.bite", DamageSchool.Physical, 9, 0, 9, 0, false));
        for (var i = 0; i < 120; i++)
        {
            s.World.Step();
        }

        var deaths = Tick(s, new EntityDied(s.Player.Id));

        var death = Assert.Single(deaths);
        Assert.Single(death.Entries); // só a linha de morte: o dano é antigo demais
        Assert.Empty(death.DamageBySources);
    }
}
