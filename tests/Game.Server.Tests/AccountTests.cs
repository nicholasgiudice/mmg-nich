using System.Diagnostics;
using Game.Auth;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Persistence;
using Game.Protocol;
using Game.Server.Persistence;
using Serilog.Core;

namespace Game.Server.Tests;

/// <summary>Banco em memória com a mesma regra de versão do repositório; pode atrasar ou falhar para os testes.</summary>
internal sealed class FakeStore : ICharacterStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, Character> _rows = new();
    private uint _version = 1;

    /// <summary>Enquanto não for completado, toda gravação espera (banco lento).</summary>
    public TaskCompletionSource SaveGate { get; set; } = Completed();

    public TaskCompletionSource LoadGate { get; set; } = Completed();

    /// <summary>Quantas gravações ainda lançam exceção (banco fora do ar).</summary>
    public int FailNextSaves { get; set; }

    public int Saves { get; private set; }

    public static TaskCompletionSource Completed()
    {
        var t = new TaskCompletionSource();
        t.SetResult();
        return t;
    }

    public Character Add(string classId = "class.warrior", Guid? account = null, string progression = "{}")
    {
        var c = new Character
        {
            Id = Guid.NewGuid(),
            AccountId = account ?? Guid.NewGuid(),
            Name = "Heroi" + _rows.Count,
            ClassId = classId,
            ZoneId = "zone.city",
            Progression = progression,
            Version = _version,
        };
        lock (_lock)
        {
            _rows[c.Id] = c;
        }

        return c;
    }

    public Character Row(Guid id)
    {
        lock (_lock)
        {
            return Copy(_rows[id]);
        }
    }

    public async Task<Character?> LoadAsync(Guid characterId, CancellationToken ct)
    {
        await LoadGate.Task;
        lock (_lock)
        {
            return _rows.TryGetValue(characterId, out var c) ? Copy(c) : null;
        }
    }

    public async Task<SaveStatus> SaveAsync(Character character, CancellationToken ct)
    {
        await SaveGate.Task;
        lock (_lock)
        {
            if (FailNextSaves > 0)
            {
                FailNextSaves--;
                throw new InvalidOperationException("banco fora do ar (teste)");
            }

            if (!_rows.TryGetValue(character.Id, out var row))
            {
                return SaveStatus.NotFound;
            }

            if (row.Version != character.Version)
            {
                return SaveStatus.Conflict;
            }

            character.Version = ++_version;
            _rows[character.Id] = Copy(character);
            Saves++;
            return SaveStatus.Saved;
        }
    }

    private static Character Copy(Character c) => new()
    {
        Id = c.Id,
        AccountId = c.AccountId,
        Name = c.Name,
        ClassId = c.ClassId,
        ZoneId = c.ZoneId,
        PositionX = c.PositionX,
        PositionY = c.PositionY,
        Level = c.Level,
        Experience = c.Experience,
        ProgressionVersion = c.ProgressionVersion,
        Progression = c.Progression,
        Version = c.Version,
    };
}

/// <summary>Sala com contas: chave de teste, servidor <c>game.test</c> e gravação com repetição rápida.</summary>
internal sealed class AccountRig : IDisposable
{
    public static readonly byte[] Key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

    public AccountRig(ICharacterStore store, bool devLogin = false, ServerOptions? options = null)
    {
        Persistence = new CharacterPersistence(store, Logger.None, TimeSpan.FromMilliseconds(10));
        // Sem reinício automático: o encontro não termina e grava "reinício da sala" no meio de um teste lento.
        Rig = new TestRig(bots: false, devLogin: devLogin, options: options ?? new ServerOptions { RestartAfterSeconds = 0 }, accounts: new AccountLogin(new GameTicketReplayGuard(Key, "game.test"), Persistence));
    }

    public TestRig Rig { get; }

    public CharacterPersistence Persistence { get; }

    public ZoneRoom Room => Rig.Room;

