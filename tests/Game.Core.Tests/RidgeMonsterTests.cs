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
/// T-511: monstros da Serra Partida. Nenhuma mecânica nova de Core: a Rocha Rolante do Golem é um telegráfo em linha (atinge
/// todo inimigo do Golem dentro dela, nenhum monstro) e a Picaretada do Mineiro é um cone com telegráfo que atordoa. Os testes
/// conferem esse uso contra o <c>data/</c> real.
/// </summary>
public class RidgeMonsterTests
{
    private static ContentCatalog Real => RealContent.Value;

    private static World NewWorld() => WorldBuilder.CreateDefault(1, new GridMap(40, 20, null, []), Real);

    /// <summary>Guerreiro de nível 1 com vida de sobra: os monstros do nível 25 agem em volta sem matá-lo durante o teste.</summary>
    private static Entity Player(World w, Vector2 pos)
    {
        var e = EntityFactory.SpawnPlayer(w, Real.Classes["class.warrior"], pos);
        var h = e.Get<Health>();
        h.Max = h.Current = 1_000_000;
        return e;
    }

    private static bool Hurt(Entity e) => e.Get<Health>().Current < e.Get<Health>().Max;

    private static void Run(World w, int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            w.Step();
        }
    }

    [Fact]
    public void Rolling_boulder_hits_every_player_in_the_line_and_no_monster()
    {
        var w = NewWorld();
        var golem = EntityFactory.SpawnMonster(w, Real.Monsters["monster.stone_golem"], new Vector2(4, 10));
        var target = Player(w, new Vector2(9, 10));
        var miner = EntityFactory.SpawnMonster(w, Real.Monsters["monster.possessed_miner"], new Vector2(12, 10));
        var farInLine = Player(w, new Vector2(14, 10));
        var farthestInLine = Player(w, new Vector2(17, 10));
        var aside = Player(w, new Vector2(14, 14));
        foreach (var e in new[] { target, farInLine, farthestInLine, aside })
        {
            e.Get<Movement>().Direction = Vector2.Zero;
        }

        var boulder = Real.Abilities["stone_golem.rolling_boulder"];
        w.Enqueue(new UseAbilityIntent(golem.Id, boulder.Id, target.Id, null));
        Run(w, boulder.Telegraph!.DelayTicks + 2);

        // Só a rocha chega a 10–13 tiles: quem está na linha, longe do Golem, foi atingido; o Mineiro na linha, não.
        Assert.True(Hurt(farInLine));
        Assert.True(Hurt(farthestInLine));
        Assert.False(Hurt(aside));
        Assert.False(Hurt(miner));
    }

    [Fact]
    public void Pickaxe_blow_stuns_the_player_in_front_for_one_second()
    {
        var w = NewWorld();
        var miner = EntityFactory.SpawnMonster(w, Real.Monsters["monster.possessed_miner"], new Vector2(4, 10));
        var hero = Player(w, new Vector2(5.2f, 10));
        hero.Get<Movement>().Direction = Vector2.Zero;

        var blow = Real.Abilities["possessed_miner.pickaxe_blow"];
        Assert.Equal(TelegraphShape.Cone, blow.Telegraph!.Shape);
        w.Enqueue(new UseAbilityIntent(miner.Id, blow.Id, hero.Id, null));
        Run(w, blow.Telegraph.DelayTicks + 2);

        Assert.True(StatusManager.Has(w, hero, "status.stunned"));
        Run(w, 20); // 1 s
        Assert.False(StatusManager.Has(w, hero, "status.stunned"));
    }

    [Fact]
    public void Ridge_monsters_carry_the_tags_ai_and_resistances_of_the_design()
    {
        var golem = Real.Monsters["monster.stone_golem"];
        Assert.Equal(MonsterRank.Elite, golem.Rank);
        Assert.Contains("construct", golem.Tags);
        Assert.True(golem.Resistances["physical"] > 0); // resiste a físico
        Assert.True(golem.Resistances["arcane"] < 0); // fraco a arcano
        Assert.True(golem.Speed < Real.Classes["class.warrior"].Speed); // lento

        var boulder = Real.Abilities["stone_golem.rolling_boulder"].Telegraph!;
        Assert.Equal(TelegraphShape.Line, boulder.Shape);
        Assert.Equal(30, boulder.DelayTicks); // 1,5 s
        Assert.True(boulder.Length >= 12); // atravessa o desfiladeiro

        Assert.Equal("ai.pack", Real.Monsters["monster.mountain_wolf"].Ai.Id);
        Assert.Equal("ai.swarm", Real.Monsters["monster.cave_bat"].Ai.Id);
        Assert.Contains("humanoid", Real.Monsters["monster.possessed_miner"].Tags);
        Assert.Contains("beast", Real.Monsters["monster.mountain_wolf"].Tags);
        Assert.Contains("beast", Real.Monsters["monster.cave_bat"].Tags);
    }
}
