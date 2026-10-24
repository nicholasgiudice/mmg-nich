using System.Numerics;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-206: vida e potência dos monstros pelo nível em que surgem (<c>rules.monsters</c>).</summary>
public class MonsterScalingTests
{
    private static readonly MonsterScalingRules Rules = new()
    {
        HealthPerLevel = 0.5,
        PowerPerLevel = 0.25,
        Boss = new MonsterGrowth { HealthPerLevel = 1, PowerPerLevel = 1 },
    };

    private static readonly AbilityDefinition Bite = new()
    {
        Id = "mob.bite",
        Name = "Mordida",
        Targeting = new UnitTargeting { Range = 5, Faction = FactionFilter.Enemy },
        Effects = [new DamageEffect { Base = 10 }],
    };

    private static readonly AbilityDefinition Call = new()
    {
        Id = "mob.call",
        Name = "Chamado",
        Targeting = new SelfTargeting(),
        Effects = [new SummonEffect { Monster = "monster.pup", Count = 1, Radius = 1 }],
    };

    private static MonsterDefinition Mob(string id, MonsterRank rank = MonsterRank.Normal, int level = 1) => new()
    {
        Id = id,
        Name = id,
        Health = 100,
        Speed = 0,
        Radius = 0.4,
        Rank = rank,
        Level = level,
        Abilities = ["mob.bite", "mob.call"],
        Ai = new AiProfileDefinition { Id = "ai.none" },
    };

    private static World NewWorld(MonsterScalingRules? rules = null) => WorldBuilder.CreateDefault(
        1,
        null,
        new ContentCatalog(
            abilities: [Bite, Call],
            monsters: [Mob("monster.wolf", level: 3), Mob("monster.pup"), Mob("monster.queen", MonsterRank.Boss)])
        {
            Rules = new RulesDefinition { Monsters = rules ?? Rules },
        });

    private static Entity Target(World w) =>
        w.Spawn(new Transform { Position = new Vector2(1, 0) }, new Health(1000), new FactionTag(Faction.Player));

    private static float DamageFrom(World w, Entity monster)
    {
        var target = Target(w);
        w.Enqueue(new UseAbilityIntent(monster.Id, "mob.bite", target.Id));
        w.Step();
        return w.LastTickEvents.OfType<DamageDealt>().Single(d => d.Source == monster.Id).Amount;
    }

    [Theory]
    [InlineData(1, 100, 10)]
    [InlineData(3, 200, 15)]
    [InlineData(9, 500, 30)]
    public void Health_and_power_grow_with_the_spawn_level(int level, float health, float damage)
    {
        var w = NewWorld();
        var m = EntityFactory.SpawnMonster(w, w.Content.Monsters["monster.pup"], Vector2.Zero, level);

        Assert.Equal(level, m.Get<UnitInfo>().Level);
        Assert.Equal(health, m.Get<Health>().Max, 3);
        Assert.Equal(health, m.Get<Health>().Current, 3);
        Assert.Equal(damage, DamageFrom(w, m), 3);
    }

    [Fact]
    public void Without_a_spawn_level_the_definition_level_is_used()
    {
        var w = NewWorld();
        var wolf = EntityFactory.SpawnMonster(w, w.Content.Monsters["monster.wolf"], Vector2.Zero);

        Assert.Equal(3, wolf.Get<UnitInfo>().Level);
        Assert.Equal(200, wolf.Get<Health>().Max, 3);
    }

    [Fact]
    public void Bosses_use_their_own_growth()
    {
        var w = NewWorld();
        var queen = EntityFactory.SpawnMonster(w, w.Content.Monsters["monster.queen"], Vector2.Zero, 3);

        Assert.Equal(300, queen.Get<Health>().Max, 3); // 1 + 1 × 2
        Assert.Equal(30, DamageFrom(w, queen), 3);
    }

    [Fact]
    public void No_rules_means_no_scaling_and_players_are_never_scaled()
    {
        var w = NewWorld(new MonsterScalingRules());
        var m = EntityFactory.SpawnMonster(w, w.Content.Monsters["monster.pup"], Vector2.Zero, 9);
        Assert.Equal(100, m.Get<Health>().Max, 3);
        Assert.Equal(10, DamageFrom(w, m), 3);

        var scaled = NewWorld();
        var player = scaled.Spawn(new Transform(), new Health(100), new FactionTag(Faction.Player), new UnitInfo { Level = 9 });
        Assert.Equal(1, Game.Core.Combat.Stats.StatMath.MonsterPower(scaled, player));
    }

    [Fact]
    public void Summons_inherit_the_summoner_level()
    {
        var w = NewWorld();
        var wolf = EntityFactory.SpawnMonster(w, w.Content.Monsters["monster.wolf"], Vector2.Zero, 5);
        w.Enqueue(new UseAbilityIntent(wolf.Id, "mob.call"));
        w.Step();

        var pup = w.Entities.Single(e => e.TryGet<UnitInfo>()?.DefinitionId == "monster.pup");
        Assert.Equal(5, pup.Get<UnitInfo>().Level);
        Assert.Equal(300, pup.Get<Health>().Max, 3);
    }

    [Fact]
    public void Encounter_monster_level_and_spawn_level_override_the_definition()
    {
        var content = new ContentCatalog(
            abilities: [Bite, Call],
            monsters: [Mob("monster.wolf", level: 3)],
            arenas: [new ArenaDefinition { Id = "arena.box", Lines = ["....", "....", "...."] }])
        {
            Rules = new RulesDefinition { Monsters = Rules },
        };
        var encounter = new EncounterDefinition
        {
            Id = "encounter.test",
            Name = "Teste",
            Arena = "arena.box",
            Players = [],
            MonsterLevel = 7,
            Enemies =
            [
                new EnemySpawnDefinition { Monster = "monster.wolf", Position = new PositionDefinition(1, 1) },
                new EnemySpawnDefinition { Monster = "monster.wolf", Position = new PositionDefinition(2, 1), Level = 2 },
            ],
        };

        var setup = EncounterSetup.Create(1, content, encounter);

        Assert.Equal(new[] { 7, 2 }, setup.Enemies.Select(e => e.Get<UnitInfo>().Level));
        Assert.Equal(new[] { 400f, 150f }, setup.Enemies.Select(e => e.Get<Health>().Max));
    }

    [Fact]
    public void Real_prototype_encounters_spawn_level_1_monsters_with_the_prototype_numbers()
    {
        var content = RealContent.Value;
        foreach (var encounter in content.Encounters.Values)
        {
            var setup = EncounterSetup.Create(1, content, encounter);
            Assert.All(setup.Enemies, e =>
            {
                Assert.Equal(1, e.Get<UnitInfo>().Level);
                Assert.Equal(content.Monsters[e.Get<UnitInfo>().DefinitionId].Health, e.Get<Health>().Max, 3);
            });
        }

        Assert.True(content.Rules.Monsters.HealthPerLevel > 0);
    }
}