    public static string Ticket(Character c, string server = "game.test", Guid? account = null) =>
        GameTicket.Create(account ?? c.AccountId, c.Id, server, DateTimeOffset.UtcNow.AddMinutes(1)).Sign(Key);

    /// <summary>Manda o Hello com token e avança ticks até chegar Welcome ou Reject (o carregamento é assíncrono).</summary>
    public TestClient Join(string ticket)
    {
        var client = new TestClient(Rig.Transport.Connect(), Rig.Content);
        client.Send(new Hello(ProtocolVersion.Current, Rig.Content.Fingerprint, "", null, ticket));
        WaitFor(() => client.Welcome is not null || client.All<Reject>().Count > 0, client);
        return client;
    }

    public void WaitFor(Func<bool> done, params TestClient[] clients)
    {
        var watch = Stopwatch.StartNew();
        while (!done())
        {
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "tempo esgotado esperando a sala");
            Rig.Tick(1, clients);
            Thread.Sleep(1);
        }
    }

    public void Flush() => Assert.True(Persistence.FlushAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(), "gravações pendentes");

    public Entity Entity(TestClient c) => Room.World.Find(c.Me)!;

    public void Dispose() => Persistence.Dispose();
}

public class AccountTests
{
    private static readonly ItemInstance RareRing = new("accessory.ring", 3, ItemRarity.Rare, ["affix.dex", "affix.str"]);

    private static void AssertSame(ItemInstance expected, ItemInstance? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal((expected.BaseId, expected.Level, expected.Rarity), (actual.BaseId, actual.Level, actual.Rarity));
        Assert.Equal(expected.Affixes, actual.Affixes);
    }

    [Fact]
    public void Enter_gain_xp_and_item_leave_enter_again_and_everything_is_there()
    {
        var store = new FakeStore();
        var row = store.Add("class.archer");

        using (var a = new AccountRig(store))
        {
            var client = a.Join(AccountRig.Ticket(row));
            Assert.NotNull(client.Welcome);
            var hero = a.Entity(client);
            Assert.Equal(1, hero.Get<CharacterStats>().Level);
            Assert.Equal("weapon.bow", hero.Get<Equipment>()[EquipSlot.Weapon]!.BaseId); // equipamento inicial

            Leveling.GrantExperience(a.Room.World, hero, 400, default, "test");
            Assert.Null(Leveling.SpendAttributePoint(a.Room.World, hero, AttributeKind.Dex));
            hero.Get<Inventory>()[4] = RareRing;
            a.Rig.Tick(2, [client]);

            client.Raw.Leave();
            a.Rig.Tick(1);
            a.Flush();
        }

        var saved = store.Row(row.Id);
        Assert.True(saved.Level >= 3);
        Assert.Contains("accessory.ring", saved.Progression);

        // Outro processo (servidor reiniciado), lendo só do banco.
        using var b = new AccountRig(store);
        var again = b.Join(AccountRig.Ticket(row));
        var back = b.Entity(again);
        var progress = back.Get<CharacterProgress>();
        Assert.Equal(saved.Level, back.Get<CharacterStats>().Level);
        Assert.Equal(saved.Experience, progress.Experience);
        Assert.Equal(1, progress.AllocatedDex);
        AssertSame(RareRing, back.Get<Inventory>()[4]);
        Assert.Equal("weapon.bow", back.Get<Equipment>()[EquipSlot.Weapon]!.BaseId);
        Assert.Equal(back.Get<Health>().Max, back.Get<Health>().Current);
    }

