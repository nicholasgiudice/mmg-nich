using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Game.Server.Tests;

/// <summary>T-405: ferramenta de teste do servidor (<c>--dev-tools</c>): subir de nível, desfazer a segunda classe, redistribuir.</summary>
public class DevToolServerTests
{
    private const string Guardian = "second_class.guardian";
    private const string Berserker = "second_class.berserker";

    private static readonly ProgressionRules Rules = TestRig.RealContent.Value.Rules.Progression;
    private static readonly ServerOptions On = new() { DevTools = true };

    private static DevToolResult Request(WorldRig w, TestClient c, DevToolAction action)
    {
        var before = c.All<DevToolResult>().Count;
        c.Send(new DevToolRequest(action));
        w.Tick(2, c);
        var results = c.All<DevToolResult>();
        Assert.Equal(before + 1, results.Count);
        Assert.Equal(action, results[^1].Action);
        return results[^1];
    }

    private static void Intent(WorldRig w, TestClient c, uint sequence, Intent intent)
    {
        c.Send(new IntentMessage(sequence, 0, NetIntent.From(intent)));
        w.Tick(2, c);
    }

    private static int LevelOf(Entity e) => e.Get<CharacterStats>().Level;

    /// <summary>Sobe <paramref name="e"/> como quem joga: XP de monstros normais do próprio nível, uma morte de cada vez.</summary>
    private static void LevelByMonsters(World world, Entity e, int target)
    {
        while (LevelOf(e) < target)
        {
            var level = LevelOf(e);
            Leveling.GrantExperience(world, e, Rules.Share(Rules.MonsterXp(level, MonsterRank.Normal, level), 1), EntityId.None, "monster.test");
        }
    }

    // ---------- desligada ----------

    [Fact]
    public void Options_keep_dev_tools_off_unless_asked()
    {
        Assert.False(ServerOptions.Parse([], _ => null).DevTools);
        Assert.True(ServerOptions.Parse(["--dev-tools"], _ => null).DevTools);
        Assert.True(ServerOptions.Parse([], k => k == "MMO_DEV_TOOLS" ? "true" : null).DevTools);
        Assert.False(ServerOptions.Parse([], k => k == "MMO_DEV_TOOLS" ? "false" : null).DevTools);
    }

    [Fact]
    public void Without_the_option_every_request_is_refused_and_logged_with_who_asked()
    {
        var sink = new ListSink();
        using var w = new WorldRig(log: new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger());
        var c = w.Join("aria");
        Assert.False(c.Welcome!.DevTools); // o cliente não mostra o botão
        var hero = w.Where(c).Entity;
        var build = Leveling.BuildOf(hero);

        foreach (var action in Enum.GetValues<DevToolAction>())
        {
            var result = Request(w, c, action);
            Assert.Equal((DevToolOutcome.Disabled, 1), (result.Outcome, result.Level));
            Assert.Contains(sink.Events, e => e.Level == LogEventLevel.Warning && e.RenderMessage().Contains("aria", StringComparison.Ordinal)
                && e.RenderMessage().Contains(action.ToString(), StringComparison.Ordinal));
        }

        Assert.Equal(1, LevelOf(hero));
        Assert.Equal(build!.Allocated, Leveling.BuildOf(hero)!.Allocated);
        Assert.Equal(Rules.SkillPointsAt(1), hero.Get<CharacterProgress>().SkillPoints);
        Assert.Empty(c.All<EventsMessage>().SelectMany(m => m.Events).OfType<LevelUp>());
    }

    [Fact]
    public void Encounter_room_also_refuses_without_the_option()
    {
        var rig = new TestRig(bots: false);
        var c = rig.Join("aria", null);
        Assert.False(c.Welcome!.DevTools);
        foreach (var action in Enum.GetValues<DevToolAction>())
        {
            c.Send(new DevToolRequest(action));
            rig.Tick(2, [c]);
            Assert.Equal(DevToolOutcome.Disabled, c.All<DevToolResult>()[^1].Outcome);
        }

        Assert.Equal(Enum.GetValues<DevToolAction>().Length, c.All<DevToolResult>().Count);
    }

    // ---------- subir de nível ----------

