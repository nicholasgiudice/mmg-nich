using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>Integração T-009 (status) x T-010 (recursos) com os módulos reais.</summary>
public class StatusResourceIntegrationTests
{
    private static readonly StatusDefinition Guard = new()
    {
        Id = "status.guard",
        Name = "Guarda",
        Tags = ["guard"],
        DefaultDurationTicks = 200,
    };

    private static readonly StatusDefinition Poison = new()
    {
        Id = "status.poison",
        Name = "Veneno",
        Tags = ["poison"],
        DefaultDurationTicks = 200,
    };

    private static ClassDefinition Cls(params ResourceRuleDefinition[] rules) => new()
    {
        Id = "class.test",
        Name = "Teste",
        Health = 100,
        Speed = 4,
        Radius = 0.4,
        Resource = new ResourceDefinition { Type = "vigor", Max = 100, Initial = 0, RegenPerSecond = 0 },
        BasicAttack = "x",
        ResourceRules = rules,
    };

    private static World NewWorld(ClassDefinition cls) => WorldBuilder.CreateDefault(
        1, null, new ContentCatalog(classes: [cls], statuses: [Guard, Poison]));

    private static Entity Unit(World w, Faction faction, Vector2 pos, string definitionId) => w.Spawn(
        new Transform { Position = pos },
        new Health(100),
        new FactionTag(faction),
        new Collider(0.4f),
        new Movement(0),
        new ResourcePool("vigor", 100, 0),
        new UnitInfo { DefinitionId = definitionId },
        new AbilityState());

    [Fact]
    public void Source_status_tag_condition_uses_real_statuses()
    {
        var cls = Cls(new ResourceRuleDefinition
        {
            Id = "block",
            Trigger = ResourceTrigger.DamageTaken,
            Amount = 10,
            SourceHasStatusTag = "guard",
        });

        float Run(bool guarded)
        {
            var w = NewWorld(cls);
            var player = Unit(w, Faction.Player, Vector2.Zero, "class.test");
            var enemy = Unit(w, Faction.Monster, new Vector2(1, 0), "monster.test");
            if (guarded)
            {
                StatusManager.Apply(w, player, player, "status.guard", null);
            }

            w.Step();
            DamageResolver.Deal(w, enemy, player, "test.hit", 5, DamageSchool.Physical);
            w.Step();
            return player.Get<ResourcePool>().Current;
        }

        Assert.Equal(0, Run(guarded: false));
        Assert.Equal(10, Run(guarded: true));
    }

    [Fact]
    public void Status_removed_by_someone_triggers_rule_but_expiry_does_not()
    {
        var cls = Cls(new ResourceRuleDefinition
        {
            Id = "purify",
            Trigger = ResourceTrigger.StatusRemoved,
            Amount = 15,
            RemovedStatusTag = "poison",
        });

        var w = NewWorld(cls);
        var cleric = Unit(w, Faction.Player, Vector2.Zero, "class.test");
        var ally = Unit(w, Faction.Player, new Vector2(1, 0), "class.test");
        StatusManager.Apply(w, null, ally, "status.poison", null);
        w.Step();

        StatusManager.RemoveByTag(w, ally, "poison", 1, cleric.Id);
        w.Step();
        Assert.Equal(15, cleric.Get<ResourcePool>().Current);

        StatusManager.Apply(w, null, ally, "status.poison", 1);
        for (var i = 0; i < 5; i++)
        {
            w.Step();
        }

        Assert.Equal(15, cleric.Get<ResourcePool>().Current);
        Assert.Equal(0, ally.Get<ResourcePool>().Current);
    }
}
