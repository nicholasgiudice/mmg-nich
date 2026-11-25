using Game.Core.Combat;
using Game.Core.CombatLog;
using Game.Core.Events;
using Game.Core.Simulation;
using MmoRpgClient.Hud;

namespace Game.Client.Tests;

/// <summary>T-540: o aviso de chefe de campo (evento da T-503) sai do nome do monstro e da subárea, nos dados.</summary>
public class FieldBossTextTests
{
    private static readonly Lazy<ContentCatalog> Content = new(SkillPlanTests.LoadContent);

    [Fact]
    public void Witch_wakes_up_in_her_area_and_is_defeated()
    {
        Assert.Equal("A Bruxa do Brejo despertou na Cabana da Bruxa",
            FieldBossText.Line(Content.Value, new FieldBossAnnounced("monster.marsh_witch", "Cabana da Bruxa", Spawned: true)));
        Assert.Equal("A Bruxa do Brejo foi derrotada",
            FieldBossText.Line(Content.Value, new FieldBossAnnounced("monster.marsh_witch", "Cabana da Bruxa", Spawned: false)));
    }

    [Fact]
    public void Masculine_names_get_the_masculine_article_and_participle()
    {
        Assert.Equal("O Capataz Forjado despertou",
            FieldBossText.Line(Content.Value, new FieldBossAnnounced("monster.forged_overseer", null, Spawned: true)));
        Assert.Equal("O Capataz Forjado foi derrotado",
            FieldBossText.Line(Content.Value, new FieldBossAnnounced("monster.forged_overseer", "Mina Funda", Spawned: false)));
    }

    [Fact]
    public void The_text_follows_the_data_name_and_falls_back_to_the_id()
    {
        // O nome sai dos dados: um id sem definição cai no próprio id, sem quebrar.
        Assert.Equal("O monster.unknown despertou",
            FieldBossText.Line(Content.Value, new FieldBossAnnounced("monster.unknown", null, Spawned: true)));
    }

    [Theory]
    [InlineData("Cabana da Bruxa", "na Cabana da Bruxa")]
    [InlineData("Brejo Fundo", "no Brejo Fundo")]
    [InlineData("Minas Profundas", "nas Minas Profundas")]
    [InlineData("Campos Baixos", "nos Campos Baixos")]
    [InlineData("Passo", "no Passo")]
    [InlineData("Vale", "em Vale")]
    public void The_place_gets_its_preposition(string place, string expected) => Assert.Equal(expected, FieldBossText.In(place));

    [Fact]
    public void The_log_gets_one_line_per_event_in_the_phase_category()
    {
        var log = new CombatLogRecorder(FieldBossText.Formatters());
        var events = new GameEvent[] { new FieldBossAnnounced("monster.marsh_witch", "Cabana da Bruxa", Spawned: true) };
        log.Record(events, new Lookup(Content.Value));

        var entry = Assert.Single(log.Entries);
        Assert.Equal(LogCategory.Fase, entry.Category);
        Assert.Equal("A Bruxa do Brejo despertou na Cabana da Bruxa", entry.Text);
        Assert.DoesNotContain(typeof(FieldBossAnnounced), log.UnformattedTypes);
    }

    [Fact]
    public void Registering_twice_does_not_fail_when_the_core_already_has_the_formatter()
    {
        var formatters = FieldBossText.Register(FieldBossText.Formatters());
        Assert.True(formatters.IsRegistered<FieldBossAnnounced>());
    }

    private sealed class Lookup(ContentCatalog content) : ICombatLogLookup
    {
        public ContentCatalog Content => content;

        public long Tick => 5;

        public string? UnitName(EntityId id) => null;

        public IEnumerable<(EntityId Id, string Name)> NamedUnits() => [];
    }
}