    [Fact]
    public void Each_level_request_matches_leveling_with_monster_experience()
    {
        using var w = new WorldRig(options: On);
        var c = w.Join("aria");
        Assert.True(c.Welcome!.DevTools);
        var (zone, hero) = w.Where(c);
        var played = w.Where(w.Join("bia")).Entity; // na mesma zona, sobe matando monstros

        (DevToolAction Action, int Level)[] steps =
        [
            (DevToolAction.LevelUp1, 2),
            (DevToolAction.LevelUp5, 7),
            (DevToolAction.ToSecondClassLevel, Rules.SecondClassLevel),
            (DevToolAction.LevelUp1, 21),
            (DevToolAction.ToMaxLevel, Rules.MaxLevel),
        ];
        foreach (var (action, level) in steps)
        {
            var result = Request(w, c, action);
            Assert.Equal((DevToolOutcome.Done, level), (result.Outcome, result.Level));
            LevelByMonsters(zone.World, played, level);

            Assert.Equal(LevelOf(played), LevelOf(hero));
            var (a, b) = (hero.Get<CharacterProgress>(), played.Get<CharacterProgress>());
            Assert.Equal((b.AttributePoints, b.SkillPoints, b.ExperienceToNext), (a.AttributePoints, a.SkillPoints, a.ExperienceToNext));
            Assert.Equal(played.Get<Health>().Max, hero.Get<Health>().Max);
        }

        // Os níveis chegaram ao cliente pelo caminho normal: um LevelUp por nível, e o componente replicado.
        var ups = c.All<EventsMessage>().SelectMany(m => m.Events).OfType<LevelUp>().Where(l => l.Entity == hero.Id).Select(l => l.Level).ToList();
        Assert.Equal(Enumerable.Range(2, Rules.MaxLevel - 1), ups);
        Assert.Equal(Rules.MaxLevel, c.Entity(hero.Id)!.Components.OfType<CharacterStats>().Single().Level);

        // Nada acima do máximo.
        foreach (var action in new[] { DevToolAction.LevelUp1, DevToolAction.LevelUp5, DevToolAction.ToSecondClassLevel, DevToolAction.ToMaxLevel })
        {
            var result = Request(w, c, action);
            Assert.Equal((DevToolOutcome.AlreadyAtLevel, Rules.MaxLevel), (result.Outcome, result.Level));
        }

        Assert.Equal(Rules.MaxLevel, LevelOf(hero));
    }

    [Fact]
    public void Five_levels_stop_at_the_max_level()
    {
        using var w = new WorldRig(options: On);
        var c = w.Join("aria");
        var (zone, hero) = w.Where(c);
        LevelByMonsters(zone.World, hero, Rules.MaxLevel - 2);

        Assert.Equal((DevToolOutcome.Done, Rules.MaxLevel), (Request(w, c, DevToolAction.LevelUp5).Outcome, LevelOf(hero)));
        Assert.Equal(Rules.AttributePointsAt(Rules.MaxLevel), hero.Get<CharacterProgress>().AttributePoints);
    }

    // ---------- segunda classe e redistribuição ----------

    [Fact]
    public void Undo_second_class_gives_the_points_back_and_allows_choosing_another()
    {
        using var w = new WorldRig(options: On);
        var c = w.Join("aria");
        var hero = w.Where(c).Entity;
        var p = hero.Get<CharacterProgress>();

        Assert.Equal(DevToolOutcome.NoSecondClass, Request(w, c, DevToolAction.UndoSecondClass).Outcome);
        Request(w, c, DevToolAction.ToSecondClassLevel);
        Intent(w, c, 1, new ChooseSecondClassIntent(default, Guardian));
        Assert.Equal(Guardian, p.SecondClass);

        // Distribui todos os pontos, inclusive os +5 da escolha.
        var all = Rules.AttributePointsAt(Rules.SecondClassLevel, true);
        for (uint i = 0; i < all; i++)
        {
            Intent(w, c, 2 + i, new SpendAttributePointIntent(default, AttributeKind.Vit));
        }

        Assert.Equal((0, all), (p.AttributePoints, p.AllocatedVit));

        var undo = Request(w, c, DevToolAction.UndoSecondClass);
        Assert.Equal((DevToolOutcome.Done, Rules.SecondClassLevel), (undo.Outcome, undo.Level));
        Assert.Null(p.SecondClass);
        Assert.Equal((Rules.AttributePointsAt(Rules.SecondClassLevel), 0), (p.AttributePoints, p.AllocatedTotal));
        Assert.Null(Leveling.Validate(w.Content, w.Content.Classes["class.warrior"], Leveling.BuildOf(hero)!));
        Assert.Null(c.Entity(hero.Id)!.Components.OfType<CharacterProgress>().Single().SecondClass); // o cliente vê

        Intent(w, c, 500, new ChooseSecondClassIntent(default, Berserker));
        Assert.Equal(Berserker, p.SecondClass);
        Assert.Equal(Rules.AttributePointsAt(Rules.SecondClassLevel, true), p.AttributePoints);
    }