    [Fact]
    public void Tick_never_waits_for_a_slow_database()
    {
        var store = new FakeStore();
        var stays = store.Add();
        var leaves = store.Add();
        using var a = new AccountRig(store);
        var c1 = a.Join(AccountRig.Ticket(stays));
        var c2 = a.Join(AccountRig.Ticket(leaves));

        // Banco "travado": a gravação de quem sai fica pendente e o loop continua.
        store.SaveGate = new TaskCompletionSource();
        c2.Raw.Leave();
        var slowest = TimeSpan.Zero;
        for (var i = 0; i < 40; i++)
        {
            var watch = Stopwatch.StartNew();
            a.Rig.Tick(1, [c1]);
            slowest = watch.Elapsed > slowest ? watch.Elapsed : slowest;
        }

        Assert.Equal(1, a.Persistence.PendingSaves);
        Assert.True(slowest < TimeSpan.FromMilliseconds(200), $"tick mais lento: {slowest.TotalMilliseconds} ms");

        // Carregamento lento também não segura o loop: o jogador só entra quando chega.
        store.LoadGate = new TaskCompletionSource();
        var c3 = new TestClient(a.Rig.Transport.Connect(), a.Rig.Content);
        c3.Send(new Hello(ProtocolVersion.Current, a.Rig.Content.Fingerprint, "", null, AccountRig.Ticket(store.Add())));
        a.Rig.Tick(20, [c1, c3]);
        Assert.Null(c3.Welcome);

        store.LoadGate.SetResult();
        store.SaveGate.SetResult();
        a.WaitFor(() => c3.Welcome is not null, c1, c3);
        a.Flush();
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public void Failed_saves_are_retried_with_the_newest_state()
    {
        var store = new FakeStore { FailNextSaves = 3 };
        var row = store.Add();
        using var a = new AccountRig(store);
        var client = a.Join(AccountRig.Ticket(row));
        Leveling.GrantExperience(a.Room.World, a.Entity(client), 120, default, "test");
        a.Rig.Tick(1, [client]); // LevelUp → grava (falha e repete)
        a.Room.SaveAll("teste");
        a.Flush();

        Assert.Equal(3, a.Persistence.SaveFailures);
        Assert.Equal(a.Entity(client).Get<CharacterStats>().Level, store.Row(row.Id).Level);
        Assert.True(store.Row(row.Id).Level >= 2);
    }

    [Fact]
    public void Level_up_and_rare_items_save_right_away_and_periodic_save_runs()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var a = new AccountRig(store, options: new ServerOptions { SaveIntervalSeconds = 1, RestartAfterSeconds = 0 });
        var client = a.Join(AccountRig.Ticket(row));

        Leveling.GrantExperience(a.Room.World, a.Entity(client), 120, default, "test");
        a.Rig.Tick(1, [client]);
        a.Flush();
        Assert.Equal(1, store.Saves);
        Assert.Equal(2, store.Row(row.Id).Level);

        a.Rig.Tick(20, [client]); // 1 s
        a.Flush();
        Assert.Equal(2, store.Saves);
    }

    [Fact]
    public void Room_restart_keeps_the_saved_character()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var a = new AccountRig(store);
        var client = a.Join(AccountRig.Ticket(row));
        Leveling.GrantExperience(a.Room.World, a.Entity(client), 400, default, "test");
        var level = a.Entity(client).Get<CharacterStats>().Level;

        a.Room.Restart("teste");
        a.Rig.Tick(1, [client]);

