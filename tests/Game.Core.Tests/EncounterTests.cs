using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation.Components;
using Game.Data;

namespace Game.Core.Tests;

public class EncounterTests
{
    private static ContentCatalog LoadFixture()
    {
        var result = GameDataLoader.LoadDirectory(Path.Combine(AppContext.BaseDirectory, "Fixtures"));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var db = result.Database!;

        // O Core não referencia Game.Data: quem carrega converte o banco em ContentCatalog.
        return new ContentCatalog(
            db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values);
    }

    [Fact]
    public void Spawn_from_definitions_sets_all_components()
    {
        var content = LoadFixture();
        var enc = EncounterSetup.Create(7, content, content.Encounters["encounter.dummy"]);
        var player = Assert.Single(enc.Players);
        var dummy = Assert.Single(enc.Enemies);

        Assert.Equal(120f, player.Get<Health>().Max);
        Assert.Equal(Faction.Player, player.Get<FactionTag>().Faction);
        Assert.Equal("vigor", player.Get<ResourcePool>().Type);
        Assert.Equal(100f, player.Get<ResourcePool>().Max);
        Assert.Equal(4.5f, player.Get<Movement>().Speed);
        Assert.Equal(0.4f, player.Get<Collider>().Radius);
        Assert.Equal("warrior.slash", player.Get<AbilityState>().BasicAttackId);
        Assert.Contains("warrior.slash", player.Get<AbilityState>().Known);
        Assert.Equal(new System.Numerics.Vector2(1.5f, 1.5f), player.Get<Transform>().Position);

        Assert.Equal(Faction.Monster, dummy.Get<FactionTag>().Faction);
        Assert.Equal(25f, dummy.Get<Health>().Max);
        Assert.Equal(0.5, dummy.Get<UnitInfo>().ResistanceTo(DamageSchool.Frost));
        Assert.Equal(MonsterRank.Normal, dummy.Get<UnitInfo>().Rank);
        Assert.NotNull(enc.World.Map);
        Assert.True(enc.World.Map!.IsBlocked(0, 0));
    }

    [Fact]
    public void Player_basic_attacks_dummy_until_death_with_correct_events()
    {
        var content = LoadFixture();
        var enc = EncounterSetup.Create(7, content, content.Encounters["encounter.dummy"]);
        var w = enc.World;
        var player = enc.Players[0];
        var dummy = enc.Enemies[0];

        var all = new List<GameEvent>();
        for (var i = 0; i < 60; i++)
        {
            if (dummy.IsAlive)
            {
                w.Enqueue(new UseAbilityIntent(player.Id, "warrior.slash", dummy.Id));
            }

            w.Step();
            all.AddRange(w.LastTickEvents);
        }

        var damage = all.OfType<DamageDealt>().ToList();
        Assert.Equal(3, damage.Count); // 10 + 10 + 10 > 25
        Assert.Equal([10f, 10f, 10f], damage.Select(d => d.Amount));
        Assert.Equal([10f, 10f, 5f], damage.Select(d => d.Applied));
        Assert.Equal(5f, damage[2].Overkill);
        Assert.Equal([false, false, true], damage.Select(d => d.Killed));
        Assert.All(damage, d => Assert.Equal(player.Id, d.Source));

        // 0,8 s = 16 ticks de recarga: usos nos ticks 1, 17 e 33
        var executed = all.OfType<AbilityExecuted>().ToList();
        Assert.Equal([1L, 17L, 33L], executed.Select(e => e.Tick));
        Assert.All(executed, e => Assert.True(e.IsBasicAttack));
        Assert.Equal(3, all.OfType<AbilityStarted>().Count());

        Assert.Single(all.OfType<EntityDied>());
        Assert.Equal(dummy.Id, all.OfType<EntityDied>().Single().Entity);
        Assert.Equal(33, all.OfType<EntityDied>().Single().Tick);
        Assert.False(dummy.IsAlive);
        Assert.True(player.IsAlive);

        // Antes da morte nenhum uso foi rejeitado (só enviamos quando pronto/vivo)? Rejeições por recarga são esperadas.
        Assert.Contains(all.OfType<AbilityRejected>(), r => r.Reason == AbilityRejectReason.OnCooldown);
        Assert.DoesNotContain(all.OfType<AbilityRejected>(), r => r.Reason != AbilityRejectReason.OnCooldown);
    }
}