    [Fact]
    public void Free_reset_gives_every_point_back()
    {
        using var w = new WorldRig(options: On);
        var c = w.Join("aria");
        var hero = w.Where(c).Entity;
        Request(w, c, DevToolAction.LevelUp5);
        Intent(w, c, 1, new SpendAttributePointIntent(default, AttributeKind.Str));
        Intent(w, c, 2, new RaiseSkillRankIntent(default, "warrior.heavy_strike"));

        Assert.Equal(DevToolOutcome.Done, Request(w, c, DevToolAction.FreeReset).Outcome);
        var p = hero.Get<CharacterProgress>();
        Assert.Equal((Rules.AttributePointsAt(6), Rules.SkillPointsAt(6), 0), (p.AttributePoints, p.SkillPoints, p.AllocatedTotal));
        Assert.Single(c.All<EventsMessage>().SelectMany(m => m.Events).OfType<PointsReset>());
    }

    [Fact]
    public void In_combat_every_request_is_refused()
    {
        using var w = new WorldRig(options: On);
        var c = w.Join("aria");
        var (zone, hero) = w.Where(c);
        Request(w, c, DevToolAction.ToSecondClassLevel);
        Intent(w, c, 1, new ChooseSecondClassIntent(default, Guardian));

        foreach (var action in Enum.GetValues<DevToolAction>())
        {
            hero.Get<CombatActivity>().LastCombatTick = zone.World.Tick + 1; // lutando no tick em que o pedido entra
            Assert.Equal(DevToolOutcome.InCombat, Request(w, c, action).Outcome);
        }

        Assert.Equal(Rules.SecondClassLevel, LevelOf(hero));
        Assert.Equal(Guardian, hero.Get<CharacterProgress>().SecondClass);
    }

    // ---------- gravação ----------

    [Fact]
    public void With_a_database_each_accepted_request_is_saved_and_refused_ones_are_not()
    {
        var store = new FakeStore();
        var row = store.Add("class.warrior");
        using var w = new WorldRig(store, devLogin: false, options: On);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        Assert.True(c.Welcome!.DevTools);
        w.Tick(1, c);
        w.Flush();

        Assert.Equal(DevToolOutcome.Done, Request(w, c, DevToolAction.ToSecondClassLevel).Outcome);
        w.Flush();
        Assert.Equal(Rules.SecondClassLevel, store.Row(row.Id).Level);

        Intent(w, c, 1, new ChooseSecondClassIntent(default, Guardian));
        w.Flush();
        Assert.Contains($"\"secondClass\":\"{Guardian}\"", store.Row(row.Id).Progression);

        var saves = store.Saves;
        Assert.Equal(DevToolOutcome.Done, Request(w, c, DevToolAction.UndoSecondClass).Outcome);
        w.Flush();
        Assert.True(store.Saves > saves);
        Assert.DoesNotContain("secondClass", store.Row(row.Id).Progression, StringComparison.Ordinal);

        saves = store.Saves;
        Assert.Equal(DevToolOutcome.NoSecondClass, Request(w, c, DevToolAction.UndoSecondClass).Outcome);
        w.Flush();
        Assert.Equal(saves, store.Saves);

        Assert.Equal(DevToolOutcome.Done, Request(w, c, DevToolAction.FreeReset).Outcome);
        w.Flush();
        Assert.True(store.Saves > saves);
    }

    [Fact]
    public void Encounter_room_saves_accepted_requests_and_refuses_characters_without_progression()
    {
        var store = new FakeStore();
        var row = store.Add("class.warrior");
        using var a = new AccountRig(store, devLogin: true, options: new ServerOptions { RestartAfterSeconds = 0, DevTools = true });
        var c = a.Join(AccountRig.Ticket(row));
        Assert.True(c.Welcome!.DevTools);
        a.Rig.Tick(1, [c]);
        a.Flush();

        c.Send(new DevToolRequest(DevToolAction.LevelUp5));
        a.Rig.Tick(2, [c]);
        a.Flush();
        Assert.Equal((DevToolOutcome.Done, 6), (c.All<DevToolResult>()[^1].Outcome, c.All<DevToolResult>()[^1].Level));
        Assert.Equal(6, store.Row(row.Id).Level);

        // Login de desenvolvimento na sala: personagem de encontro, sem progressão.
        var dev = a.Rig.Join("bia", null);
        dev.Send(new DevToolRequest(DevToolAction.LevelUp1));
        a.Rig.Tick(2, [dev]);
        Assert.Equal((DevToolOutcome.NoProgression, 0), (dev.All<DevToolResult>()[^1].Outcome, dev.All<DevToolResult>()[^1].Level));
    }

    private sealed class ListSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
