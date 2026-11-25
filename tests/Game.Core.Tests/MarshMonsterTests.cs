using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Tests;

/// <summary>
/// T-510: monstros do Brejo dos Afogados e a Bruxa do Brejo. O Lodo que Puxa põe o <c>status.deep_water</c> da T-502 por uma área
/// presa à Bruxa; o terreno não tira o estado que veio dela. O resto dos dados (tags, fases) é conferido aqui contra o
/// <c>data/</c> real.
/// </summary>
public class MarshMonsterTests
{
    private const string DeepWater = "status.deep_water";

    private static ContentCatalog Real => RealContent.Value;

    private static World NewWorld(params TerrainArea[] terrain) =>
        WorldBuilder.CreateDefault(1, new GridMap(40, 20, null, terrain), Real);

    /// <summary>Guerreiro de nível 1 com vida de sobra: os monstros do nível 20 agem em volta sem matá-lo durante o teste.</summary>
    private static Entity Player(World w, Vector2 pos)
    {
        var e = EntityFactory.SpawnPlayer(w, Real.Classes["class.warrior"], pos);
        var h = e.Get<Health>();
        h.Max = h.Current = 1_000_000;
        return e;
    }

    private static void Run(World w, int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            w.Step();
        }
    }

    [Fact]
    public void Terrain_does_not_remove_deep_water_that_came_from_a_monster()
    {
        // Água em x ∈ [30, 35); o jogador está longe dela, em seco.
        var w = NewWorld(new TerrainArea { Name = "water", Area = new RectDefinition(30, 0, 5, 20), Status = DeepWater });
        var witch = EntityFactory.SpawnMonster(w, Real.Monsters["monster.marsh_witch"], new Vector2(4, 10));
        var hero = Player(w, new Vector2(8, 10));

        StatusManager.Apply(w, witch, hero, DeepWater, durationTicks: 10);
        Run(w, 5);
        Assert.True(StatusManager.Has(w, hero, DeepWater)); // o terreno não tirou

        Run(w, 6);
        Assert.False(StatusManager.Has(w, hero, DeepWater)); // acabou pela duração
    }

    [Fact]
    public void Pulling_mud_follows_the_witch_and_slows_only_enemies_inside_it()
    {
        var w = NewWorld();
        var witch = EntityFactory.SpawnMonster(w, Real.Monsters["monster.marsh_witch"], new Vector2(10, 10));
        var lizard = EntityFactory.SpawnMonster(w, Real.Monsters["monster.marsh_lizardman"], new Vector2(12, 10));
        var near = Player(w, new Vector2(13, 10));
        var far = Player(w, new Vector2(20, 10));
        witch.Get<Movement>().Direction = Vector2.Zero;
        var health = witch.Get<Health>();
        health.Current = health.Max * 0.3f; // fase 3: o Lodo só existe nela (PhaseGate)
        Run(w, 1);

        var cast = Real.Abilities["marsh_witch.pulling_mud"];
        // Na fase 3 a IA dela já abre com o Lodo (primeiro da lista); a intenção só garante. A área aplica a cada 0,25 s.
        w.Enqueue(new UseAbilityIntent(witch.Id, cast.Id, null, null));
        Run(w, cast.CastTimeTicks + 8);

        Assert.True(StatusManager.Has(w, near, DeepWater));
        Assert.False(StatusManager.Has(w, far, DeepWater));
        Assert.False(StatusManager.Has(w, lizard, DeepWater)); // aliado dela

        // A Bruxa se afasta: a área vai junto, e quem ficou para trás sai da lentidão quando o resto da duração acaba.
        witch.Get<Transform>().Position = new Vector2(30, 10);
        Run(w, 12);
        Assert.False(StatusManager.Has(w, near, DeepWater));
    }

    [Fact]
    public void Marsh_monsters_carry_the_tags_the_classes_and_the_terrain_read()
    {
        Assert.Contains("undead", Real.Monsters["monster.drowned"].Tags); // bônus do Clérigo (bonusVsTags)
        Assert.Contains("amphibious", Real.Monsters["monster.marsh_lizardman"].Tags); // ignoreTags da água funda
        Assert.Equal("ai.ranged_kiter", Real.Monsters["monster.marsh_serpent"].Ai.Id);
        Assert.Equal(MonsterRank.Boss, Real.Monsters["monster.marsh_witch"].Rank);
        Assert.Contains(Real.Classes["class.cleric"].Abilities.Select(id => Real.Abilities[id]),
            a => a.Effects.OfType<DamageEffect>().Any(d => d.BonusVsTags.ContainsKey("undead")));
    }

    [Fact]
    public void Witch_has_three_phases_puddles_then_drowned_then_mud()
    {
        var phases = Real.Monsters["monster.marsh_witch"].Phases;
        Assert.Equal(3, phases.Count);
        Assert.Contains("marsh_witch.poison_puddle", phases[0].Abilities!);
        Assert.Contains("marsh_witch.call_drowned", phases[1].Abilities!);
        Assert.Contains(phases[1].OnEnter.OfType<SummonEffect>(), s => s.Monster == "monster.drowned");
        Assert.True(phases[2].NoSummons);
        Assert.Contains("marsh_witch.pulling_mud", phases[2].Abilities!);
        Assert.DoesNotContain("marsh_witch.call_drowned", phases[2].Abilities!);
    }
}
