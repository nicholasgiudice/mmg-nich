using Game.Core.Combat.Progression;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Protocol;
using Game.Server.Persistence;
using Serilog.Core;

namespace Game.Server.Tests;

/// <summary>T-404: segunda classe pela rede, gravada no documento do personagem e replicada.</summary>
public class SecondClassServerTests
{
    private const string Guardian = "second_class.guardian";
    private const string Berserker = "second_class.berserker";

    private static void Choose(TestClient c, uint sequence, string secondClass) =>
        c.Send(new IntentMessage(sequence, 0, NetIntent.From(new ChooseSecondClassIntent(default, secondClass))));

    private static List<T> Events<T>(TestClient c) => c.All<EventsMessage>().SelectMany(m => m.Events).OfType<T>().ToList();

    [Fact]
    public void Level_20_chooses_over_the_network_saves_at_once_and_keeps_it_after_leaving()
    {
        var store = new FakeStore();
        var row = store.Add("class.warrior", level: 20);
        var rules = TestRig.RealContent.Value.Rules.Progression;
        int attributePoints;
        using (var w = new WorldRig(store, devLogin: false))
        {
            var c = w.JoinWithTicket(AccountRig.Ticket(row));
            var hero = w.Where(c).Entity;
            w.Tick(1, c);
            w.Flush();
            var savesBefore = store.Saves;

            Choose(c, 1, Guardian);
            w.Tick(2, c);
            w.Flush();

            var chosen = Assert.Single(Events<SecondClassChosen>(c));
            Assert.Equal((hero.Id, Guardian), (chosen.Entity, chosen.SecondClassId));
            Assert.Equal(rules.AttributePointsAt(20, true), hero.Get<CharacterProgress>().AttributePoints);
            Assert.Equal(savesBefore + 1, store.Saves); // gravou pela escolha, ainda conectado
            Assert.Contains($"\"secondClass\":\"{Guardian}\"", store.Row(row.Id).Progression);
            Assert.Equal(CharacterDocument.Version, store.Row(row.Id).ProgressionVersion);

            // O cliente vê a segunda classe no componente replicado.
            var seen = c.Entity(hero.Id)!.Components.OfType<CharacterProgress>().Single();
            Assert.Equal(Guardian, seen.SecondClass);

            // Definitiva: um segundo pedido é recusado e nada muda.
            Choose(c, 2, Berserker);
            w.Tick(2, c);
            var rejected = Assert.Single(Events<ProgressionRejected>(c));
            Assert.Equal(ProgressionRejectReason.SecondClassAlreadyChosen, rejected.Reason);
            Assert.Equal(Guardian, hero.Get<CharacterProgress>().SecondClass);
            attributePoints = hero.Get<CharacterProgress>().AttributePoints;

            c.Raw.Leave();
            w.Tick(1);
            w.Flush();
        }

        // Outro processo, lendo só do banco.
        using var again = new WorldRig(store, devLogin: false);
        var back = again.JoinWithTicket(AccountRig.Ticket(row));
        Assert.NotNull(back.Welcome);
        var progress = again.Where(back).Entity.Get<CharacterProgress>();
        Assert.Equal(Guardian, progress.SecondClass);
        Assert.Equal(attributePoints, progress.AttributePoints);

        Choose(back, 1, Berserker);
        again.Tick(2, back);
        Assert.Equal(ProgressionRejectReason.SecondClassAlreadyChosen, Assert.Single(Events<ProgressionRejected>(back)).Reason);
        Assert.Equal(Guardian, progress.SecondClass);
    }

    [Fact]
    public void Choosing_below_level_20_is_refused_and_nothing_is_saved()
    {
        var store = new FakeStore();
        var row = store.Add("class.warrior", level: 19);
        using var w = new WorldRig(store, devLogin: false);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        w.Tick(1, c);
        w.Flush();
        var savesBefore = store.Saves;

        Choose(c, 1, Guardian);
        w.Tick(2, c);
        w.Flush();

        Assert.Equal(ProgressionRejectReason.LevelTooLow, Assert.Single(Events<ProgressionRejected>(c)).Reason);
        Assert.Null(w.Where(c).Entity.Get<CharacterProgress>().SecondClass);
        Assert.Equal(savesBefore, store.Saves);
    }

    [Fact]
    public void Encounter_room_also_saves_the_choice_at_once()
    {
        var store = new FakeStore();
        var row = store.Add("class.warrior", level: 20);
        using var a = new AccountRig(store);
        var c = a.Join(AccountRig.Ticket(row));
        a.Rig.Tick(1, [c]);
        a.Flush();
        var savesBefore = store.Saves;

        Choose(c, 1, Berserker);
        a.Rig.Tick(2, [c]);
        a.Flush();

        Assert.Equal(Berserker, a.Entity(c).Get<CharacterProgress>().SecondClass);
        Assert.Equal(savesBefore + 1, store.Saves);
        Assert.Contains($"\"secondClass\":\"{Berserker}\"", store.Row(row.Id).Progression);
    }

