using System.Diagnostics;
using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Rest;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Persistence;
using Game.Server.Transport;
using Game.Server.Zones;
using Serilog.Core;

namespace Game.Server.Tests;

/// <summary>Mundo aberto em memória (T-214): zonas reais de <c>data/zones.json</c>, transporte em memória.</summary>
internal sealed class WorldRig : IDisposable
{
    public WorldRig(ICharacterStore? store = null, bool devLogin = true)
    {
        Content = TestRig.RealContent.Value;
        if (store is not null)
        {
            Persistence = new CharacterPersistence(store, Logger.None, TimeSpan.FromMilliseconds(10));
        }

        var accounts = Persistence is null ? null : new AccountLogin(new Game.Auth.GameTicketReplayGuard(AccountRig.Key, "game.test"), Persistence);
        Server = new WorldServer(Content, new ServerOptions { World = true, DevLogin = devLogin }, Transport, Logger.None, accounts);
        Transport.Start(Server);
    }

    public ContentCatalog Content { get; }

    public InMemoryTransport Transport { get; } = new();

    public WorldServer Server { get; }

    public CharacterPersistence? Persistence { get; }

    public OpenZone City => Server.Zones["zone.city"];

    public OpenZone Fields => Server.Zones["zone.vigil_fields"];

    public void Tick(int n = 1, params TestClient[] clients)
    {
        for (var i = 0; i < n; i++)
        {
            Transport.Poll();
            Server.Step();
            foreach (var c in clients)
            {
                c.Sync();
            }
        }
    }

    public TestClient Join(string name, string cls = "class.warrior")
    {
        var client = new TestClient(Transport.Connect(), Content);
        client.Send(new Hello(ProtocolVersion.Current, Content.Fingerprint, name, cls));
        Tick(1, client);
        return client;
    }

    public TestClient JoinWithTicket(string ticket)
    {
        var client = new TestClient(Transport.Connect(), Content);
        client.Send(new Hello(ProtocolVersion.Current, Content.Fingerprint, "", null, ticket));
        var watch = Stopwatch.StartNew();
        while (client.Welcome is null && client.All<Reject>().Count == 0)
        {
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "tempo esgotado esperando o carregamento");
            Tick(1, client);
            Thread.Sleep(1);
        }

        return client;
    }

    /// <summary>Zona e entidade atuais do cliente.</summary>
    public (OpenZone Zone, Entity Entity) Where(TestClient c)
    {
        var (zone, id) = Server.Placement[c.Raw.Peer];
        var z = Server.Zones[zone];
        return (z, z.World.Find(id)!);
    }

    public void Flush() => Assert.True(Persistence!.FlushAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(), "gravações pendentes");

    public void Dispose() => Persistence?.Dispose();
}

public class WorldTests
{
    private static readonly ItemInstance Ring = new("accessory.ring", 2, ItemRarity.Uncommon, ["affix.vit"]);

    private static void Teleport(Entity e, double x, double y) => e.Get<Transform>().Position = new Vector2((float)x, (float)y);

    [Fact]
    public void Players_start_in_the_city_with_progression_and_rest()
    {
        using var w = new WorldRig();
        var c = w.Join("aria", "class.archer");

        Assert.Equal("zone.city", c.Welcome!.EncounterId);
        Assert.Equal("arena.city", c.Welcome.ArenaId);
        var (zone, e) = w.Where(c);
        Assert.Same(w.City, zone);
        Assert.NotNull(e.TryGet<CharacterProgress>());
        Assert.NotNull(e.TryGet<Resting>());
        Assert.Equal(w.City.SpawnPosition, e.Get<Transform>().Position);
    }

    [Fact]
    public void Walking_into_the_east_gate_moves_to_the_fields_with_the_same_state_and_back()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        var (_, hero) = w.Where(c);
        Leveling.GrantExperience(w.City.World, hero, 400, default, "test");
        hero.Get<Inventory>()[2] = Ring;
        hero.Remove<Resting>(); // sem regenerar: a vida tem que chegar igual
        var level = hero.Get<CharacterStats>().Level;
        hero.Get<Health>().Current = hero.Get<Health>().Max / 2;
        var health = hero.Get<Health>().Current;

        // Anda para leste pela estrada até o portão (intenção de verdade, colisão do Core).
        c.Send(new IntentMessage(1, 0, NetIntent.From(new MoveIntent(hero.Id, Vector2.UnitX))));
        for (var i = 0; i < 200 && c.Welcome!.EncounterId == "zone.city"; i++)
        {
            w.Tick(1, c);
        }

        Assert.Equal("zone.vigil_fields", c.Welcome!.EncounterId);
        Assert.Equal("arena.vigil_fields", c.Welcome.ArenaId);
        Assert.Equal(2, c.Welcome.Epoch);
        Assert.True(hero.IsRemoved);
        var (zone, moved) = w.Where(c);
        Assert.Same(w.Fields, zone);
        Assert.Equal(new Vector2(3.5f, 31.5f), moved.Get<Transform>().Position);
        Assert.Equal(level, moved.Get<CharacterStats>().Level);
        Assert.Equal(Ring, moved.Get<Inventory>()[2]);
        Assert.Equal(health, moved.Get<Health>().Current, 0.01f);
        Assert.Null(moved.TryGet<Resting>());

        // O cliente recebe o estado da zona nova.
        w.Tick(2, c);
        Assert.NotNull(c.Entity(c.Me));

