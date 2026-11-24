using System.Numerics;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Zones;

namespace Game.Server.Tests;

/// <summary>
/// T-501: quem morre renasce na zona segura do campo <c>respawn</c> da zona em que morreu, inclusive numa instância; sem o
/// campo, na zona inicial (a Cidade, coberta pelos testes de <see cref="WorldTests"/> e <see cref="InstanceTests"/>). Os
/// dados reais ainda não têm uma segunda zona segura (o Forte do Passo é da T-531), então o teste cria
/// <c>zone.test_fort</c>, uma cópia da Cidade, e aponta para ela os Campos e a instância de teste.
/// </summary>
public class RespawnZoneTests
{
    private const string Fort = "zone.test_fort";
    private const string Dungeon = "zone.test_instance";

    /// <summary>Portão de teste (1 × 1 tile em (24, 15)) para a instância, na Cidade e no Forte.</summary>
    private static readonly Vector2 DungeonGate = new(24.5f, 15.5f);

    /// <summary>Dentro do portão leste da Cidade, para os Campos.</summary>
    private static readonly Vector2 FieldsGate = new(78.5f, 29.5f);

    private static ContentCatalog Content()
    {
        var c = TestRig.RealContent.Value;
        var zones = c.Zones.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        var dungeon = zones[Dungeon] with { Respawn = Fort };
        var toDungeon = new GateDefinition { Area = new RectDefinition(24, 15, 1, 1), To = Dungeon, Arrival = dungeon.Spawn };
        var city = zones["zone.city"];
        zones[city.Id] = city with { Gates = [.. city.Gates, toDungeon] };
        zones[Fort] = city with { Id = Fort, Name = "Forte de Teste", Gates = [toDungeon] };
        zones["zone.vigil_fields"] = zones["zone.vigil_fields"] with { Respawn = Fort };
        zones[Dungeon] = dungeon;
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

    private static WorldRig Rig(FakeStore? store = null) => new(store, devLogin: store is null, Content());

    private static void Walk(WorldRig w, TestClient c, Vector2 to)
    {
        w.Where(c).Entity.Get<Transform>().Position = to;
        w.Tick(1, c);
    }

    private static void DieAndRespawn(WorldRig w, TestClient c)
    {
        var (zone, hero) = w.Where(c);
        zone.World.Kill(hero);
        w.Tick(5, c);
        Assert.Same(zone, w.Where(c).Zone); // caído, esperando o pedido
        c.Send(new RespawnRequest());
        w.Tick(1, c);
    }

    [Fact]
    public void Death_in_a_zone_with_respawn_comes_back_at_that_safe_zone_full()
    {
        using var w = Rig();
        var c = w.Join("aria", "class.mage");
        Walk(w, c, FieldsGate);
        Assert.Same(w.Fields, w.Where(c).Zone);

        DieAndRespawn(w, c);

        var (where, hero) = w.Where(c);
        var fort = w.Server.Zones[Fort];
        Assert.Same(fort, where);
        Assert.Equal(Fort, c.Welcome!.EncounterId);
        Assert.True(hero.IsAlive);
        Assert.Equal(hero.Get<Health>().Max, hero.Get<Health>().Current);
        Assert.Equal(fort.SpawnPosition, hero.Get<Transform>().Position);
    }

    [Fact]
    public void Dead_account_character_is_saved_at_the_respawn_zone()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var w = Rig(store);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        Walk(w, c, FieldsGate);
        w.Fields.World.Kill(w.Where(c).Entity);
        w.Tick(1, c);
        c.Raw.Leave();
        w.Tick(1);
        w.Flush();

        Assert.Equal(Fort, store.Row(row.Id).ZoneId);
        Assert.Equal(0, store.Row(row.Id).PositionX); // no nascimento
    }

    [Fact]
    public void Death_in_an_instance_with_respawn_goes_to_that_safe_zone_and_its_gate_leads_back_to_the_same_copy()
    {
        using var w = Rig();
        var c = w.Join("aria");
        Walk(w, c, DungeonGate);
        var copy = w.Where(c).Zone;
        Assert.Equal(Dungeon, copy.Id);
        var instanceId = copy.InstanceId;
        Assert.NotEqual(0, instanceId);

        DieAndRespawn(w, c);

        var fort = w.Server.Zones[Fort];
        Assert.Same(fort, w.Where(c).Zone);
        Assert.Equal(0, c.Welcome!.InstanceId);
        Assert.Single(w.Server.Instances); // a cópia espera o emptyTimeout

        Walk(w, c, DungeonGate);
        Assert.Same(copy, w.Where(c).Zone);
        Assert.Equal(instanceId, c.Welcome!.InstanceId);
    }

    [Fact]
    public void Zone_without_respawn_still_comes_back_at_the_start_zone()
    {
        using var w = Rig();
        var c = w.Join("aria");
        var forest = w.Server.Zones["zone.lower_forest"];
        Assert.Null(forest.Definition.Respawn);
        Walk(w, c, new Vector2(39.5f, 0.5f)); // portão norte da Cidade
        Assert.Same(forest, w.Where(c).Zone);

        DieAndRespawn(w, c);

        Assert.Same(w.City, w.Where(c).Zone);
    }
}
