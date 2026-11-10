using System.Numerics;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Zones;

namespace Game.Server.Tests;

/// <summary>
/// T-350: instâncias de dungeon sob demanda no mundo aberto. Usa a <c>zone.test_instance</c> de <c>data/zones.json</c> (o mapa
/// do Covil, com a Matriarca) com prazos curtos e um portão de teste na Cidade, já que nenhum mapa leva a ela até a Cripta
/// (T-352).
/// </summary>
public class InstanceTests
{
    private const string Dungeon = "zone.test_instance";
    private const int Corpse = 20;
    private const int Respawn = 40;
    private const int EmptyTimeout = 100;
    private const int OutsiderTimeout = 40;

    /// <summary>Portão de teste na Cidade (1 × 1 tile em (24, 15)), 3,5 tiles a leste do nascimento.</summary>
    private static readonly Vector2 CityGate = new(24.5f, 15.5f);

    /// <summary>Dentro da saída da instância (portão oeste do Covil).</summary>
    private static readonly Vector2 ExitGate = new(1f, 16.5f);

    /// <summary>Conteúdo real + portão de teste na Cidade + prazos curtos na instância de teste.</summary>
    private static ContentCatalog Content(int maxPlayers = 4)
    {
        var c = TestRig.RealContent.Value;
        var dungeon = c.Zones[Dungeon] with
        {
            CorpseTicks = Corpse,
            EmptyTimeoutTicks = EmptyTimeout,
            OutsiderTimeoutTicks = OutsiderTimeout,
            MaxPlayers = maxPlayers,
        };
        dungeon = dungeon with { MonsterSpawns = dungeon.MonsterSpawns.Select(s => s with { RespawnTicks = Respawn }).ToList() };
        var city = c.Zones["zone.city"];
        city = city with
        {
            Gates = [.. city.Gates, new GateDefinition { Area = new RectDefinition(24, 15, 1, 1), To = Dungeon, Arrival = dungeon.Spawn }],
        };
        var zones = c.Zones.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        zones[Dungeon] = dungeon;
        zones[city.Id] = city;
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values, c.Items.Values)
        {
            Rules = c.Rules,
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = zones,
            Shops = c.Shops,
        };
    }

    private static WorldRig Rig(int maxInstances = 20, int maxPlayers = 4, FakeStore? store = null) =>
        new(store, devLogin: store is null, Content(maxPlayers), new ServerOptions { MaxInstances = maxInstances });

    private static void Teleport(Entity e, Vector2 to) => e.Get<Transform>().Position = to;

    /// <summary>Põe o personagem no portão (Cidade → instância, ou na saída se já está dentro) e roda um tick.</summary>
    private static void WalkInto(WorldRig w, TestClient c, Vector2 gate, params TestClient[] all)
    {
        Teleport(w.Where(c).Entity, gate);
        w.Tick(1, all.Length == 0 ? [c] : all);
    }

    private static int InstanceOf(WorldRig w, TestClient c) => w.Where(c).Zone.InstanceId;

    private static void Group(WorldRig w, TestClient leader, string guestName, TestClient guest, params TestClient[] all)
    {
        leader.Send(new PartyInvite(null, guestName));
        w.Tick(1, all);
        guest.Send(new PartyRespond(guest.All<PartyInviteReceived>().Last().InviteId, true));
        w.Tick(1, all);
    }

    private static Entity Matriarch(OpenZone zone) =>
        zone.World.Entities.Single(e => e.TryGet<UnitInfo>()?.DefinitionId == "monster.forest_matriarch");

    [Fact]
    public void Instance_zones_are_not_open_zones_and_exist_only_on_demand()
    {
        using var w = Rig();
        Assert.False(w.Server.Zones.ContainsKey(Dungeon));
        Assert.Empty(w.Server.Instances);

        var c = w.Join("aria");
        WalkInto(w, c, CityGate);

        var instance = Assert.Single(w.Server.Instances);
        Assert.Equal(1, instance.Id);
        Assert.Equal(Dungeon, c.Welcome!.EncounterId);
        Assert.Equal("map.matriarch_lair", c.Welcome.ArenaId);
        Assert.Equal(instance.Id, c.Welcome.InstanceId);
        Assert.Same(instance.Zone, w.Where(c).Zone);
        Assert.Equal(instance.Zone.SpawnPosition, w.Where(c).Entity.Get<Transform>().Position);
        Assert.Equal(1, instance.Zone.LivingMonsters);

        // Pela saída volta para a zona de origem (Campos, na chegada do Covil), e o Welcome sai sem instância.
        WalkInto(w, c, ExitGate);
        Assert.Same(w.Fields, w.Where(c).Zone);
        Assert.Equal(0, c.Welcome!.InstanceId);
    }

    [Fact]
    public void Party_members_share_a_copy_and_an_outsider_gets_another()
    {
        using var w = Rig();
        var a = w.Join("aria");
        var b = w.Join("bruno", "class.cleric");
        var c = w.Join("caio", "class.mage");
        TestClient[] all = [a, b, c];
        Group(w, a, "bruno", b, all);

        WalkInto(w, a, CityGate, all);
        WalkInto(w, b, CityGate, all);
        WalkInto(w, c, CityGate, all);

        Assert.Equal(2, w.Server.Instances.Count);
        Assert.Equal(InstanceOf(w, a), InstanceOf(w, b));
        Assert.Same(w.Where(a).Zone, w.Where(b).Zone);
        Assert.NotEqual(InstanceOf(w, a), InstanceOf(w, c));
        Assert.Equal($"party:{w.Server.Parties.PartyOf(a.Raw.Peer)}", w.Server.Instances.Single(i => i.Id == InstanceOf(w, a)).Owner);

        // Cópias diferentes não se veem: cada cliente só tem a própria entidade de jogador (mais a Matriarca da cópia dele).
        w.Tick(3, all);
        Assert.NotNull(a.Entity(b.Me));
        Assert.DoesNotContain(c.State.Latest!.Entities, e => e.Id != c.Me && e.TryGet<FactionTag>()?.Faction == Faction.Player);

        // Seeds derivadas do id da cópia.
        Assert.NotEqual(DungeonInstance.SeedFor(1, 1), DungeonInstance.SeedFor(1, 2));
    }

    [Fact]
    public void Monsters_in_an_instance_do_not_respawn()
    {
        using var w = Rig();
        var c = w.Join("aria");
        WalkInto(w, c, CityGate);
        var zone = w.Where(c).Zone;
        var boss = Matriarch(zone);

        zone.World.Kill(boss);
        w.Tick(Corpse + Respawn + 20, c);

        Assert.Equal(0, zone.LivingMonsters);
        Assert.Null(zone.World.Find(boss.Id)); // o corpo sumiu
        Assert.DoesNotContain(zone.World.Entities, e => e.TryGet<FactionTag>()?.Faction == Faction.Monster);
    }

    [Fact]
    public void Empty_instance_is_destroyed_after_the_timeout_and_a_new_one_starts_fresh()
    {
        using var w = Rig();
        var c = w.Join("aria");
        WalkInto(w, c, CityGate);
        var first = Assert.Single(w.Server.Instances);
        first.Zone.World.Kill(Matriarch(first.Zone));
        w.Tick(1, c);

        WalkInto(w, c, ExitGate);
        var tickAtExit = first.Zone.World.Tick;
        w.Tick(EmptyTimeout - 1, c);
        Assert.Same(first, Assert.Single(w.Server.Instances));
        Assert.Equal(tickAtExit, first.Zone.World.Tick); // vazia, não avança

        w.Tick(2, c);
        Assert.Empty(w.Server.Instances);

        // Volta pelos Campos até a Cidade e entra de novo: cópia nova, Matriarca viva.
        Teleport(w.Where(c).Entity, new Vector2(0.5f, 31.5f)); // portão oeste dos Campos
        w.Tick(1, c);
        Assert.Same(w.City, w.Where(c).Zone);
        WalkInto(w, c, CityGate);
        var second = Assert.Single(w.Server.Instances);
        Assert.Equal(2, second.Id);
        Assert.Equal(1, second.Zone.LivingMonsters);
    }

    [Fact]
    public void Reconnecting_before_the_timeout_returns_inside_and_after_it_does_not()
    {
        using var w = Rig();
        var c = w.Join("aria");
        WalkInto(w, c, CityGate);
        var instance = Assert.Single(w.Server.Instances);
        var spot = new Vector2(8.5f, 16.5f);
        Teleport(w.Where(c).Entity, spot);
        w.Tick(1, c);

        c.Raw.Leave();
        w.Tick(EmptyTimeout / 2);
        var back = w.Join("aria");

        Assert.Equal(instance.Id, back.Welcome!.InstanceId);
        Assert.Equal(Dungeon, back.Welcome.EncounterId);
        Assert.Same(instance.Zone, w.Where(back).Zone);
        Assert.Equal(spot.X, w.Where(back).Entity.Get<Transform>().Position.X, 0.01f);

        back.Raw.Leave();
        w.Tick(EmptyTimeout + 1);
        Assert.Empty(w.Server.Instances);
        var late = w.Join("aria");
        Assert.Equal(0, late.Welcome!.InstanceId);
        Assert.Equal("zone.city", late.Welcome.EncounterId); // dev-login não grava: nasce na zona inicial
    }

    [Fact]
    public void Account_character_inside_is_saved_at_the_origin_and_comes_back_inside_or_at_the_entrance()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var w = Rig(store: store);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        WalkInto(w, c, CityGate);
        Teleport(w.Where(c).Entity, new Vector2(8.5f, 16.5f));
        w.Tick(1, c);
        var instance = Assert.Single(w.Server.Instances);
        var exit = instance.Definition.ExitGate()!;

        w.Server.SaveAll("teste");
        w.Flush();
        var saved = store.Row(row.Id);
        Assert.Equal("zone.vigil_fields", saved.ZoneId); // a zona de origem, nunca a instância
        Assert.Equal((float)exit.Arrival.X, saved.PositionX, 0.01f);
        Assert.Equal((float)exit.Arrival.Y, saved.PositionY, 0.01f);

        // Caiu e voltou dentro do prazo: dentro da cópia, onde estava.
        c.Raw.Leave();
        w.Tick(1);
        w.Flush();
        Assert.Equal("zone.vigil_fields", store.Row(row.Id).ZoneId);
        var back = w.JoinWithTicket(AccountRig.Ticket(row));
        Assert.Equal(instance.Id, back.Welcome!.InstanceId);
        Assert.Equal(8.5f, w.Where(back).Entity.Get<Transform>().Position.X, 0.01f);

        // Caiu e o prazo passou: volta para a entrada (a posição gravada, nos Campos).
        back.Raw.Leave();
        w.Tick(EmptyTimeout + 1);
        w.Flush();
        var late = w.JoinWithTicket(AccountRig.Ticket(row));
        Assert.Equal("zone.vigil_fields", late.Welcome!.EncounterId);
        Assert.Equal(0, late.Welcome.InstanceId);
        Assert.Equal((float)exit.Arrival.X, w.Where(late).Entity.Get<Transform>().Position.X, 0.01f);
    }

    [Fact]
    public void Dead_account_character_inside_is_saved_at_the_origin_too()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var w = Rig(store: store);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        WalkInto(w, c, CityGate);
        w.Where(c).Zone.World.Kill(w.Where(c).Entity);
        w.Tick(1, c);
        c.Raw.Leave();
        w.Tick(1);
        w.Flush();

        Assert.Equal("zone.vigil_fields", store.Row(row.Id).ZoneId);
    }

    [Fact]
    public void Death_respawns_at_the_instance_entrance_with_the_monsters_as_they_were()
    {
        using var w = Rig();
        var a = w.Join("aria");
        var b = w.Join("bruno", "class.cleric");
        TestClient[] all = [a, b];
        Group(w, a, "bruno", b, all);
        WalkInto(w, a, CityGate, all);
        WalkInto(w, b, CityGate, all);
        var zone = w.Where(a).Zone;
        var boss = Matriarch(zone);
        var health = boss.Get<Health>();
        health.Current = health.Max / 2;

        // Um cai com o outro vivo; depois o outro cai também (todos caídos): os dois renascem na entrada da mesma cópia.
        zone.World.Kill(w.Where(a).Entity);
        w.Tick(5, all);
        a.Send(new RespawnRequest());
        w.Tick(1, all);
        Assert.Same(zone, w.Where(a).Zone);

        zone.World.Kill(w.Where(a).Entity);
        zone.World.Kill(w.Where(b).Entity);
        w.Tick(5, all);
        a.Send(new RespawnRequest());
        b.Send(new RespawnRequest());
        w.Tick(1, all);

        foreach (var c in all)
        {
            var (where, hero) = w.Where(c);
            Assert.Same(zone, where);
            Assert.Equal(zone.InstanceId, c.Welcome!.InstanceId);
            Assert.True(hero.IsAlive);
            Assert.Equal(zone.SpawnPosition, hero.Get<Transform>().Position);
        }

        Assert.Same(boss, Matriarch(zone)); // não reinicia: mesma Matriarca, ainda ferida
        Assert.True(boss.Get<Health>().Current < boss.Get<Health>().Max);
        Assert.Single(w.Server.Instances);
    }

    [Fact]
    public void Instance_limit_refuses_with_a_reason_once_per_attempt()
    {
        using var w = Rig(maxInstances: 1);
        var a = w.Join("aria");
        var b = w.Join("bruno");
        TestClient[] all = [a, b];
        WalkInto(w, a, CityGate, all);
        WalkInto(w, b, CityGate, all);
        w.Tick(5, all);

        Assert.Single(w.Server.Instances);
        Assert.Same(w.City, w.Where(b).Zone);
        var refusal = Assert.Single(b.All<InstanceRejected>());
        Assert.Equal((Dungeon, InstanceRejectReason.TooManyInstances), (refusal.ZoneId, refusal.Reason));

        // Sai do portão e tenta de novo: outra recusa.
        Teleport(w.Where(b).Entity, w.City.SpawnPosition);
        w.Tick(1, all);
        WalkInto(w, b, CityGate, all);
        Assert.Equal(2, b.All<InstanceRejected>().Count);
    }

    [Fact]
    public void Full_copy_refuses_the_extra_member()
    {
        using var w = Rig(maxPlayers: 1);
        var a = w.Join("aria");
        var b = w.Join("bruno");
        TestClient[] all = [a, b];
        Group(w, a, "bruno", b, all);
        WalkInto(w, a, CityGate, all);
        WalkInto(w, b, CityGate, all);

        Assert.Same(w.City, w.Where(b).Zone);
        Assert.Equal(InstanceRejectReason.InstanceFull, Assert.Single(b.All<InstanceRejected>()).Reason);
    }

    [Fact]
    public void Leaving_the_party_inside_takes_you_out_after_the_warning()
    {
        using var w = Rig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        var c = w.Join("caio");
        TestClient[] all = [a, b, c];
        Group(w, a, "bruno", b, all);
        Group(w, a, "caio", c, all);
        foreach (var x in all)
        {
            WalkInto(w, x, CityGate, all);
        }

        var zone = w.Where(a).Zone;
        c.Send(new PartyLeave());
        w.Tick(1, all);
        var warning = Assert.Single(c.All<InstanceNotice>());
        Assert.Equal((InstanceNoticeKind.LeavingSoon, OutsiderTimeout), (warning.Kind, warning.Ticks));
        Assert.Empty(a.All<InstanceNotice>());

        w.Tick(OutsiderTimeout - 2, all);
        Assert.Same(zone, w.Where(c).Zone);
        w.Tick(2, all);
        Assert.Same(w.Fields, w.Where(c).Zone);
        Assert.Equal(InstanceNoticeKind.Removed, c.All<InstanceNotice>().Last().Kind);
        Assert.Same(zone, w.Where(a).Zone);
        Assert.Same(zone, w.Where(b).Zone);
    }

    [Fact]
    public void When_a_two_member_party_breaks_up_inside_the_first_to_enter_keeps_the_copy()
    {
        using var w = Rig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        TestClient[] all = [a, b];
        Group(w, a, "bruno", b, all);
        WalkInto(w, a, CityGate, all);
        WalkInto(w, b, CityGate, all);
        var instance = Assert.Single(w.Server.Instances);

        b.Send(new PartyLeave()); // grupo de 2 se desfaz: os dois ficam sem grupo
        w.Tick(1, all);

        Assert.Equal("solo:dev:aria", instance.Owner);
        Assert.Empty(a.All<InstanceNotice>());
        Assert.Equal(InstanceNoticeKind.LeavingSoon, Assert.Single(b.All<InstanceNotice>()).Kind);

        // Convidado de novo antes do prazo: fica.
        Group(w, a, "bruno", b, all);
        Assert.Equal(InstanceNoticeKind.StayCancelled, b.All<InstanceNotice>().Last().Kind);
        w.Tick(OutsiderTimeout + 5, all);
        Assert.Same(instance.Zone, w.Where(b).Zone);
    }

    [Fact]
    public void Instance_messages_round_trip()
    {
        INetMessage[] messages =
        [
            new Welcome(new EntityId(3), 10, Dungeon, "map.matriarch_lair", 2, 7),
            new InstanceRejected(Dungeon, InstanceRejectReason.InstanceFull, "cheia"),
            new InstanceNotice(InstanceNoticeKind.LeavingSoon, 600),
        ];
        foreach (var m in messages)
        {
            Assert.Equal(m, ProtocolSerializer.Deserialize(ProtocolSerializer.Serialize(m)));
        }

        Assert.Equal(0, new Welcome(new EntityId(1), 0, "zone.city", "map.city", 1).InstanceId);
    }
}