    [Fact]
    public void Saved_second_class_that_no_longer_exists_loads_without_it_and_with_the_points_back()
    {
        var rules = TestRig.RealContent.Value.Rules.Progression;
        var all = rules.AttributePointsAt(20, true); // inclui os pontos da segunda classe
        var doc = $$"""
            {"allocated":{"str":{{all}},"dex":0,"agi":0,"int":0,"vit":0},
             "skillRanks":{"warrior.heavy_strike":2,"second_class_gone.skill":3},
             "equipment":{},"gold":7,"secondClass":"second_class.gone"}
            """;
        var store = new FakeStore();
        var row = store.Add("class.warrior", progression: doc, level: 20, progressionVersion: 3);

        // Mundo aberto.
        using (var w = new WorldRig(store, devLogin: false))
        {
            var c = w.JoinWithTicket(AccountRig.Ticket(row));
            Assert.NotNull(c.Welcome);
            var hero = w.Where(c).Entity;
            var progress = hero.Get<CharacterProgress>();
            Assert.Null(progress.SecondClass);
            Assert.Equal(0, progress.AllocatedTotal); // passava do que o nível dá sem a segunda classe: todos voltam
            Assert.Equal(rules.AttributePointsAt(20), progress.AttributePoints);
            Assert.Equal(2, hero.Get<Game.Core.Combat.AbilityState>().RankOf("warrior.heavy_strike"));
            Assert.Equal(7, hero.Get<Game.Core.Combat.Items.Wallet>().Gold);
            c.Raw.Leave();
            w.Tick(1);
            w.Flush();
        }

        Assert.DoesNotContain("secondClass", store.Row(row.Id).Progression); // gravado já corrigido

        // Sala de encontro, com o documento ainda errado.
        var row2 = store.Add("class.warrior", progression: doc, level: 20, progressionVersion: 3);
        using var a = new AccountRig(store);
        var r = a.Join(AccountRig.Ticket(row2));
        Assert.NotNull(r.Welcome);
        Assert.Null(a.Entity(r).Get<CharacterProgress>().SecondClass);
    }

    [Fact]
    public void Missing_second_class_keeps_attributes_that_still_fit()
    {
        var content = TestRig.RealContent.Value;
        var cls = content.Classes["class.warrior"];
        var state = CharacterState.New with
        {
            Build = new CharacterBuild { Level = 20, Allocated = new AttributeSet { Vit = 4 }, SecondClass = "second_class.gone" },
        };

        var fixedState = state.WithoutMissingSecondClass(cls, content.Rules.Progression, out var dropped);

        Assert.Equal("second_class.gone", dropped);
        Assert.NotNull(fixedState);
        Assert.Null(fixedState.Build.SecondClass);
        Assert.Equal(4, fixedState.Build.Allocated.Vit);
        Assert.Null(Leveling.Validate(content, cls, fixedState.Build));

        // Segunda classe que existe (ou nenhuma): nada a corrigir.
        Assert.Null((state with { Build = state.Build with { SecondClass = Guardian } }).WithoutMissingSecondClass(cls, content.Rules.Progression, out _));
        Assert.Null(CharacterState.New.WithoutMissingSecondClass(cls, content.Rules.Progression, out _));

        var loaded = new LoadedCharacter(1, Guid.NewGuid(), LoadOutcome.Loaded, "Heroi", "class.warrior", state, null);
        Assert.Null(loaded.WithoutMissingSecondClass(content, Logger.None).State!.Build.SecondClass);
    }

    [Fact]
    public void Document_round_trips_the_second_class_and_older_versions_have_none()
    {
        var state = new CharacterState(
            new CharacterBuild { Level = 22, Allocated = new AttributeSet { Str = 30 }, SecondClass = Guardian },
            new Dictionary<EquipSlot, Game.Core.Combat.Items.ItemInstance>(),
            []);

        var json = CharacterDocument.Write(state);
        Assert.Equal(3, CharacterDocument.Version);
        Assert.Equal(Guardian, CharacterDocument.Read(CharacterDocument.Version, json, 22, 0).Build.SecondClass);
        Assert.Equal(state.Build.Allocated, CharacterDocument.Read(CharacterDocument.Version, json, 22, 0).Build.Allocated);

        // Versões anteriores abrem sem segunda classe (o campo não existia).
        Assert.Null(CharacterDocument.Read(2, """{"equipment":{},"gold":5}""", 22, 0).Build.SecondClass);
        Assert.Null(CharacterDocument.Read(1, """{"equipment":{}}""", 22, 0).Build.SecondClass);
        Assert.Null(CharacterDocument.Read(1, "{}", 1, 0).Build.SecondClass);

        // Sem escolha, o campo não é gravado.
        Assert.DoesNotContain("secondClass", CharacterDocument.Write(state with { Build = state.Build with { SecondClass = null } }));
        Assert.Throws<InvalidDataException>(() => CharacterDocument.Read(4, "{}", 1, 0));
    }
}