        Assert.Equal(level, a.Entity(client).Get<CharacterStats>().Level);
    }

    [Fact]
    public void Bad_tickets_duplicates_and_unloadable_characters_are_refused()
    {
        var store = new FakeStore();
        var row = store.Add();
        var other = store.Add();
        var corrupted = store.Add(progression: """{"equipment":{"weapon":{"base":""}}}""");
        using var a = new AccountRig(store);

        Reject RejectOf(TestClient c) => Assert.Single(c.All<Reject>());

        Assert.Equal(RejectReason.Unauthorized, RejectOf(a.Join("lixo")).Reason);
        Assert.Equal(RejectReason.Unauthorized, RejectOf(a.Join(AccountRig.Ticket(row, server: "outro"))).Reason);

        var ticket = AccountRig.Ticket(row);
        var first = a.Join(ticket);
        Assert.NotNull(first.Welcome);
        Assert.Equal(RejectReason.Unauthorized, RejectOf(a.Join(ticket)).Reason); // uso único
        Assert.Equal(RejectReason.AlreadyConnected, RejectOf(a.Join(AccountRig.Ticket(row))).Reason);

        Assert.Equal(RejectReason.Unavailable, RejectOf(a.Join(AccountRig.Ticket(corrupted))).Reason);
        Assert.Equal(RejectReason.Unavailable, RejectOf(a.Join(AccountRig.Ticket(other, account: Guid.NewGuid()))).Reason);

        // Sem --dev-login, nome sozinho não entra.
        var dev = new TestClient(a.Rig.Transport.Connect(), a.Rig.Content);
        dev.Send(new Hello(ProtocolVersion.Current, a.Rig.Content.Fingerprint, "dev", null));
        a.Rig.Tick(1, [dev]);
        Assert.Equal(RejectReason.Unauthorized, RejectOf(dev).Reason);

        // Saiu: o mesmo personagem entra de novo com token novo.
        first.Raw.Leave();
        a.Rig.Tick(1);
        Assert.NotNull(a.Join(AccountRig.Ticket(row)).Welcome);
    }

    [Fact]
    public void Dev_login_and_accounts_work_side_by_side()
    {
        var store = new FakeStore();
        using var a = new AccountRig(store, devLogin: true);
        var dev = a.Rig.Join("dev", "class.mage");
        var account = a.Join(AccountRig.Ticket(store.Add("class.cleric")));

        Assert.NotNull(dev.Welcome);
        Assert.NotNull(account.Welcome);
        Assert.Null(a.Room.World.Find(dev.Me)!.TryGet<CharacterProgress>());
        Assert.NotNull(a.Entity(account).TryGet<CharacterProgress>());
    }

    [Fact]
    public void Document_round_trips_and_empty_document_is_a_new_character()
    {
        var state = new CharacterState(
            new CharacterBuild { Level = 4, Experience = 17, Allocated = new AttributeSet { Str = 3, Vit = 2 }, SkillRanks = new Dictionary<string, int> { ["warrior.heavy_strike"] = 2 } },
            new Dictionary<EquipSlot, ItemInstance> { [EquipSlot.Weapon] = new("weapon.sword", 4, ItemRarity.Uncommon, ["affix.str"]) },
            [null, null, RareRing]);

        var read = CharacterDocument.Read(CharacterDocument.Version, CharacterDocument.Write(state), 4, 17);

        Assert.Equal(state.Build.Allocated, read.Build.Allocated);
        Assert.Equal(state.Build.SkillRanks, read.Build.SkillRanks);
        AssertSame(state.Equipment![EquipSlot.Weapon], read.Equipment![EquipSlot.Weapon]);
        AssertSame(RareRing, read.Inventory[2]);
        Assert.Null(read.Inventory[0]);

        var fresh = CharacterDocument.Read(1, "{}", 1, 0);
        Assert.Null(fresh.Equipment);
        Assert.Equal(1, fresh.Build.Level);
        Assert.Throws<InvalidDataException>(() => CharacterDocument.Read(99, "{}", 1, 0));
    }

    [Fact]
    public void Gold_and_stacks_are_saved_and_loaded()
    {
        const string Potion = "consumable.minor_healing_potion";
        const string Mana = "consumable.minor_mana_potion";
        var store = new FakeStore();
        var row = store.Add("class.mage");

        using (var a = new AccountRig(store))
        {
            var client = a.Join(AccountRig.Ticket(row));
            var hero = a.Entity(client);
            Assert.Equal(5, Consumables.CountOf(hero, Potion)); // personagem novo: inventário inicial
            Assert.Equal(0, hero.Get<Wallet>().Gold);

            hero.Get<Wallet>().Gold = 123;
            Assert.Null(Consumables.Use(a.Room.World, hero, Consumables.FindSlot(hero, Potion)));
            Assert.True(Gear.AddToInventory(a.Room.World, hero, ItemInstance.Stack(Mana, 27)) >= 0); // 20 + 7
            a.Rig.Tick(2, [client]);

            client.Raw.Leave();
            a.Rig.Tick(1);
            a.Flush();
        }

        var saved = store.Row(row.Id);
        Assert.Equal(2, saved.ProgressionVersion);
        Assert.Contains("\"gold\":123", saved.Progression);

        using var b = new AccountRig(store);
        var back = b.Entity(b.Join(AccountRig.Ticket(row)));
        Assert.Equal(123, back.Get<Wallet>().Gold);
        Assert.Equal(4, Consumables.CountOf(back, Potion));
        Assert.Equal([20, 7], back.Get<Inventory>().Slots.Where(i => i?.BaseId == Mana).Select(i => i!.Count));
    }

    [Fact]
    public void Version_1_document_loads_with_zero_gold_and_the_starting_potions_once()
    {
        const string Potion = "consumable.minor_healing_potion";
        var store = new FakeStore();
        var v1 = """
            {"allocated":{"str":0,"dex":0,"agi":0,"int":0,"vit":0},"skillRanks":{},
             "equipment":{"weapon":{"base":"weapon.sword","level":2,"rarity":"common","affixes":[]}},
             "inventory":[{"slot":0,"item":{"base":"accessory.ring","level":3,"rarity":"rare","affixes":["affix.dex","affix.str"]}}]}
            """;
        var row = store.Add(progression: v1);
        Assert.Equal(1, row.ProgressionVersion);

        using (var a = new AccountRig(store))
        {
            var client = a.Join(AccountRig.Ticket(row));
            var hero = a.Entity(client);
            Assert.Equal(0, hero.Get<Wallet>().Gold);
            AssertSame(RareRing, hero.Get<Inventory>()[0]);
            Assert.Equal(5, Consumables.CountOf(hero, Potion));

            // Gasta as 5: na próxima carga (já v2) ninguém devolve as poções.
            for (var i = 0; i < 5; i++)
            {
                hero.Get<Game.Core.Combat.AbilityState>().ReadyAtTick.Clear();
                Assert.Null(Consumables.Use(a.Room.World, hero, Consumables.FindSlot(hero, Potion)));
            }

            client.Raw.Leave();
            a.Rig.Tick(1);
            a.Flush();
        }

        Assert.Equal(2, store.Row(row.Id).ProgressionVersion);
        using var b = new AccountRig(store);
        var back = b.Entity(b.Join(AccountRig.Ticket(row)));
        Assert.Equal(0, Consumables.CountOf(back, Potion));
        AssertSame(RareRing, back.Get<Inventory>()[0]);
    }

    [Fact]
    public void Document_keeps_counts_and_gold_and_reads_version_1()
    {
        var state = new CharacterState(new CharacterBuild(), new Dictionary<EquipSlot, ItemInstance>(), [ItemInstance.Stack("consumable.minor_healing_potion", 13), RareRing])
        {
            Gold = 4567,
        };

        var json = CharacterDocument.Write(state);
        var read = CharacterDocument.Read(2, json, 1, 0);
        Assert.Equal((4567L, false), (read.Gold, read.GrantStartingInventory));
        Assert.Equal(13, read.Inventory[0]!.Count);
        Assert.Equal(1, read.Inventory[1]!.Count);
        Assert.DoesNotContain("\"count\":1}", json); // pilha de 1 não grava a quantidade

        var old = CharacterDocument.Read(1, json, 1, 0);
        Assert.Equal((0L, true), (old.Gold, old.GrantStartingInventory));
        Assert.Throws<InvalidDataException>(() => CharacterDocument.Read(2, """{"equipment":{},"gold":-1}""", 1, 0));
        Assert.Throws<InvalidDataException>(() => CharacterDocument.Read(2, """{"equipment":{},"inventory":[{"slot":0,"item":{"base":"x","count":0}}]}""", 1, 0));
    }
}