        Teleport(moved, 0.5, 31.5);
        w.Tick(1, c);
        Assert.Equal("zone.city", c.Welcome!.EncounterId);
        Assert.Equal(new Vector2(36.5f, 15.5f), w.Where(c).Entity.Get<Transform>().Position);
    }

    [Fact]
    public void Fields_monsters_leave_a_corpse_and_respawn_at_their_point()
    {
        using var w = new WorldRig();
        var fields = w.Fields;
        var total = fields.Definition.MonsterSpawns.Sum(s => s.Count);
        Assert.Equal(total, fields.LivingMonsters);

        var rat = fields.World.Entities.First(e => e.TryGet<UnitInfo>()?.DefinitionId == "monster.field_rat");
        fields.World.Kill(rat);
        fields.Step();
        Assert.Equal(total - 1, fields.LivingMonsters);
        Assert.False(rat.IsRemoved); // corpo

        for (var i = 0; i < fields.Definition.CorpseTicks; i++)
        {
            fields.Step();
        }

        Assert.True(rat.IsRemoved);
        var respawn = fields.Definition.MonsterSpawns.First(s => s.Monster == "monster.field_rat").RespawnTicks;
        for (var i = fields.Definition.CorpseTicks; i < respawn; i++)
        {
            fields.Step();
        }

        Assert.Equal(total, fields.LivingMonsters);
    }

    [Fact]
    public void Death_in_the_fields_waits_for_respawn_and_returns_to_the_city_with_everything()
    {
        using var w = new WorldRig();
        var c = w.Join("aria", "class.mage");
        Teleport(w.Where(c).Entity, 38.5, 15.5);
        w.Tick(1, c);
        var (zone, hero) = w.Where(c);
        Assert.Same(w.Fields, zone);
        hero.Get<Inventory>()[0] = Ring;

        w.Fields.World.Kill(hero);
        w.Tick(20, c);
        Assert.Same(w.Fields, w.Where(c).Zone); // caído, esperando

        c.Send(new RespawnRequest());
        w.Tick(1, c);
        var (city, back) = w.Where(c);
        Assert.Same(w.City, city);
        Assert.Equal("zone.city", c.Welcome!.EncounterId);
        Assert.True(back.IsAlive);
        Assert.Equal(back.Get<Health>().Max, back.Get<Health>().Current);
        Assert.Equal(Ring, back.Get<Inventory>()[0]);
        Assert.Equal("weapon.staff", back.Get<Equipment>()[EquipSlot.Weapon]!.BaseId);
    }

    [Fact]
    public void City_has_no_monsters_no_damage_and_regenerates()
    {
        using var w = new WorldRig();
        var a = w.Join("a");
        var b = w.Join("b", "class.mage");
        var target = w.Where(b).Entity;
        Teleport(w.Where(a).Entity, 21.5, 15.5);
        Teleport(target, 22.3, 15.5);

        Assert.DoesNotContain(w.City.World.Entities, e => e.TryGet<FactionTag>()?.Faction == Faction.Monster);
        for (var i = 0; i < 60; i++)
        {
            a.Send(new IntentMessage((uint)(i + 1), 0, NetIntent.From(new UseAbilityIntent(a.Me, "warrior.sword_strike", target.Id))));
            w.Tick(1, a, b);
            Assert.DoesNotContain(w.City.World.LastTickEvents, ev => ev is Game.Core.Combat.DamageDealt);
        }

        Assert.Equal(target.Get<Health>().Max, target.Get<Health>().Current);

        var health = target.Get<Health>();
        health.Current = health.Max / 2;
        w.Tick(20, a, b);
        Assert.True(health.Current > (health.Max / 2) + 1);
    }

    [Fact]
    public void Empty_zones_stay_paused()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        w.Tick(10, c);

        Assert.True(w.City.World.Tick >= 10);
        Assert.Equal(0, w.Fields.World.Tick);
    }

    [Fact]
    public void Account_character_is_saved_with_zone_and_position_and_comes_back_there()
    {
        var store = new FakeStore();
        var row = store.Add("class.cleric");
        using (var w = new WorldRig(store, devLogin: false))
        {
            var c = w.JoinWithTicket(AccountRig.Ticket(row));
            Assert.Equal("zone.city", c.Welcome!.EncounterId); // API grava zone.city (0, 0) = nascimento
            Teleport(w.Where(c).Entity, 38.5, 15.5);
            w.Tick(1, c);
            Teleport(w.Where(c).Entity, 10.5, 31.5);
            w.Tick(1, c);
            c.Raw.Leave();
            w.Tick(1);
            w.Flush();
        }

        var saved = store.Row(row.Id);
        Assert.Equal("zone.vigil_fields", saved.ZoneId);
        Assert.Equal(10.5f, saved.PositionX, 0.5f);
        Assert.Equal(31.5f, saved.PositionY, 0.5f);

        using var again = new WorldRig(store, devLogin: false);
        var back = again.JoinWithTicket(AccountRig.Ticket(row));
        Assert.Equal("zone.vigil_fields", back.Welcome!.EncounterId);
        Assert.Equal(saved.PositionX, again.Where(back).Entity.Get<Transform>().Position.X, 0.01f);
    }

    [Fact]
    public void Dead_account_character_is_saved_back_at_the_city_spawn()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var w = new WorldRig(store, devLogin: false);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        Teleport(w.Where(c).Entity, 38.5, 15.5);
        w.Tick(1, c);
        w.Fields.World.Kill(w.Where(c).Entity);
        w.Tick(1, c);
        c.Raw.Leave();
        w.Tick(1);
        w.Flush();

        Assert.Equal("zone.city", store.Row(row.Id).ZoneId);
        Assert.Equal(0, store.Row(row.Id).PositionX);
    }

    [Fact]
    public void Name_only_login_needs_dev_login()
    {
        using var w = new WorldRig(new FakeStore(), devLogin: false);
        var c = w.Join("dev");

        Assert.Null(c.Welcome);
        Assert.Equal(RejectReason.Unauthorized, Assert.Single(c.All<Reject>()).Reason);
    }
}
