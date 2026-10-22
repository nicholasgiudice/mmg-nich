using System.Numerics;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-062: NH dos dados reais rodando no Core (barra com assinatura e uso de cada NH).</summary>
public class RealRankContentTests
{
    public static TheoryData<string> Classes => new() { "class.warrior", "class.archer", "class.mage", "class.cleric" };

    [Theory]
    [MemberData(nameof(Classes))]
    public void Signature_is_only_on_the_bar_once_it_has_a_rank(string classId)
    {
        var content = RealContent.Value;
        var cls = content.Classes[classId];
        var signature = cls.Abilities.Single(a => content.Abilities[a].Signature);
        var w = WorldBuilder.CreateDefault(1, null, content);

        var fresh = EntityFactory.SpawnPlayer(w, cls, Vector2.Zero).Get<AbilityState>();
        Assert.DoesNotContain(signature, fresh.Known);
        Assert.Equal(cls.Abilities.Count - 1, fresh.Known.Count(a => a != cls.BasicAttack));

        var learned = EntityFactory.SpawnPlayer(w, cls, Vector2.Zero, new Dictionary<string, int> { [signature] = 1 }).Get<AbilityState>();
        Assert.Contains(signature, learned.Known);
    }

    /// <summary>Cada habilidade de cada classe, em cada NH, roda até o fim contra um grupo de bonecos sem erro e faz algo.</summary>
    [Theory]
    [MemberData(nameof(Classes))]
    public void Every_rank_of_every_class_ability_runs(string classId)
    {
        var content = RealContent.Value;
        var cls = content.Classes[classId];
        foreach (var id in cls.Abilities)
        {
            var ability = content.Abilities[id];
            for (var rank = 1; rank <= ability.HighestRank; rank++)
            {
                var w = WorldBuilder.CreateDefault(1, null, content);
                var hero = EntityFactory.SpawnPlayer(w, cls, new Vector2(10, 10), new Dictionary<string, int> { [id] = rank });
                hero.Get<Transform>().Facing = new Vector2(1, 0);
                var ally = EntityFactory.SpawnPlayer(w, cls, new Vector2(10, 11));
                ally.Get<Health>().Current = 10;
                var fallen = EntityFactory.SpawnPlayer(w, cls, new Vector2(11, 10.5f));
                w.Kill(fallen);
                var dummies = new[] { new Vector2(11.2f, 10), new Vector2(12, 10.6f), new Vector2(13, 10) }
                    .Select(p => w.Spawn(new Transform { Position = p }, new Health(5000), new FactionTag(Faction.Monster), new Collider(0.4f)))
                    .ToList();

                var beneficialUnit = ability.Targeting is UnitTargeting { Faction: FactionFilter.Ally };
                EntityId? target = ability.Targeting is UnitTargeting ? (beneficialUnit ? ally.Id : dummies[0].Id) : null;
                Vector2? aim = ability.Targeting is CircleAtPointTargeting ? new Vector2(12, 10) : null;
                w.Enqueue(new UseAbilityIntent(hero.Id, id, target, aim));

                var events = new List<GameEvent>();
                for (var i = 0; i < 200; i++)
                {
                    w.Step();
                    events.AddRange(w.LastTickEvents);
                }

                Assert.True(events.OfType<AbilityExecuted>().Any(e => e.AbilityId == id), $"{id} NH{rank} não executou");
                Assert.DoesNotContain(events.OfType<AbilityRejected>(), r => r.AbilityId == id);
            }
        }
    }
}
